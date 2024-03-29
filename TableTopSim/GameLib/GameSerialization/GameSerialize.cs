﻿using Blazor.Extensions.Canvas.WebGL;
using GameLib.Sprites;
using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
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
        public static void SerializeNullableInt(int? o, List<byte> bytes)
        {
            bytes.Add((byte)(o == null ? 0 : 255));
            bytes.AddRange(BitConverter.GetBytes((int)(o == null ? 0 : o.Value)));
        }
        static void SerializeRect(RectangleF r, List<byte> bytes)
        {
            bytes.AddRange(BitConverter.GetBytes(r.X));
            bytes.AddRange(BitConverter.GetBytes(r.Y));
            bytes.AddRange(BitConverter.GetBytes(r.Width));
            bytes.AddRange(BitConverter.GetBytes(r.Height));
        }
        static RectangleF DeserializeRect(ArrayWithOffset<byte> bytes)
        {
            float x = BitConverter.ToSingle(bytes.Array, bytes.Offset);
            bytes.Offset += 4;
            float y = BitConverter.ToSingle(bytes.Array, bytes.Offset);
            bytes.Offset += 4;
            float width = BitConverter.ToSingle(bytes.Array, bytes.Offset);
            bytes.Offset += 4;
            float height = BitConverter.ToSingle(bytes.Array, bytes.Offset);
            bytes.Offset += 4;
            return new RectangleF(x, y, width, height);
        }
        static GameSerialize()
        {
            AddType<int>((o, info, bytes) => 
            bytes.AddRange(BitConverter.GetBytes(o)),
                (info, bytes) => { bytes.Offset += 4; return BitConverter.ToInt32(bytes.Array, bytes.Offset - 4); }, false);
            AddType<int?>((o, info, bytes) => SerializeNullableInt(o, bytes),
                (info, bytes) =>
                {
                    bytes.Offset += 5; int? retVal = null;
                    if (bytes[-5] != 0) { retVal = BitConverter.ToInt32(bytes.Array, bytes.Offset - 4); }
                    return retVal;
                }, false);

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
            AddType<RectangleF>((v, info, bytes) => SerializeRect(v, bytes),
                (info, bytes) => DeserializeRect(bytes), false);
            AddType<RectangleF?>(
                (v, info, bytes) =>
                {
                    bytes.Add((byte)(v == null ? 0 : 255));
                    if (v == null) { SerializeRect(new RectangleF(), bytes); }
                    else { SerializeRect(v.Value, bytes); }
                },
                (info, bytes) =>
                {
                    RectangleF? retVal = null;
                    bytes.Offset++;
                    if (bytes[-1] != 0)
                    {
                        retVal = DeserializeRect(bytes);
                    }
                    else
                    {
                        bytes.Offset += 12;
                    }
                    return retVal;
                }, false);
            Sprite.InitSprite();
        }

        public static void AddType<T>(Action<T, TypeSerializableInfo<T>, List<byte>> serializeFunc,
           Func<TypeSerializableInfo<T>, ArrayWithOffset<byte>, T> deserializeFunc,
           bool getProperties = true)
        {
            Type t = typeof(T);
            if (!typeSerializeFuncs.ContainsKey(t))
            {
                typeSerializeFuncs.Add(t, new SerialzeDataFunc<T>(serializeFunc, deserializeFunc, null));
                if (!getProperties) { return; }
                AddTypeStuff<T>(t);
            }
        }
        public static void AddType<T>(Action<T, TypeSerializableInfo<T>, List<byte>> serializeFunc,
          Func<TypeSerializableInfo<T>, ArrayWithOffset<byte>, T> deserializeFunc,
           Func<T, TypeSerializableInfo<T>, Dictionary<ushort, ArrayWithOffset<byte>>, T> customDeserializeFunc)
        {
            Type t = typeof(T);
            if (!typeSerializeFuncs.ContainsKey(t))
            {
                typeSerializeFuncs.Add(t, new SerialzeDataFunc<T>(serializeFunc, deserializeFunc, customDeserializeFunc));
                AddTypeStuff<T>(t);
            }
        }

        static void AddTypeStuff<T>(Type t)
        {
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
                        var convertedSet = Expression.Convert(setParam, pExpres.Type);
                        var setExpres = Expression.Assign(pExpres, convertedSet);
                        setFunc = Expression.Lambda<Action<T, object>>(setExpres, typeInstance, setParam).Compile();
                    }
                    Func<T, object> getFunc = Expression.Lambda<Func<T, object>>(Expression.Convert(pExpres, typeof(object)), typeInstance).Compile();
                    var propertyData = new PropertyData<T>(p, getFunc, setFunc, att.DataId);
                    typeSerializableInfo.PropertiesDict.Add(att.DataId, propertyData);
                }
            }
            typeProperties.Add(t, typeSerializableInfo);
        }
        static void SerializeList(IList l, List<byte> bytes)
        {
            int startIndex = bytes.Count;
            bytes.AddRange(new byte[] { 0, 0, 0, 0 });
            for (int i = 0; i < l.Count; i++)
            {
                bytes.AddRange(BitConverter.GetBytes(i));
                int pStartIndex = bytes.Count;
                bytes.Add(0);
                bytes.Add(0);

                SerializeGameData(l[i], bytes, null);

                ushort pLength = (ushort)(bytes.Count - pStartIndex - 2);
                byte[] pLengthBytes = BitConverter.GetBytes(pLength);
                bytes[pStartIndex] = pLengthBytes[0];
                bytes[pStartIndex + 1] = pLengthBytes[1];
            }
            int length = bytes.Count - startIndex - 4;
            byte[] lengthBytes = BitConverter.GetBytes(length);
            bytes[startIndex] = lengthBytes[0];
            bytes[startIndex + 1] = lengthBytes[1];
            bytes[startIndex + 2] = lengthBytes[2];
            bytes[startIndex + 3] = lengthBytes[3];
        }
        public static List<byte> SerializeDict<TKey, TValue>(IDictionary<TKey, TValue> dict, HashSet<TKey> keysToRemove)
        {
            List<byte> bytes = new List<byte>();
            SerializeDict((IDictionary)dict, keysToRemove, bytes);
            return bytes;
        }
        static Type GetForcedType(object o, Type t)
        {
            if (o == null || Nullable.GetUnderlyingType(t) != null)
            {
                return t;
            }
            return null;
        }
        static void SerializeDict(IDictionary dict, IEnumerable keysToRemove, List<byte> bytes)
        {
            int startIndex = bytes.Count;
            bytes.AddRange(new byte[] { 0, 0, 0, 0 });
            foreach (var key in dict.Keys)
            {
                //bytes.AddRange(BitConverter.GetBytes(i));
                SerializeGameData(key, bytes, null);
                int pStartIndex = bytes.Count;
                bytes.AddRange(new byte[] { 0, 0, 0, 0 });

                var currentVal = dict[key];

                SerializeGameData(currentVal, bytes, null);

                int pLength = bytes.Count - pStartIndex - 4;
                byte[] pLengthBytes = BitConverter.GetBytes(pLength);
                bytes[pStartIndex] = pLengthBytes[0];
                bytes[pStartIndex + 1] = pLengthBytes[1];
                bytes[pStartIndex + 2] = pLengthBytes[2];
                bytes[pStartIndex + 3] = pLengthBytes[3];
            }
            if (keysToRemove != null)
            {
                foreach (var key in keysToRemove)
                {
                    SerializeGameData(key, bytes, null);
                    bytes.AddRange(BitConverter.GetBytes((int)-1));
                }
            }
            int length = bytes.Count - startIndex - 4;
            byte[] lengthBytes = BitConverter.GetBytes(length);
            bytes[startIndex] = lengthBytes[0];
            bytes[startIndex + 1] = lengthBytes[1];
            bytes[startIndex + 2] = lengthBytes[2];
            bytes[startIndex + 3] = lengthBytes[3];
        }
        static IList DeserializeList(Type t, ArrayWithOffset<byte> bytes)
        {
            IList data = (IList)t.GetConstructor(new Type[] { }).Invoke(new object[] { });
            int length = BitConverter.ToInt32(bytes.Array, bytes.Offset);
            bytes.Offset += 4;
            int startOffset = bytes.Offset;
            Type genericType = t.GenericTypeArguments[0];
            while (bytes.Offset - startOffset < length)
            {
                int index = BitConverter.ToInt32(bytes.Array, bytes.Offset);
                bytes.Offset += 4;
                ushort pLength = BitConverter.ToUInt16(bytes.Array, bytes.Offset);
                bytes.Offset += 2;
                data.Add(DeserializeGameData(genericType, bytes.Slice(0, pLength)));
                bytes.Offset += pLength;
            }
            return data;
        }
        static IDictionary DeserializeDict(Type t, ArrayWithOffset<byte> bytes)
        {
            IDictionary data = (IDictionary)t.GetConstructor(new Type[] { }).Invoke(new object[] { });
            int length = BitConverter.ToInt32(bytes.Array, bytes.Offset);
            
            bytes.Offset += 4;
            int startOffset = bytes.Offset;
            Type genericKeyType = t.GenericTypeArguments[0];
            Type genericValueType = t.GenericTypeArguments[1];
            while (bytes.Offset - startOffset < length)
            {
                object key = DeserializeGameData(genericKeyType, bytes);
                var test = (int)key;
                int pLength = BitConverter.ToInt32(bytes.Array, bytes.Offset);
                bytes.Offset += 4;
                if (pLength < 0)
                {
                    continue;
                }
                data.Add(key, DeserializeGameData(genericValueType, bytes.Slice(0, pLength)));
                bytes.Offset += pLength;
            }
            return data;
        }
        public static List<byte> SerializeGameData(object data)
        {
            List<byte> bytes = new List<byte>();
            SerializeGameData(data, bytes, data.GetType());
            return bytes;
        }
        static void SerializeGameData(object data, List<byte> bytes, Type t)
        {
            if (t == null)
            {
                t = data.GetType();
            }
            TypeSerializableInfo info = null;
            if (t.IsEnum)
            {
                t = Enum.GetUnderlyingType(t);
                data = Convert.ChangeType(data, t);
            }
            else if (t.GetInterface("IList") != null)
            {
                SerializeList((IList)data, bytes);
                return;
            }
            else if (t.GetInterface("IDictionary") != null)
            {
                SerializeDict((IDictionary)data, null, bytes);
                return;
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

        public static List<byte> SpecificSerializeGameData(object data, PathTrie<object> specificData)
        {
            List<byte> bytes = new List<byte>();
            bytes.AddRange(BitConverter.GetBytes(specificData.Count));
            SpecificSerializeGameData(data, bytes, data.GetType(), specificData, new List<int>());
            return bytes;
        }
        static void SpecificSerializeGameData(object data, List<byte> bytes, Type t, PathTrie<object> specificData, List<int> currentPath)
        {
            if(t == null)
            {
                if(data == null) { return; }
                t = data.GetType();
            }
            //string tn = t.Name;
            if (specificData.ContainsKey(currentPath))
            {
                bytes.Add((byte)currentPath.Count);
                for (int i = 0; i < currentPath.Count; i++)
                {
                    bytes.AddRange(BitConverter.GetBytes(currentPath[i]));
                }
                int startIndex = bytes.Count;
                bytes.AddRange(new byte[] { 0, 0 });
                SerializeGameData(data, bytes, t);
                int length = bytes.Count - startIndex - 2;
                byte[] lengthBytes = BitConverter.GetBytes((ushort)length);
                bytes[startIndex] = lengthBytes[0];
                bytes[startIndex + 1] = lengthBytes[1];
            }
            else
            {
                HashSet<int> nextPaths = specificData.NextPathKeys(currentPath);
                if (nextPaths == null) { return; }
                foreach (var k in nextPaths)
                {
                    (object nextData, Type nextT) = GetNextData(data, t, k);
                    if (nextData == null && nextT == null) { continue; }
                    currentPath.Add(k);

                    SpecificSerializeGameData(nextData, bytes, nextT, specificData, currentPath);
                    currentPath.RemoveAt(currentPath.Count - 1);
                }
            }
        }

        static (object data, Type type) GetNextData(object prevData, Type prevType, int nextPath)
        {
            if (prevType == null)
            {
                return (null, null);
            }
            if (prevType.GetInterface("IList") != null)
            {
                IList ls = (IList)prevData;
                object data = ls[nextPath];
                return (data, data.GetType());
            }
            else if (prevType.GetInterface("IDictionary") != null)
            {
                IDictionary dict = (IDictionary)prevData;
                object data = dict[nextPath];
                return (data, data.GetType());
            }
            if (typeProperties.ContainsKey(prevType))
            {
                TypeSerializableInfo info = typeProperties[prevType];
                Type t = info.GetPropertyType((ushort)nextPath);
                if(prevType == null) { return (null, null); }

                object data = info.GetPropertyData(prevData, (ushort)nextPath);
                return (data, GetForcedType(data, t));
            }
            return (null, null);
        }

        public static T DeserializeGameData<T>(byte[] bytes)
        {
            Type t = typeof(T);
            return (T)DeserializeGameData(t, new ArrayWithOffset<byte>(bytes));
        }
        public static T DeserializeGameData<T>(ArrayWithOffset<byte> bytes)
        {
            Type t = typeof(T);
            return (T)DeserializeGameData(t, bytes);
        }
        static object DeserializeGameData(Type t, ArrayWithOffset<byte> bytes)
        {
            TypeSerializableInfo info = null;
            if (t.IsEnum)
            {
                t = Enum.GetUnderlyingType(t);
            }
            else if (t.GetInterface("IList") != null)
            {
                return DeserializeList(t, bytes);
            }
            else if (t.GetInterface("IDictionary") != null)
            {
                return DeserializeDict(t, bytes);
            }

            string typeName = t.Name;
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
                throw new NullReferenceException($"Cant Find Serialize For Type: {typeName}");
            }
        }
        public static T DeserializeEditGameData<T>(T data, byte[] bytes, PathTrie<object> pathsToIgnore = null, PathTrie<object> deserializedPaths = null)
        {
            return (T)PvtDeserializeEditGameData(data, new ArrayWithOffset<byte>(bytes), pathsToIgnore, deserializedPaths);
        }
        public static T DeserializeEditGameData<T>(T data, ArrayWithOffset<byte> bytes, PathTrie<object> pathsToIgnore = null, PathTrie<object> deserializedPaths = null)
        {
            return (T)PvtDeserializeEditGameData(data, bytes, pathsToIgnore, deserializedPaths);
        }
        static object PvtDeserializeEditGameData(object dataObject, ArrayWithOffset<byte> bytes, PathTrie<object> pathsToIgnore, PathTrie<object> paths)
        {
            int dataCount = BitConverter.ToInt32(bytes.Array, bytes.Offset);
            bytes.Offset += 4;
            for (int i = 0; i < dataCount; i++)
            {
                byte pathCount = bytes[0];
                bytes.Offset++;
                List<int> path = new List<int>();
                for (byte j = 0; j < pathCount; j++)
                {
                    path.Add(BitConverter.ToInt32(bytes.Array, bytes.Offset));
                    bytes.Offset += 4;
                }
                ushort dataLength = BitConverter.ToUInt16(bytes.Array, bytes.Offset);
                bytes.Offset += 2;
                if (pathsToIgnore == null || !pathsToIgnore.ContainsKey(path))
                {
                    if (paths != null)
                    {
                        paths.Insert(path, null, true);
                    }

                    dataObject = PvtDeserializeEditSpecifcData(dataObject, path, bytes.Slice(0, dataLength));
                }
                bytes.Offset += dataLength;
            }
            return dataObject;
        }

        static object PvtDeserializeEditSpecifcData(object dataObject, List<int> path, ArrayWithOffset<byte> dataBytes)
        {
            object currentObj = dataObject;
            Type currentType = dataObject.GetType();
            if (path.Count == 0)
            {
                return DeserializeGameData(currentType, dataBytes);
            }
            for (int i = 0; i < path.Count - 1; i++)
            {
                (object nextData, Type nextT) = GetNextData(currentObj, currentType, path[i]);
                if (nextData == null) { return dataObject; }
                if(nextT == null) { nextT = nextData.GetType(); }
                currentObj = nextData;
                currentType = nextT;
            }
            int lastPath = path[path.Count - 1];
            SetData(currentObj, currentType, dataBytes, lastPath);
            return dataObject;
        }


        static void SetData(object data, Type type, ArrayWithOffset<byte> dataBytes, int pathId)
        {
            if (data == null)
            {
                return;
            }
            else if (type.GetInterface("IList") != null)
            {
                IList ls = (IList)data;

                Type genericType = type.GenericTypeArguments[0];
                object genericTypeDefault = null;
                if (genericType.IsValueType)
                {
                    genericTypeDefault = Activator.CreateInstance(genericType);
                }
                while (ls.Count <= pathId)
                {
                    ls.Add(genericTypeDefault);
                }
                ls[pathId] = DeserializeGameData(genericType, dataBytes);
            }
            else if (type.GetInterface("IDictionary") != null)
            {
                IDictionary dict = (IDictionary)data;
                Type valueType = type.GenericTypeArguments[0];
                object setData = DeserializeGameData(valueType, dataBytes);
                if (dict.Contains(pathId))
                {
                    dict[pathId] = setData;
                }
                else
                {
                    dict.Add(pathId, setData);
                }
            }
            else if (typeProperties.ContainsKey(type))
            {
                TypeSerializableInfo info = typeProperties[type];
                Type propertyType = info.GetPropertyType((ushort)pathId);
                if(propertyType == null) { return; }
                object setData = DeserializeGameData(propertyType, dataBytes);
                info.SetPropertyData(data, setData, (ushort)pathId);
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
                    object getData = p.GetData.Invoke(data);
                    SerializeGameData(getData, bytes, GetForcedType(getData, p.PropertyInfo.PropertyType));

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
                    object propertyVal = null;

                    //if (propertyData.PropertyInfo.PropertyType.IsClass)
                    //{
                    //    object propertyDataValue = propertyData.GetData.Invoke(dataObject);
                    //    propertyVal = DeserializeEditGameData(propertyDataValue, pBytes, dataToIgnore);
                    //}
                    //else
                    //{
                    string testTypeName = propertyData.PropertyInfo.PropertyType.Name;
                        propertyVal = DeserializeGameData(propertyData.PropertyInfo.PropertyType, pBytes);
                    //}
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
            //public abstract bool HasDeserializeEdit { get; }
            public abstract void Serialize(object value, TypeSerializableInfo typeInfo, List<byte> bytes);
            public abstract object Deserialize(TypeSerializableInfo typeInfo, ArrayWithOffset<byte> bytes);
            public abstract object CustomDeserialize(object dataObject, TypeSerializableInfo typeInfo, Dictionary<ushort, ArrayWithOffset<byte>> propertyBytes);
        }
        class SerialzeDataFunc<T> : SerialzeDataFunc
        {
            Action<T, TypeSerializableInfo<T>, List<byte>> serializeFunc;
            Func<TypeSerializableInfo<T>, ArrayWithOffset<byte>, T> deserializeFunc;
            Func<T, TypeSerializableInfo<T>, Dictionary<ushort, ArrayWithOffset<byte>>, T> customDeserializeFunc;

            //public override bool HasDeserializeEdit => deserializeEditFunc != null;

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
    public abstract class TypeSerializableInfo
    {
        public abstract Type GetPropertyType(ushort dataId);
        public abstract object GetPropertyData(object obj, ushort dataId);
        public abstract void SetPropertyData(object obj, object propertyObj, ushort dataId);
    }
    public class TypeSerializableInfo<T> : TypeSerializableInfo
    {
        public Dictionary<ushort, PropertyData<T>> PropertiesDict { get; private set; } = new Dictionary<ushort, PropertyData<T>>();

        public override object GetPropertyData(object obj, ushort dataId)
        {
            if (PropertiesDict.ContainsKey(dataId))
            {
                var propD = PropertiesDict[dataId];
                return propD.GetData((T)obj);
            }
            return null;
        }

        public override Type GetPropertyType(ushort dataId)
        {
            if (PropertiesDict.ContainsKey(dataId))
            {
                return PropertiesDict[dataId].PropertyInfo.PropertyType;
            }
            return null;
        }

        public override void SetPropertyData(object obj, object propertyObj, ushort dataId)
        {
            if (PropertiesDict.ContainsKey(dataId))
            {
                var propD = PropertiesDict[dataId];
                if(propD.SetData != null)
                {
                    propD.SetData.Invoke((T)obj, propertyObj);
                }
            }
        }
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
