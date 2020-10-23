using System;
using System.Collections.Generic;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace DataLayer
{
    public class MyClientWebSocket
    {
        public static readonly int ResponseWaitTime = 200000;
        public WebSocketState WebSocketState { get => webSocket.State; }
        ClientWebSocket webSocket;
        CancellationTokenSource cts;
        Uri uri;
        public event Action<ArraySegment<byte>> OnRecieved;
        Dictionary<MessageTypeId, Action<ArraySegment<byte>>> recievedMessageIdActions;
        long currentMessageId;
        object messageIdLock = new object();
        object messageIdActionLock = new object();
        public MyClientWebSocket(CancellationTokenSource cts, Uri uri)
        {
            recievedMessageIdActions = new Dictionary<MessageTypeId, Action<ArraySegment<byte>>>();
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
        public long GetMessageId(MessageType messageType, Action<ArraySegment<byte>> action)
        {
            long messageId;
            lock (messageIdLock)
            {
                messageId = currentMessageId;
                currentMessageId++;
            }
            MessageTypeId key = new MessageTypeId(messageType, messageId);
            lock (messageIdActionLock)
            {
                if (recievedMessageIdActions.ContainsKey(key))
                {
                    recievedMessageIdActions[key] = action;
                }
                else
                {
                    recievedMessageIdActions.Add(key, action);
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
            if (arrSeg.Count >= 9)
            {
                MessageType mt = (MessageType)arrSeg[0];
                long messageId = BitConverter.ToInt64(arrSeg.Slice(1, 8));

                MessageTypeId key = new MessageTypeId(mt, messageId);
                lock (messageIdActionLock)
                {
                    if (recievedMessageIdActions.ContainsKey(key))
                    {
                        recievedMessageIdActions[key]?.Invoke(arrSeg.Slice(9));
                        recievedMessageIdActions.Remove(key);
                    }
                }
            }
        }

        public void Close()
        {
            _ = webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Bye", CancellationToken.None);
        }
    }
    struct MessageTypeId
    {
        public MessageType MessageType { get; set; }
        public long MessageId { get; set; }
        public MessageTypeId(MessageType messageType, long messageId)
        {
            MessageType = messageType;
            MessageId = messageId;
        }
    }
}
