using Blazor.Extensions.Canvas.Canvas2D;
using GameLib.Sprites;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading;

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
        GameProgram gameProgram;
        public MouseState MouseState { get; private set; }
        public ElementReferences ElementReferences { get; }
        public KeyboardState Keyboard { get; }
        public Sprite MouseOnSprite { get; private set; }
        public int PlayerId { get; }
        public int RoomId { get; }
        public GameManager(Size size, ElementReferences elementReferences,  int playerId, int roomId)
        {
            PlayerId = playerId;
            RoomId = roomId;
            MouseOnSprite = null;
            Keyboard = new KeyboardState();
            ElementReferences = elementReferences;
            gameSprite = new EmptySprite(Vector2.Zero, Vector2.One, Vector2.Zero, 0);
            this.size = size;
            MouseState = MouseState.Hover;
            gameProgram = new GameProgram(this);
        }
        public async void Update(Canvas2DContext context, TimeSpan elapsedTime, CancellationToken ct)
        {
            MouseOnSprite = gameSprite.GameManagerUpdate(MousePos, MouseState, elapsedTime);
            OnUpdate?.Invoke(elapsedTime);
            await context.BeginBatchAsync();
            await context.SetFillStyleAsync(BackColor.ToString());
            await context.FillRectAsync(0, 0, Width, Height);
            await gameSprite.Draw(context);
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

        public void AddSprite(Sprite sprite)
        {
            gameSprite.AddChild(sprite);
        }
        public bool RemoveSprite(Sprite sprite)
        {
            return gameSprite.RemoveChild(sprite);
        }

        public void MoveChildToFront(Sprite sprite)
        {
            gameSprite.MoveChildToFront(sprite);
        }
        public void MoveChildToBack(Sprite sprite)
        {
            gameSprite.MoveChildToBack(sprite);
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
    public class ElementReferences
    {
        public ElementReference CardBack;
    }

}
