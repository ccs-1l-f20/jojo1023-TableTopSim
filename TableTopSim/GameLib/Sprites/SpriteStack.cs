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
    public class SpriteStack : Sprite
    {
        const ushort stackDataId = 50;
        [GameSerializableData(stackDataId)]
        public List<int> Stack { get; set; }
        CircleSprite countCircle = new CircleSprite(null, Vector2.Zero, 10, new Color(80, 80, 80));
        public TextSprite countText = new TextSprite(null, "0", new Color(255, 255, 255), "100px arial", Vector2.Zero, Vector2.Zero);
        static SpriteStack()
        {
            GameSerialize.AddType<SpriteStack>(GameSerialize.GenericSerializeFunc, GameSerialize.GenericDeserializeFunc, GameSerialize.CustomGenericDeserializeFunc);
        }
        public SpriteStack()
            : base(ObjectTypes.SpriteStack)
        {
            Stack = new List<int>();
        }
        public SpriteStack(SpriteRefrenceManager refManager, Vector2 position, int stackableIndex)
            : base(position, Vector2.One, 0, ObjectTypes.SpriteStack, refManager)
        {
            Stack = new List<int>();
            StackableIndex = stackableIndex;
        }
        public override async Task PreDrawUpdate(MyCanvas2DContext context)
        {
            countText.Text = Stack.Count.ToString();
            await countCircle.PreDrawUpdate(context);
            await countText.PreDrawUpdate(context);
        }

        protected override async Task OverrideDraw(MyCanvas2DContext context)
        {
            if (Stack.Count <= 0) { return; }

            Sprite s = refManager.GetSprite(Stack[Stack.Count - 1]);
            await ProtectedDraw(s, context);

            float maxTextLength = countCircle.Radius * countCircle.Transform.Scale.X * 2 - 10;
            countText.Transform.Scale = new Vector2((float)(maxTextLength / countText.TextWidth));
            await ProtectedDraw(countCircle, context);
            await ProtectedDraw(countText, context);
        }

        protected override bool PointInHitbox(Vector2 point, Matrix<float> glbMatrix)
        {
            if (Stack.Count <= 0) { return false; }
            Sprite s = refManager.GetSprite(Stack[Stack.Count - 1]);
            return ProtectedPointInHitbox(s, point, glbMatrix * s.Transform.GetMatrix());
        }
        public override (bool select, Sprite spriteToSelect) OnClick(bool isAlt)
        {
            if (Selectable)
            {
                if (isAlt)
                {
                    return (true, this);
                }
                if (Stack.Count > 0)
                {
                    Sprite s = PopTopStack();
                    return (true, s);
                }
            }
            return (false, null);
        }
        public override bool DroppedOn(int add, bool isAlt)
        {
            if (!isAlt && refManager.ContainsAddress(add))
            {
                Sprite s = refManager.GetSprite(add);
                if (s.StackableIndex != null && s.StackableIndex == StackableIndex)
                {
                    AddToStack(s, add);
                    return true;
                }
            }
            return false;
        }
        internal void AddToStack(Sprite sprite, int add)
        {
            int thisSpriteId = refManager.GetAddress(this);
            AddToStack(thisSpriteId, sprite, add);
        }
        public void AddToStack(int thisSpriteId, Sprite sprite, int add)
        {
            if (sprite.ObjectType == ObjectTypes.SpriteStack)
            {
                SpriteStack otherSpriteStack = (SpriteStack)sprite;
                for (int i = 0; i < otherSpriteStack.Stack.Count; i++)
                {
                    int innerAdd = otherSpriteStack.Stack[i];
                    if (refManager.ContainsAddress(innerAdd))
                    {
                        Sprite innerSprite = refManager.GetSprite(innerAdd);
                        innerSprite.Transform.Position = Vector2.Zero;
                        innerSprite.Transform.Rotation -= Transform.Rotation;
                        innerSprite.Parent = thisSpriteId;
                        innerSprite.Visiable = false;
                        Stack.Add(innerAdd);
                    }
                }
                otherSpriteStack.Visiable = false;

                if (otherSpriteStack.Stack.Count > 0)
                {
                    NotifyPropertyChanged(stackDataId);
                }
            }
            else
            {
                sprite.Transform.Position = Vector2.Zero;
                if (Stack.Count == 0)
                {
                    Transform.Rotation = sprite.Transform.Rotation;
                }
                sprite.Transform.Rotation -= Transform.Rotation;
                sprite.Parent = thisSpriteId;
                sprite.Visiable = false;
                Stack.Add(add);
                NotifyPropertyChanged(stackDataId);
            }
        }

        Sprite PopTopStack()
        {
            int add = Stack[Stack.Count - 1];
            Stack.RemoveAt(Stack.Count - 1);
            Sprite sprite = refManager.GetSprite(add);
            sprite.Transform.Position = Transform.Position;
            sprite.Transform.Rotation += Transform.Rotation;
            sprite.Parent = null;
            sprite.Visiable = true;
            if (Stack.Count <= 0)
            {
                Visiable = false;
            }

            NotifyPropertyChanged(stackDataId);
            return sprite;
        }

        public override void SelectedUpdate(KeyboardState keyboard)
        {
            if (keyboard.ContainsKeyCode("KeyS"))
            {
                var keyInfo = keyboard["KeyS"];
                if (keyInfo.Down && !keyInfo.LastDown)
                {
                    Random random = new Random();
                    List<int> newStack = new List<int>();
                    while(Stack.Count > 0)
                    {
                        int rnd = random.Next(0, Stack.Count);
                        newStack.Add(Stack[rnd]);
                        Stack.RemoveAt(rnd);
                    }
                    Stack = newStack;

                    NotifyPropertyChanged(stackDataId);
                }
            }
        }
    }
}
