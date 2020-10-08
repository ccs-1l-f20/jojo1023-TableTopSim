using System;
using System.Collections.Generic;
using System.Text;

namespace TableTopSim.Shared
{

    public class PlayerAndRoomId
    {
        public int PlayerId { get; set; }
        public int RoomId { get; set; }
        public PlayerAndRoomId(int playerId, int roomId)
        {
            PlayerId = playerId;
            RoomId = roomId;
        }
    }
}
