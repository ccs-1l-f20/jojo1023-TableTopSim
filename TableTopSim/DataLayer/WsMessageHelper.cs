using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace DataLayer
{
    class WSMessageHelper
    {
        public MessageType MessageType { get; }
        CancellationTokenSource cts;
        ArraySegment<byte> response;
        public long MessageId { get; private set; }
        public WSMessageHelper(MessageType messageType, MyClientWebSocket ws)
        {
            MessageType = messageType;
            cts = new CancellationTokenSource();
            MessageId = ws.GetMessageId(messageType, RecievedResponse);
        }
        public List<byte> StartBytes()
        {
            List<byte> bytes = new List<byte>();
            bytes.Add((byte)MessageType);
            bytes.AddRange(BitConverter.GetBytes(MessageId));
            return bytes;
        }
        public async Task<(ArraySegment<byte> response, bool error)> GetResponse()
        {
            response = null;
            try
            {
                await Task.Delay(MyClientWebSocket.ResponseWaitTime, cts.Token);
            }
            catch (TaskCanceledException) { }
            if (response == null) { return (null, false); }
            bool responseError = MessageExtensions.GetNextBool(ref response);
            return (response, responseError);
        }
        void RecievedResponse(ArraySegment<byte> r)
        {
            response = r;
            cts.Cancel();
        }
    }
}

