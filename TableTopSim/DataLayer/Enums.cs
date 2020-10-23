using System;
using System.Collections.Generic;
using System.Text;

namespace DataLayer
{
    public enum MessageType
    {
        CreateRoom = 0x00,
        JoinRoom = 0x01,
        StartGame = 0x02,
        ReJoin = 0x03,
        RequestGameState = 0x04,
        GameState = 0x05,
        ChangeGameState = 0x06,
    }
}
