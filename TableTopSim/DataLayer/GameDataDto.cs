using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace DataLayer
{
    public class GameDataDto
    {
        public string Name { get; set; }
        public int Width{ get; set; }
        public int Height { get; set; }
        public int MinPlayers { get; set; }
        public int MaxPlayers { get; set; }
        public string SerializedSpriteDictionary { get; set; }
        [JsonConstructor]
        public GameDataDto() { }
        public GameDataDto(string name, int width, int height, int minPlayers, int maxPlayers, string serializedSpriteDictionary)
        {
            Name = name;
            Width = width;
            Height = height;
            MinPlayers = minPlayers;
            MaxPlayers = maxPlayers;
            SerializedSpriteDictionary = serializedSpriteDictionary;
        }
    }
}
