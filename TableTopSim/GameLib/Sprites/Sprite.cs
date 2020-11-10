﻿
using GameLib.GameSerialization;
using MyCanvasLib;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Diagnostics;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;

namespace GameLib.Sprites
{
    public abstract class Sprite
    {
        [GameSerializableData(objectTypeDataId, true)]
        public ObjectTypes ObjectType { get => objectType; }

        internal static Dictionary<ObjectTypes, (Func<Sprite> constructor, Type type)> GetDeafaultSprites = new Dictionary<ObjectTypes, (Func<Sprite> constructor, Type type)>();
        //public event Action<Sprite> OnLayerDepthChanged;
        public event Action<Sprite, Vector2, MouseState> OnMouseEnter;
        public event Action<Sprite, Vector2, MouseState> OnMouseLeave;
        Vector2 positon;

        [GameSerializableData(1)]
        public Vector2 Position { get => positon; set { positon = value; NotifyPropertyChanged(1); } }
        public float X { get { return Position.X; } set { Position = new Vector2(value, Position.Y); } }
        public float Y { get { return Position.Y; } set { Position = new Vector2(Position.X, value); } }

        Vector2 scale;
        [GameSerializableData(2)]
        public Vector2 Scale { get => scale; set { scale = value; NotifyPropertyChanged(2); } }

        Vector2 origin;
        [GameSerializableData(3)]
        public Vector2 Origin { get => origin; set { origin = value; NotifyPropertyChanged(3); } }

        float rotation;
        [GameSerializableData(4)]
        public float Rotation { get => rotation; set { rotation = value; NotifyPropertyChanged(4); } }

        public List<int> Children { get; private set; }
        Dictionary<int, int> childrenIndexes;
        Sprite frontChild = null;
        Sprite backChild = null;

        float layerDepth = 0;

        bool mouseOver = false;
        Sprite parent;


        [GameSerializableData(5)]
        public float LayerDepth
        {
            get { return layerDepth; }
            set { layerDepth = value; /*LayerDepthChanged();*/ NotifyPropertyChanged(5); }
        }//Negative Layer Depth is Front

        const ushort objectTypeDataId = 0;
        ObjectTypes objectType;

        public event Action<Sprite, ushort> OnPropertyChanged;
        static Sprite()
        {
            GetDeafaultSprites.Add(ObjectTypes.RectSprite, (() => new RectSprite(), typeof(RectSprite)));
            GetDeafaultSprites.Add(ObjectTypes.ImageSprite, (() => new ImageSprite(), typeof(ImageSprite)));
            GetDeafaultSprites.Add(ObjectTypes.EmptySprite, (() => new EmptySprite(), typeof(EmptySprite)));
            GameSerialize.AddType<Sprite>(GameSerialize.GenericSerializeFunc, DeserializeSprite, true, DeserializeEditSprite);

        }
        public Sprite(ObjectTypes objectType)
        {
            this.objectType = objectType;
            Position = Vector2.Zero;
            Origin = Vector2.Zero;
            Scale = Vector2.One;
            Rotation = 0;
            Children = new List<int>();
            childrenIndexes = new Dictionary<int, int>();
            parent = null;
        }
        public Sprite(Vector2 position, Vector2 scale, Vector2 origin, float rotation, ObjectTypes objectType)
        {
            this.objectType = objectType;
            Position = position;
            Origin = origin;
            Scale = scale;
            Rotation = rotation;
            Children = new List<int>();
            childrenIndexes = new Dictionary<int, int>();
            parent = null;
        }

