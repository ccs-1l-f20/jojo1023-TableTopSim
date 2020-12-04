using GameLib;
using GameLib.Sprites;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace DataLayer
{
    public class JsonGame
    {
        public Dictionary<int, string> ImageNames { get; set; } = new Dictionary<int, string>();
        //public Dictionary<int, Sprite> Sprites { get; set; } = new Dictionary<int, Sprite>();
        public Dictionary<int, Sprite> Sprites { get; set; }
        public Size CanvasSize { get; set; }
        public int MinPlayers { get; set; }
        public int MaxPlayers { get; set; }
        public Color BackroundColor { get; set; }
        public Dictionary<int, StackableDataInfo> StackableDataInfo { get; set; }

        [JsonConstructor]
        public JsonGame() { }
        public JsonGame(int minPlayers, int maxPlayers, Size canvasSize, Color backroundColor, Dictionary<int, Sprite> sprites, Dictionary<int, string> imageNames, 
            Dictionary<int, StackableDataInfo> stackableDataInfo) 
        {
            MinPlayers = minPlayers;
            MaxPlayers = maxPlayers;
            CanvasSize = canvasSize;
            Sprites = sprites;
            ImageNames = imageNames;
            BackroundColor = backroundColor;
            StackableDataInfo = stackableDataInfo;
        }
    }
}
