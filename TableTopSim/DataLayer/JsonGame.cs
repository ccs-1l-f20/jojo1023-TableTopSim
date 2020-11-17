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
        public string SerializedSprites { get; set; }
        public Size CanvasSize { get; set; }
        public int MinPlayers { get; set; }
        public int MaxPlayers { get; set; }
        [JsonConstructor]
        public JsonGame() { }
        public JsonGame(int minPlayers, int maxPlayers, Size canvasSize, string serializedSprites, Dictionary<int, string> imageNames) 
        {
            MinPlayers = minPlayers;
            MaxPlayers = maxPlayers;
            CanvasSize = canvasSize;
            SerializedSprites = serializedSprites;
            ImageNames = imageNames;
        }
    }
}
