using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;

namespace GameLib
{
    public class StackableDataInfo
    {
        public bool ShowCount { get; set; } = true;
        public float CountRadius { get; set; } = 10;
        public Vector2 CountPosition { get; set; } = Vector2.Zero;
        public Color CountBackColor { get; set; } = new Color(80, 80, 80);
        public Color CountTextColor { get; set; } = new Color(255, 255, 255);
        public int? RotationMultiple { get; set; } = null;
        public bool Flippable { get; set; } = false;
        [JsonConstructor]
        public StackableDataInfo() { }
        public StackableDataInfo(Vector2 countPostion, float countRadius, int? rotationMultiple = null,  bool flipable= false) 
        {
            CountPosition = countPostion;
            CountRadius = countRadius;
            RotationMultiple = rotationMultiple;
            Flippable = flipable;
        }

    }
}
