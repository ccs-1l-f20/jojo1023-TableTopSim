
using GameLib.Sprites;
using MathNet.Numerics.LinearAlgebra;
using Microsoft.AspNetCore.Components.Web;
using MyCanvasLib;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Numerics;
using System.Threading;
using System.Threading.Tasks;

namespace GameLib
{
    public class GameManager
    {
        public Vector2 MousePos { get; private set; }
        public Vector2 RawMousePos { get; private set; }
        //public event Action OnMouseUp;
        //public event Action OnMouseDown;
        //public event Action OnMouseMove;
        public event Action<KeyInfo> OnKeyUp;
        public event Action<KeyInfo> OnKeyDown;
        public event Action<TimeSpan, MouseState, MouseState, double> OnUpdate;
        public Color BackColor { get; set; } = new Color(255, 0, 0);
        List<int> pvtSprites;
        public List<int> Sprites { get => pvtSprites; private set { pvtSprites = value; } }
        List<int> pvtUiSprites;
        public List<int> UiSprites { get => pvtUiSprites; private set { pvtUiSprites = value; } }
        Size size;
        public long Width => size.Width;
        public long Height => size.Height;
        //GameProgram gameProgram;
        public MouseState MouseState { get; private set; }
        public MouseState LastMouseState { get; private set; } = MouseState.Hover;
        public KeyboardState Keyboard { get; }
        public int? MouseOnSprite { get; private set; }
        public int? MouseOnBehindSprite { get; private set; }
        public SpriteRefrenceManager SpriteRefrenceManager;
        public SpriteRefrenceManager UISpriteRefrenceManager;

