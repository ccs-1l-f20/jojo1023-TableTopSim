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
    public class GameDataUpdate
    {
        public ArrayWithOffset<byte> Data { get; set; }
        public ArrayWithOffset<byte> GameSpritesData { get; set; }
        public GameDataUpdate(ArrayWithOffset<byte> data, ArrayWithOffset<byte> gameSpritesData)
        {
            Data = data;
            GameSpritesData = gameSpritesData;
        }
    }
    public class GameProgram
    {
        Vector2 MousePos => manager.MousePos;
        GameManager manager;
        TimeSpan totalTime = new TimeSpan(0);
        int? selectedSpriteKey = null;
        Vector2 selectionOffset;
        //ElementReference cardBack, king, queen;
        MyClientWebSocket ws;
        SpriteRefrenceManager spriteRefManager => manager.SpriteRefrenceManager;

        int roomId;
        GameDataUpdate completeUpdateData = null;
        object gameStateLockObject = new object();
        Queue<GameDataUpdate> partialDataUpdates = new Queue<GameDataUpdate>();
        internal GameProgram(GameManager manager, MyClientWebSocket ws, int roomId)
        //ElementReference cardBack, ElementReference king, ElementReference queen)
        {
            this.manager = manager;
            this.ws = ws;
            this.roomId = roomId;
            //string test = manager.JsonSerializeSprites();



            //manager.DataLayer.AddData(dataObjects, dataRelationships, false);

            manager.OnUpdate += Update;
            manager.OnMouseDown += MouseDown;
            manager.OnMouseUp += MouseUp;

            manager.OnKeyDown += OnKeyDown;
            manager.OnKeyUp += OnKeyUp;


            ws.OnRecieved += OnRecivedWSMessage;
        }
        public void Dispose()
        {
            ws.OnRecieved -= OnRecivedWSMessage;
        }
        void OnRecivedWSMessage(ArraySegment<byte> origMessage)
        {
            ArrayWithOffset<byte> message = new ArrayWithOffset<byte>(origMessage.ToArray());

            MessageType mt = (MessageType)message[0];
            message = message.Slice(1);
            if (mt == MessageType.GameState || mt == MessageType.ChangeGameState)
            {
                int roomId = BitConverter.ToInt32(message.Array, message.Offset);
                message.Offset += 4;
                if (this.roomId != roomId)
                {
                    throw new NotImplementedException();
                }
                int dataLength = BitConverter.ToInt32(message.Array, message.Offset);
                message.Offset += 4;
                ArrayWithOffset<byte> serializedData = message.Slice(0, dataLength);
                message.Offset += dataLength;
                int spritesSpritesLength = BitConverter.ToInt32(message.Array, message.Offset);
                message.Offset += 4;
                ArrayWithOffset<byte> serializeGameSprites = message.Slice(0, spritesSpritesLength);
                if (mt == MessageType.GameState)
                {
                    lock (gameStateLockObject)
                    {
                        completeUpdateData = new GameDataUpdate(serializedData, serializeGameSprites);
                        partialDataUpdates.Clear();
                    }
                }
                else
                {
                    lock (gameStateLockObject)
                    {
                        partialDataUpdates.Enqueue(new GameDataUpdate(serializedData, serializeGameSprites));
                    }
                }
            }
        }

        void SendChangedWs()
        {
            if (changedProperties.Count > 0)
            {
                List<byte> sendBytes = new List<byte>();
                sendBytes.Add((byte)MessageType.ChangeGameState);
                sendBytes.AddRange(BitConverter.GetBytes((long)0));
                sendBytes.AddRange(BitConverter.GetBytes(roomId));

                List<byte> specificSerializedData = GameSerialize.SerializeGameData(spriteRefManager.SpriteRefrences, changedProperties);
                sendBytes.AddRange(BitConverter.GetBytes(specificSerializedData.Count));
                sendBytes.AddRange(specificSerializedData);
                List<byte> serializedGameSprites = GameSerialize.SerializeGameData(manager.GameSprite.Children);
                sendBytes.AddRange(BitConverter.GetBytes(serializedGameSprites.Count));
                sendBytes.AddRange(serializedGameSprites);

                _ = ws.SendMessageAsync(new ArraySegment<byte>(sendBytes.ToArray()));
            }
            changedProperties.Clear();
        }

        bool ignorePropertyChanged = false;
        Dictionary<object, HashSet<int>> changedProperties = new Dictionary<object, HashSet<int>>();

        void OnPropertyChanged(Sprite sprite, ushort propertyId)
        {
            if (!ignorePropertyChanged)
            {
                if(changedProperties.Count == 0)
                {
                    changedProperties.Add(spriteRefManager.SpriteRefrences, new HashSet<int>());
                }
                var dictHash = changedProperties[spriteRefManager.SpriteRefrences];
                int spriteAddress = spriteRefManager.SpriteAddresses[sprite];
                if (!dictHash.Contains(spriteAddress))
                {
                    dictHash.Add(spriteAddress);
                }
                if (!changedProperties.ContainsKey(sprite))
                {
                    changedProperties.Add(sprite, new HashSet<int>());
                }
                var spriteHash = changedProperties[sprite];
                if (!spriteHash.Contains(propertyId))
                {
                    spriteHash.Add(propertyId);
                }
            }
        }

        private void OnKeyUp(KeyInfo keyInfo)
        {
            //if (!keyInfo.LastRepeat && keyInfo.Code == "KeyR")
            //{
            //    float currentRot = Extensions.GetPositiveRotation(imageSprite.Rotation);
            //    float mod90 = currentRot % 90;
            //    if (manager.Keyboard.ShiftKey)
            //    {
            //        imageSprite.Rotation -= mod90;
            //        imageSprite.Rotation -= mod90 == 0 ? 90 : 0;
            //    }
            //    else
            //    {
            //        imageSprite.Rotation += 90 - mod90;
            //    }
            //}
        }

        private void OnKeyDown(KeyInfo keyInfo)
        {
            //if (keyInfo.Repeat && keyInfo.Code == "KeyR")
            //{
            //    Vector2 relativePoint = manager.MousePos - imageSprite.Position;
            //    imageSprite.Rotation = Extensions.RadiansToDegrees(-(float)Math.Atan2(relativePoint.X, relativePoint.Y));
            //}
        }

        private void MouseDown()
        {
            if (selectedSpriteKey != null)
            {
                spriteRefManager.SpriteRefrences[selectedSpriteKey.Value].Scale /= 1.1f;
                selectedSpriteKey = null;
            }
            else if (manager.MouseOnSprite != null)
            {
                Sprite s = manager.MouseOnSprite;
                selectedSpriteKey = spriteRefManager.SpriteAddresses[s];
                selectionOffset = MousePos - s.Position;
                manager.MoveChildToFront(s);
                s.Scale *= 1.1f;
            }
        }
        private void MouseUp()
        {
            //selectedSprite = null;
        }
        private void Update(TimeSpan elapsedTime)
        {
            totalTime += elapsedTime;

            GameDataUpdate completeUpdate = null;
            Queue<GameDataUpdate> pDataUpdates = new Queue<GameDataUpdate>();
            lock (gameStateLockObject)
            {
                if (completeUpdateData != null)
                {
                    completeUpdate = completeUpdateData;
                    completeUpdateData = null;
                }
                while (partialDataUpdates.Count > 0)
                {
                    pDataUpdates.Enqueue(partialDataUpdates.Dequeue());
                }
            }
            ignorePropertyChanged = true;
            GameDataUpdate lastUpdate = completeUpdate;
            if (completeUpdate != null)
            {
                var spritesData = GameSerialize.DeserializeGameData<Dictionary<int, Sprite>>(completeUpdate.Data);
                spriteRefManager.Reset();
                foreach (var key in spritesData.Keys)
                {
                    Sprite sprite = spritesData[key];
                    //spriteRefManager.SpriteAddresses.Add(sprite, key);
                    spriteRefManager.SpriteRefrences.Add(key, sprite);
                }
            }
            while (pDataUpdates.Count > 0)
            {
                GameDataUpdate pUpdate = pDataUpdates.Dequeue();

                spriteRefManager.SpriteRefrences = GameSerialize.DeserializeEditGameData(spriteRefManager.SpriteRefrences, pUpdate.Data, null);

                lastUpdate = pUpdate;
            }
            spriteRefManager.UpdateSpriteAddresses();

            if (lastUpdate != null)
            {
                List<int> gameSpriteSprites = GameSerialize.DeserializeGameData<List<int>>(lastUpdate.GameSpritesData);
                manager.ClearSprites();
                foreach (var s in gameSpriteSprites)
                {
                    manager.AddSprite(s);
                }
            }

            foreach (var s in spriteRefManager.SpriteRefrences.Values)
            {
                s.OnPropertyChanged -= OnPropertyChanged;
                s.OnPropertyChanged += OnPropertyChanged;
            }
            ignorePropertyChanged = false;
            if (selectedSpriteKey != null)
            {
                if (spriteRefManager.SpriteRefrences.ContainsKey(selectedSpriteKey.Value))
                {
                    spriteRefManager.SpriteRefrences[selectedSpriteKey.Value].Position = manager.MousePos - selectionOffset;
                }
                else
                {

                    selectedSpriteKey = null;
                }
            }




            SendChangedWs();
        }

    }
}
