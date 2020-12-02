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
        int queuedSelectedSprite = 0;
        //ElementReference cardBack, king, queen;
        MyClientWebSocket ws;
        SpriteRefrenceManager refManager => Manager.SpriteRefrenceManager;
        SpriteRefrenceManager uiRefManager => Manager.UISpriteRefrenceManager;

        int roomId, playerId;
        GameDataUpdate updateData = null;
        object gameStateLockObject = new object();
        public GameManager Manager { get; set; }
        Dictionary<int, CursorInfo> cursorSprites = null;
        Sprite thisCursor = null;
        CursorInfo thisCursorInfo = null;
        Size size;
        RectSprite shiftSprite;
        RectSprite centerSprite;
        internal GameProgram(Size size, MyClientWebSocket ws, int roomId, int playerId,
            Dictionary<int, ElementReference> imageElementRefs, ElementReference imageNotFound)
        //ElementReference cardBack, ElementReference king, ElementReference queen)
        {
            CursorInfo.Init();
            this.size = size;
            Manager = new GameManager(size, new SpriteRefrenceManager(imageElementRefs, imageNotFound), playerId);
            refManager.SpriteAdded += SpriteAdded;

            uiRefManager.AddSprite(0,
                shiftSprite = new RectSprite(uiRefManager, Vector2.Zero, new Vector2(size.Width, size.Height), new Color(0, 0, 0), Vector2.Zero, 0)
                { Alpha = 0.25f, Visiable = false });
            Vector2 centerSpriteSize = new Vector2(10, 10);
            uiRefManager.AddSprite(1,
                 centerSprite = new RectSprite(uiRefManager, new Vector2(size.Width / 2, size.Height / 2), centerSpriteSize, new Color(0, 0, 255), centerSpriteSize / 2, 45)
                 { Alpha = 0.85f, Visiable = false });
            Manager.UiSprites.Add(0);
            Manager.UiSprites.Add(1);


            this.ws = ws;
            this.roomId = roomId;
            this.playerId = playerId;



            Manager.OnUpdate += Update;
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
                List<byte> serializedAdded = new List<byte>();
                if (addedSpriteIds.Count > 0)
                {
                    serializedAdded = GameSerialize.SerializeGameData(addedSpriteIds);
                    addedSpriteIds.Clear();
                }
                sendBytes.AddRange(BitConverter.GetBytes(serializedAdded.Count));
                sendBytes.AddRange(serializedAdded);
            }

            _ = ws.SendMessageAsync(new ArraySegment<byte>(sendBytes.ToArray()));
        }

        bool ignorePropertyChanged = false;
        PathTrie<object> changedProperties = new PathTrie<object>();
        List<int> addedSpriteIds = new List<int>();

        void OnPropertyChanged(Sprite sprite, List<int> propertyPath)
        {
            if (!ignorePropertyChanged)
            {
                int add = refManager.GetAddress(sprite);
                if (!changedProperties.ContainsKey(new int[] { add }))
                {
                    propertyPath.Insert(0, add);
                    changedProperties.Insert(propertyPath, null, true);
                }
            }
        }
        void SpriteAdded(int add)
        {
            Sprite s = refManager.GetSprite(add);
            addedSpriteIds.Add(add);
            Manager.Sprites.Add(add);
            s.SetRefManager(refManager);
            s.OnPropertyChanged -= OnPropertyChanged;
            s.OnPropertyChanged += OnPropertyChanged;
            int[] path = new int[] { add };
            changedProperties.Insert(path, null, true);
            changedProperties.ClearNodeChildren(path);
        }
        private void OnKeyUp(KeyInfo keyInfo) { }

        private void OnKeyDown(KeyInfo keyInfo) { }

        bool rotateMode = false;
        float rotateStart = 0;
        void KeyUpdate(KeyboardState keyboard)
        {
            if (keyboard.ContainsKeyCode("KeyH") && keyboard["KeyH"].Down)
            {
                ResetTransform();
                return;
            }
            bool lastRotateMode = rotateMode;
            rotateMode = false;
            if (panMode) { return; }
            bool rotate90 = false;
            bool mouseRotate = false;
            if (keyboard.ContainsKeyCode("KeyR"))
            {
                KeyInfo rInfo = keyboard["KeyR"];
                if (rInfo.Repeat && rInfo.Down)
                {
                    mouseRotate = true;
                }
                else if (!rInfo.Down && rInfo.LastDown && !rInfo.LastRepeat)
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
                    currentPos = new Vector2(Manager.Width / 2, Manager.Height / 2);//;
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
                            rotateStart = -relativeRot + currentRot;
                        }
                    }
                    if (keyboard.ShiftKey)
                    {
                        Manager.BoardTransform.Rotation = currentRot;
                        //Manager.BoardTransform.Position = Vector2.Zero;
                        //var rotOrigin = Transform.TransformPoint(Manager.BoardTransform.GetMatrix(), currentPos);
                        //Manager.BoardTransform.Position = currentPos - rotOrigin;
                    }
                    else
                    {
                        selectedSprite.Transform.Rotation = currentRot;
                    }
                }
            }
        }
        bool panMode = false;
        Vector2 lastPan = Vector2.Zero;
        void MouseUpdate(bool isShift, bool isAlt, MouseState ms, MouseState lms)
        {
            if (ms == MouseState.Down)
            {
                if (isShift)
                {
                    if (rotateMode) { panMode = false; }
                    else
                    {
                        var invBoardTransform = Transform.InverseTransformMatrix(Manager.BoardTransform.GetMatrix());
                        if (panMode)
                        {
                            var rawPos = Manager.RawMousePos;
                            Vector2 curPos = Transform.TransformPoint(invBoardTransform, Manager.RawMousePos);
                            Vector2 mDiff = curPos - lastPan;
                            if (mDiff.X != 0 || mDiff.Y != 0)
                            {
                                Manager.BoardTransformOrigin -= mDiff;
                                lastPan = curPos;
                            }
                        }
                        else
                        {
                            panMode = true;
                            lastPan = Transform.TransformPoint(invBoardTransform, Manager.RawMousePos);
                        }
                    }
                }
                else if (lms == MouseState.Hover)
                {
                    if (panMode) { panMode = false; }
                    if (thisCursorInfo != null)
                    {
                        if (thisCursorInfo.SelectedSpriteId == null)
                        {
                            int? add = Manager.MouseOnSprite;
                            if (add != null && refManager.ContainsAddress(add.Value))
                            {
                                Sprite s = refManager.GetSprite(add.Value);
                                if (thisCursorInfo.SelectedSpriteId != add)
                                {
                                    var selectInfo = s.OnClick(isAlt);
                                    int selectAdd = refManager.GetAddress(selectInfo.spriteToSelect);
                                    if (selectInfo.select && thisCursorInfo.SelectedSpriteId != selectAdd)
                                    {
                                        queuedSelectedSprite = selectAdd;
                                        spriteSelectedChanged = true;
                                    }
                                }
                            }
                        }
                        else
                        {
                            int? dropOnSprite = null;
                            if (Manager.MouseOnSprite != null)
                            {
                                if (Manager.MouseOnSprite != thisCursorInfo.SelectedSpriteId)
                                {
                                    dropOnSprite = Manager.MouseOnSprite;
                                }
                                else
                                {
                                    dropOnSprite = Manager.MouseOnBehindSprite;
                                }
                            }
                            int droppedAdd = thisCursorInfo.SelectedSpriteId.Value;
                            DropSelected(droppedAdd);
                            if (refManager.ContainsAddress(droppedAdd))
                            {
                                bool pushToTop = true;
                                if (dropOnSprite != null && refManager.ContainsAddress(dropOnSprite.Value) &&
                                    droppedAdd != dropOnSprite)
                                {
                                    Sprite sDropOnSprite = refManager.GetSprite(dropOnSprite.Value);
                                    pushToTop = !sDropOnSprite.DroppedOn(droppedAdd, isAlt);
                                }
                                if(pushToTop)
                                {
                                    Sprite droppedS = refManager.GetSprite(droppedAdd);
                                    if (droppedS.LayerDepth.Count > 0)
                                    {
                                        LayerDepth fld = Manager.GetFrontLayerDepth(droppedS.LayerDepth.Count);
                                        if (fld != null && fld.Count > 0)
                                        {
                                            droppedS.LayerDepth[droppedS.LayerDepth.Count - 1] = Extensions.MinDecrement(fld[fld.Count - 1]);
                                        }
                                    }
                                }
                            }
                            thisCursorInfo.SelectedSpriteId = null;
                        }
                    }
                }
            }
            else
            {
                panMode = false;
            }
        }
        public void ResetTransform()
        {
            panMode = false;
            rotateMode = false;
            Manager.ResetTransform();
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
        private void Update(TimeSpan elapsedTime, MouseState mouseState, MouseState lastMouseState, double mouseWheelUpdate)
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
                    var t = sprite.Transform.Parent;
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
                else if (prevSelected != null && refManager.ContainsAddress(prevSelected.Value))
                {
                    if (thisCursorInfo.SelectedSpriteId == null)
                    {
                        if (CanSelectSprite(prevSelected.Value))
                        {
                            thisCursorInfo.SelectedSpriteId = prevSelected.Value;
                        }
                        else
                        {
                            //Debug.WriteLine($"Can't Select: {prevSelected.Value}");
                            DropSelected(prevSelected.Value);
                        }
                    }
                    else if (thisCursorInfo.SelectedSpriteId != prevSelected.Value)
                    {
                        //Debug.WriteLine($"Drop PS:{prevSelected.Value}, Selected:{thisCursorInfo.SelectedSpriteId}");
                        DropSelected(prevSelected.Value);
                    }
                }
                else if (thisCursorInfo.SelectedSpriteId != null)
                {
                    Sprite s = refManager.GetSprite(thisCursorInfo.SelectedSpriteId.Value);
                    if (s.Parent == null)
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
                    //Debug.WriteLine("Drop No Curosr");
                    DropSelected(thisCursorInfo.SelectedSpriteId.Value);
                }
                thisCursorInfo = null;
                thisCursor = null;
            }

            shiftSprite.Visiable = Manager.Keyboard.ShiftKey;
            centerSprite.Visiable = Manager.Keyboard.ShiftKey;

            MouseUpdate(Manager.Keyboard.ShiftKey, Manager.Keyboard.AltKey, mouseState, lastMouseState);
            KeyUpdate(Manager.Keyboard);
            if (Manager.Keyboard.ShiftKey && mouseWheelUpdate != 0)
            {
                float scaleValue = Manager.BoardTransform.Scale.X;
                scaleValue += (float)(-0.0003 * mouseWheelUpdate);
                scaleValue = Math.Max(0.0001f, scaleValue);
                Manager.BoardTransform.Scale = new Vector2(scaleValue, scaleValue);
            }

            if (thisCursor != null)
            {
                Vector2 cursorPos = Manager.MousePos;
                if (cursorPos != thisCursor.Transform.Position && !rotateMode && !panMode &&
                    cursorPos.X >= 0 && cursorPos.Y >= 0 && cursorPos.X < size.Width && cursorPos.Y < size.Height)
                {
                    thisCursor.Transform.Position = cursorPos;
                }

                if (spriteSelectedChanged)
                {
                    int queuedChange = queuedSelectedSprite;
                    if (!refManager.ContainsAddress(queuedChange))
                    {
                        spriteSelectedChanged = false;
                    }
                    if (!CanSelectSprite(queuedChange))
                    {
                        spriteSelectedChanged = false;
                    }
                    if (spriteSelectedChanged && thisCursorInfo.SelectedSpriteId != null && queuedChange != thisCursorInfo.SelectedSpriteId.Value)
                    {
                        Debug.WriteLine("Drop Cause Queued");
                        DropSelected(thisCursorInfo.SelectedSpriteId.Value);
                    }

                    if (spriteSelectedChanged && queuedChange != thisCursorInfo.SelectedSpriteId)
                    {
                        Sprite newSelected = refManager.GetSprite(queuedChange);
                        Vector2 glbPosition = newSelected.Transform.GetGlobalPosition();
                        float glbRotation = newSelected.Transform.GetGlobalRotation();
                        newSelected.Parent = null;
                        newSelected.Transform.Position = glbPosition - thisCursor.Transform.Position;
                        newSelected.Transform.Rotation = glbRotation;
                        newSelected.Parent = thisCursorInfo.CursorSpriteId;
                        thisCursorInfo.SelectedSpriteId = queuedChange;
                        newSelected.LayerDepth.AddAtStart(1);
                        thisCursorInfo.SelectedSpriteId = queuedChange;
                        var tr = newSelected.Transform;
                    }
                    spriteSelectedChanged = false;
                }
            }




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
            if (refManager.ContainsAddress(110))
            {
                Sprite s = refManager.GetSprite(110);
                var tr = s.Transform;
                var p = s.Parent;
                var v = s.Visiable;
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
