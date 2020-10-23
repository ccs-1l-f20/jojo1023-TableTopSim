using Blazor.Extensions.Canvas.WebGL;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Numerics;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;

namespace GameLib.GameSerialization
{

    public static class GameSerialize
    {
        static Dictionary<Type, TypeSerializableInfo> typeProperties = new Dictionary<Type, TypeSerializableInfo>();
        static Dictionary<Type, SerialzeDataFunc> typeSerializeFuncs = new Dictionary<Type, SerialzeDataFunc>();
        static GameSerialize()
        {
            AddType<int>((o, info, bytes) => bytes.AddRange(BitConverter.GetBytes(o)),
                (info, bytes) => { bytes.Offset += 4; return BitConverter.ToInt32(bytes.Array, bytes.Offset - 4); }, false);

            AddType<float>((o, info, bytes) => bytes.AddRange(BitConverter.GetBytes(o)),
                (info, bytes) => { bytes.Offset += 4; return BitConverter.ToSingle(bytes.Array, bytes.Offset - 4); }, false);
            AddType<double>((o, info, bytes) => bytes.AddRange(BitConverter.GetBytes(o)),
                (info, bytes) => { bytes.Offset += 8; return BitConverter.ToDouble(bytes.Array, bytes.Offset - 8); }, false);

            AddType<ushort>((o, info, bytes) => bytes.AddRange(BitConverter.GetBytes(o)),
                (info, bytes) => { bytes.Offset += 2; return BitConverter.ToUInt16(bytes.Array, bytes.Offset - 2); }, false);

            AddType<byte>((o, info, bytes) => bytes.Add(o),
                (info, bytes) => { bytes.Offset++; return bytes[-1]; }, false);

            AddType<bool>((o, info, bytes) => bytes.Add((byte)(o ? 255 : 0)),
                (info, bytes) => { bytes.Offset++; return bytes[-1] == 0 ? false : true; }, false);

            AddType<Vector2>((v, info, bytes) => { bytes.AddRange(BitConverter.GetBytes(v.X)); bytes.AddRange(BitConverter.GetBytes(v.Y)); },
                (info, bytes) =>
                {
                    float x = BitConverter.ToSingle(bytes.Array, bytes.Offset);
                    bytes.Offset += 4;
                    float y = BitConverter.ToSingle(bytes.Array, bytes.Offset);
                    bytes.Offset += 4;
                    return new Vector2(x, y);
                }, false);
        }

        public static void AddType<T>(Action<T, TypeSerializableInfo<T>, List<byte>> serializeFunc,
           Func<TypeSerializableInfo<T>, ArrayWithOffset<byte>, T> deserializeFunc,
           bool getProperties = true,
           Func<T, TypeSerializableInfo<T>, Dictionary<ushort, ArrayWithOffset<byte>>, T> customDeserializeFunc = null)
        {
            Type t = typeof(T);
            if (!typeSerializeFuncs.ContainsKey(t))
            {
                typeSerializeFuncs.Add(t, new SerialzeDataFunc<T>(serializeFunc, deserializeFunc, customDeserializeFunc));
                if (!getProperties) { return; }
                TypeSerializableInfo<T> typeSerializableInfo = new TypeSerializableInfo<T>();
                var properties = t.GetProperties();
                var typeInstance = Expression.Parameter(t);
                foreach (var p in properties)
                {
                    var att = p.GetCustomAttribute<GameSerializableDataAttribute>();
                    if (att != null)
                    {
                        var pExpres = Expression.Property(typeInstance, p);
                        Action<T, object> setFunc = null;
                        if (!att.GetOnly)
                        {
                            var setParam = Expression.Parameter(typeof(object));
                            var setExpres = Expression.Assign(pExpres, Expression.Convert(setParam, pExpres.Type));
                            setFunc = Expression.Lambda<Action<T, object>>(setExpres, typeInstance, setParam).Compile();
                        }
                        Func<T, object> getFunc = Expression.Lambda<Func<T, object>>(Expression.Convert(pExpres, typeof(object)), typeInstance).Compile();
                        var propertyData = new PropertyData<T>(p, getFunc, setFunc, att.DataId);
                        typeSerializableInfo.PropertiesDict.Add(att.DataId, propertyData);
                    }
                }
                typeProperties.Add(t, typeSerializableInfo);
            }
        }
        public static List<byte> SerializeGameData(object data)
        {
            List<byte> bytes = new List<byte>();
            SerializeGameData(data, bytes);
            return bytes;
        }
        public static void SerializeGameData(object data, List<byte> bytes)
        {
            Type t = data.GetType();
            TypeSerializableInfo info = null;
            if (t.IsEnum)
            {
                t = Enum.GetUnderlyingType(t);
                data = Convert.ChangeType(data, t);
            }
            if (typeProperties.ContainsKey(t))
            {
                info = typeProperties[t];
            }
            if (typeSerializeFuncs.ContainsKey(t))
            {
                typeSerializeFuncs[t]?.Serialize(data, info, bytes);
            }
        }
        public static T DeserializeGameData<T>(byte[] bytes)
        {
            Type t = typeof(T);
            return (T)DeserializeGameData(t, new ArrayWithOffset<byte>(bytes));
        }
        static object DeserializeGameData(Type t, ArrayWithOffset<byte> bytes)
        {
            TypeSerializableInfo info = null;
            if (t.IsEnum)
            {
                t = Enum.GetUnderlyingType(t);
            }
            if (typeProperties.ContainsKey(t))
            {
                info = typeProperties[t];
            }
            if (typeSerializeFuncs.ContainsKey(t))
            {
                return typeSerializeFuncs[t].Deserialize(info, bytes);
            }
            else
            {
                throw new NullReferenceException();
            }
        }

