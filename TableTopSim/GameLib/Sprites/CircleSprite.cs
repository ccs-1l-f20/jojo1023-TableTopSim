using GameLib.GameSerialization;
using MathNet.Numerics.LinearAlgebra;
using MyCanvasLib;
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace GameLib.Sprites
{
    public class CircleSprite:Sprite
    {
        Color color;
        [GameSerializableData(50)]
        public Color Color { get => color; set { color = value; NotifyPropertyChanged(50); } }

        float radius;
        [GameSerializableData(51)]
        public float Radius { get => radius; set { radius = value; NotifyPropertyChanged(51); } }

        static CircleSprite()
        {
            GameSerialize.AddType<CircleSprite>(GameSerialize.GenericSerializeFunc, GameSerialize.GenericDeserializeFunc, GameSerialize.CustomGenericDeserializeFunc);
        }
        public CircleSprite()
           : base(ObjectTypes.CircleSprite)
        {

        }
        public CircleSprite(SpriteRefrenceManager refManager, Vector2 position, float radius, Color color)
            : base(position, Vector2.One, 0, ObjectTypes.CircleSprite, refManager)
        {
            this.color = color;
            this.radius = radius;
        }
        protected override async Task OverrideDraw(MyCanvas2DContext context)
        {
            await context.SetFillStyleAsync(Color.ToString());
            await context.BeginPathAsync();
            await context.ArcAsync(0, 0, radius, 0, Math.PI * 2);
            await context.FillAsync();
        }

        protected override bool PointInHitbox(Vector2 point, Matrix<float> glbMatrix)
        {
            return PointInCircle(glbMatrix, point, Radius);
        }
    }
}
