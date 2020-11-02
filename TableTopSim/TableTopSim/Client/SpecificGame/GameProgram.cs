using DataLayer;
using GameLib;
using GameLib.GameSerialization;
using GameLib.Sprites;
using Microsoft.AspNetCore.Components;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;

namespace TableTopSim.Client.SpecificGame
{
    public class GameProgram
    {
        RectSprite rectSprite;
        RectSprite childSprite;
        Vector2 MousePos => manager.MousePos;
        GameManager manager;
        ImageSprite imageSprite;
        TimeSpan totalTime = new TimeSpan(0);
        Sprite selectedSprite = null;
        Vector2 selectionOffset;
        ElementReference cardBack, king, queen;
        MyClientWebSocket ws;
        SpriteRefrenceManager spriteRefManager => manager.SpriteRefrenceManager;

        int roomId;
        internal GameProgram(GameManager manager, MyClientWebSocket ws, int roomId,
            ElementReference cardBack, ElementReference king, ElementReference queen)
        {
            this.manager = manager;
            this.cardBack = cardBack;
            this.king = king;
            this.queen = queen;
            this.ws = ws;
            this.roomId = roomId;
            rectSprite = new RectSprite(new Vector2(200, 200), new Vector2(100, 200), new Color(0, 0, 255), new Vector2(50, 100), 0);

            AddSprite(rectSprite);
            AddSprite(childSprite = new RectSprite(new Vector2(200, 200), new Vector2(10, 10), new Color(255, 0, 255), new Vector2(0, 0), 45));
            AddSprite(imageSprite = new ImageSprite(new Vector2(300, 300), cardBack, new Vector2(170, 235), new Vector2(170, 235) / 2));
            AddSprite(imageSprite = new ImageSprite(new Vector2(100, 50), king, new Vector2(100, 100), new Vector2(100, 100) / 2));
            AddSprite(imageSprite = new ImageSprite(new Vector2(250, 50), queen, new Vector2(100, 100), new Vector2(100, 100) / 2));
            //string test = manager.JsonSerializeSprites();
            


            //manager.DataLayer.AddData(dataObjects, dataRelationships, false);

            manager.OnUpdate += Update;
            manager.OnMouseDown += MouseDown;
            manager.OnMouseUp += MouseUp;

            manager.OnKeyDown += OnKeyDown;
            manager.OnKeyUp += OnKeyUp;


            ws.OnRecieved += OnRecivedWSMessage;
        }

        void OnRecivedWSMessage(ArraySegment<byte> message)
        {
            MessageType mt = (MessageType)message[0];
            if (mt == MessageType.GameState || mt == MessageType.ChangeGameState)
            {
                int roomId = MessageExtensions.GetNextInt(ref message);
                if (this.roomId != roomId)
                {
                    throw new NotImplementedException();
                }
                int dataLength = MessageExtensions.GetNextInt(ref message);
                byte[] serializedData = message.Array.Skip(message.Offset).Take(dataLength).ToArray();
                if (mt == MessageType.GameState)
                {
                    Dictionary<int, Sprite> newSprites = GameSerialize.DeserializeGameData<Dictionary<int, Sprite>>(serializedData);
                    spriteRefManager.Reset();
                    foreach (var key in newSprites.Keys)
                    {
                        Sprite sprite = newSprites[key];
                        spriteRefManager.SpriteAddresses.Add(sprite, key);
                        spriteRefManager.SpriteRefrences.Add(key, sprite);
                        sprite.OnPropertyChanged += OnPropertyChanged;
                    }
                }
                else
                {
                    spriteRefManager.SpriteRefrences = GameSerialize.DeserializeEditGameData(spriteRefManager.SpriteRefrences, serializedData);
                    foreach(var key in spriteRefManager.SpriteRefrences.Keys)
                    {
                        Sprite sprite = spriteRefManager.SpriteRefrences[key];
                        sprite.OnPropertyChanged -= OnPropertyChanged;
                        sprite.OnPropertyChanged += OnPropertyChanged;
                    }
                    spriteRefManager.UpdateSpriteAddresses();
                }
            }
        }


        void AddSprite(Sprite sprite)
        {
            throw new NotImplementedException();
            manager.AddSprite(sprite);
            //sprites.Add(sprite);
            sprite.OnPropertyChanged += OnPropertyChanged;
        }

        void OnPropertyChanged(Sprite sprite, ushort propertyId)
        {
        }

        private void OnKeyUp(KeyInfo keyInfo)
        {
            if (!keyInfo.LastRepeat && keyInfo.Code == "KeyR")
            {
                float currentRot = Extensions.GetPositiveRotation(imageSprite.Rotation);
                float mod90 = currentRot % 90;
                if (manager.Keyboard.ShiftKey)
                {
                    imageSprite.Rotation -= mod90;
                    imageSprite.Rotation -= mod90 == 0 ? 90 : 0;
                }
                else
                {
                    imageSprite.Rotation += 90 - mod90;
                }
            }
        }

        private void OnKeyDown(KeyInfo keyInfo)
        {
            if (keyInfo.Repeat && keyInfo.Code == "KeyR")
            {
                Vector2 relativePoint = manager.MousePos - imageSprite.Position;
                imageSprite.Rotation = Extensions.RadiansToDegrees(-(float)Math.Atan2(relativePoint.X, relativePoint.Y));
            }
        }

        private void MouseDown()
        {
            if (selectedSprite != null)
            {
                selectedSprite.Scale /= 1.1f;
                selectedSprite = null;
            }
            else if (manager.MouseOnSprite != null)
            {
                selectedSprite = manager.MouseOnSprite;
                selectionOffset = MousePos - selectedSprite.Position;
                manager.MoveChildToFront(selectedSprite);
                selectedSprite.Scale *= 1.1f;
            }
        }
        private void MouseUp()
        {
            //selectedSprite = null;
        }
        private void Update(TimeSpan elapsedTime)
        {
            totalTime += elapsedTime;

            if (selectedSprite != null)
            {
                selectedSprite.Position = manager.MousePos - selectionOffset;
            }
            //rectSprite.Scale += new Vector2(0.001f, 0.001f);
            //rectSprite.Rotation += 0.1f;
            //rectSprite.Position += new Vector2(0.001f, 0.001f);
            //childSprite.Position -= new Vector2(0.2f, 0.2f);
            //childSprite.Rotation += 1;
            //childSprite.Scale += new Vector2(0.001f, 0.001f);


            //if (selected)
            //{
            //    rectSprite.Position = MousePos;
            //    rectSprite.Rotation += 1;
            //    manager.DataLayer.SetPos(rectSprite.X, rectSprite.Y, rectSprite.Rotation);
            //}
            //else
            //{
            //    var getPos = manager.DataLayer.GetPos();
            //    if(getPos != null)
            //    {
            //        rectSprite.Position = new Vector2(getPos.Value.x, getPos.Value.y);
            //        rectSprite.Rotation = getPos.Value.rot;
            //    }
            //}
        }

    }
}
