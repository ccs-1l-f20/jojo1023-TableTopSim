using GameLib.GameSerialization;
using MyCanvasLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;

namespace GameLib.Sprites
{
    public class RectSprite : Sprite
    {
        [GameSerializableData(50)]
        public Color Color { get; set; }

        [GameSerializableData(51)]
        public Vector2 Size { get; set; }
        public float Width { get { return Size.X; } set { Size = new Vector2(value, Size.Y); } }
        public float Height { get { return Size.Y; } set { Size = new Vector2(Size.X, value); } }

        static RectSprite()
        {
            GetDeafaultSprites.Add(ObjectTypes.RectSprite, (() => new RectSprite(), typeof(RectSprite)));
            GameSerialize.AddType<RectSprite>(GameSerialize.GenericSerializeFunc, GameSerialize.GenericDeserializeFunc, true, 
                GameSerialize.GenericDeserializeEditFunc, GameSerialize.CustomGenericDeserializeFunc);
        }
        public RectSprite()
            : base(ObjectTypes.RectSprite)
        {

        }
        public RectSprite(Vector2 position, Vector2 size, Color color, Vector2 origin, float rotation = 0)
            : base(position, Vector2.One, origin, rotation, ObjectTypes.RectSprite)
        {
            Color = color;
            Size = size;
        }
        protected override async Task OverideDraw(MyCanvas2DContext context)
        {
            await context.SetFillStyleAsync(Color.ToString());
            await context.FillRectAsync(-Origin.X, -Origin.Y, Size.X, Size.Y);
        }

        protected override bool PointInHitbox(Vector2 point)
        {
            return PointInRotatedRect(point, Size);
        }
    }
}
