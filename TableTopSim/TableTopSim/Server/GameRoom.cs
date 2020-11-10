using DataLayer;
using GameLib;
using GameLib.GameSerialization;
using GameLib.Sprites;
using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
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
        SpriteRefrenceManager refManager => GameManager.SpriteRefrenceManager;
        object gameLockObject = new object();

        Random random = new Random();
        EmptySprite spriteContainer = new EmptySprite();
        EmptySprite selectedSpritesContainer = new EmptySprite() { LayerDepth = -100 };
        public GameRoom(int roomId, int? initPlayerId, WebSocket initPlayerWs)
        {
            GameManager = new GameManager(new Size(1000, 1000), new SpriteRefrenceManager(new Dictionary<int, Sprite>() { { 0, spriteContainer }, { 1, selectedSpritesContainer } }));
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
            AddSprite(new RectSprite(new Vector2(200, 200), new Vector2(100, 200), new Color(0, 0, 255), new Vector2(50, 100), 0));
            AddSprite(new RectSprite(new Vector2(200, 200), new Vector2(10, 10), new Color(255, 0, 255), new Vector2(0, 0), 45));
            AddSprite(new RectSprite(new Vector2(500, 500), new Vector2(50, 50), new Color(0, 0, 0), new Vector2(0, 0), 0));
            AddSprite(new RectSprite(new Vector2(600, 500), new Vector2(50, 50), new Color(128, 128, 128), new Vector2(0, 0), 0));
        }
        int GetNewSpriteAddress()
        {
            int address;
            do
            {
                address = random.Next();
            } while (refManager.ContainsAddress(address));
            return address;
        }
        void AddSprite(Sprite sprite)
        {
            refManager.AddSprite(GetNewSpriteAddress(), sprite);
            spriteContainer.AddChild(sprite, refManager);
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
                    var spritesBytes = GameSerialize.SerializeGameData(refManager.SpriteRefrences);
                    sendBytes.AddRange(BitConverter.GetBytes(spritesBytes.Count));
                    sendBytes.AddRange(spritesBytes);
                    var baseSpritesBytes = GameSerialize.SerializeGameData(new List<List<int>>() { spriteContainer.Children, selectedSpritesContainer.Children });
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
                refManager.SpriteRefrences = GameSerialize.DeserializeEditGameData(refManager.SpriteRefrences, serializedSpritesDict);
                refManager.UpdateSpriteAddresses();


                List<List<int>> gameSpriteSprites = GameSerialize.DeserializeGameData<List<List<int>>>(serializedGameSprites);
                List<int> spriteContainerSprites = gameSpriteSprites[0];
                List<int> selectedSpriteSprites = gameSpriteSprites[1];
                spriteContainer.ClearChildren(refManager);
                selectedSpritesContainer.ClearChildren(refManager);
                foreach (var s in spriteContainerSprites)
                {
                    spriteContainer.AddChild(s, refManager);
                }
                foreach (var s in selectedSpriteSprites)
                {
                    selectedSpritesContainer.AddChild(s, refManager);
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
