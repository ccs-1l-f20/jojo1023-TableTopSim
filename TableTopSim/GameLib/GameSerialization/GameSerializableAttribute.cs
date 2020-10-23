using System;
using System.Collections.Generic;
using System.Text;

namespace GameLib.GameSerialization
{
    [AttributeUsage(AttributeTargets.Property,
                       AllowMultiple = true)]
    public class GameSerializableDataAttribute : Attribute
    {
        //static ushort currentDataId = 0;
        //static object dataIdLockObject = new object();
        public ushort DataId { get; private set; }
        public bool GetOnly { get; private set; }
        public GameSerializableDataAttribute(ushort dataId, bool getOnly = false)
        {
            //lock (dataIdLockObject)
            //{
            //    DataId = currentDataId;
            //    currentDataId++;
            //}
            DataId = dataId;
            GetOnly = getOnly;
        }
    }
}
