using DataLayer;
using GameLib.GameSerialization;
using GameLib.Sprites;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Net.WebSockets;
using System.Numerics;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using TableTopSim.Server.Controllers;

namespace TableTopSim.Server
{
    public class SocketHandler
    {
        private static SocketHandler instance;
        private static object instanceLock = new object();
        static Dictionary<MessageType, Func<WebSocket, long, ArraySegment<byte>, Task>> MessageFunctions = null;
        public Dictionary<int, (WebSocket ws, int? room)> PlayerWebSockets { get; set; }
        public Dictionary<int, GameRoom> GameRooms { get; set; }
        SqlConnection sqlConnection;
        private SocketHandler(SqlConnection sqlConnection)
        {
            this.sqlConnection = sqlConnection;
            PlayerWebSockets = new Dictionary<int, (WebSocket ws, int? room)>();
            GameRooms = new Dictionary<int, GameRoom>();
            if(MessageFunctions == null)
            {
                MessageFunctions = new Dictionary<MessageType, Func<WebSocket, long, ArraySegment<byte>, Task>>();
                MessageFunctions.Add(MessageType.CreateRoom, OnCreateRoom);
                MessageFunctions.Add(MessageType.JoinRoom, OnJoinRoom);
                MessageFunctions.Add(MessageType.StartGame, OnStartGame);
                MessageFunctions.Add(MessageType.ReJoin, OnReJoin);
            }


            RectSprite rectSprite = new RectSprite(new Vector2(1, 2), new Vector2(3, 4), new GameLib.Color(123, 223, 255), Vector2.Zero, 45);
            EmptySprite emptySprite = new EmptySprite(new Vector2(10, 11), new Vector2(3, 3), new Vector2(-1, -1), -135);
            rectSprite.LayerDepth = 0.3141592f;
            var dict = new Dictionary<int, Sprite> { { 0, rectSprite }, { 1, emptySprite } };
            var b = GameSerialize.SerializeDict(dict, new Dictionary<object, HashSet<int>> { { dict, new HashSet<int> { 0, 1 } },
                {emptySprite, new HashSet<int>() { 0, 1,4} } }, new HashSet<int>() { 2 });
            var newDict = new Dictionary<int, Sprite> { { 0,null}, { 2, null } };
            newDict = GameSerialize.DeserializeEditGameData(newDict, b.ToArray());
        }

        public static SocketHandler Get(SqlConnection sqlConnection)
        {
            if (instance == null)
            {
                lock (instanceLock)
                {
                    if (instance == null)
                    {
                        instance = new SocketHandler(sqlConnection);
                    }
                }
            }
            return instance;
        }

        public async Task StartWebsocket(HttpContext context, WebSocket webSocket)
        {
            var buffer = new byte[1024 * 4];
            WebSocketReceiveResult result = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
            while (!result.CloseStatus.HasValue)
            {
                if (result.Count >= 9)
                {
                    ArraySegment<byte> messageBytes = new ArraySegment<byte>(buffer, 0, result.Count);
                    MessageType msgType = (MessageType)messageBytes[0];
                    long messageId = BitConverter.ToInt64(messageBytes.Slice(1, 8));
                    messageBytes = messageBytes.Slice(9);
                    if (MessageFunctions.ContainsKey(msgType))
                    {
                        await MessageFunctions[msgType]?.Invoke(webSocket, messageId, messageBytes);
                    }
                }
                result = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
            }
            await webSocket.CloseAsync(result.CloseStatus.Value, result.CloseStatusDescription, CancellationToken.None);
        }
        byte GetBoolByte(bool b)
        {
            return (byte)(b ? 255 : 0);
        }