        internal static object CustomDeserialize(object dataObject, Dictionary<ushort, ArrayWithOffset<byte>> propertyBytes)
        {
            TypeSerializableInfo info = null;
            Type t = dataObject.GetType();
            if (t.IsEnum)
            {
                t = Enum.GetUnderlyingType(t);
            }
            if (typeProperties.ContainsKey(t))
            {
                info = typeProperties[t];
            }
            if (typeSerializeFuncs.ContainsKey(t))
            {
                return typeSerializeFuncs[t].CustomDeserialize(dataObject, info, propertyBytes);
            }
            else
            {
                throw new NullReferenceException();
            }
        }
        public static void GenericSerializeFunc<T>(T data, TypeSerializableInfo<T> info, List<byte> bytes)
        {
            int startIndex = bytes.Count;
            bytes.Add(0);
            bytes.Add(0);
            if (info != null)
            {
                foreach (var p in info.PropertiesDict.Values)
                {
                    bytes.AddRange(BitConverter.GetBytes(p.DataId));
                    int pStartIndex = bytes.Count;
                    bytes.Add(0);
                    bytes.Add(0);

                    SerializeGameData(p.GetData.Invoke(data), bytes);

                    ushort pLength = (ushort)(bytes.Count - pStartIndex - 2);
                    byte[] pLengthBytes = BitConverter.GetBytes(pLength);
                    bytes[pStartIndex] = pLengthBytes[0];
                    bytes[pStartIndex + 1] = pLengthBytes[1];
                }
            }
            ushort length = (ushort)(bytes.Count - startIndex - 2);
            byte[] lengthBytes = BitConverter.GetBytes(length);
            bytes[startIndex] = lengthBytes[0];
            bytes[startIndex + 1] = lengthBytes[1];
        }

        public static T GenericDeserializeFunc<T>(TypeSerializableInfo<T> info, ArrayWithOffset<byte> bytes)
        {
            var data = GetPropertyBytes(bytes);
            return CustomGenericDeserializeFunc(default(T), info, data);
        }
        public static T CustomGenericDeserializeFunc<T>(T dataObject, TypeSerializableInfo<T> info, Dictionary<ushort, ArrayWithOffset<byte>> data)
        {
            if (dataObject == null)
            {
                dataObject = (T)(typeof(T).GetConstructor(new Type[0]).Invoke(new object[0]));
            }
            foreach (var dataId in data.Keys)
            {
                ArrayWithOffset<byte> pBytes = data[dataId];

                var propertyData = info.PropertiesDict[dataId];
                if (propertyData.SetData != null)
                {
                    object propertyVal = DeserializeGameData(propertyData.PropertyInfo.PropertyType, pBytes);
                    propertyData.SetData.Invoke(dataObject, propertyVal);
                }
            }
            return dataObject;
        }
        public static Dictionary<ushort, ArrayWithOffset<byte>> GetPropertyBytes(ArrayWithOffset<byte> bytes)
        {
            ushort length = BitConverter.ToUInt16(bytes.Array, bytes.Offset);
            bytes.Offset += 2;
            int startOffset = bytes.Offset;
            Dictionary<ushort, ArrayWithOffset<byte>> data = new Dictionary<ushort, ArrayWithOffset<byte>>();

            while (bytes.Offset - startOffset < length)
            {
                ushort dataId = BitConverter.ToUInt16(bytes.Array, bytes.Offset);
                bytes.Offset += 2;
                ushort pLength = BitConverter.ToUInt16(bytes.Array, bytes.Offset);
                bytes.Offset += 2;
                data.Add(dataId, bytes.Slice(0, pLength));
                bytes.Offset += pLength;
            }
            return data;
        }

