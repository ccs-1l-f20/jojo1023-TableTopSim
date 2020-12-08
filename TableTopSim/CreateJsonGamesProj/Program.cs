using DataLayer;
using GameLib;
using GameLib.Sprites;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Numerics;
using RectangleF = System.Drawing.RectangleF;
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
            Console.WriteLine("Pick a Game Type");
            Console.WriteLine("1: Chess");
            Console.WriteLine("2: Splendor");
            Console.WriteLine("3: Basic");
            string input = Console.ReadLine();

            Console.WriteLine("Enter Game Json File Name");
            string gameName = Console.ReadLine();

            int intInput = 0;
            JsonGame jg = null;
            if (int.TryParse(input.Trim(), out intInput))
            {
                if (intInput == 1)
                {
                    jg = Chess(gameName);
                }
                else if (intInput == 2)
                {
                    jg = Splendor(gameName);
                }
            }
            if (jg == null)
            {
                jg = Basic(gameName);
            }
            string jsonGameText = JsonConvert.SerializeObject(jg);
            File.WriteAllText($"../../../../TestData/{gameName}.json", jsonGameText);
        }
        static JsonGame Basic(string gameName)
        {
            Size canvasSize = new Size(1000, 1000);
            Dictionary<int, Sprite> sprites = new Dictionary<int, Sprite>();
            Dictionary<int, string> images = new Dictionary<int, string>();

            sprites.Add(0, new RectSprite(null, new Vector2(200, 200), new Vector2(100, 200), new Color(100, 100, 255), new Vector2(50, 100), 0) { Alpha = 0.5f });
            sprites.Add(1, new RectSprite(null, new Vector2(200, 200), new Vector2(10, 10), new Color(255, 0, 255), new Vector2(0, 0), 45));
            sprites.Add(2, new RectSprite(null, new Vector2(500, 500), new Vector2(50, 50), new Color(0, 0, 0), new Vector2(0, 0), 0));
            sprites.Add(3, new RectSprite(null, new Vector2(600, 500), new Vector2(50, 50), new Color(128, 128, 128), new Vector2(0, 0), 0));
            JsonGame jsonGame = new JsonGame(1, 3, canvasSize, new Color(50, 50, 200), sprites, images, new Dictionary<int, StackableDataInfo>());
            return jsonGame;
        }

        static JsonGame Chess(string gameName)
        {
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
                //{567, "cardBack.png" },
            };
            int boardImgId = 100;
            images.Add(boardImgId, "600px-Chess_Board.png");

            Vector2 boardSize = new Vector2(750, 750);
            ImageSprite boardSprite;
            sprites.Add(boardImgId, boardSprite = new ImageSprite(null, new Vector2(canvasSize.Width / 2, canvasSize.Height / 2), boardImgId, boardSize, boardSize / 2) { Selectable = false, });
            boardSprite.LayerDepth[0] = 100;

            Vector2 boardPadding = boardSize * (4f / 600f);
            Vector2 checkerSize = (boardSize - (2 * boardPadding)) / 8;

            Vector2 GetCenterCheckerPos(int x, int y)
            {
                return boardSprite.Transform.Position - boardSprite.Origin + boardPadding + (new Vector2(x + 0.5f, y + 0.5f) * checkerSize);
            }
            int spiteId = boardImgId + 1;
            for (int i = 0; i < 8; i++)
            {
                int pawnBPath = PieceHash(ChessPiece.Pawn, false);
                int pawnWPath = PieceHash(ChessPiece.Pawn, true);
                sprites.Add(spiteId, new ImageSprite(null, GetCenterCheckerPos(i, 1), pawnBPath, checkerSize, checkerSize / 2, 180) { StackableIndex = pawnBPath });
                spiteId++;
                sprites.Add(spiteId, new ImageSprite(null, GetCenterCheckerPos(i, 6), pawnWPath, checkerSize, checkerSize / 2) { StackableIndex = pawnWPath });
                spiteId++;
            }
            List<ChessPiece> cpOrder = new List<ChessPiece>() { ChessPiece.Rook, ChessPiece.Knight, ChessPiece.Bishop, ChessPiece.Queen, ChessPiece.King,
                                                                ChessPiece.Bishop, ChessPiece.Knight, ChessPiece.Rook };
            for (int i = 0; i < cpOrder.Count; i++)
            {
                ChessPiece cp = cpOrder[i];
                int bPath = PieceHash(cp, false);
                int wPath = PieceHash(cp, true);
                sprites.Add(spiteId, new ImageSprite(null, GetCenterCheckerPos(i, 0), bPath, checkerSize, checkerSize / 2, 180) { StackableIndex = bPath });
                spiteId++;
                sprites.Add(spiteId, new ImageSprite(null, GetCenterCheckerPos(i, 7), wPath, checkerSize, checkerSize / 2) { StackableIndex = wPath });
                spiteId++;
            }

            int bKnightPath = PieceHash(ChessPiece.Knight, false);
            int wKnightPath = PieceHash(ChessPiece.Knight, true);
            int bQueenPath = PieceHash(ChessPiece.Queen, false);
            int wQueenPath = PieceHash(ChessPiece.Queen, true);
            SpriteStack bKnightStack = new SpriteStack(null, GetCenterCheckerPos(-1, 3), bKnightPath);
            sprites.Add(spiteId, bKnightStack);
            int bKnightStackId = spiteId;
            spiteId++;

            SpriteStack bQueenStack = new SpriteStack(null, GetCenterCheckerPos(-1, 4), bQueenPath);
            sprites.Add(spiteId, bQueenStack);
            int bQueenStackId = spiteId;
            spiteId++;

            SpriteStack wKnightStack = new SpriteStack(null, GetCenterCheckerPos(8, 3), wKnightPath);
            sprites.Add(spiteId, wKnightStack);
            int wKnightStackId = spiteId;
            spiteId++;

            SpriteStack wQueenStack = new SpriteStack(null, GetCenterCheckerPos(8, 4), wQueenPath);
            sprites.Add(spiteId, wQueenStack);
            int wQueenStackId = spiteId;
            spiteId++;

            for (int i = 0; i < 8; i++)
            {
                sprites.Add(spiteId, new ImageSprite(null, Vector2.Zero, bKnightPath, checkerSize, checkerSize / 2, 180) { StackableIndex = bKnightPath });
                bKnightStack.AddToStack(bKnightStackId, sprites[spiteId], spiteId);
                spiteId++;

                sprites.Add(spiteId, new ImageSprite(null, Vector2.Zero, bQueenPath, checkerSize, checkerSize / 2, 180) { StackableIndex = bQueenPath });
                bQueenStack.AddToStack(bQueenStackId, sprites[spiteId], spiteId);
                spiteId++;

                sprites.Add(spiteId, new ImageSprite(null, Vector2.Zero, wKnightPath, checkerSize, checkerSize / 2) { StackableIndex = wKnightPath });
                wKnightStack.AddToStack(wKnightStackId, sprites[spiteId], spiteId);
                spiteId++;

                sprites.Add(spiteId, new ImageSprite(null, Vector2.Zero, wQueenPath, checkerSize, checkerSize / 2) { StackableIndex = wQueenPath });
                wQueenStack.AddToStack(wQueenStackId, sprites[spiteId], spiteId);
                spiteId++;
            }

            Dictionary<int, StackableDataInfo> stackableInfo = new Dictionary<int, StackableDataInfo>();
            for (int i = 0; i < 5; i++)
            {
                ChessPiece cp = (ChessPiece)i;
                int wId = PieceHash(cp, true);
                int bId = PieceHash(cp, false);
                StackableDataInfo wInfo = new StackableDataInfo((checkerSize / 3) * new Vector2(1, -1), 15);
                wInfo.CountBackColor = new Color(0, 0, 0);
                wInfo.CountTextColor = new Color(255, 255, 255);
                StackableDataInfo bInfo = new StackableDataInfo((checkerSize / 3) * new Vector2(1, -1), 15);
                bInfo.CountBackColor = new Color(255, 255, 255);
                bInfo.CountTextColor = new Color(0, 0, 0);
                stackableInfo.Add(wId, wInfo);
                stackableInfo.Add(bId, bInfo);
            }

            return new JsonGame(1, 3, canvasSize, new Color(0, 0, 255), sprites, images, stackableInfo);
        }


        static JsonGame Splendor(string gameName)
        {
            int imageId = 0;
            Dictionary<int, string> images = new Dictionary<int, string>();
            //string cardPath = "Card";
            //string cardSuffex = ".png";
            //List<List<int>> cardImages = new List<List<int>>();
            //List<int> nobelImages = new List<int>();
            Vector2 boardPadding = new Vector2(1500, 1500);
            float padding = 30;
            Vector2 cardSize = new Vector2(491, 692);
            Vector2 cardSizeImg = new Vector2(200, 282);
            Vector2 nobelSize = new Vector2(470, 470);
            Vector2 nobelSizeImg = new Vector2(148, 148);
            Vector2 coinSize = new Vector2(335, 335);
            int cardsI_Id = imageId;
            images.Add(cardsI_Id, "CardsI.png");
            imageId++;
            int cardsII_Id = imageId;
            images.Add(cardsII_Id, "CardsII.png");
            imageId++;
            int cardsIII_Id = imageId;
            images.Add(cardsIII_Id, "CardsIII.png");
            imageId++;
            List<(int imgId, int index)> cardImages = new List<(int imgId, int index)>();
            Dictionary<int, RectangleF> backSourceRects = new Dictionary<int, RectangleF>();
            for(int i = 0; i < 40; i++) { cardImages.Add((cardsI_Id, i)); }
            backSourceRects.Add(cardsI_Id, new RectangleF(cardSizeImg.X * 40, 0, cardSizeImg.X, cardSizeImg.Y));
            for (int i = 0; i < 30; i++) { cardImages.Add((cardsII_Id, i)); }
            backSourceRects.Add(cardsII_Id, new RectangleF(cardSizeImg.X * 30, 0, cardSizeImg.X, cardSizeImg.Y));
            for (int i = 0; i < 20; i++) { cardImages.Add((cardsIII_Id, i)); }
            backSourceRects.Add(cardsIII_Id, new RectangleF(cardSizeImg.X * 20, 0, cardSizeImg.X, cardSizeImg.Y));
            int nobels_Id = imageId;
            images.Add(nobels_Id, "Nobels.png");
            imageId++;
            //for (int i = 0; i < 3; i++)
            //{
            //    List<int> stageImages = new List<int>();
            //    string cardPathIs = RepeatChar('I', i+1);
            //    int amountOfCards;
            //    if(i == 0) { amountOfCards = 40; }
            //    else if(i == 1) { amountOfCards = 30; }
            //    else { amountOfCards = 20; }
            //    for(int j = 0; j < amountOfCards; j++)
            //    {
            //        images.Add(imageId, cardPath + cardPathIs + j.ToString() + cardSuffex);
            //        stageImages.Add(imageId);
            //        imageId++; 
            //    }
            //    images.Add(imageId, cardPath + cardPathIs + "Back" + cardSuffex);
            //    stageImages.Add(imageId);
            //    imageId++;

            //    cardImages.Add(stageImages);
            //}

            //string nobelPath = "Nobel";
            //string nobelSuffex = ".png";
            //for (int i  = 0; i < 10; i++)
            //{
            //    images.Add(imageId, nobelPath + i.ToString() + nobelSuffex);
            //    nobelImages.Add(imageId);
            //    imageId++;
            //}
            //images.Add(imageId, nobelPath + "Back" + nobelSuffex);
            //nobelImages.Add(imageId);
            //imageId++;

            int cBlackImg = imageId;
            images.Add(imageId, "CoinBlack.png");
            imageId++;
            int cBlueImg = imageId;
            images.Add(imageId, "CoinBlue.png");
            imageId++;
            int cGoldImg = imageId;
            images.Add(imageId, "CoinGold.png");
            imageId++;
            int cGreenImg = imageId;
            images.Add(imageId, "CoinGreen.png");
            imageId++;
            int cRedImg = imageId;
            images.Add(imageId, "CoinRed.png");
            imageId++;
            int cWhiteImg = imageId;
            images.Add(imageId, "CoinWhite.png");
            imageId++;

            Dictionary<int, Sprite> sprites = new Dictionary<int, Sprite>();
            int spriteId = 0;
            Vector2 cardStackPos = boardPadding + nobelSize + new Vector2(0, padding);
            Vector2 maxPos = new Vector2(cardStackPos.X + 5 * (cardSize.X + padding), 0);
            Dictionary<int, (SpriteStack deck, int spriteId)> decks = new Dictionary<int, (SpriteStack deck, int spriteId)>();
            for (int i = 0; i < cardImages.Count; i++)
            {
                (int imgId, int index) = cardImages[i];
                if (index == 0)
                {
                    SpriteStack spriteStack = new SpriteStack(null, cardStackPos, imgId);
                    sprites.Add(spriteId, spriteStack);
                    decks.Add(imgId, (spriteStack, spriteId));
                    spriteId++;
                    cardStackPos += new Vector2(0, cardSize.Y + padding);
                }
                (SpriteStack deck, int stackSpriteId) = decks[imgId];
                FlippableSprite flippableSprite = new FlippableSprite(null, Vector2.Zero, false,
                        new ImageSprite(null, Vector2.Zero, imgId, cardSize, cardSize / 2, 0, new RectangleF(cardSizeImg.X*index, 0, cardSizeImg.X, cardSizeImg.Y)),
                        new ImageSprite(null, Vector2.Zero, imgId, cardSize, cardSize / 2, 0, backSourceRects[imgId]))
                { StackableIndex = imgId };

                sprites.Add(spriteId, flippableSprite);
                deck.AddToStack(stackSpriteId, flippableSprite, spriteId);
                spriteId++;
                //int spriteBackImage = cardImages[i][cardImages[i].Count - 1];
                //for(int j =0; j < cardImages[i].Count-1; j++)
                //{
                //    FlippableSprite flippableSprite = new FlippableSprite(null, Vector2.Zero, false,
                //        new ImageSprite(null, Vector2.Zero, cardImages[i][j], cardSize, cardSize / 2),
                //        new ImageSprite(null, Vector2.Zero, spriteBackImage, cardSize, cardSize / 2))
                //    { StackableIndex = i };

                //    sprites.Add(spriteId, flippableSprite);
                //    spriteStack.AddToStack(stackId, flippableSprite, spriteId);
                //    spriteId++;
                //}
            }
            maxPos.Y = cardStackPos.Y;

            int nobelStackIndex = decks.Count + 5;
            SpriteStack nobelStack = new SpriteStack(null, boardPadding, nobelStackIndex);
            int nobelStackId = spriteId;
            sprites.Add(nobelStackId, nobelStack);
            spriteId++;
            int nobelCount = 10;
            RectangleF nobelBackSource = new RectangleF(nobelSizeImg.X * nobelCount, 0, nobelSizeImg.X, nobelSizeImg.Y);
            for(int i = 0; i < nobelCount; i++)
            {
                FlippableSprite flippableSprite = new FlippableSprite(null, Vector2.Zero, false,
                        new ImageSprite(null, Vector2.Zero, nobels_Id, nobelSize, nobelSize / 2, 0, new RectangleF(nobelSizeImg.X * i, 0, nobelSizeImg.X, nobelSizeImg.Y)),
                        new ImageSprite(null, Vector2.Zero, nobels_Id, nobelSize, nobelSize / 2, 0, nobelBackSource))
                { StackableIndex = nobelStackIndex };

                sprites.Add(spriteId, flippableSprite);
                nobelStack.AddToStack(nobelStackId, flippableSprite, spriteId);
                spriteId++;
            }

            List<SpriteStack> coinStacks = new List<SpriteStack>();
            int cointStackIndex = 10;
            SpriteStack greenCoinStack = GetImageSpriteStack(cointStackIndex, 7, ref spriteId, sprites, cGreenImg, coinSize, coinSize / 2);
            coinStacks.Add(greenCoinStack);
            cointStackIndex++;
            SpriteStack whiteCoinStack = GetImageSpriteStack(cointStackIndex, 7, ref spriteId, sprites, cWhiteImg, coinSize, coinSize / 2);
            coinStacks.Add(whiteCoinStack);
            cointStackIndex++;
            SpriteStack blueCoinStack = GetImageSpriteStack(cointStackIndex, 7, ref spriteId, sprites, cBlueImg, coinSize, coinSize / 2);
            coinStacks.Add(blueCoinStack);
            cointStackIndex++;
            SpriteStack blackCoinStack = GetImageSpriteStack(cointStackIndex, 7, ref spriteId, sprites, cBlackImg, coinSize, coinSize / 2);
            coinStacks.Add(blackCoinStack);
            cointStackIndex++;
            SpriteStack redCoinStack = GetImageSpriteStack(cointStackIndex, 7, ref spriteId, sprites, cRedImg, coinSize, coinSize / 2);
            coinStacks.Add(redCoinStack);
            cointStackIndex++;
            SpriteStack goldCoinStack = GetImageSpriteStack(cointStackIndex, 5, ref spriteId, sprites, cGoldImg, coinSize, coinSize / 2);
            coinStacks.Add(goldCoinStack);
            cointStackIndex++;

            Vector2 coinPos = new Vector2(maxPos.X - boardPadding.X - 3 * (coinSize.X + 2 * padding) - padding, 
                maxPos.Y + padding); 
            for(int i =0; i < coinStacks.Count; i++)
            {
                coinStacks[i].Transform.Position = coinPos;
                coinPos += new Vector2(coinSize.X + 2 * padding, 0);
            }
            maxPos.Y = coinPos.Y + coinSize.Y;

            Dictionary<int, StackableDataInfo> stackableInfo = new Dictionary<int, StackableDataInfo>();
            float stackRadius = 45;
            for (int i = cointStackIndex - 1; i >= 10; i--)
            {
                StackableDataInfo coinInfo = new StackableDataInfo(coinSize/2 * new Vector2(1, -1) - new Vector2(stackRadius/2, 0), stackRadius);
                coinInfo.CountBackColor = new Color(50, 50, 50);
                coinInfo.CountTextColor = new Color(255, 255, 255);
                stackableInfo.Add(i, coinInfo);
            }
            StackableDataInfo nobelInfo = new StackableDataInfo(nobelSize / 2 * new Vector2(1, -1) - new Vector2(stackRadius / 2, 0), stackRadius, 90);
            nobelInfo.CountBackColor = new Color(50, 50, 50);
            nobelInfo.CountTextColor = new Color(255, 255, 255);
            stackableInfo.Add(nobelStackIndex, nobelInfo);

            foreach(var d in decks.Keys)
            {
                StackableDataInfo deckInfo = new StackableDataInfo(cardSize / 2 * new Vector2(1, -1) - new Vector2(stackRadius / 2, 0), stackRadius, 180, true);
                deckInfo.CountBackColor = new Color(50, 50, 50);
                deckInfo.CountTextColor = new Color(255, 255, 255);
                stackableInfo.Add(d, deckInfo);
            }

            return new JsonGame(1, 4, new Size((long)(maxPos.X + boardPadding.X), (long)(maxPos.Y + boardPadding.Y)), 
                new Color(78, 63, 171), sprites, images, stackableInfo);
        }
        static SpriteStack GetImageSpriteStack(int stackableId, int amount, ref int spriteId, Dictionary<int, Sprite> sprites,
            int imageRef, Vector2 size, Vector2 origin)
        {
            SpriteStack ss = new SpriteStack(null, Vector2.Zero, stackableId);
            int stackId = spriteId;
            sprites.Add(stackId, ss);
            spriteId++;

            for (int i = 0; i < amount; i++)
            {
                ImageSprite imgS = new ImageSprite(null, Vector2.Zero, imageRef, size, origin)
                { StackableIndex = stackableId };
                sprites.Add(spriteId, imgS);
                ss.AddToStack(stackId, imgS, spriteId);
                spriteId++;
            }
            return ss;
        }
        static string RepeatChar(char c, int amount)
        {
            string s = "";
            for(int i = 0; i < amount; i++)
            {
                s += c;
            }
            return s;
        }
    }
}
