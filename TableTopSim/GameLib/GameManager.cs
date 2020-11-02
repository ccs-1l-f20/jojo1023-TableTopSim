
using GameLib.Sprites;
using Microsoft.AspNetCore.Components.Web;
using MyCanvasLib;
using Newtonsoft.Json;
using System;
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
        EmptySprite gameSprite;
        Size size;
        public long Width => size.Width;
        public long Height => size.Height;
        //GameProgram gameProgram;
        public MouseState MouseState { get; private set; }
        public KeyboardState Keyboard { get; }
        public Sprite MouseOnSprite { get; private set; }
        public int PlayerId { get; }
        public int RoomId { get; }
        public SpriteRefrenceManager SpriteRefrenceManager;
        Random random = new Random();
        public GameManager(Size size, int playerId, int roomId)
        {
            PlayerId = playerId;
            RoomId = roomId;
            MouseOnSprite = null;
            Keyboard = new KeyboardState();
            gameSprite = new EmptySprite(Vector2.Zero, Vector2.One, Vector2.Zero, 0);
            SpriteRefrenceManager = new SpriteRefrenceManager();
            this.size = size;
            MouseState = MouseState.Hover;
            //gameProgram = new GameProgram(this);
        }
        public async Task Update(MyCanvas2DContext context, TimeSpan elapsedTime, CancellationToken ct)
        {
            MouseOnSprite = gameSprite.GameManagerUpdate(MousePos, MouseState, elapsedTime, SpriteRefrenceManager);
            OnUpdate?.Invoke(elapsedTime);
            await context.BeginBatchAsync();
            await context.SetFillStyleAsync(BackColor.ToString());
            await context.FillRectAsync(0, 0, Width, Height);
            await gameSprite.Draw(context, SpriteRefrenceManager);
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

        int GetNewSpriteAddress()
        {
            int address;
            do
            {
                address = random.Next();
            } while (SpriteRefrenceManager.SpriteRefrences.ContainsKey(address));
            return address;
        }
        public void AddSprite(Sprite sprite)
        {
            int spriteAddress = GetNewSpriteAddress();
            SpriteRefrenceManager.SpriteAddresses.Add(sprite, spriteAddress);
            SpriteRefrenceManager.SpriteRefrences.Add(spriteAddress, sprite);
            gameSprite.AddChild(sprite, SpriteRefrenceManager);
        }
        public void AddSprite(int sprite)
        {
            gameSprite.AddChild(SpriteRefrenceManager.SpriteRefrences[sprite], SpriteRefrenceManager);
        }
        public bool RemoveSprite(Sprite sprite)
        {
            if (gameSprite.RemoveChild(sprite, SpriteRefrenceManager))
            {
                int spriteAddress = SpriteRefrenceManager.SpriteAddresses[sprite];
                SpriteRefrenceManager.SpriteRefrences.Remove(spriteAddress);
                SpriteRefrenceManager.SpriteAddresses.Remove(sprite);
                return true;
            }
            return false;
        }
        public void ClearSprites()
        {
            gameSprite.ClearChildren(SpriteRefrenceManager);
        }
        public void MoveChildToFront(Sprite sprite)
        {
            gameSprite.MoveChildToFront(sprite, SpriteRefrenceManager);
        }
        public void MoveChildToBack(Sprite sprite)
        {
            gameSprite.MoveChildToBack(sprite, SpriteRefrenceManager);
        }

        public string JsonSerializeSprites()
        {
            return JsonConvert.SerializeObject(gameSprite);
        }
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