        List<byte> ConfirmationList(long messageId, MessageType messageType, bool error)
        {
            List<byte> bytes = new List<byte>();
            bytes.Add((byte)messageType);
            bytes.AddRange(BitConverter.GetBytes(messageId));
            bytes.Add(GetBoolByte(error));
            return bytes;
        }
        void AddPlayerWS(int playerId, WebSocket ws, int? roomId)
        {
            if (PlayerWebSockets.ContainsKey(playerId))
            {
                PlayerWebSockets[playerId] = (ws, roomId);
            }
            else
            {
                PlayerWebSockets.Add(playerId, (ws, roomId));
            }
        }
        #region MessageFunctions
        async Task OnCreateRoom(WebSocket ws, long messageId, ArraySegment<byte> msgBytes)
        {
            MessageType msgType = MessageType.CreateRoom;
            string playerName = msgBytes.GetNextString();
            var playerAndRoom = await RoomController.CreatePlayerAndRoom(sqlConnection, playerName);
            List<byte> sendBytes = ConfirmationList(messageId, msgType, playerAndRoom == null);
            if(playerAndRoom != null)
            {
                sendBytes.AddRange(BitConverter.GetBytes(playerAndRoom.PlayerId));
                sendBytes.AddRange(BitConverter.GetBytes(playerAndRoom.RoomId));
                AddPlayerWS(playerAndRoom.PlayerId, ws, playerAndRoom.RoomId);
                var gr = new GameRoom(playerAndRoom.RoomId, playerAndRoom.PlayerId, ws);
                if (GameRooms.ContainsKey(playerAndRoom.RoomId))
                {
                    GameRooms[playerAndRoom.RoomId] = gr;
                }
                else
                {
                    GameRooms.Add(playerAndRoom.RoomId, gr);
                }
            }
            if (ws.State == WebSocketState.Open)
            {
                await ws.SendAsync(new ArraySegment<byte>(sendBytes.ToArray()), WebSocketMessageType.Binary, true, CancellationToken.None);
            }
            else
            {
                if(playerAndRoom != null)
                {
                    PlayerWebSockets.Remove(playerAndRoom.PlayerId);
                    GameRooms.Remove(playerAndRoom.RoomId);
                }
            }
        }
        async Task OnJoinRoom(WebSocket ws, long messageId, ArraySegment<byte> msgBytes)
        {
            MessageType msgType = MessageType.JoinRoom;
            int roomId = MessageExtensions.GetNextInt(ref msgBytes);
            string playerName = msgBytes.GetNextString();
            int? playerId = await RoomController.CreatePlayerInRoom(sqlConnection, playerName, roomId);
            bool error = playerId == null || !GameRooms.ContainsKey(roomId) || GameRooms[roomId].GameStarted;
            List<byte> sendBytes = ConfirmationList(messageId, msgType, error);
            if (!error)
            {
                sendBytes.AddRange(BitConverter.GetBytes(playerId.Value));
                sendBytes.AddRange(BitConverter.GetBytes(roomId));
                AddPlayerWS(playerId.Value, ws, roomId);
                GameRooms[roomId].AddPlayerWS(playerId.Value, ws);
            }
            await ws.SendAsync(new ArraySegment<byte>(sendBytes.ToArray()), WebSocketMessageType.Binary, true, CancellationToken.None);
        }

        async Task OnStartGame(WebSocket ws, long messageId, ArraySegment<byte> msgBytes)
        {
            MessageType msgType = MessageType.StartGame;
            int roomId = MessageExtensions.GetNextInt(ref msgBytes);
            int playerId = MessageExtensions.GetNextInt(ref msgBytes);

            bool gameStarted = await RoomController.StartGame(sqlConnection, roomId, playerId);
            if (gameStarted && GameRooms.ContainsKey(roomId))
            {
                GameRooms[roomId].GameStarted = true;
                List<byte> sendBytes = new List<byte>();
                sendBytes.Add((byte)msgType);
                sendBytes.AddRange(BitConverter.GetBytes(roomId));
                await GameRooms[roomId].SendToRoom(new ArraySegment<byte>(sendBytes.ToArray()));
            }
        }

        async Task OnReJoin(WebSocket ws, long messageId, ArraySegment<byte> msgBytes)
        {
            MessageType msgType = MessageType.ReJoin;
            int playerId = MessageExtensions.GetNextInt(ref msgBytes);
            var playerInfo = await RoomController.GetPlayerRoom(sqlConnection, playerId);
            bool error = playerInfo == null || !PlayerWebSockets.ContainsKey(playerId);
            List<byte> sendBytes = ConfirmationList(messageId, msgType, error);
            if (!error)
            {
                int sendRoomId = playerInfo.RoomId == null ? -1 : playerInfo.RoomId.Value;
                sendBytes.AddRange(BitConverter.GetBytes(sendRoomId));
                sendBytes.Add(GetBoolByte(playerInfo.IsHost));
                sendBytes.Add(GetBoolByte(playerInfo.RoomOpen));
                MessageExtensions.AddStringBytes(sendBytes, playerInfo.Name);
                PlayerWebSockets[playerId] = (ws, playerInfo.RoomId);
                if(playerInfo.RoomId != null && GameRooms.ContainsKey(sendRoomId) 
                    && GameRooms[sendRoomId].PlayerWebSockets.ContainsKey(playerId))
                {
                    GameRooms[sendRoomId].PlayerWebSockets[playerId] = ws;
                }
            }
            await ws.SendAsync(new ArraySegment<byte>(sendBytes.ToArray()), WebSocketMessageType.Binary, true, CancellationToken.None);
        }
        #endregion
    }
}
