using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;

namespace TableTopSim.Server
{
    public class GameRoom
    {
        public int RoomId { get;  }
        public Dictionary<int, WebSocket> PlayerWebSockets { get; private set; }
        public bool GameStarted { get; set; }
        public GameRoom(int roomId, int? initPlayerId, WebSocket initPlayerWs)
        {
            GameStarted = false;
            RoomId = roomId;
            PlayerWebSockets = new Dictionary<int, WebSocket>();
            if(initPlayerId != null)
            {
                PlayerWebSockets.Add(initPlayerId.Value, initPlayerWs);
            }
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
        public async Task SendToRoom(ArraySegment<byte> bytes)
        {
            foreach(var pId in PlayerWebSockets.Keys)
            {
                var ws = PlayerWebSockets[pId];
                if(ws != null) 
                {
                    if (ws.State != WebSocketState.Open)
                    {
                        PlayerWebSockets[pId] = null;
                        continue;
                    }

                    await ws.SendAsync(bytes, WebSocketMessageType.Binary, true, CancellationToken.None);
                }
            }
        }
    }
}
