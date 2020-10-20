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
        public bool IsHost { get; set; }
        public Player(int playerId, string name, int? roomId, bool isHost)
        {
            PlayerId = playerId;
            Name = name;
            RoomId = roomId;
            IsHost = isHost;
        }
    }
}
