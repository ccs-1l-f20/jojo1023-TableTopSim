
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
        public event Action<TimeSpan, MouseState, MouseState> OnUpdate;
        public Color BackColor { get; set; } = new Color(255, 0, 0);
        public List<int> Sprites { get; private set; }
        Size size;
        public long Width => size.Width;
        public long Height => size.Height;
        //GameProgram gameProgram;
        public MouseState MouseState { get; private set; }
        public MouseState LastMouseState { get; private set; } = MouseState.Hover;
        public KeyboardState Keyboard { get; }
        public int? MouseOnSprite { get; private set; }
        public SpriteRefrenceManager SpriteRefrenceManager;

        public Transform BoardTransform;
        Random random = new Random();
        public GameManager(Size size, SpriteRefrenceManager spriteRefrenceManager, int playerId)
        {
            //Vector2 origin = new Vector2(size.Width / 2, size.Height / 2);
            BoardTransform = new Transform(Vector2.Zero,Vector2.One, 0, null);
            //var rotOrigin = Transform.TransformPoint((BoardTransform.GetMatrix()), origin);
            //BoardTransform.Position = origin - rotOrigin;

            MouseOnSprite = null;
            Keyboard = new KeyboardState();
            Sprites = new List<int>();
            SpriteRefrenceManager = spriteRefrenceManager;
            this.size = size;
            MouseState = MouseState.Hover;
        }
        public async Task Update(MyCanvas2DContext context, TimeSpan elapsedTime, CancellationToken ct, MouseState ms, Vector2 mousePos)
        {
            RawMousePos = mousePos;
            Matrix<float> boardTransform = BoardTransform.GetMatrix();
            var invBoardTransform = Transform.InverseTransformMatrix(boardTransform);
            MousePos = Transform.TransformPoint(invBoardTransform, mousePos);
            MouseState = ms;
            OnUpdate?.Invoke(elapsedTime, ms, LastMouseState);

            Dictionary<int, Matrix<float>> spriteMatries = new Dictionary<int, Matrix<float>>();
            Dictionary<int, LayerDepth> spriteLayerDepths = new Dictionary<int, LayerDepth>();
            LayerDepth lastLd = null;
            bool reSort = false;
            bool mouseBlocked = false;
            MouseOnSprite = null;
            for (int i = 0; i < Sprites.Count; i++)
            {
                var ad = Sprites[i];
                var sprite = SpriteRefrenceManager.GetSprite(ad);
                LayerDepth currentLd = sprite.GetGlobalLayerDepth();
                spriteLayerDepths.Add(ad, currentLd);
                if (!reSort && lastLd != null && currentLd < lastLd)
                {
                    reSort = true;
                }
                lastLd = currentLd;
            }

            if (reSort)
            {
                Sprites = Sprites.OrderBy(s => spriteLayerDepths[s]).ToList();
            }
            for (int i = 0; i < Sprites.Count; i++)
            {
                var ad = Sprites[i];
                var sprite = SpriteRefrenceManager.GetSprite(ad);
                if (sprite.Update(MousePos, MouseState, mouseBlocked, elapsedTime, spriteMatries))
                {
                    if (!mouseBlocked)
                    {
                        MouseOnSprite = ad;
                        mouseBlocked = true;
                    }
                }
            }


            await context.BeginBatchAsync();
            await context.SetFillStyleAsync(BackColor.ToString());
            await context.SaveAsync();
            await context.FillRectAsync(0, 0, Width, Height);
            await context.TransformAsync(boardTransform[0, 0], boardTransform[1, 0], boardTransform[0, 1], boardTransform[1, 1], boardTransform[0, 2], boardTransform[1, 2]);

            for (int i = Sprites.Count - 1; i >= 0; i--)
            {
                var sprite = SpriteRefrenceManager.GetSprite(Sprites[i]);
                await sprite.Draw(context, spriteMatries);
            }

            await context.RestoreAsync();
            await context.EndBatchAsync();
            LastMouseState = ms;
            Keyboard.StateUpdate();
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
