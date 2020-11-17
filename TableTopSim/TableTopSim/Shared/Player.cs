using System;
using System.Collections.Generic;
using System.Text;

namespace TableTopSim.Shared
{
    public class Player
    {
        public int PlayerId { get; set; }
        public string Name { get; set; }
        public int? RoomId { get; set; }
        public int GameId { get; set; }
        public bool IsHost { get; set; }
        public bool RoomOpen { get; set; }
        public Player(int playerId, string name, int? roomId, int gameId, bool isHost, bool roomOpen)
        {
            PlayerId = playerId;
            Name = name;
            RoomId = roomId;
            GameId = gameId;
            IsHost = isHost;
            RoomOpen = roomOpen;
        }
    }
}
