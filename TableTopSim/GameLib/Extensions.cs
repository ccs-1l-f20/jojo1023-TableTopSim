using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Numerics;
using System.Runtime;
using System.Threading.Tasks;

namespace GameLib
{
    public static class Extensions
    {
        public static float DegreesToRadians(float degrees)
        {
            return (float)(degrees * Math.PI / 180);
        }
        public static float RadiansToDegrees(float radians)
        {
            return (float)(radians * 180 / Math.PI);
        }

        public static float GetPositiveRotation(float rotation)
        {
            rotation %= 360;
            if(rotation < 0)
            {
                rotation += 360;
            }
            return rotation;
        }

        public static Vector2 ToVector2(this PointF pointF)
        {
            return new Vector2(pointF.X, pointF.Y);
        }
        public static Vector2 ToVector2(this SizeF sizeF)
        {
            return new Vector2(sizeF.Width, sizeF.Height);
        }
    }
    
}
