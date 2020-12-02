using GameLib.GameSerialization;
using MathNet.Numerics.LinearAlgebra;
using MyCanvasLib;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace GameLib.Sprites
{
    public class FlippableSprite : Sprite
    {
        const ushort frontSpriteDataId = 50;
        const ushort backSpriteDataId = 51;
        Sprite frontSprite;
        [GameSerializableData(frontSpriteDataId)]
        [JsonConverter(typeof(SpriteJsonConverter))]
        public Sprite FrontSprite { get => frontSprite; set { frontSprite = value; NotifyPropertyChanged(frontSpriteDataId); } }
        Sprite backSprite;
        [GameSerializableData(backSpriteDataId)]
        [JsonConverter(typeof(SpriteJsonConverter))]
        public Sprite BackSprite { get => backSprite; set { backSprite = value; NotifyPropertyChanged(backSpriteDataId); } }
        bool isFront = true;
        [GameSerializableData(52)]
        public bool IsFront { get => isFront; set { isFront = value; NotifyPropertyChanged(52); } }
        static FlippableSprite()
        {
            GameSerialize.AddType<FlippableSprite>(GameSerialize.GenericSerializeFunc, GameSerialize.GenericDeserializeFunc, GameSerialize.CustomGenericDeserializeFunc);
        }
        public FlippableSprite()
           : base(ObjectTypes.FlippableSprite)
        {

        }
        public FlippableSprite(SpriteRefrenceManager refManager, Vector2 position, bool isFront, Sprite frontSprite, Sprite backSprite)
            : base(position, Vector2.One, 0, ObjectTypes.FlippableSprite, refManager)
        {
            this.isFront = isFront;
            this.frontSprite = frontSprite;
            this.backSprite = backSprite;
        }
        public override void SetRefManager(SpriteRefrenceManager refManager)
        {
            base.SetRefManager(refManager);
            if (frontSprite != null)
            {
                frontSprite.SetRefManager(refManager);
                frontSprite.OnPropertyChanged -= FrontSprite_OnPropertyChanged;
                frontSprite.OnPropertyChanged += FrontSprite_OnPropertyChanged;
            }
            if (backSprite != null)
            {
                backSprite.SetRefManager(refManager);
                backSprite.OnPropertyChanged -= BackSprite_OnPropertyChanged;
                backSprite.OnPropertyChanged += BackSprite_OnPropertyChanged;
            }
        }

        private void BackSprite_OnPropertyChanged(Sprite arg1, List<int> arg2)
        {
            arg2.Insert(0, backSpriteDataId);
            InvokePropertyChanged(arg2);
        }

        private void FrontSprite_OnPropertyChanged(Sprite arg1, List<int> arg2)
        {
            arg2.Insert(0, frontSpriteDataId);
            InvokePropertyChanged(arg2);
        }

        protected override async Task OverrideDraw(MyCanvas2DContext context)
        {
            if (isFront)
            {
                if (frontSprite != null)
                {
                    await ProtectedDraw(frontSprite, context);
                }
            }
            else
            {
                if (backSprite != null)
                {
                    await ProtectedDraw(backSprite, context);
                }
            }
        }

        protected override bool PointInHitbox(Vector2 point, Matrix<float> glbMatrix)
        {
            if (isFront)
            {
                return frontSprite != null &&
                    ProtectedPointInHitbox(frontSprite, point, glbMatrix * frontSprite.Transform.GetMatrix());
            }
            else
            {
                return backSprite != null &&
                    ProtectedPointInHitbox(backSprite, point, glbMatrix * backSprite.Transform.GetMatrix());
            }
        }

        public override void SelectedUpdate(KeyboardState keyboard)
        {
            if (keyboard.ContainsKeyCode("KeyX"))
            {
                var keyInfo = keyboard["KeyX"];
                if(keyInfo.Down && !keyInfo.LastDown)
                {
                    IsFront = !isFront;
                }
            }
        }
    }
}
