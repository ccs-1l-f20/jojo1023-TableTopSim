
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

        bool visable = true;
        [GameSerializableData(3)]
        public bool Visiable { get => visable; set { visable = value; NotifyPropertyChanged(3); } }

        bool selectable = true;
        [GameSerializableData(4)]
        public bool Selectable { get => selectable; set { selectable = value; NotifyPropertyChanged(4); } }

        float alpha = 1;
        [GameSerializableData(5)]
        public float Alpha { get => alpha; set { alpha = value; NotifyPropertyChanged(5); } }

        int? stackableIndex = null;

        [GameSerializableData(6)]
        public int? StackableIndex { get => stackableIndex; set { stackableIndex = value; NotifyPropertyChanged(6); } }

        bool mouseOver = false;


        public event Action<Sprite, List<int>> OnPropertyChanged;
        protected SpriteRefrenceManager refManager { get; private set; }
        static Sprite()
        {
            InitSprite();
        }
        public static void InitSprite()
        {
            if (GetDeafaultSprites == null)
            {
                GetDeafaultSprites = new Dictionary<ObjectTypes, (Func<Sprite> constructor, Type type)>();
                GetDeafaultSprites.Add(ObjectTypes.RectSprite, (() => new RectSprite(), typeof(RectSprite)));
                GetDeafaultSprites.Add(ObjectTypes.ImageSprite, (() => new ImageSprite(), typeof(ImageSprite)));
                GetDeafaultSprites.Add(ObjectTypes.EmptySprite, (() => new EmptySprite(), typeof(EmptySprite)));
                GetDeafaultSprites.Add(ObjectTypes.SpriteStack, (() => new SpriteStack(), typeof(SpriteStack)));
                GetDeafaultSprites.Add(ObjectTypes.CircleSprite, (() => new CircleSprite(), typeof(CircleSprite)));
                GetDeafaultSprites.Add(ObjectTypes.TextSprite, (() => new TextSprite(), typeof(TextSprite)));
                GameSerialize.AddType<Sprite>(GameSerialize.GenericSerializeFunc, DeserializeSprite, true);
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
            OnPropertyChanged?.Invoke(this, new List<int>() { layerDepthDataId, arg2 });
        }

        private void Transform_OnPropertyChanged(Transform arg1, ushort arg2)
        {
            OnPropertyChanged?.Invoke(this, new List<int>() { transformDataId, arg2 });
        }
        public void SetRefManager(SpriteRefrenceManager refManager)
        {
            this.refManager = refManager;
            Transform.SetRefManager(refManager, this);

            Transform.OnPropertyChanged -= Transform_OnPropertyChanged;
            LayerDepth.OnLayersChanged -= LayerDepth_OnLayersChanged;
            Transform.OnPropertyChanged += Transform_OnPropertyChanged;
            LayerDepth.OnLayersChanged += LayerDepth_OnLayersChanged;
        }

        public async Task Draw(MyCanvas2DContext context, Dictionary<int, Matrix<float>> spriteMatries)
        {
            if (visable)
            {
                await context.SaveAsync();
                await context.SetGlobalAlphaAsync(GetGlobalAlpha());
                Matrix<float> glbTransform = Transform.GetGlobalMatrix(spriteMatries);
                await context.TransformAsync(glbTransform[0, 0], glbTransform[1, 0], glbTransform[0, 1], glbTransform[1, 1], glbTransform[0, 2], glbTransform[1, 2]);
                await OverrideDraw(context);

                await context.RestoreAsync();
            }
        }

        protected float GetGlobalAlpha()
        {
            if (Parent != null)
            {
                return Alpha * refManager.GetSprite(Parent.Value).GetGlobalAlpha();
            }
            return Alpha;
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

        protected static async Task ProtectedDraw(Sprite sprite, MyCanvas2DContext context)
        {
            await context.SaveAsync();
            await context.SetGlobalAlphaAsync(sprite.GetGlobalAlpha());
            Matrix<float> transformMtx = sprite.Transform.GetMatrix();
            await context.TransformAsync(transformMtx[0, 0], transformMtx[1, 0], transformMtx[0, 1], transformMtx[1, 1], transformMtx[0, 2], transformMtx[1, 2]);
            
            await sprite.OverrideDraw(context);

            await context.RestoreAsync();
        }
        protected abstract Task OverrideDraw(MyCanvas2DContext context);

        /// <summary>
        /// Shoul Only Be Called By Game Manager
        /// </summary>
        /// <param name="mousePos"></param>
        /// <param name="mouseState"></param>
        /// <returns>If the mouse is blocked</returns>
        public bool Update(Vector2 mousePos, MouseState mouseState, bool mouseBlocked, TimeSpan elapsedTime, Dictionary<int, Matrix<float>> spriteMatries)
        {
            OverideUpdate(mousePos, mouseState, elapsedTime);

            if (!visable) { return false; }
            bool prevMouseOver = mouseOver;
            mouseOver = false;
            bool blocking = false;
            Matrix<float> glbMatrix = Transform.GetGlobalMatrix(spriteMatries);
            //if (!mouseBlocked)
            //{
            if (PointInHitbox(mousePos, glbMatrix))
            {
                mouseOver = true;
                blocking = true;
                if (!prevMouseOver && !mouseBlocked)
                {
                    OnMouseEnter?.Invoke(this, mousePos, mouseState);
                }
            }
            else if (prevMouseOver && !mouseBlocked)
            {
                OnMouseLeave?.Invoke(this, mousePos, mouseState);
            }
            //}
            return blocking;
        }
        
        public virtual Task PreDrawUpdate(MyCanvas2DContext context) { return Task.CompletedTask; }
        protected virtual void OverideUpdate(Vector2 mousePos, MouseState mouseState, TimeSpan elapsedTime) { }

        protected static bool ProtectedPointInHitbox(Sprite sprite, Vector2 point, Matrix<float> glbMatrix)
        {
            return sprite.PointInHitbox(point, glbMatrix);
        }
        protected abstract bool PointInHitbox(Vector2 point, Matrix<float> glbMatrix);

        protected static bool PointInRotatedRect(Matrix<float> glbMatrix, Vector2 point, Vector2 size, Vector2 origin)
        {
            point = Transform.TransformPoint(Transform.InverseTransformMatrix(glbMatrix), point);
            return point.X >= -origin.X && point.Y >= -origin.Y && point.X < size.X - origin.X && point.Y < size.Y - origin.Y;
        }
        protected static bool PointInCircle(Matrix<float> glbMatrix, Vector2 point, float radius)
        {
            point = Transform.TransformPoint(Transform.InverseTransformMatrix(glbMatrix), point);
            point *= point; 
            return radius * radius <= point.X + point.Y;
        }
        static Sprite DeserializeSprite(TypeSerializableInfo<Sprite> info, ArrayWithOffset<byte> bytes)
        {
            var propertyBytes = GameSerialize.GetPropertyBytes(bytes);
            ObjectTypes objectType = (ObjectTypes)propertyBytes[objectTypeDataId][0];
            var countT = GetDeafaultSprites.Count;
            var keyTest = GetDeafaultSprites.First().Key;
            var test = GetDeafaultSprites[objectType];
            Sprite dataObject = GetDeafaultSprites[objectType].constructor.Invoke();
            return (Sprite)GameSerialize.CustomDeserialize(dataObject, propertyBytes);
        }
        protected void NotifyPropertyChanged(ushort propertyDataId)
        {
            OnPropertyChanged?.Invoke(this, new List<int>() { propertyDataId });
        }
        public virtual (bool select, Sprite spriteToSelect) OnClick(bool isAlt)
        {
            return (Selectable, this);
        }

        public virtual bool DroppedOn(int add, bool isAlt)
        {
            if (isAlt || stackableIndex == null) { return false; }
            Sprite droppedSprite = refManager.GetSprite(add);
            if (droppedSprite.stackableIndex != stackableIndex) { return false; }
            SpriteStack spriteStack = new SpriteStack(refManager, Transform.Position, stackableIndex.Value);
            spriteStack.LayerDepth = new LayerDepth();
            spriteStack.LayerDepth.AddTo(LayerDepth);
            refManager.AddSprite(spriteStack);
            spriteStack.AddToStack(this, refManager.GetAddress(this));
            spriteStack.AddToStack(droppedSprite, add);
            return true;
        }
    }
    public class EmptySprite : Sprite
    {
        static EmptySprite()
        {
            GameSerialize.AddType<EmptySprite>(GameSerialize.GenericSerializeFunc, GameSerialize.GenericDeserializeFunc, GameSerialize.CustomGenericDeserializeFunc);
        }
        public EmptySprite()
            : base(ObjectTypes.EmptySprite) { }
        public EmptySprite(SpriteRefrenceManager refrenceManager, Vector2 position, Vector2 scale, float rotation = 0)
            : base(position, scale, rotation, ObjectTypes.EmptySprite, refrenceManager)
        {

        }


        protected override Task OverrideDraw(MyCanvas2DContext context) { return Task.CompletedTask; }

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
