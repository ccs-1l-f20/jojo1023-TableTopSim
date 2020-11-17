using Blazor.Extensions.Canvas.WebGL;
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
            AddType<int>((o, info, s, bytes) => bytes.AddRange(BitConverter.GetBytes(o)),
                (info, bytes) => { bytes.Offset += 4; return BitConverter.ToInt32(bytes.Array, bytes.Offset - 4); }, false);
            AddType<int?>((o, info, s, bytes) => SerializeNullableInt(o, bytes),
                (info, bytes) =>
                {
                    bytes.Offset += 5; int? retVal = null;
                    if (bytes[-5] != 0) { retVal = BitConverter.ToInt32(bytes.Array, bytes.Offset - 4); }
                    return retVal;
                }, false);

            AddType<float>((o, info, s, bytes) => bytes.AddRange(BitConverter.GetBytes(o)),
                (info, bytes) => { bytes.Offset += 4; return BitConverter.ToSingle(bytes.Array, bytes.Offset - 4); }, false);
            AddType<double>((o, info, s, bytes) => bytes.AddRange(BitConverter.GetBytes(o)),
                (info, bytes) => { bytes.Offset += 8; return BitConverter.ToDouble(bytes.Array, bytes.Offset - 8); }, false);

            AddType<ushort>((o, info, s, bytes) => bytes.AddRange(BitConverter.GetBytes(o)),
                (info, bytes) => { bytes.Offset += 2; return BitConverter.ToUInt16(bytes.Array, bytes.Offset - 2); }, false);

            AddType<byte>((o, info, s, bytes) => bytes.Add(o),
                (info, bytes) => { bytes.Offset++; return bytes[-1]; }, false);

            AddType<bool>((o, info, s, bytes) => bytes.Add((byte)(o ? 255 : 0)),
                (info, bytes) => { bytes.Offset++; return bytes[-1] == 0 ? false : true; }, false);

            AddType<Vector2>((v, info, s, bytes) => { bytes.AddRange(BitConverter.GetBytes(v.X)); bytes.AddRange(BitConverter.GetBytes(v.Y)); },
                (info, bytes) =>
                {
                    float x = BitConverter.ToSingle(bytes.Array, bytes.Offset);
                    bytes.Offset += 4;
                    float y = BitConverter.ToSingle(bytes.Array, bytes.Offset);
                    bytes.Offset += 4;
                    return new Vector2(x, y);
                }, false);
            AddType<RectangleF>((v, info, s, bytes) => SerializeRect(v, bytes),
                (info, bytes) => DeserializeRect(bytes), false);
            AddType<RectangleF?>(
                (v, info, s, bytes) =>
                {
                    bytes.Add((byte)(v == null ? 0 : 255));
                    if (v == null) { SerializeRect(new RectangleF(), bytes); }
                    else { SerializeRect(v.Value, bytes); }
                },
                (info, bytes) =>
                {
                    RectangleF? retVal = null;
                    bytes.Offset++;
                    if(bytes[-1] != 0)
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

        public static void AddType<T>(Action<T, TypeSerializableInfo<T>, Dictionary<object, HashSet<int>>, List<byte>> serializeFunc,
           Func<TypeSerializableInfo<T>, ArrayWithOffset<byte>, T> deserializeFunc,
           bool getProperties = true,
           Func<T, TypeSerializableInfo<T>, ArrayWithOffset<byte>, Dictionary<object, HashSet<int>>, T> deserializeEditFunc = null,
           Func<T, TypeSerializableInfo<T>, Dictionary<ushort, ArrayWithOffset<byte>>, Dictionary<object, HashSet<int>>, T> customDeserializeFunc = null)
        {
            Type t = typeof(T);
            if (!typeSerializeFuncs.ContainsKey(t))
            {
                typeSerializeFuncs.Add(t, new SerialzeDataFunc<T>(serializeFunc, deserializeFunc, deserializeEditFunc, customDeserializeFunc));
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
        }
        static void SerializeList(IList l, Dictionary<object, HashSet<int>> dataToSerialize, List<byte> bytes)
        {
            int startIndex = bytes.Count;
            bytes.AddRange(new byte[] { 0, 0, 0, 0 });
            HashSet<int> dataIdsToSerialize = null;
            if (dataToSerialize != null && dataToSerialize.ContainsKey(l))
            {
                dataIdsToSerialize = dataToSerialize[l];
            }
            for (int i = 0; i < l.Count; i++)
            {
                if (dataIdsToSerialize == null || dataIdsToSerialize.Contains(i))
                {
                    bytes.AddRange(BitConverter.GetBytes(i));
                    int pStartIndex = bytes.Count;
                    bytes.Add(0);
                    bytes.Add(0);

                    SerializeGameData(l[i], dataToSerialize, bytes, null);

                    ushort pLength = (ushort)(bytes.Count - pStartIndex - 2);
                    byte[] pLengthBytes = BitConverter.GetBytes(pLength);
                    bytes[pStartIndex] = pLengthBytes[0];
                    bytes[pStartIndex + 1] = pLengthBytes[1];
                }
            }
            int length = bytes.Count - startIndex - 4;
            byte[] lengthBytes = BitConverter.GetBytes(length);
            bytes[startIndex] = lengthBytes[0];
            bytes[startIndex + 1] = lengthBytes[1];
            bytes[startIndex + 2] = lengthBytes[2];
            bytes[startIndex + 3] = lengthBytes[3];
        }
        public static List<byte> SerializeDict<TKey, TValue>(IDictionary<TKey, TValue> dict, Dictionary<object, HashSet<int>> dataToSerialize, HashSet<TKey> keysToRemove)
        {
            List<byte> bytes = new List<byte>();
            SerializeDict((IDictionary)dict, dataToSerialize, keysToRemove, bytes);
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
        static void SerializeDict(IDictionary dict, Dictionary<object, HashSet<int>> dataToSerialize, IEnumerable keysToRemove, List<byte> bytes)
        {
            int startIndex = bytes.Count;
            bytes.AddRange(new byte[] { 0, 0, 0, 0 });
            HashSet<int> dataIdsToSerialize = null;
            if (dataToSerialize != null && dataToSerialize.ContainsKey(dict))
            {
                dataIdsToSerialize = dataToSerialize[dict];
            }
            foreach (var key in dict.Keys)
            {
                if (dataIdsToSerialize == null || dataIdsToSerialize.Contains(key.GetHashCode()))
                {
                    //bytes.AddRange(BitConverter.GetBytes(i));
                    SerializeGameData(key, null, bytes, null);
                    int pStartIndex = bytes.Count;
                    bytes.AddRange(new byte[] { 0, 0, 0, 0 });

                    var currentVal = dict[key];

                    SerializeGameData(currentVal, dataToSerialize, bytes, null);

                    int pLength = bytes.Count - pStartIndex - 4;
                    byte[] pLengthBytes = BitConverter.GetBytes(pLength);
                    bytes[pStartIndex] = pLengthBytes[0];
                    bytes[pStartIndex + 1] = pLengthBytes[1];
                    bytes[pStartIndex + 2] = pLengthBytes[2];
                    bytes[pStartIndex + 3] = pLengthBytes[3];
                }
            }
            if (keysToRemove != null)
            {
                foreach (var key in keysToRemove)
                {
                    SerializeGameData(key, null, bytes, null);
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
        static IList DeserializeEditList(IList list, ArrayWithOffset<byte> bytes, Dictionary<object, HashSet<int>> dataToIgnore)
        {
            Type t = list.GetType();
            int length = BitConverter.ToInt32(bytes.Array, bytes.Offset);
            bytes.Offset += 4;
            int startOffset = bytes.Offset;
            Type genericType = t.GenericTypeArguments[0];
            string genTName = genericType.Name;
            object genericTypeDefault = null;
            if (genericType.IsValueType)
            {
                genericTypeDefault = Activator.CreateInstance(genericType);
            }
            HashSet<int> thisDataToIgnore = null;
            if (dataToIgnore != null && dataToIgnore.ContainsKey(list))
            {
                thisDataToIgnore = dataToIgnore[list];
            }
            while (bytes.Offset - startOffset < length)
            {
                int index = BitConverter.ToInt32(bytes.Array, bytes.Offset);
                while (list.Count <= index)
                {
                    list.Add(genericTypeDefault);
                }
                bytes.Offset += 4;
                ushort pLength = BitConverter.ToUInt16(bytes.Array, bytes.Offset);
                bytes.Offset += 2;
                if (thisDataToIgnore == null || !thisDataToIgnore.Contains(index))
                {
                    list[index] = DeserializeEditGameData(list[index], bytes.Slice(0, pLength), dataToIgnore);
                }
                bytes.Offset += pLength;
            }
            return list;
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
        static IDictionary DeserializeEditDict(IDictionary dict, ArrayWithOffset<byte> bytes, Dictionary<object, HashSet<int>> dataToIgnore)
        {
            Type t = dict.GetType();
            int length = BitConverter.ToInt32(bytes.Array, bytes.Offset);
            bytes.Offset += 4;
            int startOffset = bytes.Offset;
            Type genericKeyType = t.GenericTypeArguments[0];
            Type genericValueType = t.GenericTypeArguments[1];
            HashSet<int> thisDataToIgnore = null;
            if (dataToIgnore != null && dataToIgnore.ContainsKey(dict))
            {
                thisDataToIgnore = dataToIgnore[dict];
            }
            while (bytes.Offset - startOffset < length)
            {
                object key = DeserializeGameData(genericKeyType, bytes);
                int keyHash = key.GetHashCode();
                int pLength = BitConverter.ToInt32(bytes.Array, bytes.Offset);
                bytes.Offset += 4;

                if (pLength < 0)
                {
                    if (dict.Contains(key) && (thisDataToIgnore == null ||
                        !thisDataToIgnore.Contains(keyHash)))
                    {
                        dict.Remove(key);
                    }
                    continue;
                }
                if (thisDataToIgnore == null || !thisDataToIgnore.Contains(keyHash))
                {
                    object currentVal = null;
                    if (dict.Contains(key))
                    {
                        currentVal = dict[key];
                    }
                    object newValue;
                    if (currentVal == null || !genericValueType.IsClass)
                    {
                        newValue = DeserializeGameData(genericValueType, bytes.Slice(0, pLength));
                    }
                    else
                    {
                        newValue = DeserializeEditGameData(currentVal, bytes.Slice(0, pLength), dataToIgnore);
                    }
                    if (dict.Contains(key))
                    {
                        dict[key] = newValue;
                    }
                    else
                    {
                        dict.Add(key, newValue);
                    }
                }
                bytes.Offset += pLength;
            }
            return dict;
        }
        public static List<byte> SerializeGameData(object data, Dictionary<object, HashSet<int>> specificDataToSerialize = null)
        {
            List<byte> bytes = new List<byte>();
            SerializeGameData(data, specificDataToSerialize, bytes, data.GetType());
            return bytes;
        }
        static void SerializeGameData(object data, Dictionary<object, HashSet<int>> dataToSerialize, List<byte> bytes, Type t)
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
                SerializeList((IList)data, dataToSerialize, bytes);
                return;
            }
            else if (t.GetInterface("IDictionary") != null)
            {
                SerializeDict((IDictionary)data, dataToSerialize, null, bytes);
                return;
            }
            if (typeProperties.ContainsKey(t))
            {
                info = typeProperties[t];
            }
            if (typeSerializeFuncs.ContainsKey(t))
            {
                typeSerializeFuncs[t]?.Serialize(data, info, dataToSerialize, bytes);
            }
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
        public static T DeserializeEditGameData<T>(T data, byte[] bytes, Dictionary<object, HashSet<int>> dataToIgnore = null)
        {
            return (T)PvtDeserializeEditGameData(data, new ArrayWithOffset<byte>(bytes), dataToIgnore);
        }
        public static T DeserializeEditGameData<T>(T data, ArrayWithOffset<byte> bytes, Dictionary<object, HashSet<int>> dataToIgnore = null)
        {
            return (T)PvtDeserializeEditGameData(data, bytes, dataToIgnore);
        }
        static object PvtDeserializeEditGameData(object dataObject, ArrayWithOffset<byte> bytes, Dictionary<object, HashSet<int>> dataToIgnore)
        {
            Type t = dataObject.GetType();
            TypeSerializableInfo info = null;
            if (t.IsEnum)
            {
                t = Enum.GetUnderlyingType(t);
            }
            else if (t.GetInterface("IList") != null)
            {
                return DeserializeEditList((IList)dataObject, bytes, dataToIgnore);
            }
            else if (t.GetInterface("IDictionary") != null)
            {
                return DeserializeEditDict((IDictionary)dataObject, bytes, dataToIgnore);
            }

            if (typeProperties.ContainsKey(t))
            {
                info = typeProperties[t];
            }
            if (typeSerializeFuncs.ContainsKey(t))
            {
                var sereializeInfo = typeSerializeFuncs[t];
                if (sereializeInfo.HasDeserializeEdit)
                {
                    return sereializeInfo.DeserializeEdit(dataObject, info, bytes, dataToIgnore);
                }
                else
                {
                    return sereializeInfo.Deserialize(info, bytes);
                }
            }
            else
            {
                throw new NullReferenceException();
            }
        }

        internal static object CustomDeserialize(object dataObject, Dictionary<ushort, ArrayWithOffset<byte>> propertyBytes, Dictionary<object, HashSet<int>> dataToIgnore)
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
                return typeSerializeFuncs[t].CustomDeserialize(dataObject, info, propertyBytes, dataToIgnore);
            }
            else
            {
                throw new NullReferenceException();
            }
        }
        public static void GenericSerializeFunc<T>(T data, TypeSerializableInfo<T> info, Dictionary<object, HashSet<int>> dataToSerialize, List<byte> bytes)
        {
            int startIndex = bytes.Count;
            bytes.Add(0);
            bytes.Add(0);
            HashSet<int> dataIdsToSerialize = null;
            if (dataToSerialize != null && dataToSerialize.ContainsKey(data))
            {
                dataIdsToSerialize = dataToSerialize[data];
            }
            if (info != null)
            {
                foreach (var p in info.PropertiesDict.Values)
                {
                    if (p.SetData == null || dataIdsToSerialize == null || dataIdsToSerialize.Contains(p.DataId))
                    {
                        bytes.AddRange(BitConverter.GetBytes(p.DataId));
                        int pStartIndex = bytes.Count;
                        bytes.Add(0);
                        bytes.Add(0);
                        object getData = p.GetData.Invoke(data);
                        SerializeGameData(getData, dataToSerialize, bytes, GetForcedType(getData, p.PropertyInfo.PropertyType));

                        ushort pLength = (ushort)(bytes.Count - pStartIndex - 2);
                        byte[] pLengthBytes = BitConverter.GetBytes(pLength);
                        bytes[pStartIndex] = pLengthBytes[0];
                        bytes[pStartIndex + 1] = pLengthBytes[1];
                    }
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
            return CustomGenericDeserializeFunc(default(T), info, data, null);
        }
        public static T GenericDeserializeEditFunc<T>(T dataObject, TypeSerializableInfo<T> info, ArrayWithOffset<byte> bytes, Dictionary<object, HashSet<int>> dataToIgnore)
        {
            var data = GetPropertyBytes(bytes);
            return CustomGenericDeserializeFunc(dataObject, info, data, dataToIgnore);
        }
        public static T CustomGenericDeserializeFunc<T>(T dataObject, TypeSerializableInfo<T> info, Dictionary<ushort, ArrayWithOffset<byte>> data,
            Dictionary<object, HashSet<int>> dataToIgnore)
        {
            if (dataObject == null)
            {
                dataObject = (T)(typeof(T).GetConstructor(new Type[0]).Invoke(new object[0]));
            }
            HashSet<int> thisDataToIgnore = null;
            if (dataToIgnore != null && dataToIgnore.ContainsKey(dataObject))
            {
                thisDataToIgnore = dataToIgnore[dataObject];
            }
            foreach (var dataId in data.Keys)
            {
                ArrayWithOffset<byte> pBytes = data[dataId];

                var propertyData = info.PropertiesDict[dataId];
                if (propertyData.SetData != null && (thisDataToIgnore == null || !thisDataToIgnore.Contains(dataId)))
                {
                    object propertyVal = null;

                    if (propertyData.PropertyInfo.PropertyType.IsClass)
                    {
                        object propertyDataValue = propertyData.GetData.Invoke(dataObject);
                        propertyVal = DeserializeEditGameData(propertyDataValue, pBytes, dataToIgnore);
                    }
                    else
                    {
                        propertyVal = DeserializeGameData(propertyData.PropertyInfo.PropertyType, pBytes);
                    }
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
            public abstract bool HasDeserializeEdit { get; }
            public abstract void Serialize(object value, TypeSerializableInfo typeInfo, Dictionary<object, HashSet<int>> dataToSerialize, List<byte> bytes);
            public abstract object Deserialize(TypeSerializableInfo typeInfo, ArrayWithOffset<byte> bytes);
            public abstract object DeserializeEdit(object dataObject, TypeSerializableInfo typeInfo, ArrayWithOffset<byte> bytes,
                Dictionary<object, HashSet<int>> dataToIgnore);
            public abstract object CustomDeserialize(object dataObject, TypeSerializableInfo typeInfo, Dictionary<ushort, ArrayWithOffset<byte>> propertyBytes,
                Dictionary<object, HashSet<int>> dataToIgnore);
        }
        class SerialzeDataFunc<T> : SerialzeDataFunc
        {
            Action<T, TypeSerializableInfo<T>, Dictionary<object, HashSet<int>>, List<byte>> serializeFunc;
            Func<TypeSerializableInfo<T>, ArrayWithOffset<byte>, T> deserializeFunc;
            Func<T, TypeSerializableInfo<T>, Dictionary<ushort, ArrayWithOffset<byte>>, Dictionary<object, HashSet<int>>, T> customDeserializeFunc;
            Func<T, TypeSerializableInfo<T>, ArrayWithOffset<byte>, Dictionary<object, HashSet<int>>, T> deserializeEditFunc;

            public override bool HasDeserializeEdit => deserializeEditFunc != null;

            public SerialzeDataFunc(Action<T, TypeSerializableInfo<T>, Dictionary<object, HashSet<int>>, List<byte>> serializeFunc,
                Func<TypeSerializableInfo<T>, ArrayWithOffset<byte>, T> deserializeFunc,
                Func<T, TypeSerializableInfo<T>, ArrayWithOffset<byte>, Dictionary<object, HashSet<int>>, T> deserializeEditFunc,
                Func<T, TypeSerializableInfo<T>, Dictionary<ushort, ArrayWithOffset<byte>>, Dictionary<object, HashSet<int>>, T> customDeserializeFunc = null)
            {
                this.serializeFunc = serializeFunc;
                this.deserializeFunc = deserializeFunc;
                this.deserializeEditFunc = deserializeEditFunc;
                this.customDeserializeFunc = customDeserializeFunc;
            }
            public void Serialize(T value, TypeSerializableInfo<T> typeInfo, Dictionary<object, HashSet<int>> dataToSerialize, List<byte> bytes)
            {
                serializeFunc?.Invoke(value, typeInfo, dataToSerialize, bytes);
            }
            public override void Serialize(object value, TypeSerializableInfo typeInfo, Dictionary<object, HashSet<int>> dataToSerialize, List<byte> bytes)
            {
                Serialize((T)value, (TypeSerializableInfo<T>)typeInfo, dataToSerialize, bytes);
            }


            public T Deserialize(TypeSerializableInfo<T> typeInfo, ArrayWithOffset<byte> bytes)
            {
                return deserializeFunc.Invoke(typeInfo, bytes);
            }
            public override object Deserialize(TypeSerializableInfo typeInfo, ArrayWithOffset<byte> bytes)
            {
                return Deserialize((TypeSerializableInfo<T>)typeInfo, bytes);
            }

            public T DeserializeEdit(T dataObject, TypeSerializableInfo<T> typeInfo, ArrayWithOffset<byte> bytes, Dictionary<object, HashSet<int>> dataToIgnore)
            {
                return deserializeEditFunc.Invoke(dataObject, typeInfo, bytes, dataToIgnore);
            }
            public override object DeserializeEdit(object dataObject, TypeSerializableInfo typeInfo, ArrayWithOffset<byte> bytes, Dictionary<object, HashSet<int>> dataToIgnore)
            {
                return DeserializeEdit((T)dataObject, (TypeSerializableInfo<T>)typeInfo, bytes, dataToIgnore);
            }

            public T CustomDeserialize(T dataObject, TypeSerializableInfo<T> typeInfo, Dictionary<ushort, ArrayWithOffset<byte>> propertyBytes,
                Dictionary<object, HashSet<int>> dataToIgnore)
            {
                if (customDeserializeFunc == null)
                {
                    throw new NullReferenceException();
                }
                return customDeserializeFunc.Invoke(dataObject, typeInfo, propertyBytes, dataToIgnore);
            }
            public override object CustomDeserialize(object dataObject, TypeSerializableInfo typeInfo, Dictionary<ushort, ArrayWithOffset<byte>> propertyBytes,
                Dictionary<object, HashSet<int>> dataToIgnore)
            {
                return CustomDeserialize((T)dataObject, (TypeSerializableInfo<T>)typeInfo, propertyBytes, dataToIgnore);
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
