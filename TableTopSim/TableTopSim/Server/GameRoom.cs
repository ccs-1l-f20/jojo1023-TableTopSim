using DataLayer;
using GameLib;
using GameLib.GameSerialization;
using GameLib.Sprites;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net.WebSockets;
using System.Numerics;
using System.Threading;
using System.Threading.Tasks;

namespace TableTopSim.Server
{
    public class GameRoom
    {
        static readonly int gameStateUpdateLength = 500;
        public int RoomId { get; }
        public Dictionary<int, WebSocket> PlayerWebSockets { get; private set; }
        public bool GameStarted { get; set; }
        public GameManager GameManager { get; set; }
        CancellationTokenSource cts = new CancellationTokenSource();
        SpriteRefrenceManager spriteRefManager => GameManager.SpriteRefrenceManager;
        object gameLockObject = new object();
        public GameRoom(int roomId, int? initPlayerId, WebSocket initPlayerWs)
        {
            GameManager = new GameManager(new Size(1000, 1000));
            GameStarted = false;
            RoomId = roomId;
            PlayerWebSockets = new Dictionary<int, WebSocket>();
            if (initPlayerId != null)
            {
                PlayerWebSockets.Add(initPlayerId.Value, initPlayerWs);
            }
            TempInit();
        }
        void TempInit()
        {
            GameManager.AddSprite(new RectSprite(new Vector2(200, 200), new Vector2(100, 200), new Color(0, 0, 255), new Vector2(50, 100), 0));
            GameManager.AddSprite(new RectSprite(new Vector2(200, 200), new Vector2(10, 10), new Color(255, 0, 255), new Vector2(0, 0), 45));
            GameManager.AddSprite(new RectSprite(new Vector2(500, 500), new Vector2(50, 50), new Color(0, 0, 0), new Vector2(0, 0), 0));
            GameManager.AddSprite(new RectSprite(new Vector2(600, 500), new Vector2(50, 50), new Color(128, 128, 128), new Vector2(0, 0), 0));
        }

        public async Task StartGame()
        {
            GameStarted = true;
            List<byte> sendBytes = new List<byte>();
            sendBytes.Add((byte)MessageType.StartGame);
            sendBytes.AddRange(BitConverter.GetBytes(RoomId));
            await SendToRoom(sendBytes);
            CancellationToken ct = cts.Token;

            //await Task.Delay(gameStateUpdateLength, ct);
            while (!ct.IsCancellationRequested)
            {
                await Task.Delay(gameStateUpdateLength, ct);

                sendBytes.Clear();
                sendBytes.Add((byte)MessageType.GameState);
                sendBytes.AddRange(BitConverter.GetBytes(RoomId));
                lock (gameLockObject)
                {
                    var spritesBytes = GameSerialize.SerializeGameData(spriteRefManager.SpriteRefrences);
                    sendBytes.AddRange(BitConverter.GetBytes(spritesBytes.Count));
                    sendBytes.AddRange(spritesBytes);
                    var baseSpritesBytes = GameSerialize.SerializeGameData(GameManager.GameSprite.Children);
                    sendBytes.AddRange(BitConverter.GetBytes(baseSpritesBytes.Count));
                    sendBytes.AddRange(baseSpritesBytes);
                }

                await SendToRoom(sendBytes);
            }
        }

        public async Task RecievedChangeGame(WebSocket ws, ArraySegment<byte> arrSegBytes)
        {
            int origOffset = arrSegBytes.Offset;
            ArrayWithOffset<byte> bytes = new ArrayWithOffset<byte>(arrSegBytes.Array, arrSegBytes.Offset);
            int spritesDictLength = BitConverter.ToInt32(bytes.Array, bytes.Offset);
            bytes.Offset += 4;
            ArrayWithOffset<byte> serializedSpritesDict = bytes.Slice(0, spritesDictLength);
            bytes.Offset += spritesDictLength;
            int gameSpritesLength = BitConverter.ToInt32(bytes.Array, bytes.Offset);
            bytes.Offset += 4;
            ArrayWithOffset<byte> serializedGameSprites = bytes.Slice(0, gameSpritesLength);

            lock (gameLockObject)
            {
                spriteRefManager.SpriteRefrences = GameSerialize.DeserializeEditGameData(spriteRefManager.SpriteRefrences, serializedSpritesDict);
                spriteRefManager.UpdateSpriteAddresses();

                List<int> gameSpriteSprites = GameSerialize.DeserializeGameData<List<int>>(serializedGameSprites);
                GameManager.ClearSprites();
                foreach (var s in gameSpriteSprites)
                {
                    GameManager.AddSprite(s);
                }
            }

            List<byte> sendBackMessage = new List<byte>() { (byte)MessageType.ChangeGameState };
            sendBackMessage.AddRange(BitConverter.GetBytes(RoomId));
            sendBackMessage.AddRange(bytes.Array.Skip(origOffset).ToArray());

            await SendToRoom(sendBackMessage, ws);
        }

        public void AddPlayerWS(int playerId, WebSocket ws)
        {
            if (PlayerWebSockets.ContainsKey(playerId))
            {
                PlayerWebSockets[playerId] = ws;
            }
            else
            {
                PlayerWebSockets.Add(playerId, ws);
            }
        }
        public async Task SendToRoom(IEnumerable<byte> bytes, WebSocket ignoreWs = null)
        {
            var arraySeg = new ArraySegment<byte>(bytes.ToArray());
            foreach (var pId in PlayerWebSockets.Keys)
            {
                var ws = PlayerWebSockets[pId];
                if (ws != null)
                {
                    if (ws.State != WebSocketState.Open)
                    {
                        PlayerWebSockets[pId] = null;
                        continue;
                    }
                    if (ws == ignoreWs) { continue; }
                    await ws.SendAsync(arraySeg, WebSocketMessageType.Binary, true, CancellationToken.None);
                }
            }
        }
    }
}
