using DataLayer;
using GameLib;
using GameLib.Sprites;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Numerics;

namespace CreateJsonGamesProj
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Enter Game Json File Name");
            string gameName = Console.ReadLine();
            Dictionary<int, Sprite> sprites = new Dictionary<int, Sprite>();
            sprites.Add(0, new RectSprite(null, new Vector2(200, 200), new Vector2(100, 200), new Color(100, 100, 255), new Vector2(50, 100), 0));
            sprites.Add(1, new RectSprite(null, new Vector2(200, 200), new Vector2(10, 10), new Color(255, 0, 255), new Vector2(0, 0), 45));
            sprites.Add(2, new RectSprite(null, new Vector2(500, 500), new Vector2(50, 50), new Color(0, 0, 0), new Vector2(0, 0), 0));
            sprites.Add(3, new RectSprite(null, new Vector2(600, 500), new Vector2(50, 50), new Color(128, 128, 128), new Vector2(0, 0), 0));
            sprites.Add(4, new ImageSprite(null, new Vector2(500, 100), 2, new Vector2(75, 75), Vector2.Zero));
            sprites.Add(5, new ImageSprite(null, new Vector2(500, 100), 3, new Vector2(100, 100), Vector2.Zero));
            sprites.Add(6, new ImageSprite(null, new Vector2(500, 100), 1, new Vector2(338, 469), Vector2.Zero, 45));
            Dictionary<int, string> images = new Dictionary<int, string>();
            images.Add(1, "cardBack.png");
            images.Add(2, "Chess_kdt45.svg");
            images.Add(3, "Chess_qdt45.svg");
            JsonGame jsonGame = new JsonGame(1, 2, new Size(1000, 1000), JsonConvert.SerializeObject(sprites), images);
            string jsonGameText = JsonConvert.SerializeObject(jsonGame);
            File.WriteAllText($"../../../../TestData/{gameName}.json", jsonGameText);
        }
    }
}
