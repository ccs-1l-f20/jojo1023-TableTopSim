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
        public async Task<ArraySegment<byte>> GetResponse()
        {
            response = null;
            try
            {
                await Task.Delay(MyClientWebSocket.ResponseWaitTime, cts.Token);
            }
            catch (TaskCanceledException e) { }
            if (response == null) { return null; }
            bool responseError = MessageExtensions.GetNextBool(ref response);
            if (responseError) { return null; }
            return response;
        }
        void RecievedResponse(ArraySegment<byte> r)
        {
            response = r;
            cts.Cancel();
        }
    }
}

