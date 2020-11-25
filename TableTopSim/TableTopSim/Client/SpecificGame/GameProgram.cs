using DataLayer;
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
        GameDataUpdate updateData = null;
        object gameStateLockObject = new object();
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
            Manager = new GameManager(size, new SpriteRefrenceManager(imageElementRefs, imageNotFound), playerId);
            //Debug.WriteLine($"Post Manager {playerId}");

            this.ws = ws;
            this.roomId = roomId;
            this.playerId = playerId;
            //string test = manager.JsonSerializeSprites();



            //manager.DataLayer.AddData(dataObjects, dataRelationships, false);

            Manager.OnUpdate += Update;
            //Manager.OnMouseDown += MouseDown;
            //Manager.OnMouseUp += MouseUp;
            Manager.OnKeyDown += OnKeyDown;
            Manager.OnKeyUp += OnKeyUp;


            ws.OnRecieved += OnRecivedWSMessage;
            SendChangedWs(null);
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

                bool discardChanges = message[0] != 0;
                message.Offset++;

                int dataLength = BitConverter.ToInt32(message.Array, message.Offset);
                message.Offset += 4;
                ArrayWithOffset<byte> serializedData = message.Slice(0, dataLength);
                message.Offset += dataLength;
                dataLength = BitConverter.ToInt32(message.Array, message.Offset);
                message.Offset += 4;
                ArrayWithOffset<byte> cursorSprites = message.Slice(0, dataLength);
                message.Offset += dataLength;

                lock (gameStateLockObject)
                {
                    updateData = new GameDataUpdate(serializedData, cursorSprites, mt == MessageType.ChangeGameState, discardChanges);
                }
            }
        }

        void SendChangedWs(List<byte> specificSerializedData)
        {
            List<byte> sendBytes = new List<byte>();
            sendBytes.Add((byte)MessageType.ChangeGameState);
            sendBytes.AddRange(BitConverter.GetBytes((long)0));
            sendBytes.AddRange(BitConverter.GetBytes(roomId));
            if (specificSerializedData == null)
            {
                sendBytes.AddRange(BitConverter.GetBytes((int)-1));
            }
            else
            {
                sendBytes.AddRange(BitConverter.GetBytes(specificSerializedData.Count));
                sendBytes.AddRange(specificSerializedData);
                int? selectedSprite = null;
                if (thisCursorInfo != null)
                {
                    selectedSprite = thisCursorInfo.SelectedSpriteId;
                }
                GameSerialize.SerializeNullableInt(selectedSprite, sendBytes);
            }

            _ = ws.SendMessageAsync(new ArraySegment<byte>(sendBytes.ToArray()));
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

        bool rotateMode = false;
        float rotateStart = 0;
        void KeyUpdate(KeyboardState keyboard)
        {
            bool rotate90 = false;
            bool mouseRotate = false;
            bool lastRotateMode = rotateMode;
            rotateMode = false;
            if (keyboard.ContainsKeyCode("KeyR"))
            {
                KeyInfo rInfo = keyboard["KeyR"];
                if(rInfo.Repeat && rInfo.Down)
                {
                    mouseRotate = true;
                }
                else if(!rInfo.Down && rInfo.LastDown && !rInfo.LastRepeat)
                {
                    rotate90 = true;
                }
            }
            if (rotate90 || mouseRotate)
            {
                bool hasRot = false;
                float currentRot = 0;
                Vector2 currentPos = Vector2.Zero;
                Sprite selectedSprite = null;
                Vector2 mousePos = Manager.MousePos;
                if (keyboard.ShiftKey)
                {
                    hasRot = true;
                    currentRot = Manager.BoardTransform.Rotation;
                    currentPos = new Vector2(Manager.Width / 2, Manager.Height / 2);
                    mousePos = Manager.RawMousePos;
                }
                else if (thisCursorInfo != null && thisCursorInfo.SelectedSpriteId != null)
                {
                    hasRot = true;
                    selectedSprite = refManager.GetSprite(thisCursorInfo.SelectedSpriteId.Value);
                    currentRot = selectedSprite.Transform.Rotation;
                    currentPos = selectedSprite.Transform.GetGlobalPosition();
                }

                if (hasRot)
                {
                    if (rotate90)
                    {
                        float posRot = Extensions.GetPositiveRotation(currentRot);
                        float mod90 = posRot % 90;
                        if (keyboard.AltKey)
                        {
                            currentRot += 90 - mod90;
                        }
                        else
                        {
                            currentRot -= mod90;
                            currentRot -= mod90 == 0 ? 90 : 0;
                        }
                    }
                    else
                    {
                        rotateMode = true;

                        Vector2 relativePoint = mousePos - currentPos;
                        float relativeRot = Extensions.RadiansToDegrees((float)Math.Atan2(relativePoint.X, relativePoint.Y));
                        if (lastRotateMode)
                        {
                            currentRot = relativeRot + rotateStart;
                        }
                        else
                        {
                            rotateStart = -relativeRot;
                        }
                    }

                    if (keyboard.ShiftKey)
                    {
                        Manager.BoardTransform.Rotation = currentRot;
                        Manager.BoardTransform.Position = Vector2.Zero;
                        var rotOrigin = Transform.TransformPoint((Manager.BoardTransform.GetMatrix()), currentPos);
                        Manager.BoardTransform.Position = currentPos - rotOrigin;
                    }
                    else
                    {
                        selectedSprite.Transform.Rotation = currentRot;
                    }
                }
            }
        }
        private void MouseDown()
        {
            if (thisCursorInfo != null)
            {
                if (thisCursorInfo.SelectedSpriteId == null)
                {
                    int? add = Manager.MouseOnSprite;
                    if (add != null && refManager.ContainsAddress(add.Value))
                    {
                        Sprite s = refManager.GetSprite(add.Value);
                        if (s.Selectable && thisCursorInfo.SelectedSpriteId != add)
                        {
                            queuedSelectedSprite = refManager.GetAddress(s);
                            spriteSelectedChanged = true;
                            Debug.WriteLine("Pick Up");
                        }
                    }
                }
                else
                {
                    Debug.WriteLine("Drop");
                    queuedSelectedSprite = null;
                    spriteSelectedChanged = true;
                }
            }
            else
            {
                Debug.WriteLine("Do Nothing");
            }
        }
        bool CanSelectSprite(int sprite)
        {
            foreach (var k in cursorSprites.Keys)
            {
                if (k == playerId) { continue; }
                var v = cursorSprites[k];
                if (v.SelectedSpriteId == sprite)
                {
                    return false;
                }
            }
            return true;
        }
        private void Update(TimeSpan elapsedTime, MouseState mouseState, MouseState lastMouseState)
        {
            totalTime += elapsedTime;

            ignorePropertyChanged = true;
            List<byte> specificSerializedData = null;
            GameDataUpdate gameDataUpdate;
            lock (gameStateLockObject)
            {
                gameDataUpdate = updateData;
                updateData = null;
            }
            if (gameDataUpdate != null)
            {
                if (!gameDataUpdate.DiscardChanges && changedProperties.Count > 0 && !gameDataUpdate.IsPartialUpdate)
                {
                    specificSerializedData = GameSerialize.SpecificSerializeGameData(refManager.SpriteRefrences, changedProperties);
                }
                if (gameDataUpdate.IsPartialUpdate)
                {
                    refManager.SpriteRefrences = GameSerialize.DeserializeEditGameData(refManager.SpriteRefrences, gameDataUpdate.Data, changedProperties);
                }
                else
                {
                    var spritesData = GameSerialize.DeserializeGameData<Dictionary<int, Sprite>>(gameDataUpdate.Data);
                    if (specificSerializedData != null)
                    {
                        spritesData = GameSerialize.DeserializeEditGameData(spritesData, specificSerializedData.ToArray());
                    }
                    refManager.Reset();
                    foreach (var key in spritesData.Keys)
                    {
                        Sprite sprite = spritesData[key];
                        refManager.SpriteRefrences.Add(key, sprite);
                    }
                }
                cursorSprites = GameSerialize.DeserializeGameData<Dictionary<int, CursorInfo>>(gameDataUpdate.CursorSprites);


                refManager.UpdateSpriteAddresses();

                Manager.ClearSprites();
                foreach (var s in refManager.SpriteRefrences.Keys)
                {
                    Sprite sprite = refManager.GetSprite(s);
                    Manager.Sprites.Add(s);
                    sprite.SetRefManager(refManager);
                    sprite.OnPropertyChanged -= OnPropertyChanged;
                    sprite.OnPropertyChanged += OnPropertyChanged;
                }
            }

            ignorePropertyChanged = false;

            if (cursorSprites != null && cursorSprites.ContainsKey(playerId))
            {
                var cursorInfo = cursorSprites[playerId];
                int? prevSelected = null;
                if (gameDataUpdate != null && thisCursorInfo != null && !gameDataUpdate.DiscardChanges)
                {
                    prevSelected = thisCursorInfo.SelectedSpriteId;
                }
                thisCursor = refManager.GetSprite(cursorInfo.CursorSpriteId);
                thisCursorInfo = cursorInfo;
                if (thisCursorInfo.SelectedSpriteId != null && !refManager.ContainsAddress(thisCursorInfo.SelectedSpriteId.Value))
                {
                    thisCursorInfo.SelectedSpriteId = null;
                }
                else if(prevSelected != null && refManager.ContainsAddress(prevSelected.Value))
                {
                    if (thisCursorInfo.SelectedSpriteId == null) 
                    {
                        if (CanSelectSprite(prevSelected.Value))
                        {
                            thisCursorInfo.SelectedSpriteId = prevSelected.Value;
                        }
                        else
                        {
                            Debug.WriteLine($"Can't Select: {prevSelected.Value}");
                            DropSelected(prevSelected.Value);
                        }
                    }
                    else if(thisCursorInfo.SelectedSpriteId != prevSelected.Value)
                    {
                        Debug.WriteLine($"Drop PS:{prevSelected.Value}, Selected:{thisCursorInfo.SelectedSpriteId}");
                        DropSelected(prevSelected.Value);
                    }
                }
                else if(thisCursorInfo.SelectedSpriteId != null)
                {
                    Sprite s = refManager.GetSprite(thisCursorInfo.SelectedSpriteId.Value);
                    if(s.Parent == null)
                    {
                        thisCursorInfo.SelectedSpriteId = null;
                    }
                }
            }
            else
            {
                ignorePropertyChanged = false;
                if (thisCursorInfo != null && thisCursorInfo.SelectedSpriteId != null && refManager.ContainsAddress(thisCursorInfo.SelectedSpriteId.Value))
                {
                    Debug.WriteLine("Drop No Curosr");
                    DropSelected(thisCursorInfo.SelectedSpriteId.Value);
                }
                thisCursorInfo = null;
                thisCursor = null;
            }


            if (mouseState == MouseState.Down && lastMouseState == MouseState.Hover)
            {
                //Debug.WriteLine("Click");
                MouseDown();
            }

            if (thisCursor != null)
            {
                Vector2 cursorPos = Manager.MousePos;
                if (cursorPos != thisCursor.Transform.Position && !rotateMode && 
                    cursorPos.X >= 0 && cursorPos.Y >= 0 && cursorPos.X < size.Width && cursorPos.Y < size.Height)
                {
                    thisCursor.Transform.Position = cursorPos;
                }

                if (spriteSelectedChanged)
                {
                    int? queuedChange = queuedSelectedSprite;
                    if (queuedChange != null && !refManager.ContainsAddress(queuedChange.Value))
                    {
                        queuedChange = null;
                    }
                    if (queuedChange != null && !CanSelectSprite(queuedChange.Value))
                    {
                        queuedChange = null;
                    }
                    if (thisCursorInfo.SelectedSpriteId != null && queuedChange != thisCursorInfo.SelectedSpriteId.Value)
                    {
                        Debug.WriteLine("Drop Cause Queued");
                        DropSelected(thisCursorInfo.SelectedSpriteId.Value);
                    }

                    if (queuedChange != null && queuedChange.Value != thisCursorInfo.SelectedSpriteId)
                    {
                        Sprite newSelected = refManager.GetSprite(queuedChange.Value);
                        Vector2 glbPosition = newSelected.Transform.GetGlobalPosition();
                        float glbRotation = newSelected.Transform.GetGlobalRotation();
                        newSelected.Parent = null;
                        newSelected.Transform.Position = glbPosition - thisCursor.Transform.Position;
                        newSelected.Transform.Rotation = glbRotation;
                        newSelected.Parent = thisCursorInfo.CursorSpriteId;
                        thisCursorInfo.SelectedSpriteId = queuedChange.Value;
                        newSelected.LayerDepth.AddAtStart(1);
                    }
                    thisCursorInfo.SelectedSpriteId = queuedChange;
                    spriteSelectedChanged = false;
                    queuedSelectedSprite = null;
                }
            }

            KeyUpdate(Manager.Keyboard);

            

            if (gameDataUpdate != null)
            {
                if (thisCursor != null)
                {
                    ignorePropertyChanged = true;
                    thisCursor.Visiable = true;
                }
                if (changedProperties.Count > 0)
                {
                    specificSerializedData = GameSerialize.SpecificSerializeGameData(refManager.SpriteRefrences, changedProperties);
                    changedProperties.Clear();
                }
                else
                {
                    specificSerializedData = new List<byte>();
                }
                SendChangedWs(specificSerializedData);
            }

            if (thisCursor != null)
            {
                thisCursor.Visiable = false;
            }
            ignorePropertyChanged = false;
        }
        void DropSelected(int prevSelectedAdd)
        {
            Sprite prevSelected = refManager.GetSprite(prevSelectedAdd);
            if (prevSelected.Parent == thisCursorInfo.CursorSpriteId)
            {
                prevSelected.Transform.Position += thisCursor.Transform.Position;
                prevSelected.LayerDepth.RemoveAt(0);
                prevSelected.Parent = null;
            }
        }
    }

    public class GameDataUpdate
    {
        public ArrayWithOffset<byte> Data { get; set; }
        public ArrayWithOffset<byte> CursorSprites { get; set; }
        public bool IsPartialUpdate { get; set; }
        public bool DiscardChanges { get; set; }
        //public int? SelectedSprite { get; set; }
        //public int SendingPlayer { get; set; }
        public GameDataUpdate(ArrayWithOffset<byte> data, ArrayWithOffset<byte> cursorSprites, bool isPartialUpdate, bool discardChanges)
        {
            Data = data;
            CursorSprites = cursorSprites;
            IsPartialUpdate = isPartialUpdate;
            DiscardChanges = discardChanges;
            //SelectedSprite = null;
        }
        //public GameDataUpdate(ArrayWithOffset<byte> data, int? selectedSprite, int sendingPlayer)
        //{
        //    Data = data;
        //    CursorSprites = null;
        //    SelectedSprite = selectedSprite;
        //    SendingPlayer = sendingPlayer;
        //}
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
