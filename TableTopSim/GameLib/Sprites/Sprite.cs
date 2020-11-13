
using GameLib.GameSerialization;
using MathNet.Numerics.LinearAlgebra;
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
        const ushort objectTypeDataId = 0;
        const ushort transformDataId = 1;
        const ushort layerDepthDataId = 2;


        ObjectTypes objectType;
        [GameSerializableData(objectTypeDataId, true)]
        public ObjectTypes ObjectType { get => objectType; }


        internal static Dictionary<ObjectTypes, (Func<Sprite> constructor, Type type)> GetDeafaultSprites = null;
        //public event Action<Sprite> OnLayerDepthChanged;
        public event Action<Sprite, Vector2, MouseState> OnMouseEnter;
        public event Action<Sprite, Vector2, MouseState> OnMouseLeave;
        
        [GameSerializableData(transformDataId)]
        public Transform Transform { get; internal set; }

        [GameSerializableData(layerDepthDataId)]
        public LayerDepth LayerDepth { get; internal set; }

        public int? Parent { get => Transform.Parent; set { Transform.Parent = value; } }



        bool mouseOver = false;


        public event Action<Sprite, ushort, object, ushort> OnPropertyChanged;
        SpriteRefrenceManager refManager;
        static Sprite()
        {
            InitSprite();
        }
        public static void InitSprite()
        {
            if(GetDeafaultSprites== null)
            {
                GetDeafaultSprites = new Dictionary<ObjectTypes, (Func<Sprite> constructor, Type type)>();
                GetDeafaultSprites.Add(ObjectTypes.RectSprite, (() => new RectSprite(), typeof(RectSprite)));
                GetDeafaultSprites.Add(ObjectTypes.ImageSprite, (() => new ImageSprite(), typeof(ImageSprite)));
                GetDeafaultSprites.Add(ObjectTypes.EmptySprite, (() => new EmptySprite(), typeof(EmptySprite)));
                GameSerialize.AddType<Sprite>(GameSerialize.GenericSerializeFunc, DeserializeSprite, true, DeserializeEditSprite);
            }
        }
        public Sprite(ObjectTypes objectType)
        {
            this.objectType = objectType;
            Transform = new Transform(this);
            Transform.OnPropertyChanged += Transform_OnPropertyChanged;

            LayerDepth = new LayerDepth(0);
            LayerDepth.OnLayersChanged += LayerDepth_OnLayersChanged;
        }

        public Sprite(Vector2 position, Vector2 scale, float rotation, ObjectTypes objectType, SpriteRefrenceManager refManager)
        {
            this.objectType = objectType;
            Transform = new Transform(position, scale, rotation, this);
            Transform.OnPropertyChanged += Transform_OnPropertyChanged;

            LayerDepth = new LayerDepth(0);
            LayerDepth.OnLayersChanged += LayerDepth_OnLayersChanged;

            SetRefManager(refManager);
        }
        private void LayerDepth_OnLayersChanged(LayerDepth arg1, ushort arg2)
        {
            OnPropertyChanged?.Invoke(this, layerDepthDataId, arg1, arg2);
        }

        private void Transform_OnPropertyChanged(Transform arg1, ushort arg2)
        {
            OnPropertyChanged?.Invoke(this, transformDataId, arg1, arg2);
        }
        public void SetRefManager(SpriteRefrenceManager refManager)
        {
            this.refManager = refManager;
            Transform.SetRefManager(refManager);
        }

        public async Task Draw(MyCanvas2DContext context, Dictionary<int, Matrix<float>> spriteMatries)
        {
            await context.SaveAsync();
            Matrix<float> glbTransform = Transform.GetGlobalMatrix(spriteMatries);
            await context.TransformAsync(glbTransform[0, 0], glbTransform[1, 0], glbTransform[0, 1], glbTransform[1, 1], glbTransform[0, 2], glbTransform[1, 2]);
            await OverideDraw(context);
            
            await context.RestoreAsync();
        }

        public LayerDepth GetGlobalLayerDepth()
        {
            LayerDepth ld = new LayerDepth();
            GetGlobalLayerDepthR(ld);
            return ld;
        }
        void GetGlobalLayerDepthR(LayerDepth ld)
        {
            if (Parent != null)
            {
                refManager.GetSprite(Parent.Value).GetGlobalLayerDepthR(ld);
            }
            ld.AddTo(LayerDepth);
        }
        //public async Task Draw(MyCanvas2DContext context, SpriteRefrenceManager refManager)
        //{
        //    await context.SaveAsync();
        //    await context.TranslateAsync(Position.X, Position.Y);
        //    await context.ScaleAsync(Scale.X, Scale.Y);
        //    var radians = Extensions.DegreesToRadians(Rotation);
        //    await context.RotateAsync(radians);
        //    await OverideDraw(context);

        //    SortChildren(refManager);
        //    //Children are drawn backwards so index 0 is in front
        //    for (int i = Children.Count - 1; i >= 0; i--)
        //    {
        //        await refManager.GetSprite(Children[i]).Draw(context, refManager);
        //    }
        //    await context.RestoreAsync();
        //    //await context.RotateAsync(-radians);
        //    //await context.ScaleAsync(1 / Scale.X, 1 / Scale.Y);
        //    //await context.TranslateAsync(-translateVector.X, -translateVector.Y);
        //}
        
        protected abstract Task OverideDraw(MyCanvas2DContext context);

        /// <summary>
        /// Shoul Only Be Called By Game Manager
        /// </summary>
        /// <param name="mousePos"></param>
        /// <param name="mouseState"></param>
        /// <returns>If the mouse is blocked</returns>
        public bool Update(Vector2 mousePos, MouseState mouseState, bool mouseBlocked, TimeSpan elapsedTime, Dictionary<int, Matrix<float>> spriteMatries)
        {
            OverideUpdate(mousePos, mouseState, elapsedTime);

            bool prevMouseOver = mouseOver;
            mouseOver = false;
            bool blocking = false;
            Matrix<float> glbMatrix = Transform.GetGlobalMatrix(spriteMatries);
            if (!mouseBlocked)
            {
                if (PointInHitbox(mousePos, glbMatrix))
                {
                    mouseOver = true;
                    blocking = true;
                    if (!prevMouseOver)
                    {
                        OnMouseEnter?.Invoke(this, mousePos, mouseState);
                    }
                }
                else if (prevMouseOver)
                {
                    OnMouseLeave?.Invoke(this, mousePos, mouseState);
                }
            }
            return blocking;
        }
        protected virtual void OverideUpdate(Vector2 mousePos, MouseState mouseState, TimeSpan elapsedTime) { }

        //(bool mouseOver, Sprite mouseOnSprite) MouseInHitboxOrChildren(Vector2 point, MouseState mouseState, bool mouseBlocked, SpriteRefrenceManager refManager)
        //{
        //    bool prevMouseOver = mouseOver;
        //    Sprite mouseOnSprite = null;
        //    mouseOver = false;
        //    foreach (var child in Children)
        //    {
        //        var childInfo = refManager.GetSprite(child).MouseInHitboxOrChildren(point, mouseState, mouseBlocked, refManager);
        //        if (childInfo.mouseOver)
        //        {
        //            if (childInfo.mouseOnSprite != null)
        //            {
        //                mouseOnSprite = childInfo.mouseOnSprite;
        //            }
        //            mouseBlocked = true;
        //        }
        //    }
        //    if (!mouseBlocked && PointInHitbox(point))
        //    {
        //        mouseOnSprite = this;
        //        mouseOver = true;
        //        if (!prevMouseOver)
        //        {
        //            OnMouseEnter?.Invoke(this, point, mouseState);
        //        }
        //        mouseBlocked = true;
        //    }
        //    if (prevMouseOver && !mouseOver)
        //    {
        //        OnMouseLeave?.Invoke(this, point, mouseState);
        //    }
        //    return (mouseBlocked, mouseOnSprite);
        //}
        protected abstract bool PointInHitbox(Vector2 point, Matrix<float> glbMatrix);
        //protected abstract bool MouseEvent(Vector2 mousePos, MouseState mouseState, bool mouseBlocking);

        protected static bool PointInRotatedRect(Matrix<float> glbMatrix, Vector2 point, Vector2 size, Vector2 origin)
        {
            point = Transform.TransformPoint(glbMatrix, point);
            return point.X >= -origin.X && point.Y >= -origin.Y && point.X < size.X - origin.X && point.Y < size.Y - origin.Y;
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
            OnPropertyChanged?.Invoke(this, propertyDataId, null, 0);
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
        public EmptySprite(SpriteRefrenceManager refrenceManager, Vector2 position, Vector2 scale, float rotation = 0)
            : base(position, scale, rotation, ObjectTypes.EmptySprite, refrenceManager)
        {

        }


        protected override Task OverideDraw(MyCanvas2DContext context) { return Task.CompletedTask; }

        protected override bool PointInHitbox(Vector2 point, Matrix<float> glbMatrix)
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
