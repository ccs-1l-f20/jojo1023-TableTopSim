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
        JoinedMidgame = 0x03,
        GameState = 0x04,
        ChangeGameState = 0x05,
    }
}
