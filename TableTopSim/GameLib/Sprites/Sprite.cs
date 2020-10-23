
using GameLib.GameSerialization;
using MyCanvasLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;

namespace GameLib.Sprites
{
    public abstract class Sprite
    {
        protected static Dictionary<ObjectTypes, Func<Sprite>> GetDeafaultSprites = new Dictionary<ObjectTypes, Func<Sprite>>();
        public event Action<Sprite> OnLayerDepthChanged;
        public event Action<Sprite, Vector2, MouseState> OnMouseEnter;
        public event Action<Sprite, Vector2, MouseState> OnMouseLeave;

        [GameSerializableData(1)]
        public Vector2 Position { get; set; }
        public float X { get { return Position.X; } set { Position = new Vector2(value, Position.Y); } }
        public float Y { get { return Position.Y; } set { Position = new Vector2(Position.X, value); } }

        [GameSerializableData(2)]
        public Vector2 Scale { get; set; }
        [GameSerializableData(3)]
        public Vector2 Origin { get; set; }
        [GameSerializableData(4)]
        public float Rotation { get; set; }

        protected List<Sprite> children;
        Dictionary<Sprite, int> childrenIndexes;

        float layerDepth = 0;

        bool mouseOver = false;
        Sprite parent;


        [GameSerializableData(5)]
        public float LayerDepth
        {
            get { return layerDepth; }
            set { layerDepth = value; LayerDepthChanged(); }
        }//Negative Layer Depth is Front

        [GameSerializableData(6)]
        public int Id { get; set; } = -1;

        const ushort objectTypeDataId = 0;
        [GameSerializableData(objectTypeDataId, true)]
        public ObjectTypes ObjectType { get; private set; }
        static Sprite()
        {
            GameSerialize.AddType<Sprite>(GameSerialize.GenericSerializeFunc, DeserializeSprite);
        }
        public Sprite(ObjectTypes objectType)
        {
            ObjectType = objectType;
            Position = Vector2.Zero;
            Origin = Vector2.Zero;
            Scale = Vector2.One;
            Rotation = 0;
            children = new List<Sprite>();
            childrenIndexes = new Dictionary<Sprite, int>();
            parent = null;
        }
        public Sprite(Vector2 position, Vector2 scale, Vector2 origin, float rotation, ObjectTypes objectType)
        {
            ObjectType = objectType;
            Position = position;
            Origin = origin;
            Scale = scale;
            Rotation = rotation;
            children = new List<Sprite>();
            childrenIndexes = new Dictionary<Sprite, int>();
            parent = null;
        }

        public void AddChild(Sprite sprite)
        {
            childrenIndexes.Add(sprite, -1);
            Sprite prev = sprite;
            for (int i = 0; i < children.Count; i++)
            {
                if (sprite.LayerDepth <= children[i].LayerDepth)
                {
                    var temp = children[i];
                    children[i] = prev;
                    childrenIndexes[children[i]] = i;
                    prev = temp;
                }
            }
            childrenIndexes[prev] = children.Count;
            children.Add(prev);
            sprite.parent = this;
            sprite.OnLayerDepthChanged += ChildLayerDepthChanged;
        }
        public bool RemoveChild(Sprite sprite)
        {
            if (childrenIndexes.ContainsKey(sprite))
            {
                int index = childrenIndexes[sprite];
                for (int i = index + 1; i < children.Count; i++)
                {
                    children[i - 1] = children[i];
                    childrenIndexes[children[i - 1]] = i - 1;
                }
                childrenIndexes.Remove(sprite);
                children.RemoveAt(children.Count - 1);
                sprite.parent = null;
                sprite.OnLayerDepthChanged -= ChildLayerDepthChanged;
                return true;
            }
            else
            {
                return false;
            }
        }

