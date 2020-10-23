using System;
using System.Collections.Generic;
using System.Text;

namespace DataLayer
{
    public static class MessageExtensions
    {
        public static string GetNextString(this ArraySegment<byte> msg, int offset = 0)
        {
            ushort strLength = BitConverter.ToUInt16(msg.Slice(offset, 2));
            return Encoding.UTF8.GetString(msg.Slice(2 + offset, strLength));
        }
        public static int GetNextInt(ref ArraySegment<byte> msg)
        {
            int i = BitConverter.ToInt32(msg.Slice(0, 4));
            msg = msg.Slice(4);
            return i;
        }
        public static byte GetNextByte(ref ArraySegment<byte> msg)
        {
            byte b = msg[0];
            msg = msg.Slice(1);
            return b;
        }
        public static bool GetNextBool(ref ArraySegment<byte> msg)
        {
            byte b = msg[0];
            msg = msg.Slice(1);
            return b != 0;
        }
        public static void AddStringBytes(List<byte> bytes, string str)
        {
            if(str.Length > ushort.MaxValue)
            {
                str = str.Substring(0, ushort.MaxValue);
            }
            bytes.AddRange(BitConverter.GetBytes((ushort)str.Length));
            bytes.AddRange(Encoding.UTF8.GetBytes(str));
        }
        public static void AddIntBytes(List<byte> bytes, int i)
        {
            bytes.AddRange(BitConverter.GetBytes(i));
        }
    }
}
