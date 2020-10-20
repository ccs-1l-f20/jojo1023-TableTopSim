using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace TableTopSim.Server
{
    public class SocketHandler
    {
        private static SocketHandler instance;
        private static object instanceLock = new object();
        public List<WebSocket> WebSockets { get; set; }
        private SocketHandler()
        {
            WebSockets = new List<WebSocket>();
        }

        public static SocketHandler Get()
        {
            if (instance == null)
            {
                lock (instanceLock)
                {
                    if (instance == null)
                    {
                        instance = new SocketHandler();
                    }
                }
            }
            return instance;
        }

        public async Task StartWebsocket(HttpContext context, WebSocket webSocket)
        {
            WebSockets.Add(webSocket);
            var buffer = new byte[1024 * 4];
            WebSocketReceiveResult result = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
            while (!result.CloseStatus.HasValue)
            {
                string message = Encoding.UTF8.GetString(buffer, 0, result.Count);
                message += " It Worked!!";
                //await webSocket.SendAsync(new ArraySegment<byte>(Encoding.UTF8.GetBytes(message)), result.MessageType, result.EndOfMessage, CancellationToken.None);

                List<WebSocket> wsToRemove = new List<WebSocket>();
                foreach (var ws in WebSockets)
                {
                    if (ws.State != WebSocketState.Open)
                    {
                        wsToRemove.Add(ws);
                        continue;
                    }
                    await ws.SendAsync(new ArraySegment<byte>(Encoding.UTF8.GetBytes(message)), result.MessageType, result.EndOfMessage, CancellationToken.None);
                }
                foreach (var ws in wsToRemove)
                {
                    WebSockets.Remove(ws);
                }
                result = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
            }
            await webSocket.CloseAsync(result.CloseStatus.Value, result.CloseStatusDescription, CancellationToken.None);
        }
    }
}
