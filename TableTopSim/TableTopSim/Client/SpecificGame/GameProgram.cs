﻿using DataLayer;
using GameLib;
using GameLib.GameSerialization;
using GameLib.Sprites;
using Microsoft.AspNetCore.Components;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;

namespace TableTopSim.Client.SpecificGame
{
    public class GameProgram
    {
        Vector2 MousePos => Manager.MousePos;
        TimeSpan totalTime = new TimeSpan(0);
        //int? selectedSpriteKey = null;
        bool spriteSelectedChanged = false;
        int? queuedSelectedSprite = null;
        //ElementReference cardBack, king, queen;
        MyClientWebSocket ws;
        SpriteRefrenceManager refManager => Manager.SpriteRefrenceManager;

        int roomId, playerId;
        GameDataUpdate completeUpdateData = null;
        object gameStateLockObject = new object();
        Queue<GameDataUpdate> partialDataUpdates = new Queue<GameDataUpdate>();
        public GameManager Manager { get; set; }
        Dictionary<int, CursorInfo> cursorSprites = null;
        Sprite thisCursor = null;
        CursorInfo thisCursorInfo = null;
        Size size;
        internal GameProgram(Size size, MyClientWebSocket ws, int roomId, int playerId, 
            Dictionary<int, ElementReference> imageElementRefs, ElementReference imageNotFound)
        //ElementReference cardBack, ElementReference king, ElementReference queen)
        {
            CursorInfo.Init();
            this.size = size;
            //Debug.WriteLine($"Pre Manager {playerId}");
            Manager = new GameManager(size, new SpriteRefrenceManager(imageElementRefs, imageNotFound));
            //Debug.WriteLine($"Post Manager {playerId}");

            this.ws = ws;
            this.roomId = roomId;
            this.playerId = playerId;
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
                if (mt == MessageType.GameState)
                {
                    dataLength = BitConverter.ToInt32(message.Array, message.Offset);
                    message.Offset += 4;
                    ArrayWithOffset<byte> cursorSprites = message.Slice(0, dataLength);

                    message.Offset += dataLength;
                    lock (gameStateLockObject)
                    {
                        completeUpdateData = new GameDataUpdate(serializedData, cursorSprites);
                        partialDataUpdates.Clear();
                    }
                }
                else
                {
                    int? selectedSprite = null;
                    if(message[0] != 0)
                    {
                        selectedSprite = BitConverter.ToInt32(message.Array, message.Offset + 1);
                    }
                    message.Offset += 5;
                    int sendingPlayer = BitConverter.ToInt32(message.Array, message.Offset);
                    message.Offset += 4;
                    lock (gameStateLockObject)
                    {
                        partialDataUpdates.Enqueue(new GameDataUpdate(serializedData, selectedSprite, sendingPlayer));
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

                List<byte> specificSerializedData = GameSerialize.SpecificSerializeGameData(refManager.SpriteRefrences, changedProperties);
                sendBytes.AddRange(BitConverter.GetBytes(specificSerializedData.Count));
                sendBytes.AddRange(specificSerializedData);
                int? selectedSprite = null;
                if(thisCursorInfo != null)
                {
                    selectedSprite = thisCursorInfo.SelectedSpriteId;
                }
                GameSerialize.SerializeNullableInt(selectedSprite, sendBytes);

                _ = ws.SendMessageAsync(new ArraySegment<byte>(sendBytes.ToArray()));
            }
            changedProperties.Clear();
        }

        bool ignorePropertyChanged = false;
        PathTrie<object> changedProperties = new PathTrie<object>();

        void OnPropertyChanged(Sprite sprite, List<int> propertyPath)
        {
            if (!ignorePropertyChanged)
            {
                int add = refManager.GetAddress(sprite);
                propertyPath.Insert(0, add);
                changedProperties.Insert(propertyPath, null, true);
            }
        }

        private void OnKeyUp(KeyInfo keyInfo) { }

        private void OnKeyDown(KeyInfo keyInfo) { }
        private void MouseDown()
        {
            if (thisCursorInfo != null)
            {
                if (thisCursorInfo.SelectedSpriteId == null)
                {
                    Sprite s = Manager.MouseOnSprite;
                    if (s != null && s.Selectable)
                    {
                        int add = refManager.GetAddress(s);
                        if (thisCursorInfo.SelectedSpriteId != add)
                        {
                            queuedSelectedSprite = refManager.GetAddress(s);
                            spriteSelectedChanged = true;
                        }
                    }
                }
                else
                {
                    queuedSelectedSprite = null;
                    spriteSelectedChanged = true;
                }
            }
        }
        private void MouseUp() { }
        bool CanSelectSprite(int sprite)
        {
            foreach(var k in cursorSprites.Keys)
            {
                if(k == playerId) { continue; }
                var v = cursorSprites[k];
                if(v.SelectedSpriteId == sprite)
                {
                    return false;
                }
            }
            return true;
        }
        private void Update(TimeSpan elapsedTime)
        {
            //Debug.WriteLine($"Update {playerId}");
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
                cursorSprites = GameSerialize.DeserializeGameData<Dictionary<int, CursorInfo>>(completeUpdate.CursorSprites);
            }
            while (pDataUpdates.Count > 0)
            {
                GameDataUpdate pUpdate = pDataUpdates.Dequeue();
                if (cursorSprites.ContainsKey(pUpdate.SendingPlayer))
                {
                    cursorSprites[pUpdate.SendingPlayer].SelectedSpriteId = pUpdate.SelectedSprite;
                }
                refManager.SpriteRefrences = GameSerialize.DeserializeEditGameData(refManager.SpriteRefrences, pUpdate.Data);

                lastUpdate = pUpdate;
            }
            refManager.UpdateSpriteAddresses();

            if (lastUpdate != null)
            {
                Manager.ClearSprites();
                foreach (var s in refManager.SpriteRefrences.Keys)
                {
                    Sprite sprite = refManager.GetSprite(s);
                    Manager.Sprites.Add(s);
                    sprite.SetRefManager(refManager);
                    sprite.OnPropertyChanged -= OnPropertyChanged;
                    sprite.OnPropertyChanged += OnPropertyChanged;
                }
                //List<List<int>> gameSpriteSprites = GameSerialize.DeserializeGameData<List<List<int>>>(lastUpdate.GameSpritesData);
                //List<int> spriteContainerSprites = gameSpriteSprites[0];
                //List<int> selectedSpriteSprites = gameSpriteSprites[1];
                //lock (selectedLockObject)
                //{
                //    spriteContainer.ClearChildren(refManager);
                //    selectedSpritesContainer.ClearChildren(refManager);
                //    foreach (var s in spriteContainerSprites)
                //    {
                //        spriteContainer.AddChild(s, refManager);
                //    }
                //    foreach (var s in selectedSpriteSprites)
                //    {
                //        selectedSpritesContainer.AddChild(s, refManager);
                //    }
                //}
            }
            if(cursorSprites != null && cursorSprites.ContainsKey(playerId))
            {
                var cursorInfo = cursorSprites[playerId];
                int? prevSelected = null;
                if(thisCursorInfo != null)
                {
                    prevSelected = thisCursorInfo.SelectedSpriteId;
                }
                thisCursor = refManager.GetSprite(cursorInfo.CursorSpriteId);
                thisCursorInfo = cursorInfo;
                if (thisCursorInfo.SelectedSpriteId != null && !refManager.ContainsAddress(thisCursorInfo.SelectedSpriteId.Value))
                {
                    thisCursorInfo.SelectedSpriteId = null;
                }
                else if(prevSelected != null && refManager.ContainsAddress(prevSelected.Value) && thisCursorInfo.SelectedSpriteId != prevSelected.Value)
                {
                    DropSelected(prevSelected.Value);
                }
            }
            else
            {
                ignorePropertyChanged = false;
                if (thisCursorInfo != null && thisCursorInfo.SelectedSpriteId != null && refManager.ContainsAddress(thisCursorInfo.SelectedSpriteId.Value))
                {
                    DropSelected(thisCursorInfo.SelectedSpriteId.Value);
                }
                thisCursorInfo = null;
                thisCursor = null;
            }
            
            ignorePropertyChanged = false;

            if (thisCursor != null)
            {
                Vector2 cursorPos = Manager.MousePos;
                if (cursorPos != thisCursor.Transform.Position &&
                    cursorPos.X >= 0 && cursorPos.Y >= 0 && cursorPos.X < size.Width && cursorPos.Y < size.Height) 
                {
                    thisCursor.Transform.Position = cursorPos;
                }
                //System.Diagnostics.Debug.WriteLine($"Cursor Id: {thisCursorInfo.CursorSpriteId}");
                //string s = thisCursorInfo.SelectedSpriteId == null ? "null" : thisCursorInfo.SelectedSpriteId.ToString();
                //System.Diagnostics.Debug.WriteLine($"Cursor Child: {s}");
                //if(thisCursorInfo.SelectedSpriteId != null)
                //{
                //    string sP = refManager.GetSprite(thisCursorInfo.SelectedSpriteId.Value).Parent == null ? "null" : thisCursorInfo.SelectedSpriteId.ToString();
                //    System.Diagnostics.Debug.WriteLine($"Cursor Child Parent: {sP}");
                //}

                if (spriteSelectedChanged)
                {
                    int? queuedChange = queuedSelectedSprite;
                    if (queuedChange != null && !refManager.ContainsAddress(queuedChange.Value))
                    {
                        queuedChange = null;
                    }
                    if(queuedChange != null && !CanSelectSprite(queuedChange.Value))
                    {
                        queuedChange = null;
                    }
                    if (thisCursorInfo.SelectedSpriteId != null && queuedChange != thisCursorInfo.SelectedSpriteId.Value)
                    {
                        DropSelected(thisCursorInfo.SelectedSpriteId.Value);
                    }

                    if(queuedChange != null && queuedChange.Value != thisCursorInfo.SelectedSpriteId)
                    {
                        Sprite newSelected = refManager.GetSprite(queuedChange.Value);
                        Vector2 glbPosition = newSelected.Transform.GetGlobalPosition();
                        float glbRotation = newSelected.Transform.GetGlobalRotation();
                        newSelected.Parent = null;
                        newSelected.Transform.Position = glbPosition - thisCursor.Transform.Position;
                        newSelected.Transform.Rotation = glbRotation;
                        newSelected.Parent = thisCursorInfo.CursorSpriteId;
                        thisCursorInfo.SelectedSpriteId = queuedChange.Value;
                        newSelected.LayerDepth.Layers.Insert(0, 1);
                    }
                    thisCursorInfo.SelectedSpriteId = queuedChange;
                    spriteSelectedChanged = false;
                    queuedSelectedSprite = null;
                }
            }
            //lock (selectedLockObject)
            //{
            //    if (selectedSpriteKey != null)
            //    {
            //        if (refManager.ContainsAddress(selectedSpriteKey.Value))
            //        {
            //            Sprite selectedSprite = refManager.GetSprite(selectedSpriteKey.Value);
            //            selectedSprite.Position = Manager.MousePos - selectionOffset;
            //        }
            //        else
            //        {
            //            selectedSpriteKey = null;
            //        }
            //    }
            //}


            if (thisCursor != null)
            {
                ignorePropertyChanged = true;
                thisCursor.Visiable = true;
            }

            SendChangedWs(); 

            if (thisCursor != null)
            {
                thisCursor.Visiable = false;
                ignorePropertyChanged = false;
            }
        }
        void DropSelected(int prevSelectedAdd)
        {
            Sprite prevSelected = refManager.GetSprite(prevSelectedAdd);
            if (prevSelected.Parent == thisCursorInfo.CursorSpriteId)
            {
                prevSelected.Transform.Position += thisCursor.Transform.Position;
                prevSelected.LayerDepth.Layers.RemoveAt(0);
                prevSelected.Parent = null;
            }
        }
    }

