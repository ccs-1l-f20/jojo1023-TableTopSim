using GameLib.GameSerialization;
using MathNet.Numerics.LinearAlgebra;
using Microsoft.AspNetCore.Components;
using MyCanvasLib;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace GameLib.Sprites
{
    public class ImageSprite : Sprite
    {
        public ElementReference Image { get; set; }
        public RectangleF? SourceRectangle { get; set; }
        [GameSerializableData(50)]
        public Vector2 Size { get; set; }
        public float Width { get { return Size.X; } set { Size = new Vector2(value, Size.Y); } }
        public float Height { get { return Size.Y; } set { Size = new Vector2(Size.X, value); } }


        Vector2 origin;
        [GameSerializableData(52)]
        public Vector2 Origin { get => origin; set { origin = value; NotifyPropertyChanged(52); } }
        static ImageSprite()
        {
            GameSerialize.AddType<ImageSprite>(GameSerialize.GenericSerializeFunc, GameSerialize.GenericDeserializeFunc, true, 
                GameSerialize.GenericDeserializeEditFunc, GameSerialize.CustomGenericDeserializeFunc);
        }
        public ImageSprite()
            :base(ObjectTypes.ImageSprite)
        {

        }
        public ImageSprite(SpriteRefrenceManager refManager, Vector2 position, ElementReference image, Vector2 size, Vector2 origin, 
            float rotation = 0, RectangleF? sourceRectangle = null)
            : base(position, Vector2.One, rotation, ObjectTypes.ImageSprite, refManager)
        {
            Image = image;
            SourceRectangle = sourceRectangle;
            Size = size;
            Origin = origin;
        }
        protected override async Task OverideDraw(MyCanvas2DContext context)
        {
            //await context.SetFillStyleAsync(Color.ToString());
            //await context.FillRectAsync(-Origin.X, -Origin.Y, Size.X, Size.Y);
            if (SourceRectangle == null)
            {
                await context.DrawImageAsync(Image, -Origin.X, -Origin.Y, Size.X, Size.Y);
            }
            else
            {
                await context.DrawImageAsync(Image,
                    SourceRectangle.Value.X, SourceRectangle.Value.Y, SourceRectangle.Value.Width, SourceRectangle.Value.Height,
                    -Origin.X, -Origin.Y, Size.X, Size.Y);
            }
        }

        protected override bool PointInHitbox(Vector2 point, Matrix<float> glbMatrix)
        {
            return PointInRotatedRect(glbMatrix, point, Size, Origin);
        }
    }
}