        public void AddChild(Sprite sprite, SpriteRefrenceManager refManager)
        {
            int spriteAddress = refManager.GetAddress(sprite);
            AddChild(sprite, spriteAddress);
        }
        public void AddChild(int spriteAddress, SpriteRefrenceManager refManager)
        {
            Sprite sprite = refManager.GetSprite(spriteAddress);
            AddChild(sprite, spriteAddress);
        }
        void AddChild(Sprite sprite, int spriteAddress)
        {
            if (frontChild == null || sprite.layerDepth < frontChild.layerDepth)
            {
                frontChild = sprite;
            }
            if (backChild == null || sprite.layerDepth > backChild.layerDepth)
            {
                backChild = sprite;
            }
            childrenIndexes.Add(spriteAddress, Children.Count);
            Children.Add(spriteAddress);
            sprite.parent = this;
        }
        public bool RemoveChild(Sprite sprite, SpriteRefrenceManager refManager)
        {
            int spriteAddress = refManager.GetAddress(sprite);
            return RemoveChild(sprite, spriteAddress, refManager);
        }
        public bool RemoveChild(int spriteAddress, SpriteRefrenceManager refManager)
        {
            Sprite sprite = refManager.GetSprite(spriteAddress);
            return RemoveChild(sprite, spriteAddress, refManager);
        }
        public bool RemoveChild(Sprite sprite, int spriteAddress, SpriteRefrenceManager refManager)
        {
            if (childrenIndexes.ContainsKey(spriteAddress))
            {
                if (frontChild == sprite)
                {
                    frontChild = null;
                }
                if (backChild == sprite)
                {
                    backChild = null;
                }
                if (frontChild == null || backChild == null)
                {
                    SortChildren(refManager);
                }
                Children.RemoveAt(childrenIndexes[spriteAddress]);
                sprite.parent = null;
                return true;
            }
            return false;
        }
        public void ClearChildren(SpriteRefrenceManager refManager)
        {
            childrenIndexes.Clear();
            foreach (var sprite in Children)
            {
                if (refManager.ContainsAddress(sprite))
                {
                    refManager.GetSprite(sprite).parent = null;
                }
                //refManager.SpriteRefrences[sprite].OnLayerDepthChanged -= ChildLayerDepthChanged;
            }
            Children.Clear();
        }

        public void MoveChildToFront(Sprite sprite, SpriteRefrenceManager refManager)
        {
            int spriteAddress = refManager.GetAddress(sprite);
            if (childrenIndexes.ContainsKey(spriteAddress) && frontChild != sprite)
            {
                sprite.layerDepth = frontChild.LayerDepth;
                sprite.LayerDepth = Extensions.MinDecrement(sprite.layerDepth);
                frontChild = sprite;
                //ChildLayerDepthChanged(sprite, refManager, true, true);
            }
        }
        public void MoveChildToBack(Sprite sprite, SpriteRefrenceManager refManager)
        {
            int spriteAddress = refManager.GetAddress(sprite);
            if (childrenIndexes.ContainsKey(spriteAddress) && backChild != sprite)
            {
                sprite.layerDepth = backChild.LayerDepth;
                sprite.LayerDepth = Extensions.MinIncrement(sprite.layerDepth);
                backChild = sprite;
                //ChildLayerDepthChanged(sprite, refManager, true, false);
            }
        }
        //void LayerDepthChanged()
        //{
        //    OnLayerDepthChanged?.Invoke(this);
        //}
        //void ChildLayerDepthChanged(Sprite child, SpriteRefrenceManager refManager)
        //{
        //    ChildLayerDepthChanged(child, refManager, false, null);
        //}
        //void ChildLayerDepthChanged(Sprite child, SpriteRefrenceManager refManager, bool moveWithEqualDepth, bool? upIsChangeDiretion)
        //{
        //    int childAddress = refManager.SpriteAddresses[child];
        //    int currentIndex = childrenIndexes[childAddress];

        //    if (upIsChangeDiretion == null || upIsChangeDiretion.Value)
        //    {
        //        for (int i = currentIndex; i > 0; i--)
        //        {
        //            if (children[i - 1].LayerDepth > child.LayerDepth || (moveWithEqualDepth && children[i - 1].LayerDepth == child.LayerDepth))
        //            {
        //                var temp = children[i - 1];
        //                children[i - 1] = childAddress;
        //                children[i] = temp;
        //                childrenIndexes[temp] = i;
        //                childrenIndexes[childAddress] = i - 1;
        //            }
        //            else
        //            {
        //                break;
        //            }
        //        }
        //    }

        //    if (upIsChangeDiretion == null || !upIsChangeDiretion.Value)
        //    {
        //        for (int i = currentIndex; i + 1 < children.Count; i++)
        //        {
        //            if (children[i + 1].LayerDepth < child.LayerDepth && (moveWithEqualDepth && children[i - 1].LayerDepth == child.LayerDepth))
        //            {
        //                var temp = children[i + 1];
        //                children[i + 1] = child;
        //                children[i] = temp;
        //                childrenIndexes[temp] = i;
        //                childrenIndexes[child] = i + 1;
        //            }
        //            else
        //            {
        //                break;
        //            }
        //        }
        //    }
        //}