    public class GameDataUpdate
    {
        public ArrayWithOffset<byte> Data { get; set; }
        public ArrayWithOffset<byte> CursorSprites { get; set; }
        public int? SelectedSprite { get; set; }
        public int SendingPlayer { get; set; }
        public GameDataUpdate(ArrayWithOffset<byte> data, ArrayWithOffset<byte> cursorSprites)
        {
            Data = data;
            CursorSprites = cursorSprites;
            SelectedSprite = null;
        }
        public GameDataUpdate(ArrayWithOffset<byte> data, int? selectedSprite, int sendingPlayer)
        {
            Data = data;
            CursorSprites = null;
            SelectedSprite = selectedSprite;
            SendingPlayer = sendingPlayer;
        }
    }
    public class CursorInfo
    {
        [GameSerializableData(0)]
        public int CursorSpriteId { get; set; }
        [GameSerializableData(1)]
        public int? SelectedSpriteId { get; set; }
        static bool hasInit = false;
        public static void Init()
        {
            if (!hasInit)
            {
                hasInit = true;
                GameSerialize.AddType<CursorInfo>(GameSerialize.GenericSerializeFunc, GameSerialize.GenericDeserializeFunc);
            }
        }
        static CursorInfo() { Init(); }
        public CursorInfo() { CursorSpriteId = -1; SelectedSpriteId = null; }
        public CursorInfo(int cursorSpriteId, int? selectedSpriteId)
        {
            if (!hasInit) { Init(); }
            CursorSpriteId = cursorSpriteId;
            SelectedSpriteId = selectedSpriteId;
        }
    }
}
