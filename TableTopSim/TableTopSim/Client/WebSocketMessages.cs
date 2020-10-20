using DataLayer;
using System;
using System.Collections.Generic;
using System.Linq;
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
            var msgType = MessageType.CreateRoom;
            if (playerName.Length > short.MaxValue)
            {
                return null;
            }
            CancellationTokenSource tempCts = new CancellationTokenSource();
            ArraySegment<byte> response = null;

            long messageId = ws.GetMessageId(RecievedResponse);
            List<byte> bytes = new List<byte>();
            bytes.AddRange(BitConverter.GetBytes(messageId));
            bytes.Add((byte)msgType);
            MessageExtensions.AddStringBytes(bytes, playerName);

            await ws.SendMessageAsync(new ArraySegment<byte>(bytes.ToArray()));
            try
            {
                await Task.Delay(MyClientWebSocket.ResponseWaitTime, tempCts.Token);
            }
            catch (TaskCanceledException e) { }
            byte[] testArray1 = response.ToArray();
            if (response == null 
                && MessageExtensions.GetNextByte(ref response) == (byte)msgType
                && MessageExtensions.GetNextBool(ref response) && !MessageExtensions.GetNextBool(ref response))
            {
                return null;
            }
            byte[] testArrat = response.ToArray();
            int playerId = MessageExtensions.GetNextInt(ref response);
            int roomId = MessageExtensions.GetNextInt(ref response);
            return new PlayerAndRoomId(playerId, roomId);
            void RecievedResponse(ArraySegment<byte> r)
            {
                response = r;
                tempCts.Cancel();
            }
        }
    }
}
