using DataLayer;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Net.WebSockets;
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
            }
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
                    long messageId = BitConverter.ToInt64(messageBytes.Slice(0, 8));
                    MessageType msgType = (MessageType)messageBytes[8];
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
        List<byte> ConfirmationList(long messageId, MessageType messageType, bool error)
        {
            List<byte> bytes = new List<byte>();
            bytes.AddRange(BitConverter.GetBytes(messageId));
            bytes.Add((byte)messageType);
            bytes.Add((byte)(error ? 255 : 0));
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
            string playerName = msgBytes.GetNextString();
            var playerAndRoom = await RoomController.CreatePlayerAndRoom(sqlConnection, playerName);
            List<byte> sendBytes = ConfirmationList(messageId, MessageType.CreateRoom, playerAndRoom == null);
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
            int roomId = MessageExtensions.GetNextInt(ref msgBytes);
            string playerName = msgBytes.GetNextString();
            int? playerId = await RoomController.CreatePlayerInRoom(sqlConnection, playerName, roomId);
            bool error = playerId == null || !GameRooms.ContainsKey(roomId);
            List<byte> sendBytes = ConfirmationList(messageId, MessageType.JoinRoom, error);
            if (!error)
            {
                sendBytes.AddRange(BitConverter.GetBytes(playerId.Value));
                sendBytes.AddRange(BitConverter.GetBytes(roomId));
                AddPlayerWS(playerId.Value, ws, roomId);
                GameRooms[roomId].AddPlayerWS(playerId.Value, ws);
            }
            await ws.SendAsync(new ArraySegment<byte>(sendBytes.ToArray()), WebSocketMessageType.Binary, true, CancellationToken.None);
        }
        #endregion
    }
}
