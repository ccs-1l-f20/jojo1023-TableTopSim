using GameLib;
using GameLib.Sprites;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace DataLayer
{
    public class JsonGame
    {
        public Dictionary<int, string> ImageNames { get; set; } = new Dictionary<int, string>();
        public Dictionary<int, Sprite> Sprites { get; set; } = new Dictionary<int, Sprite>();
        public List<int> GameSprites { get; set; } = new List<int>();
        public Size CanvasSize { get; set; }

        public JsonGame() { }
    }
}
