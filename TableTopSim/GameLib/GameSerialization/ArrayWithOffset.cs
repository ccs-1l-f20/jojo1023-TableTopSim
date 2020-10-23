using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;

namespace GameLib.GameSerialization
{
    public class ArrayWithOffset<T>
    {
        public T this[int i]
        {
            get => Array[i + Offset];
            set => Array[i + Offset] = value;
        }
        public T[] Array { get; private set; }
        public int Offset { get; set; }
        public int Count { get; set; }
        public ArrayWithOffset(T[] array, int offset = 0, int count = -1)
        {
            if(count < 0)
            {
                Count = array.Length;
            }
            Array = array;
            Offset = offset;
            Count = array.Length;
        }
        public ArrayWithOffset<T> Slice(int startIndex, int length = -1)
        {
            if(length < 0)
            {
                length = Count - (startIndex + Offset);
            }
            return new ArrayWithOffset<T>(Array, startIndex + Offset, length);
        }
    }
}
