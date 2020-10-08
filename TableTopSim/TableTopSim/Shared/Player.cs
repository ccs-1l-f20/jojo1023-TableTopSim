using System;
using System.Collections.Generic;
using System.Text;

namespace TableTopSim.Shared
{
    public class Player
    {
        public int PlayerId { get; }
        public string Name { get; }
        public int? RoomId { get; }
        public Player(int playerId, string name, int? roomId)
        {
            PlayerId = playerId;
            Name = name;
            RoomId = roomId;
        }
    }
}