        public Transform BoardTransform { get; set; }
        public Vector2 BoardTransformOrigin { get; set; }
        Random random = new Random();
        public GameManager(Size size, SpriteRefrenceManager spriteRefrenceManager, int playerId)
        {
            this.size = size;
            ResetTransform();

            MouseOnSprite = null;
            MouseOnBehindSprite = null;
            Keyboard = new KeyboardState();
            Sprites = new List<int>();
            UiSprites = new List<int>();
            SpriteRefrenceManager = spriteRefrenceManager;
            UISpriteRefrenceManager = new SpriteRefrenceManager(spriteRefrenceManager.ImageElementRefs, spriteRefrenceManager.ImageNotFound);

            MouseState = MouseState.Hover;
        }
        public void ResetTransform()
        {
            BoardTransformOrigin = new Vector2(size.Width / 2, size.Height / 2);
            BoardTransform = new Transform(BoardTransformOrigin, Vector2.One, 0, null);
        }
        public async Task Update(MyCanvas2DContext context, TimeSpan elapsedTime, CancellationToken ct, MouseState ms, Vector2 mousePos, double mouseWheelUpdate)
        {
            RawMousePos = mousePos;
            Matrix<float> boardTransform = BoardTransform.GetMatrix();
            var originMatrix = CreateMatrix.DenseIdentity<float>(3, 3);
            originMatrix[0, 2] = -BoardTransformOrigin.X;
            originMatrix[1, 2] = -BoardTransformOrigin.Y;
            var boardTransformWithOrigin = boardTransform * originMatrix;
            var invBoardTransform = Transform.InverseTransformMatrix(boardTransformWithOrigin);
            MousePos = Transform.TransformPoint(invBoardTransform, mousePos);
            MouseState = ms;
            OnUpdate?.Invoke(elapsedTime, ms, LastMouseState, mouseWheelUpdate);

            bool mouseBlocked = false;
            MouseOnSprite = null;
            MouseOnBehindSprite = null;
            var uiSpriteMatries = SortAndUpdateSprites(ref pvtUiSprites, UISpriteRefrenceManager, false, ref mouseBlocked, elapsedTime, RawMousePos, MouseState, context);
            var spriteMatries = SortAndUpdateSprites(ref pvtSprites, SpriteRefrenceManager, true, ref mouseBlocked, elapsedTime, MousePos, MouseState, context);

            //foreach(int i in pvtSprites)
            //{
            //    Sprite s = SpriteRefrenceManager.GetSprite(i);
            //    if(s is SpriteStack)
            //    {
            //        SpriteStack ss = (SpriteStack)s;
            //        var tm = await ss.countText.MeasureText(context);
            //        Debug.WriteLine("Width sdf: " + tm.Width);
            //    }
            //}

            for (int i = Sprites.Count - 1; i >= 0; i--)
            {
                int add = Sprites[i];
                Sprite sprite = SpriteRefrenceManager.GetSprite(add);
                await sprite.PreDrawUpdate(context);
            }

            for (int i = UiSprites.Count - 1; i >= 0; i--)
            {
                int add = UiSprites[i];
                Sprite sprite = UISpriteRefrenceManager.GetSprite(add);
                await sprite.PreDrawUpdate(context);
            }

            await context.BeginBatchAsync();
            await context.SetFillStyleAsync(BackColor.ToString());
            await context.SaveAsync();
            await context.FillRectAsync(0, 0, Width, Height);

            await context.TransformAsync(boardTransform[0, 0], boardTransform[1, 0], boardTransform[0, 1], boardTransform[1, 1], boardTransform[0, 2], boardTransform[1, 2]);
            await context.TranslateAsync(-BoardTransformOrigin.X, -BoardTransformOrigin.Y);

            for (int i = Sprites.Count - 1; i >= 0; i--)
            {
                int add = Sprites[i];
                Sprite sprite = SpriteRefrenceManager.GetSprite(add);
                await sprite.Draw(context, spriteMatries);
            }

            await context.RestoreAsync();
            await context.SaveAsync();

            for (int i = UiSprites.Count - 1; i >= 0; i--)
            {
                int add = UiSprites[i];
                Sprite sprite = UISpriteRefrenceManager.GetSprite(add);
                await sprite.Draw(context, uiSpriteMatries);
            }

            await context.RestoreAsync();
            await context.EndBatchAsync();
            LastMouseState = ms;
            Keyboard.StateUpdate();
        }
        public LayerDepth GetFrontLayerDepth(int ldLength)
        {
            SortSprites(ref pvtSprites, SpriteRefrenceManager);
            foreach(var s in pvtSprites)
            {
                Sprite sprite = SpriteRefrenceManager.GetSprite(s);
                if(sprite.LayerDepth.Layers.Count == ldLength)
                {
                    return sprite.LayerDepth;
                }
            }
            return null;
        }

        void SortSprites(ref List<int> sprites, SpriteRefrenceManager refManager)
        {
            Dictionary<int, LayerDepth> spriteLayerDepths = new Dictionary<int, LayerDepth>();
            LayerDepth lastLd = null;
            bool reSort = false;
            for (int i = 0; i < sprites.Count; i++)
            {
                var add = sprites[i];
                Sprite sprite;
                sprite = refManager.GetSprite(add);
                LayerDepth currentLd = sprite.GetGlobalLayerDepth();

                spriteLayerDepths.Add(add, currentLd);
                if (!reSort && lastLd != null && currentLd < lastLd)
                {
                    reSort = true;
                }
                lastLd = currentLd;
            }

            if (reSort)
            {
                sprites = sprites.OrderBy(s => spriteLayerDepths[s]).ToList();
            }
        }
        Dictionary<int, Matrix<float>> SortAndUpdateSprites(ref List<int> sprites, SpriteRefrenceManager refManager, bool setMouseOnSprite,
            ref bool mouseBlocked, TimeSpan elapsedTime, Vector2 mousePos, MouseState mouseState, MyCanvas2DContext context)
        {
            SortSprites(ref sprites, refManager);
            Dictionary<int, Matrix<float>> spriteMatries = new Dictionary<int, Matrix<float>>();
            for (int i = 0; i < sprites.Count; i++)
            {
                var add = sprites[i];
                Sprite sprite;
                sprite = refManager.GetSprite(add);
                if (sprite.Update(mousePos, mouseState, mouseBlocked, elapsedTime, spriteMatries))
                {
                    if (!mouseBlocked)
                    {
                        if (setMouseOnSprite)
                        {
                            MouseOnSprite = add;
                        }
                        mouseBlocked = true;
                    }
                    else if (setMouseOnSprite && MouseOnBehindSprite == null)
                    {
                        MouseOnBehindSprite = add;
                    }
                }
            }
            return spriteMatries;
        }
        struct IsUiSpriteAddress
        {
            public int Address { get; set; }
            public bool IsUi { get; set; }
            public IsUiSpriteAddress(int address, bool isUi)
            {
                Address = address;
                IsUi = isUi;
            }
        }

