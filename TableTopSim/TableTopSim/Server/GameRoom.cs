using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.WebSockets;
using System.Threading.Tasks;

namespace TableTopSim.Server
{
    public class GameRoom
    {
        public int RoomId { get;  }
        public Dictionary<int, WebSocket> PlayerWebSockets { get; private set; }
        public GameRoom(int roomId, int? initPlayerId, WebSocket initPlayerWs)
        {
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
    }
}
