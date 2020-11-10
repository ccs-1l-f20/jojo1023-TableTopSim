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
        Vector2 MousePos => Manager.MousePos;
        TimeSpan totalTime = new TimeSpan(0);
        int? selectedSpriteKey = null;
        Vector2 selectionOffset;
        object selectedLockObject = new object();
        //ElementReference cardBack, king, queen;
        MyClientWebSocket ws;
        SpriteRefrenceManager refManager => Manager.SpriteRefrenceManager;
        EmptySprite gameSprite => Manager.GameSprite;
        EmptySprite spriteContainer = new EmptySprite();
        EmptySprite selectedSpritesContainer = new EmptySprite() { LayerDepth = -100 };

        int roomId;
        GameDataUpdate completeUpdateData = null;
        object gameStateLockObject = new object();
        Queue<GameDataUpdate> partialDataUpdates = new Queue<GameDataUpdate>();
        public GameManager Manager { get; set; }

        internal GameProgram(Size size, MyClientWebSocket ws, int roomId)
        //ElementReference cardBack, ElementReference king, ElementReference queen)
        {
            Manager = new GameManager(size, new SpriteRefrenceManager(new Dictionary<int, Sprite>() { { 0, spriteContainer }, { 1, selectedSpritesContainer } }));
            gameSprite.AddChild(spriteContainer, refManager);
            gameSprite.AddChild(selectedSpritesContainer, refManager);

            this.ws = ws;
            this.roomId = roomId;
            //string test = manager.JsonSerializeSprites();



            //manager.DataLayer.AddData(dataObjects, dataRelationships, false);

            Manager.OnUpdate += Update;
            Manager.OnMouseDown += MouseDown;
            Manager.OnMouseUp += MouseUp;

            Manager.OnKeyDown += OnKeyDown;
            Manager.OnKeyUp += OnKeyUp;


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

                List<byte> specificSerializedData = GameSerialize.SerializeGameData(refManager.SpriteRefrences, changedProperties);
                sendBytes.AddRange(BitConverter.GetBytes(specificSerializedData.Count));
                sendBytes.AddRange(specificSerializedData);
                List<byte> serializedGameSprites;
                lock (selectedLockObject)
                {
                    serializedGameSprites = GameSerialize.SerializeGameData(new List<List<int>>() { spriteContainer.Children, selectedSpritesContainer.Children });
                }
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
                if (changedProperties.Count == 0)
                {
                    changedProperties.Add(refManager.SpriteRefrences, new HashSet<int>());
                }
                var dictHash = changedProperties[refManager.SpriteRefrences];
                int spriteAddress = refManager.GetAddress(sprite);
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
            lock (selectedLockObject)
            {
                if (selectedSpriteKey != null)
                {
                    Sprite selectedSprite = refManager.GetSprite(selectedSpriteKey.Value);
                    selectedSprite.Scale /= 1.15f;

                    selectedSpritesContainer.RemoveChild(selectedSpriteKey.Value, refManager);
                    spriteContainer.AddChild(selectedSpriteKey.Value, refManager);
                    spriteContainer.MoveChildToFront(selectedSprite, refManager);
                    selectedSpriteKey = null;
                }
                else if (Manager.MouseOnSprite != null)
                {
                    Sprite s = Manager.MouseOnSprite;
                    selectedSpriteKey = refManager.GetAddress(s);
                    selectionOffset = MousePos - s.Position;

                    spriteContainer.RemoveChild(selectedSpriteKey.Value, refManager);
                    selectedSpritesContainer.AddChild(selectedSpriteKey.Value, refManager);
                    selectedSpritesContainer.MoveChildToFront(s, refManager);
                    s.Scale *= 1.15f;
                    //Manager.MoveChildToFront(s);
                    //s.Scale *= 1.1f;
                }
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
                refManager.Reset();
                foreach (var key in spritesData.Keys)
                {
                    Sprite sprite = spritesData[key];
                    //spriteRefManager.SpriteAddresses.Add(sprite, key);
                    refManager.SpriteRefrences.Add(key, sprite);
                }
            }
            while (pDataUpdates.Count > 0)
            {
                GameDataUpdate pUpdate = pDataUpdates.Dequeue();

                refManager.SpriteRefrences = GameSerialize.DeserializeEditGameData(refManager.SpriteRefrences, pUpdate.Data, null);

                lastUpdate = pUpdate;
            }
            refManager.UpdateSpriteAddresses();

            if (lastUpdate != null)
            {
                List<List<int>> gameSpriteSprites = GameSerialize.DeserializeGameData<List<List<int>>>(lastUpdate.GameSpritesData);
                List<int> spriteContainerSprites = gameSpriteSprites[0];
                List<int> selectedSpriteSprites = gameSpriteSprites[1];
                lock (selectedLockObject)
                {
                    spriteContainer.ClearChildren(refManager);
                    selectedSpritesContainer.ClearChildren(refManager);
                    foreach (var s in spriteContainerSprites)
                    {
                        spriteContainer.AddChild(s, refManager);
                    }
                    foreach (var s in selectedSpriteSprites)
                    {
                        selectedSpritesContainer.AddChild(s, refManager);
                    }
                }
            }

            foreach (var s in refManager.SpriteRefrences.Values)
            {
                s.OnPropertyChanged -= OnPropertyChanged;
                s.OnPropertyChanged += OnPropertyChanged;
            }
            ignorePropertyChanged = false;

            lock (selectedLockObject)
            {
                if (selectedSpriteKey != null)
                {
                    if (refManager.ContainsAddress(selectedSpriteKey.Value))
                    {
                        Sprite selectedSprite = refManager.GetSprite(selectedSpriteKey.Value);
                        selectedSprite.Position = Manager.MousePos - selectionOffset;
                    }
                    else
                    {
                        selectedSpriteKey = null;
                    }
                }
            }




            SendChangedWs();
        }

    }
}
