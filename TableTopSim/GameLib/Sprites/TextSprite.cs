using Blazor.Extensions.Canvas.Model;
using GameLib.GameSerialization;
using MathNet.Numerics.LinearAlgebra;
using MyCanvasLib;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace GameLib.Sprites
{
    public class TextSprite : Sprite
    {
        string text = "";
        [GameSerializableData(50)]
        public string Text { get => text; set { text = value; NotifyPropertyChanged(50); } }
        Color color;
        [GameSerializableData(51)]
        public Color Color { get => color; set { color = value; NotifyPropertyChanged(51); } }
        string font = "";
        [GameSerializableData(52)]
        public string Font { get => font; set { font = value; NotifyPropertyChanged(52); } }
        Vector2 origin;
        [GameSerializableData(53)]
        public Vector2 Origin { get => origin; set { origin = value; NotifyPropertyChanged(53); } }


        bool hasOutline = false;
        [GameSerializableData(54)]
        public bool HasOutline { get => hasOutline; set { hasOutline = value; NotifyPropertyChanged(54); } }

        Color outlineColor;
        [GameSerializableData(55)]
        public Color OutlineColor { get => outlineColor; set { outlineColor = value; NotifyPropertyChanged(55); } }

        float outlineWidth = 1;
        [GameSerializableData(56)]
        public float OutlineWidth { get => outlineWidth; set { outlineWidth = value; NotifyPropertyChanged(56); } }

        public double TextWidth { get; private set; }
        static TextSprite()
        {
            GameSerialize.AddType<TextSprite>(GameSerialize.GenericSerializeFunc, GameSerialize.GenericDeserializeFunc, GameSerialize.CustomGenericDeserializeFunc);
        }
        public TextSprite()
           : base(ObjectTypes.TextSprite)
        {
            Selectable = false;
        }
        public TextSprite(SpriteRefrenceManager refManager, string text, Color color, string font, Vector2 position, Vector2 origin, float rotation = 0)
            : base(position, Vector2.One, rotation, ObjectTypes.TextSprite, refManager)
        {
            Selectable = false;
            this.text = text;
            this.color = color;
            this.font = font;
            this.origin = origin;
            hasOutline = false;
        }
        protected override async Task OverrideDraw(MyCanvas2DContext context)
        {
            await context.SetTextAlignAsync(Blazor.Extensions.Canvas.Canvas2D.TextAlign.Center);
            await context.SetTextBaselineAsync(Blazor.Extensions.Canvas.Canvas2D.TextBaseline.Middle);
            await context.SetFillStyleAsync(Color.ToString());
            await context.SetFontAsync(Font);
            await context.FillTextAsync(Text, -Origin.X, -Origin.Y);
            if (hasOutline)
            {
                await context.SetLineWidthAsync(OutlineWidth);
                await context.SetStrokeStyleAsync(OutlineColor.ToString());
                await context.StrokeTextAsync(Text, -Origin.X, -Origin.Y);
            }
            await context.SetLineWidthAsync(1);
        }
        public override async Task PreDrawUpdate(MyCanvas2DContext context)
        {
            TextWidth = (await MeasureText(context)).Width;
        }
        
        protected override bool PointInHitbox(Vector2 point, Matrix<float> glbMatrix)
        {
            return false;
        }
        public async Task<TextMetrics> MeasureText(MyCanvas2DContext context)
        {
            await context.SetFontAsync(Font);
            var tm =  await context.MeasureTextAsync(Text);
            return tm;
        }
    }
}
