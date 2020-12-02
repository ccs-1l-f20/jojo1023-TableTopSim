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
    enum ChessPiece
    {
        Pawn = 0,
        Knight = 1,
        Bishop = 2,
        Rook = 3,
        Queen = 4,
        King = 5
    }
    class Program
    {
        static int PieceHash(ChessPiece chessPiece, bool isWhite)
        {
            return ((int)chessPiece) * 2 + (isWhite ? 1 : 0);
        }
        static void Main(string[] args)
        {
            Console.WriteLine("Enter Game Json File Name");
            string gameName = Console.ReadLine();

            if (false)
            {
                Basic(gameName);
                return;
            }

            Size canvasSize = new Size(1000, 1000);
            Dictionary<int, Sprite> sprites = new Dictionary<int, Sprite>();
            Dictionary<int, string> images = new Dictionary<int, string>()
            {
                {PieceHash(ChessPiece.King, false), "Chess_kdt45.svg" },
                {PieceHash(ChessPiece.King, true), "Chess_klt45.svg" },
                {PieceHash(ChessPiece.Queen, false), "Chess_qdt45.svg" },
                {PieceHash(ChessPiece.Queen, true), "Chess_qlt45.svg" },
                {PieceHash(ChessPiece.Rook, false), "Chess_rdt45.svg" },
                {PieceHash(ChessPiece.Rook, true), "Chess_rlt45.svg" },
                {PieceHash(ChessPiece.Bishop, false), "Chess_bdt45.svg" },
                {PieceHash(ChessPiece.Bishop, true), "Chess_blt45.svg" },
                {PieceHash(ChessPiece.Knight, false), "Chess_ndt45.svg" },
                {PieceHash(ChessPiece.Knight, true), "Chess_nlt45.svg" },
                {PieceHash(ChessPiece.Pawn, false), "Chess_pdt45.svg" },
                {PieceHash(ChessPiece.Pawn, true), "Chess_plt45.svg" },
            };
            //images.Add(1, "cardBack.png");
            int boardImgId = 100;
            images.Add(boardImgId, "600px-Chess_Board.png");

            Vector2 boardSize = new Vector2(750, 750);
            ImageSprite boardSprite;
            sprites.Add(boardImgId, boardSprite = new ImageSprite(null, new Vector2(canvasSize.Width/2, canvasSize.Height/2), boardImgId, boardSize, boardSize/2) { Selectable = false, });
            boardSprite.LayerDepth[0] = 100;

            Vector2 boardPadding = boardSize * (4f/600f);
            Vector2 checkerSize = (boardSize - (2 * boardPadding)) / 8;

            Vector2 GetCenterCheckerPos(int x, int y)
            {
                return boardSprite.Transform.Position - boardSprite.Origin + boardPadding + (new Vector2(x + 0.5f, y + 0.5f) * checkerSize);
            }
            int spiteId = boardImgId + 1;
            for(int i = 0; i < 8; i++)
            {
                int pawnBPath = PieceHash(ChessPiece.Pawn, false);
                int pawnWPath = PieceHash(ChessPiece.Pawn, true);
                sprites.Add(spiteId,  new ImageSprite(null, GetCenterCheckerPos(i, 1), pawnBPath, checkerSize, checkerSize / 2, 180) { StackableIndex = pawnBPath } );
                spiteId++;
                sprites.Add(spiteId, new ImageSprite(null, GetCenterCheckerPos(i, 6), pawnWPath, checkerSize, checkerSize / 2) { StackableIndex = pawnWPath });
                spiteId++;
            }
            List<ChessPiece> cpOrder = new List<ChessPiece>() { ChessPiece.Rook, ChessPiece.Knight, ChessPiece.Bishop, ChessPiece.Queen, ChessPiece.King,
                                                                ChessPiece.Bishop, ChessPiece.Knight, ChessPiece.Rook };
            for(int i =0;i < cpOrder.Count; i++)
            {
                ChessPiece cp = cpOrder[i];
                int bPath = PieceHash(cp, false);
                int wPath = PieceHash(cp, true);
                sprites.Add(spiteId, new ImageSprite(null, GetCenterCheckerPos(i, 0), bPath, checkerSize, checkerSize / 2, 180) { StackableIndex = bPath });
                spiteId++;
                sprites.Add(spiteId, new ImageSprite(null, GetCenterCheckerPos(i, 7), wPath, checkerSize, checkerSize / 2) { StackableIndex = wPath });
                spiteId++;
            }

            for(int i = 0; i < 8; i++)
            {
                //int bKnightPath = PieceHash(ChessPiece.Knight, false);
                //int wKnightPath = PieceHash(ChessPiece.Knight, true);
                //int bQueenPath = PieceHash(ChessPiece.Queen, false);
                //int wQueenPath = PieceHash(ChessPiece.Queen, true);
                //sprites.Add(spiteId, new ImageSprite(null, GetCenterCheckerPos(-1, 3), bKnightPath, checkerSize, checkerSize / 2, 0));
                //spiteId++;
                //sprites.Add(spiteId, new ImageSprite(null, GetCenterCheckerPos(-1, 4), bQueenPath, checkerSize, checkerSize / 2, 0));
                //spiteId++;
                //sprites.Add(spiteId, new ImageSprite(null, GetCenterCheckerPos(8, 3), wKnightPath, checkerSize, checkerSize / 2, 0));
                //spiteId++;
                //sprites.Add(spiteId, new ImageSprite(null, GetCenterCheckerPos(8, 4), wQueenPath, checkerSize, checkerSize / 2, 0));
                //spiteId++;
            }

            JsonGame jsonGame = new JsonGame(1, 3, canvasSize, JsonConvert.SerializeObject(sprites), images);
            string jsonGameText = JsonConvert.SerializeObject(jsonGame);
            File.WriteAllText($"../../../../TestData/{gameName}.json", jsonGameText);
        }
        static void Basic(string gameName)
        {
            Size canvasSize = new Size(1000, 1000);
            Dictionary<int, Sprite> sprites = new Dictionary<int, Sprite>();
            Dictionary<int, string> images = new Dictionary<int, string>();

            sprites.Add(0, new RectSprite(null, new Vector2(200, 200), new Vector2(100, 200), new Color(100, 100, 255), new Vector2(50, 100), 0) { Alpha = 0.5f });
            sprites.Add(1, new RectSprite(null, new Vector2(200, 200), new Vector2(10, 10), new Color(255, 0, 255), new Vector2(0, 0), 45));
            sprites.Add(2, new RectSprite(null, new Vector2(500, 500), new Vector2(50, 50), new Color(0, 0, 0), new Vector2(0, 0), 0));
            sprites.Add(3, new RectSprite(null, new Vector2(600, 500), new Vector2(50, 50), new Color(128, 128, 128), new Vector2(0, 0), 0));
            JsonGame jsonGame = new JsonGame(1, 3, canvasSize, JsonConvert.SerializeObject(sprites), images);
            string jsonGameText = JsonConvert.SerializeObject(jsonGame);
            File.WriteAllText($"../../../../TestData/{gameName}.json", jsonGameText);
            //sprites.Add(4, new ImageSprite(null, new Vector2(500, 100), 2, new Vector2(75, 75), Vector2.Zero));
            //sprites.Add(5, new ImageSprite(null, new Vector2(500, 100), 3, new Vector2(100, 100), Vector2.Zero) { Selectable = false });
            //sprites.Add(6, new ImageSprite(null, new Vector2(500, 100), 1, new Vector2(338, 469), Vector2.Zero, 45));
        }
    }
}
