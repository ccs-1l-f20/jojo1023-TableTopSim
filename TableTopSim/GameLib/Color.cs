using GameLib.GameSerialization;
using System;
using System.Collections.Generic;
using System.Text;

namespace GameLib
{
    public struct Color
    {
        [GameSerializableData(0)]
        public byte R { get; set; }

        [GameSerializableData(1)]
        public byte G { get; set; }

        [GameSerializableData(2)]
        public byte B { get; set; }
        static Color()
        {
            GameSerialize.AddType<Color>(Serialize, Deserialize);
        }
        public Color(byte r, byte g, byte b)
        {
            R = r;
            G = g;
            B = b;
        }
        public override string ToString()
        {
            return $"rgb({R}, {G}, {B})";
        }
        static void Serialize(Color c, TypeSerializableInfo<Color> info, List<byte> bytes)
        {
            bytes.Add(c.R);
            bytes.Add(c.G);
            bytes.Add(c.B);
        }
        static Color Deserialize(TypeSerializableInfo<Color> info, ArrayWithOffset<byte> bytes)
        {
            byte r = bytes[0];
            bytes.Offset++;
            byte g = bytes[0];
            bytes.Offset++;
            byte b = bytes[0];
            bytes.Offset++;
            return new Color(r, g, b);
        }
    }
}
