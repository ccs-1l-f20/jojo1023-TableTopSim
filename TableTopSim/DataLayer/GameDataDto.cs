using GameLib;
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
        public Color BackroundColor { get; set; }
        public string SerializedSpriteDictionary { get; set; }
        public string SerializedStackableInfoDictionary { get; set; }
        [JsonConstructor]
        public GameDataDto() { }
        public GameDataDto(string name, int width, int height, int minPlayers, int maxPlayers, Color backroundColor, string serializedSpriteDictionary,
            string serializedStackableInfoDictionary)
        {
            Name = name;
            Width = width;
            Height = height;
            MinPlayers = minPlayers;
            MaxPlayers = maxPlayers;
            SerializedSpriteDictionary = serializedSpriteDictionary;
            BackroundColor = backroundColor;
            SerializedStackableInfoDictionary = serializedStackableInfoDictionary;
        }

        public GameDataDto(string name, JsonGame jsonGame)
        {
            Name = name;
            Width = (int)jsonGame.CanvasSize.Width;
            Height = (int)jsonGame.CanvasSize.Height;
            MinPlayers = jsonGame.MinPlayers;
            MaxPlayers = jsonGame.MaxPlayers;
            SerializedSpriteDictionary = JsonConvert.SerializeObject(jsonGame.Sprites);
            BackroundColor = jsonGame.BackroundColor;
            if (jsonGame.StackableDataInfo == null)
            {
                jsonGame.StackableDataInfo = new Dictionary<int, StackableDataInfo>();
            }
            SerializedStackableInfoDictionary = JsonConvert.SerializeObject(jsonGame.StackableDataInfo);
        }
    }
}