        public async Task Draw(MyCanvas2DContext context, SpriteRefrenceManager refManager)
        {
            await context.SaveAsync();
            await context.TranslateAsync(Position.X, Position.Y);
            await context.ScaleAsync(Scale.X, Scale.Y);
            var radians = Extensions.DegreesToRadians(Rotation);
            await context.RotateAsync(radians);
            await OverideDraw(context);

            SortChildren(refManager);
            //Children are drawn backwards so index 0 is in front
            for (int i = Children.Count - 1; i >= 0; i--)
            {
                await refManager.GetSprite(Children[i]).Draw(context, refManager);
            }
            await context.RestoreAsync();
            //await context.RotateAsync(-radians);
            //await context.ScaleAsync(1 / Scale.X, 1 / Scale.Y);
            //await context.TranslateAsync(-translateVector.X, -translateVector.Y);
        }
        void SortChildren(SpriteRefrenceManager refManager)
        {
            bool reSort = false;
            float lastLayerDepth = float.MinValue;
            List<int> childrenIndexesToRemove = new List<int>();
            for (int i = 0; i < Children.Count; i++)
            {
                int child = Children[i];
                if (!refManager.ContainsAddress(child))
                {
                    childrenIndexesToRemove.Add(i);
                    continue;
                }
                Sprite cSprite = refManager.GetSprite(child);
                if (cSprite.LayerDepth < lastLayerDepth)
                {
                    reSort = true;
                    break;
                }
                lastLayerDepth = cSprite.LayerDepth;
            }
            foreach (var childIndex in childrenIndexesToRemove)
            {
                int child = Children[childIndex];
                Children.RemoveAt(childIndex);
                childrenIndexes.Remove(child);
            }
            if (reSort)
            {
                Children = Children.OrderBy(s => refManager.GetSprite(s).LayerDepth).ToList();
                for (int i = 0; i < Children.Count; i++)
                {
                    childrenIndexes[Children[i]] = i;
                }
            }

            if (Children.Count > 0)
            {
                frontChild = refManager.GetSprite(Children[0]);
                backChild = refManager.GetSprite(Children[Children.Count - 1]);
            }
            else
            {
                frontChild = null;
                backChild = null;
            }
        }
        protected abstract Task OverideDraw(MyCanvas2DContext context);

        /// <summary>
        /// Shoul Only Be Called By Game Manager and Only Once
        /// </summary>
        /// <param name="mousePos"></param>
        /// <param name="mouseState"></param>
        /// <returns>If the mouse is blocked</returns>
        public Sprite GameManagerUpdate(Vector2 mousePos, MouseState mouseState, TimeSpan elapsedTime, SpriteRefrenceManager refManager)
        {
            Sprite mouseOnSprite;
            mouseOnSprite = MouseInHitboxOrChildren(mousePos, mouseState, false, refManager).mouseOnSprite;

            OverideUpdate(mousePos, mouseState, elapsedTime);

            foreach (var child in Children)
            {
                refManager.GetSprite(child).OverideUpdate(mousePos, mouseState, elapsedTime);
            }
            return mouseOnSprite;
        }
        protected virtual void OverideUpdate(Vector2 mousePos, MouseState mouseState, TimeSpan elapsedTime) { }

        (bool mouseOver, Sprite mouseOnSprite) MouseInHitboxOrChildren(Vector2 point, MouseState mouseState, bool mouseBlocked, SpriteRefrenceManager refManager)
        {
            bool prevMouseOver = mouseOver;
            Sprite mouseOnSprite = null;
            mouseOver = false;
            foreach (var child in Children)
            {
                var childInfo = refManager.GetSprite(child).MouseInHitboxOrChildren(point, mouseState, mouseBlocked, refManager);
                if (childInfo.mouseOver)
                {
                    if (childInfo.mouseOnSprite != null)
                    {
                        mouseOnSprite = childInfo.mouseOnSprite;
                    }
                    mouseBlocked = true;
                }
            }
            if (!mouseBlocked && PointInHitbox(point))
            {
                mouseOnSprite = this;
                mouseOver = true;
                if (!prevMouseOver)
                {
                    OnMouseEnter?.Invoke(this, point, mouseState);
                }
                mouseBlocked = true;
            }
            if (prevMouseOver && !mouseOver)
            {
                OnMouseLeave?.Invoke(this, point, mouseState);
            }
            return (mouseBlocked, mouseOnSprite);
        }
        protected abstract bool PointInHitbox(Vector2 point);
        //protected abstract bool MouseEvent(Vector2 mousePos, MouseState mouseState, bool mouseBlocking);

