using DataLayer;
using GameLib;
using GameLib.GameSerialization;
using GameLib.Sprites;
using Microsoft.AspNetCore.Components;
using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Linq;
using System.Net.WebSockets;
using System.Numerics;
using System.Threading;
using System.Threading.Tasks;
using TableTopSim.Client.SpecificGame;
using TableTopSim.Server.Controllers;
using TableTopSim.Shared;

namespace TableTopSim.Server
{
    public class GameRoom
    {
        static readonly int gameStateUpdateLength = 500;
        public int RoomId { get; }
        public Dictionary<int, WebSocket> PlayerWebSockets { get; private set; }
        Dictionary<WebSocket, int> webSocketPlayers;
        public bool GameStarted { get; set; }
        public int GameId { get; set; }
        //public GameManager GameManager { get; set; }
        CancellationTokenSource cts = new CancellationTokenSource();
        SpriteRefrenceManager refManager;
        object gameLockObject = new object();

        Random random = new Random();
        Dictionary<int, CursorInfo> playerCursors;
        bool roomInit = false;
        public GameRoom(int roomId, int initPlayerId, int gameId, WebSocket initPlayerWs)
        {
            refManager = new SpriteRefrenceManager(new Dictionary<int, ElementReference>(), new ElementReference());
            //GameManager = new GameManager(new Size(1000, 1000), new SpriteRefrenceManager());
            GameStarted = false;
            RoomId = roomId;
            GameId = gameId;
            PlayerWebSockets = new Dictionary<int, WebSocket>();
            webSocketPlayers = new Dictionary<WebSocket, int>();
            //if (initPlayerId != null)
            //{
            webSocketPlayers.Add(initPlayerWs, initPlayerId);
            PlayerWebSockets.Add(initPlayerId, initPlayerWs);
            //}
        }
        public async Task InitalizeRoom(SqlConnection sqlConnection)
        {
            GameDataDto gameData= await ReTryer.Try(100, 5, async () => await GameController.GetGame(sqlConnection, GameId));
            if(gameData == null)
            {
                throw new NullReferenceException($"Cound't Get Game: {GameId}");
            }
            var spriteData = JsonConvert.DeserializeObject<Dictionary<int, Sprite>>(gameData.SerializedSpriteDictionary, new SpriteJsonConverter());
            foreach(var k in spriteData.Keys)
            {
                Sprite s = spriteData[k];
                refManager.AddSprite(k, s);
                s.LayerDepth.Layers.Insert(0, 0);
            }
            roomInit = true;
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
        int AddSprite(Sprite sprite)
        {
            int ad = GetNewSpriteAddress();
            refManager.AddSprite(ad, sprite);
            sprite.LayerDepth.Layers.Insert(0, 0);
            return ad;
        }

        public async Task StartGame()
        {
            while (!roomInit) { }
            GameStarted = true;
            List<byte> sendBytes = new List<byte>();
            sendBytes.Add((byte)MessageType.StartGame);
            sendBytes.AddRange(BitConverter.GetBytes(RoomId));
            await SendToRoom(sendBytes);
            CancellationToken ct = cts.Token;
            playerCursors = new Dictionary<int, CursorInfo>();
            foreach (var pId in PlayerWebSockets.Keys)
            {
                RectSprite cursor = new RectSprite(refManager, new Vector2(0, 0), new Vector2(8, 8), new Color(200, 200, 200), Vector2.Zero, 0) { Selectable = false };
                cursor.Transform.Scale *= 1.15f;
                int cAd = AddSprite(cursor);
                cursor.LayerDepth[0] = -1;
                playerCursors.Add(pId, new CursorInfo(cAd, null));
            }
           
            //await Task.Delay(gameStateUpdateLength, ct);
            while (!ct.IsCancellationRequested)
            {
                await Task.Delay(gameStateUpdateLength, ct);

                sendBytes.Clear();
                sendBytes.Add((byte)MessageType.GameState);
                sendBytes.AddRange(BitConverter.GetBytes(RoomId));

                foreach (var pId in PlayerWebSockets.Keys)
                {
                    if (PlayerWebSockets[pId] != null)
                    {
                        if (!playerCursors.ContainsKey(pId))
                        {
                            RectSprite cursor = new RectSprite(refManager, new Vector2(0, 0), new Vector2(8, 8), new Color(200, 200, 200), Vector2.Zero, 0) { Selectable = false };
                            cursor.Transform.Scale *= 1.15f;
                            int cAd = AddSprite(cursor);
                            cursor.LayerDepth[0] = -1;
                            playerCursors.Add(pId, new CursorInfo(cAd, null));
                        }
                    }
                    else if (playerCursors.ContainsKey(pId))
                    {
                        int? selectedSprite = playerCursors[pId].SelectedSpriteId;
                        if (selectedSprite != null)
                        {
                            Sprite droppedSprite = refManager.GetSprite(selectedSprite.Value);
                            if (droppedSprite.Parent == playerCursors[pId].CursorSpriteId)
                            {
                                Sprite parentSprite = refManager.GetSprite(droppedSprite.Parent.Value);
                                droppedSprite.Transform.Position += parentSprite.Transform.Position;
                                droppedSprite.Parent = null;
                            }
                        }
                        refManager.RemoveSprite(playerCursors[pId].CursorSpriteId);
                        playerCursors.Remove(pId);
                    }
                }
                lock (gameLockObject)
                {
                    var spritesBytes = GameSerialize.SerializeGameData(refManager.SpriteRefrences);
                    sendBytes.AddRange(BitConverter.GetBytes(spritesBytes.Count));
                    sendBytes.AddRange(spritesBytes);
                }
                var playerCursorBytes = GameSerialize.SerializeGameData(playerCursors);
                //Debug.WriteLine($"Server Cursor Length: {playerCursorBytes.Count}");
                sendBytes.AddRange(BitConverter.GetBytes(playerCursorBytes.Count));
                sendBytes.AddRange(playerCursorBytes);

                await SendToRoom(sendBytes);
            }
        }

        public async Task RecievedChangeGame(WebSocket ws, ArraySegment<byte> arrSegBytes)
        {
            int senderPlayer;
            if (webSocketPlayers.ContainsKey(ws))
            {
                senderPlayer = webSocketPlayers[ws];
            }
            else
            {
                return;
            }
            int origOffset = arrSegBytes.Offset;
            ArrayWithOffset<byte> bytes = new ArrayWithOffset<byte>(arrSegBytes.Array, arrSegBytes.Offset);
            int spritesDictLength = BitConverter.ToInt32(bytes.Array, bytes.Offset);
            bytes.Offset += 4;
            ArrayWithOffset<byte> serializedSpritesDict = bytes.Slice(0, spritesDictLength);
            bytes.Offset += spritesDictLength;
            int? selectedSprite = null;
            if(bytes[0] != 0)
            {
                selectedSprite = BitConverter.ToInt32(bytes.Array, bytes.Offset + 1);
            }
            bytes.Offset += 5;
            bool canSelect = true;
            lock (gameLockObject)
            {
                if (selectedSprite != null)
                {
                    foreach(var k in playerCursors.Keys)
                    {
                        if (k == senderPlayer) { continue; }
                        var v = playerCursors[k];
                        if(v.SelectedSpriteId == selectedSprite.Value)
                        {
                            canSelect = false;
                            break;
                        }
                    }
                }
                if (canSelect)
                {
                    playerCursors[senderPlayer].SelectedSpriteId = selectedSprite;
                    refManager.SpriteRefrences = GameSerialize.DeserializeEditGameData(refManager.SpriteRefrences, serializedSpritesDict);
                    refManager.UpdateSpriteAddresses();
                }
            }

            if (canSelect)
            {
                List<byte> sendBackMessage = new List<byte>() { (byte)MessageType.ChangeGameState };
                sendBackMessage.AddRange(BitConverter.GetBytes(RoomId));
                sendBackMessage.AddRange(bytes.Array.Skip(origOffset).ToArray());
                sendBackMessage.AddRange(BitConverter.GetBytes(senderPlayer));
                await SendToRoom(sendBackMessage, ws);
            }
        }

        public void AddPlayerWS(int playerId, WebSocket ws)
        {
            if (PlayerWebSockets.ContainsKey(playerId))
            {
                var prevWs = PlayerWebSockets[playerId];
                if (prevWs != ws)
                {
                    if (webSocketPlayers.ContainsKey(prevWs))
                    {
                        webSocketPlayers.Remove(prevWs);
                    }
                    PlayerWebSockets[playerId] = ws;
                }
            }
            else
            {
                PlayerWebSockets.Add(playerId, ws);
            }

            if (webSocketPlayers.ContainsKey(ws))
            {
                webSocketPlayers[ws] = playerId;
            }
            else
            {
                webSocketPlayers.Add(ws, playerId);
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
                        webSocketPlayers.Remove(ws);
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
