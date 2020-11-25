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
    class PlayerUpdateRoomData
    {
        readonly int FullUpdateRate = 10;
        //public object LockObject { get; set; }
        public PathTrie<object> UpdatePaths { get; set; }
        int pUpdateCounter = 0;
        public int PartialUpdateCounter { get => pUpdateCounter; set => pUpdateCounter = value % FullUpdateRate; }
        public PlayerUpdateRoomData()
        {
            //LockObject = new object();
            UpdatePaths = new PathTrie<object>();
            pUpdateCounter = 0;
        }
    }
    public class GameRoom
    {
        public int RoomId { get; }
        public Dictionary<int, WebSocket> PlayerWebSockets { get; private set; }
        Dictionary<int, PlayerUpdateRoomData> playerData;
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

            GameStarted = false;
            RoomId = roomId;
            GameId = gameId;
            PlayerWebSockets = new Dictionary<int, WebSocket>();
            webSocketPlayers = new Dictionary<WebSocket, int>();
            playerData = new Dictionary<int, PlayerUpdateRoomData>();

            webSocketPlayers.Add(initPlayerWs, initPlayerId);
            PlayerWebSockets.Add(initPlayerId, initPlayerWs);
            playerData.Add(initPlayerId, new PlayerUpdateRoomData());
        }
        public async Task InitalizeRoom(SqlConnection sqlConnection)
        {
            GameDataDto gameData = await ReTryer.Try(100, 5, async () => await GameController.GetGame(sqlConnection, GameId));
            if (gameData == null)
            {
                throw new NullReferenceException($"Cound't Get Game: {GameId}");
            }
            var spriteData = JsonConvert.DeserializeObject<Dictionary<int, Sprite>>(gameData.SerializedSpriteDictionary, new SpriteJsonConverter());
            foreach (var k in spriteData.Keys)
            {
                Sprite s = spriteData[k];
                refManager.AddSprite(k, s);
                s.LayerDepth.AddAtStart(0);
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
            sprite.LayerDepth.AddAtStart(0);
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
            //CancellationToken ct = cts.Token;
            playerCursors = new Dictionary<int, CursorInfo>();
            foreach (var pId in PlayerWebSockets.Keys)
            {
                RectSprite cursor = new RectSprite(refManager, new Vector2(0, 0), new Vector2(8, 16), new Color(200, 200, 200), Vector2.Zero, 0) { Selectable = false };
                cursor.Transform.Scale *= 1.15f;
                int cAd = AddSprite(cursor);
                cursor.LayerDepth[0] = -1;
                playerCursors.Add(pId, new CursorInfo(cAd, null));
            }

        }

        public Task RecievedChangeGame(WebSocket ws, ArraySegment<byte> arrSegBytes)
        {
            int senderPlayer;
            if (webSocketPlayers.ContainsKey(ws))
            {
                senderPlayer = webSocketPlayers[ws];
            }
            else
            {
                return Task.CompletedTask;
            }
            int origOffset = arrSegBytes.Offset;
            ArrayWithOffset<byte> bytes = new ArrayWithOffset<byte>(arrSegBytes.Array, arrSegBytes.Offset);
            int spritesDictLength = BitConverter.ToInt32(bytes.Array, bytes.Offset);
            bytes.Offset += 4;
            bool canSelect = true;
            ArrayWithOffset<byte> serializedSpritesDict = null;
            if (spritesDictLength >= 0)
            {
                serializedSpritesDict = bytes.Slice(0, spritesDictLength);
                bytes.Offset += spritesDictLength;
                int? selectedSprite = null;
                if (bytes[0] != 0)
                {
                    selectedSprite = BitConverter.ToInt32(bytes.Array, bytes.Offset + 1);
                }
                bytes.Offset += 5;
                lock (gameLockObject)
                {
                    if (selectedSprite != null)
                    {
                        foreach (var k in playerCursors.Keys)
                        {
                            if (k == senderPlayer) { continue; }
                            var v = playerCursors[k];
                            if (v.SelectedSpriteId == selectedSprite.Value)
                            {
                                canSelect = false;
                                break;
                            }
                        }
                    }
                    if (canSelect)
                    {
                        playerCursors[senderPlayer].SelectedSpriteId = selectedSprite;

                        if (spritesDictLength > 0)
                        {
                            PathTrie<object> deserializedPaths = new PathTrie<object>();
                            refManager.SpriteRefrences = GameSerialize.DeserializeEditGameData(refManager.SpriteRefrences, serializedSpritesDict, null, deserializedPaths);

                            refManager.UpdateSpriteAddresses();

                            foreach (var p in playerData.Keys)
                            {
                                if (p == senderPlayer) { continue; }
                                var d = playerData[p];
                                d.UpdatePaths.InsertTrie(deserializedPaths, true);
                            }
                        }
                    }
                }
            }
            _ = SendToPlayer(senderPlayer, canSelect, spritesDictLength < 0);
            return Task.CompletedTask;
            //lock (gameLockObject)
            //{
            //    if (selectedSprite != null)
            //    {
            //        foreach(var k in playerCursors.Keys)
            //        {
            //            if (k == senderPlayer) { continue; }
            //            var v = playerCursors[k];
            //            if(v.SelectedSpriteId == selectedSprite.Value)
            //            {
            //                canSelect = false;
            //                break;
            //            }
            //        }
            //    }
            //    if (canSelect)
            //    {
            //        playerCursors[senderPlayer].SelectedSpriteId = selectedSprite;
            //        refManager.SpriteRefrences = GameSerialize.DeserializeEditGameData(refManager.SpriteRefrences, serializedSpritesDict);
            //        refManager.UpdateSpriteAddresses();
            //    }
            //}

            //if (canSelect)
            //{
            //    List<byte> sendBackMessage = new List<byte>() { (byte)MessageType.ChangeGameState };
            //    sendBackMessage.AddRange(BitConverter.GetBytes(RoomId));
            //    sendBackMessage.AddRange(bytes.Array.Skip(origOffset).ToArray());
            //    sendBackMessage.AddRange(BitConverter.GetBytes(senderPlayer));
            //    await SendToRoom(sendBackMessage, ws);
            //}
        }
        async Task SendToPlayer(int playerId, bool couldSelect, bool forceFullUpdate)
        {
            WebSocket ws = PlayerWebSockets[playerId];
            if (ws != null)
            {
                List<byte> sendBackMessage = new List<byte>() { 0 };
                sendBackMessage.AddRange(BitConverter.GetBytes(RoomId));
                sendBackMessage.Add((byte)(couldSelect ? 0 : 255));

                var pData = playerData[playerId];
                if (forceFullUpdate || !couldSelect)
                {
                    pData.PartialUpdateCounter = 0;
                }

                List<byte> cursorData;
                List<byte> serialzedData;
                lock (gameLockObject)
                {
                    cursorData = GameSerialize.SerializeGameData(playerCursors);

                    if (pData.PartialUpdateCounter == 0)
                    {
                        sendBackMessage[0] = (byte)MessageType.GameState;
                        serialzedData = GameSerialize.SerializeGameData(refManager.SpriteRefrences);
                    }
                    else
                    {
                        sendBackMessage[0] = (byte)MessageType.ChangeGameState;
                        serialzedData = GameSerialize.SpecificSerializeGameData(refManager.SpriteRefrences, pData.UpdatePaths);
                    }
                    pData.PartialUpdateCounter++;
                    pData.UpdatePaths.Clear();
                }
                sendBackMessage.AddRange(BitConverter.GetBytes(serialzedData.Count));
                sendBackMessage.AddRange(serialzedData);
                sendBackMessage.AddRange(BitConverter.GetBytes(cursorData.Count));
                sendBackMessage.AddRange(cursorData);
                if (ws.State != WebSocketState.Open)
                {
                    webSocketPlayers.Remove(ws);
                    PlayerWebSockets[playerId] = null;
                    return;
                }

                await ws.SendAsync(new ArraySegment<byte>(sendBackMessage.ToArray()), WebSocketMessageType.Binary, true, CancellationToken.None);
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
                    if (playerData.ContainsKey(playerId))
                    {
                        playerData[playerId] = new PlayerUpdateRoomData();
                    }
                    else
                    {
                        playerData.Add(playerId, new PlayerUpdateRoomData());
                    }
                }
            }
            else
            {
                PlayerWebSockets.Add(playerId, ws);
                playerData.Add(playerId, new PlayerUpdateRoomData());
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
