﻿using DataLayer;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;
using TableTopSim.Shared;

namespace TableTopSim.Client
{
    public class GlobalData
    {
        public event Action<string> OnShowMessage;
        public event Func<Task> OnBrowserResize;
        public MyClientWebSocket WebSocket;
        public CancellationTokenSource WebSocketCts;
        public void ShowMessage(string message)
        {
            OnShowMessage?.Invoke(message);
        }
        public void Reset()
        {
            if (OnBrowserResize != null)
            {
                foreach (var d in OnBrowserResize.GetInvocationList())
                {
                    OnBrowserResize -= (d as Func<Task>);
                }
            }
        }
        public async Task BrowserResized()
        {
            if (OnBrowserResize != null)
            {
                await OnBrowserResize.Invoke();
            }
        }
        public MyClientWebSocket CreateWebSocket(Uri uri)
        {
            if(WebSocketCts != null) { WebSocketCts.Cancel(); }
            WebSocketCts = new CancellationTokenSource();
            if(WebSocket != null) { WebSocket.Close(); }
            WebSocket = new MyClientWebSocket(WebSocketCts, uri);
            return WebSocket;
        }

        public async Task<Player> ReconnectWebSocket(Uri uri, int playerId)
        {
            if (WebSocket == null)
            {
                CreateWebSocket(uri);
            }
            if (WebSocket.WebSocketState != WebSocketState.Open)
            {
                await WebSocket.Connect();
            }
            return await RoomDL.ReJoin(WebSocket, playerId);
        }
    }
}
