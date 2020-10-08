using System;
using System.Collections.Generic;
using System.Text;

namespace GameLib
{
    public struct Color
    {
        public int R { get; set; }
        public int G { get; set; }
        public int B { get; set; }
        public Color(int r, int g, int b)
        {
            R = r;
            G = g;
            B = b;
        }
        public override string ToString()
        {
            return $"rgb({R}, {G}, {B})";
        }
    }
}
