using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace TableTopSim.Client
{
    public class MyClientWebSocket
    {
        public static readonly int ResponseWaitTime = 200000;
        ClientWebSocket webSocket;
        CancellationTokenSource cts;
        Uri uri;
        public event Action<ArraySegment<byte>> OnRecieved;
        Dictionary<long, Action<ArraySegment<byte>>> recievedMessageIdActions;
        long currentMessageId;
        object messageIdLock = new object();
        object messageIdActionLock = new object();
        public MyClientWebSocket(CancellationTokenSource cts, Uri uri)
        {
            recievedMessageIdActions = new Dictionary<long, Action<ArraySegment<byte>>>();
            currentMessageId = 1;
            webSocket = new ClientWebSocket();
            this.cts = cts;
            this.uri = uri;
        }
        public async Task Connect()
        {
            await webSocket.ConnectAsync(uri, cts.Token);
            _ = ReceiveLoop();
        }
        public long GetMessageId(Action<ArraySegment<byte>> action)
        {
            long messageId;
            lock (messageIdLock)
            {
                messageId = currentMessageId;
                currentMessageId++;
            }
            lock (messageIdActionLock)
            {
                if (recievedMessageIdActions.ContainsKey(messageId))
                {
                    recievedMessageIdActions[messageId] = action;
                }
                else
                {
                    recievedMessageIdActions.Add(messageId, action);
                }
            }
            return messageId;
        }
        public async Task SendMessageAsync(ArraySegment<byte> message)
        {
            await webSocket.SendAsync(message, WebSocketMessageType.Binary, true, cts.Token);
        }

        async Task ReceiveLoop()
        {
            var buffer = new ArraySegment<byte>(new byte[1024 * 4]);
            while (!cts.IsCancellationRequested)
            {
                var received = await webSocket.ReceiveAsync(buffer, cts.Token);
                var arrSeg = new ArraySegment<byte>(buffer.Array, 0, received.Count);
                OnRecivedMessageId(arrSeg);
                OnRecieved?.Invoke(arrSeg);
            }
        }
        void OnRecivedMessageId(ArraySegment<byte> arrSeg)
        {
            if (arrSeg.Count >= 8)
            {
                long messageId = BitConverter.ToInt64(arrSeg.Slice(0, 8));
                lock (messageIdActionLock)
                {
                    if (recievedMessageIdActions.ContainsKey(messageId))
                    {
                        recievedMessageIdActions[messageId]?.Invoke(arrSeg.Slice(8));
                        recievedMessageIdActions.Remove(messageId);
                    }
                }
            }
        }

        public void Close()
        {
            _ = webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Bye", CancellationToken.None);
        }
    }
}
