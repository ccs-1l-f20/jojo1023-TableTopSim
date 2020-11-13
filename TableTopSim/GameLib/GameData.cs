using GameLib.Sprites;
using System;
using System.Collections.Generic;
using System.Text;

namespace GameLib
{
    public class GameData
    {
        static GameData instance = new GameData();
        object insLockObject = new object();
        public SpriteRefrenceManager SpriteRefMngr { get; private set; }
        private GameData()
        {

        }

        public GameData Get()
        {
            lock (insLockObject)
            {
                return instance;
            }
        }
    }
}
