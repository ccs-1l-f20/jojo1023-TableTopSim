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
        int imageRef;
        [GameSerializableData(50)]
        public int ImageRef { get => imageRef; set { imageRef = value; NotifyPropertyChanged(50); } }

        Vector2 size;
        [GameSerializableData(51)]
        public Vector2 Size { get => size; set { size = value; NotifyPropertyChanged(51); } }
        public float Width { get { return Size.X; } set { Size = new Vector2(value, Size.Y); } }
        public float Height { get { return Size.Y; } set { Size = new Vector2(Size.X, value); } }



        Vector2 origin;
        [GameSerializableData(52)]
        public Vector2 Origin { get => origin; set { origin = value; NotifyPropertyChanged(52); } }


        RectangleF? sourceRectangle = null;
        [GameSerializableData(53)]
        public RectangleF? SourceRectangle { get => sourceRectangle; set { sourceRectangle = value; NotifyPropertyChanged(53); } }
        static ImageSprite()
        {
            GameSerialize.AddType<ImageSprite>(GameSerialize.GenericSerializeFunc, GameSerialize.GenericDeserializeFunc, GameSerialize.CustomGenericDeserializeFunc);
        }
        public ImageSprite()
            :base(ObjectTypes.ImageSprite)
        {

        }
        public ImageSprite(SpriteRefrenceManager refManager, Vector2 position, int imageRef, Vector2 size, Vector2 origin, 
            float rotation = 0, RectangleF? sourceRectangle = null)
            : base(position, Vector2.One, rotation, ObjectTypes.ImageSprite, refManager)
        {
            this.imageRef = imageRef;
            this.sourceRectangle = sourceRectangle;
            this.size = size;
            this.origin = origin;
        }
        protected override async Task OverideDraw(MyCanvas2DContext context)
        {
            //await context.SetFillStyleAsync(Color.ToString());
            //await context.FillRectAsync(-Origin.X, -Origin.Y, Size.X, Size.Y);
            bool containsRef = refManager.ImageElementRefs.ContainsKey(imageRef);
            ElementReference image = refManager.ImageNotFound;
            if (containsRef) { image = refManager.ImageElementRefs[imageRef]; }
            if (SourceRectangle == null || !containsRef)
            {
                await context.DrawImageAsync(image, -Origin.X, -Origin.Y, Size.X, Size.Y);
            }
            else
            {
                await context.DrawImageAsync(image,
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