        //public void MouseUp()
        //{
        //    MouseState = MouseState.Hover;
        //    OnMouseUp?.Invoke();
        //}

        //public void MouseDown()
        //{
        //    MouseState = MouseState.Down;
        //    OnMouseDown?.Invoke();
        //}

        //public void MouseMove(Vector2 mousePos)
        //{
        //    MousePos = mousePos;
        //    OnMouseMove?.Invoke();
        //}

        public void KeyUp(KeyboardEventArgs args)
        {
            var info = Keyboard.KeyUp(args);
            OnKeyUp?.Invoke(info);
        }

        public void KeyDown(KeyboardEventArgs args)
        {
            var info = Keyboard.KeyDown(args);
            OnKeyDown?.Invoke(info);
        }
        public void AddSprite(int id, Sprite sprite)
        {
            if (!SpriteRefrenceManager.ContainsAddress(id))
            {
                SpriteRefrenceManager.AddSprite(id, sprite);
            }
            else
            {
                SpriteRefrenceManager.SpriteRefrences[id] = sprite;
                SpriteRefrenceManager.SpriteAddresses[sprite] = id;
            }
            Sprites.Add(id);
        }
        public void ClearSprites()
        {
            Sprites.Clear();
        }

        //int GetNewSpriteAddress()
        //{
        //    int address;
        //    do
        //    {
        //        address = random.Next();
        //    } while (SpriteRefrenceManager.ContainsAddress(address));
        //    return address;
        //}
        //public int AddSprite(Sprite sprite)
        //{
        //    int spriteAddress = GetNewSpriteAddress();
        //    SpriteRefrenceManager.AddSprite(spriteAddress, sprite);
        //    //SpriteRefrenceManager.SpriteAddresses.Add(sprite, spriteAddress);
        //    //SpriteRefrenceManager.SpriteRefrences.Add(spriteAddress, sprite);
        //    GameSprite.AddChild(sprite, SpriteRefrenceManager);
        //    return spriteAddress;
        //}
        //public void AddSprite(int sprite)
        //{
        //    GameSprite.AddChild(SpriteRefrenceManager.GetSprite(sprite), SpriteRefrenceManager);
        //}
        //public bool RemoveSprite(Sprite sprite)
        //{
        //    if (GameSprite.RemoveChild(sprite, SpriteRefrenceManager))
        //    {
        //        return SpriteRefrenceManager.RemoveSprite(sprite);
        //        //int spriteAddress = SpriteRefrenceManager.GetAddress(sprite);
        //        //SpriteRefrenceManager.SpriteRefrences.Remove(spriteAddress);
        //        //SpriteRefrenceManager.SpriteAddresses.Remove(sprite);
        //        //return true;
        //    }
        //    return false;
        //}

        //public void ClearSprites()
        //{
        //    GameSprite.ClearChildren(SpriteRefrenceManager);
        //}
        //public void MoveChildToFront(Sprite sprite)
        //{
        //    GameSprite.MoveChildToFront(sprite, SpriteRefrenceManager);
        //}
        //public void MoveChildToBack(Sprite sprite)
        //{
        //    GameSprite.MoveChildToBack(sprite, SpriteRefrenceManager);
        //}

    }

    public class Size
    {
        public long Width { get; set; }
        public long Height { get; set; }
        public Size(long width, long height)
        {
            Width = width;
            Height = height;
        }
    }

}
