
using GameLib.Sprites;
using MathNet.Numerics.LinearAlgebra;
using Microsoft.AspNetCore.Components.Web;
using MyCanvasLib;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Threading;
using System.Threading.Tasks;

namespace GameLib
{
    public class GameManager
    {
        public Vector2 MousePos { get; private set; }
        public event Action OnMouseUp;
        public event Action OnMouseDown;
        public event Action OnMouseMove;
        public event Action<KeyInfo> OnKeyUp;
        public event Action<KeyInfo> OnKeyDown;
        public event Action<TimeSpan> OnUpdate;
        public Color BackColor { get; set; } = new Color(255, 0, 0);
        public List<int> Sprites { get; private set; }
        Size size;
        public long Width => size.Width;
        public long Height => size.Height;
        //GameProgram gameProgram;
        public MouseState MouseState { get; private set; }
        public KeyboardState Keyboard { get; }
        public Sprite MouseOnSprite { get; private set; }
        public SpriteRefrenceManager SpriteRefrenceManager;

        Random random = new Random();
        public GameManager(Size size, SpriteRefrenceManager spriteRefrenceManager)
        {
            MouseOnSprite = null;
            Keyboard = new KeyboardState();
            //GameSprite = new EmptySprite(Vector2.Zero, Vector2.One, 0, spriteRefrenceManager);
            Sprites = new List<int>();
            SpriteRefrenceManager = spriteRefrenceManager;
            this.size = size;
            MouseState = MouseState.Hover;
            //gameProgram = new GameProgram(this);
        }
        public async Task Update(MyCanvas2DContext context, TimeSpan elapsedTime, CancellationToken ct)
        {
            OnUpdate?.Invoke(elapsedTime);

            Dictionary<int, Matrix<float>> spriteMatries = new Dictionary<int, Matrix<float>>();
            Dictionary<int, LayerDepth> spriteLayerDepths = new Dictionary<int, LayerDepth>();
            LayerDepth lastLd = null;
            bool reSort = false;
            bool mouseBlocked = false;
            MouseOnSprite = null;
            try
            {
                for (int i = 0; i < Sprites.Count; i++)
                {
                    var ad = Sprites[i];
                    var sprite = SpriteRefrenceManager.GetSprite(ad);
                    if (sprite.Update(MousePos, MouseState, mouseBlocked, elapsedTime, spriteMatries))
                    {
                        if (!mouseBlocked)
                        {
                            MouseOnSprite = sprite;
                            mouseBlocked = true;
                        }
                    }
                    LayerDepth currentLd = sprite.GetGlobalLayerDepth();
                    var test = currentLd.Layers.ToArray();
                    spriteLayerDepths.Add(ad, currentLd);
                    if (!reSort && lastLd != null && currentLd < lastLd)
                    {
                        reSort = true;
                    }
                    lastLd = currentLd;
                }
            }
            catch(Exception e)
            {
                string st = e.StackTrace;
            }
            if (reSort)
            {
                Sprites = Sprites.OrderBy(s => spriteLayerDepths[s]).ToList();
            }

            await context.BeginBatchAsync();
            await context.SetFillStyleAsync(BackColor.ToString());
            await context.FillRectAsync(0, 0, Width, Height);
            for(int i = Sprites.Count - 1; i >= 0; i--)
            {
                var sprite = SpriteRefrenceManager.GetSprite(Sprites[i]);
                await sprite.Draw(context, spriteMatries);
            }
            await context.EndBatchAsync();
        }


        public void MouseUp()
        {
            MouseState = MouseState.Hover;
            OnMouseUp?.Invoke();
        }

        public void MouseDown()
        {
            MouseState = MouseState.Down;
            OnMouseDown?.Invoke();
        }

        public void MouseMove(Vector2 mousePos)
        {
            MousePos = mousePos;
            OnMouseMove?.Invoke();
        }

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