        Vector2 RotatePoint(Vector2 point)
        {
            if (parent != null) { point = parent.RotatePoint(point); }
            Vector2 rotPoint = point - Position;
            double currentRotation = Math.Atan2(rotPoint.Y, rotPoint.X);
            double distance = rotPoint.Length();
            double rotRadians = Extensions.DegreesToRadians(Rotation);
            rotPoint.X = (float)(Math.Cos(currentRotation - rotRadians) * distance) / Scale.X;
            rotPoint.Y = (float)(Math.Sin(currentRotation - rotRadians) * distance) / Scale.Y;

            return rotPoint;
        }
        protected bool PointInRotatedRect(Vector2 point, Vector2 size)
        {
            Vector2 rotPoint = RotatePoint(point);
            float left = -Origin.X;
            float right = (size.X - Origin.X);
            float top = -Origin.Y;
            float bottom = (size.Y - Origin.Y);
            return rotPoint.X >= left && rotPoint.X < right && rotPoint.Y >= top && rotPoint.Y < bottom;
        }
        static Sprite DeserializeSprite(TypeSerializableInfo<Sprite> info, ArrayWithOffset<byte> bytes)
        {
            var propertyBytes = GameSerialize.GetPropertyBytes(bytes);
            ObjectTypes objectType = (ObjectTypes)propertyBytes[objectTypeDataId][0];
            var countT = GetDeafaultSprites.Count;
            var keyTest = GetDeafaultSprites.First().Key;
            var test = GetDeafaultSprites[objectType];
            Sprite dataObject = GetDeafaultSprites[objectType].constructor.Invoke();
            return (Sprite)GameSerialize.CustomDeserialize(dataObject, propertyBytes, null);
        }
        static Sprite DeserializeEditSprite(Sprite dataObject, TypeSerializableInfo<Sprite> info, ArrayWithOffset<byte> bytes, Dictionary<object, HashSet<int>> dataToIgnore)
        {
            var propertyBytes = GameSerialize.GetPropertyBytes(bytes);
            ObjectTypes objectType = (ObjectTypes)propertyBytes[objectTypeDataId][0];
            var spriteTypeInfo = GetDeafaultSprites[objectType];
            if (dataObject.GetType() != spriteTypeInfo.type)
            {
                dataObject = spriteTypeInfo.constructor.Invoke();
            }
            return (Sprite)GameSerialize.CustomDeserialize(dataObject, propertyBytes, dataToIgnore);
        }
        protected void NotifyPropertyChanged(ushort propertyDataId)
        {
            OnPropertyChanged?.Invoke(this, propertyDataId);
        }

    }
    public class EmptySprite : Sprite
    {
        static EmptySprite()
        {
            GameSerialize.AddType<EmptySprite>(GameSerialize.GenericSerializeFunc, GameSerialize.GenericDeserializeFunc, true,
                GameSerialize.GenericDeserializeEditFunc, GameSerialize.CustomGenericDeserializeFunc);
        }
        public EmptySprite()
            : base(ObjectTypes.EmptySprite) { }
        public EmptySprite(Vector2 position, Vector2 scale, Vector2 origin, float rotation = 0)
            : base(position, scale, origin, rotation, ObjectTypes.EmptySprite)
        {

        }


        protected override async Task OverideDraw(MyCanvas2DContext context) { }

        protected override bool PointInHitbox(Vector2 point)
        {
            return false;
        }

    }

    public class SpriteJsonConverter : JsonConverter
    {
        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            throw new NotImplementedException("Can't Write");
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            if (reader.TokenType == JsonToken.Null)
            {
                return string.Empty;
            }
            else if (reader.TokenType == JsonToken.String)
            {
                return serializer.Deserialize(reader, objectType);
            }
            else
            {
                JObject obj = JObject.Load(reader);
                if (obj["ObjectType"] != null)
                {
                    ObjectTypes ot = (ObjectTypes)obj["ObjectType"].Value<int>();
                    if (Sprite.GetDeafaultSprites.ContainsKey(ot))
                    {
                        return JsonConvert.DeserializeObject(obj.ToString(), Sprite.GetDeafaultSprites[ot].type);
                    }
                }
                throw new JsonException();
            }
        }

        public override bool CanWrite
        {
            get { return false; }
        }

        public override bool CanConvert(Type objectType)
        {
            return (objectType == typeof(Sprite));
        }
    }
}