        abstract class SerialzeDataFunc
        {
            public abstract void Serialize(object value, TypeSerializableInfo typeInfo, List<byte> bytes);
            public abstract object Deserialize(TypeSerializableInfo typeInfo, ArrayWithOffset<byte> bytes);
            public abstract object CustomDeserialize(object dataObject, TypeSerializableInfo typeInfo, Dictionary<ushort, ArrayWithOffset<byte>> propertyBytes);
        }
        class SerialzeDataFunc<T> : SerialzeDataFunc
        {
            Action<T, TypeSerializableInfo<T>, List<byte>> serializeFunc;
            Func<TypeSerializableInfo<T>, ArrayWithOffset<byte>, T> deserializeFunc;
            Func<T, TypeSerializableInfo<T>, Dictionary<ushort, ArrayWithOffset<byte>>, T> customDeserializeFunc;
            public SerialzeDataFunc(Action<T, TypeSerializableInfo<T>, List<byte>> serializeFunc,
                Func<TypeSerializableInfo<T>, ArrayWithOffset<byte>, T> deserializeFunc,
                Func<T, TypeSerializableInfo<T>, Dictionary<ushort, ArrayWithOffset<byte>>, T> customDeserializeFunc = null)
            {
                this.serializeFunc = serializeFunc;
                this.deserializeFunc = deserializeFunc;
                this.customDeserializeFunc = customDeserializeFunc;
            }
            public void Serialize(T value, TypeSerializableInfo<T> typeInfo, List<byte> bytes)
            {
                serializeFunc?.Invoke(value, typeInfo, bytes);
            }
            public override void Serialize(object value, TypeSerializableInfo typeInfo, List<byte> bytes)
            {
                Serialize((T)value, (TypeSerializableInfo<T>)typeInfo, bytes);
            }


            public T Deserialize(TypeSerializableInfo<T> typeInfo, ArrayWithOffset<byte> bytes)
            {
                return deserializeFunc.Invoke(typeInfo, bytes);
            }
            public override object Deserialize(TypeSerializableInfo typeInfo, ArrayWithOffset<byte> bytes)
            {
                return Deserialize((TypeSerializableInfo<T>)typeInfo, bytes);
            }

            public T CustomDeserialize(T dataObject, TypeSerializableInfo<T> typeInfo, Dictionary<ushort, ArrayWithOffset<byte>> propertyBytes)
            {
                if (customDeserializeFunc == null)
                {
                    throw new NullReferenceException();
                }
                return customDeserializeFunc.Invoke(dataObject, typeInfo, propertyBytes);
            }
            public override object CustomDeserialize(object dataObject, TypeSerializableInfo typeInfo, Dictionary<ushort, ArrayWithOffset<byte>> propertyBytes)
            {
                return CustomDeserialize((T)dataObject, (TypeSerializableInfo<T>)typeInfo, propertyBytes);
            }
        }
    }
    public abstract class TypeSerializableInfo { }
    public class TypeSerializableInfo<T> : TypeSerializableInfo
    {
        public Dictionary<ushort, PropertyData<T>> PropertiesDict { get; private set; } = new Dictionary<ushort, PropertyData<T>>();
    }
    public class PropertyData<T>
    {
        public PropertyInfo PropertyInfo { get; }
        public Func<T, object> GetData { get; }
        public Action<T, object> SetData { get; }
        public ushort DataId { get; }
        public PropertyData(PropertyInfo propertyInfo, Func<T, object> getData, Action<T, object> setData, ushort dataId)
        {
            PropertyInfo = propertyInfo;
            GetData = getData;
            SetData = setData;
            DataId = dataId;
        }
    }
}