        public void MoveChildToFront(Sprite sprite)
        {
            if (childrenIndexes.ContainsKey(sprite))
            {
                sprite.layerDepth = children[0].LayerDepth;
                ChildLayerDepthChanged(sprite, true, true);
            }
        }
        public void MoveChildToBack(Sprite sprite)
        {
            if (childrenIndexes.ContainsKey(sprite))
            {
                sprite.layerDepth = children[children.Count - 1].LayerDepth;
                ChildLayerDepthChanged(sprite, true, false);
            }
        }
        void LayerDepthChanged()
        {
            OnLayerDepthChanged?.Invoke(this);
        }
        void ChildLayerDepthChanged(Sprite child)
        {
            ChildLayerDepthChanged(child, false, null);
        }
        void ChildLayerDepthChanged(Sprite child, bool moveWithEqualDepth, bool? upIsChangeDiretion)
        {
            int currentIndex = childrenIndexes[child];

            if (upIsChangeDiretion == null || upIsChangeDiretion.Value)
            {
                for (int i = currentIndex; i > 0; i--)
                {
                    if (children[i - 1].LayerDepth > child.LayerDepth || (moveWithEqualDepth && children[i - 1].LayerDepth == child.LayerDepth))
                    {
                        var temp = children[i - 1];
                        children[i - 1] = child;
                        children[i] = temp;
                        childrenIndexes[temp] = i;
                        childrenIndexes[child] = i - 1;
                    }
                    else
                    {
                        break;
                    }
                }
            }

            if (upIsChangeDiretion == null || !upIsChangeDiretion.Value)
            {
                for (int i = currentIndex; i + 1 < children.Count; i++)
                {
                    if (children[i + 1].LayerDepth < child.LayerDepth && (moveWithEqualDepth && children[i - 1].LayerDepth == child.LayerDepth))
                    {
                        var temp = children[i + 1];
                        children[i + 1] = child;
                        children[i] = temp;
                        childrenIndexes[temp] = i;
                        childrenIndexes[child] = i + 1;
                    }
                    else
                    {
                        break;
                    }
                }
            }
        }

        public async Task Draw(MyCanvas2DContext context)
        {
            await context.SaveAsync();
            await context.TranslateAsync(Position.X, Position.Y);
            await context.ScaleAsync(Scale.X, Scale.Y);
            var radians = Extensions.DegreesToRadians(Rotation);
            await context.RotateAsync(radians);
            await OverideDraw(context);
            //Children are drawn backwards so index 0 is in front
            for (int i = children.Count - 1; i >= 0; i--)
            {
                await children[i].Draw(context);
            }
            await context.RestoreAsync();
            //await context.RotateAsync(-radians);
            //await context.ScaleAsync(1 / Scale.X, 1 / Scale.Y);
            //await context.TranslateAsync(-translateVector.X, -translateVector.Y);
        }
        protected abstract Task OverideDraw(MyCanvas2DContext context);

        /// <summary>
        /// Shoul Only Be Called By Game Manager and Only Once
        /// </summary>
        /// <param name="mousePos"></param>
        /// <param name="mouseState"></param>
        /// <returns>If the mouse is blocked</returns>
        public Sprite GameManagerUpdate(Vector2 mousePos, MouseState mouseState, TimeSpan elapsedTime)
        {
            Sprite mouseOnSprite;
            mouseOnSprite = MouseInHitboxOrChildren(mousePos, mouseState, false).mouseOnSprite;

            OverideUpdate(mousePos, mouseState, elapsedTime);

            foreach (var child in children)
            {
                child.OverideUpdate(mousePos, mouseState, elapsedTime);
            }
            return mouseOnSprite;
        }
        protected virtual void OverideUpdate(Vector2 mousePos, MouseState mouseState, TimeSpan elapsedTime) { }

        (bool mouseOver, Sprite mouseOnSprite) MouseInHitboxOrChildren(Vector2 point, MouseState mouseState, bool mouseBlocked)
        {
            bool prevMouseOver = mouseOver;
            Sprite mouseOnSprite = null;
            mouseOver = false;
            foreach (var child in children)
            {
                var childInfo = child.MouseInHitboxOrChildren(point, mouseState, mouseBlocked);
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
        public static Sprite DeserializeSprite(TypeSerializableInfo<Sprite> info, ArrayWithOffset<byte> bytes)
        {
            var propertyBytes = GameSerialize.GetPropertyBytes(bytes);
            ObjectTypes objectType = (ObjectTypes)propertyBytes[objectTypeDataId][0];
            Sprite dataObject = GetDeafaultSprites[objectType]?.Invoke();
            return (Sprite)GameSerialize.CustomDeserialize(dataObject, propertyBytes);
        }
    }
    public class EmptySprite : Sprite
    {
        static EmptySprite()
        {
            GetDeafaultSprites.Add(ObjectTypes.EmptySprite, () => new EmptySprite());
            GameSerialize.AddType<EmptySprite>(GameSerialize.GenericSerializeFunc, GameSerialize.GenericDeserializeFunc, true, GameSerialize.CustomGenericDeserializeFunc);
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


}
