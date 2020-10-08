using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;


namespace TableTopSim.Shared
{
    public class RoomData
    {
        public string PlayerName { get; set; }
        public int RoomId { get; set; }
        public bool IsHost { get; set; } = false;

        public int PlayerId { get; set; } = -1;

        public void Reset()
        {
            
            PlayerName = "";
            PlayerId = -1;
            RoomId = -1;
            IsHost = false;
        }
    }
}
