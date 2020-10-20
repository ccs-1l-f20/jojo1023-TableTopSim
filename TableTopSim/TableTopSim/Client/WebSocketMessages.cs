using DataLayer;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.WebSockets;
using System.Runtime.InteropServices.ComTypes;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using TableTopSim.Shared;

namespace TableTopSim.Client
{
    
    public static class WebSocketMessages
    {
        public static async Task<PlayerAndRoomId> CreateRoom(MyClientWebSocket ws, string playerName)
        {
            if (playerName.Length > short.MaxValue)
            {
                return null;
            }
            WSMessageHelper helper = new WSMessageHelper(MessageType.CreateRoom, ws);

            var bytes = helper.StartBytes();
            MessageExtensions.AddStringBytes(bytes, playerName);

            await ws.SendMessageAsync(new ArraySegment<byte>(bytes.ToArray()));
            var response = await helper.GetResponse();
            if(response == null) { return null; }
            
            int playerId = MessageExtensions.GetNextInt(ref response);
            int roomId = MessageExtensions.GetNextInt(ref response);
            return new PlayerAndRoomId(playerId, roomId);
        }

        public static async Task<int?> JoinRoom(MyClientWebSocket ws, int roomId, string playerName)
        {
            if (playerName.Length > short.MaxValue)
            {
                return null;
            }
            WSMessageHelper helper = new WSMessageHelper(MessageType.JoinRoom, ws);

            var bytes = helper.StartBytes();
            MessageExtensions.AddIntBytes(bytes, roomId);
            MessageExtensions.AddStringBytes(bytes, playerName);
            await ws.SendMessageAsync(new ArraySegment<byte>(bytes.ToArray()));
            var response = await helper.GetResponse();
            if (response == null) { return null; }

            int playerId = MessageExtensions.GetNextInt(ref response);
            return playerId;
        }
    }

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
            MessageId = ws.GetMessageId(RecievedResponse);
        }
        public List<byte> StartBytes()
        {
            List<byte> bytes = new List<byte>();
            bytes.AddRange(BitConverter.GetBytes(MessageId));
            bytes.Add((byte)MessageType);
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
            MessageType responseMsgType = (MessageType)MessageExtensions.GetNextByte(ref response);
            bool responseError = MessageExtensions.GetNextBool(ref response);
            if (responseError || responseMsgType != MessageType) { return null; }
            return response;
        }
        void RecievedResponse(ArraySegment<byte> r)
        {
            response = r;
            cts.Cancel();
        }
    }
}
