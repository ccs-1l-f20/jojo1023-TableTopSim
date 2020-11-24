using GameLib.GameSerialization;
using MathNet.Numerics;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;

namespace GameLib
{
    public class LayerDepth : IComparable<LayerDepth>
    {

        const ushort layersDataId = 0;
        public event Action<LayerDepth, ushort> OnLayersChanged;
        public int Count => layers.Count;
        public float this[int i] { get => layers[i]; set { layers[i] = value; OnLayersChanged?.Invoke(this, layersDataId); } }

        List<float> layers;
        [GameSerializableData(layersDataId)]
        public List<float> Layers { get => layers; internal set { layers = value; OnLayersChanged?.Invoke(this, layersDataId); } }


        static LayerDepth()
        {
            GameSerialize.AddType<LayerDepth>(GameSerialize.GenericSerializeFunc, GameSerialize.GenericDeserializeFunc, true);
        }
        public LayerDepth()
        {
            layers = new List<float>();
        }
        public LayerDepth(float layer)
        {
            layers = new List<float>();
            layers.Add(layer);
        }
        public LayerDepth(IList<float> layerList)
        {
            layers = new List<float>(layerList);
        }
        public void Add(float layer)
        {
            layers.Add(layer);
            OnLayersChanged?.Invoke(this, layersDataId);
        }
        public void AddAtStart(float layer)
        {
            layers.Insert(0, layer);
            OnLayersChanged?.Invoke(this, layersDataId);
        }

        public void RemoveAt(int index)
        {
            layers.RemoveAt(index);
            OnLayersChanged?.Invoke(this, layersDataId);
        }

        public void AddTo(LayerDepth other)
        {
            for (int i = 0; i < other.layers.Count; i++)
            {
                layers.Add(other.layers[i]);
            }
            OnLayersChanged?.Invoke(this, layersDataId);
        }
        private void Layers_ListChanged(object sender, ListChangedEventArgs e)
        {
            OnLayersChanged?.Invoke(this, layersDataId);
        }


        public int CompareTo(LayerDepth other)
        {
            for (int i = 0; i < layers.Count || i < other.layers.Count; i++)
            {
                float thisLayer = 0;
                float otherLayer = 0;
                if (i < layers.Count)
                {
                    thisLayer = layers[i];
                }
                if (i < other.layers.Count)
                {
                    otherLayer = other.layers[i];
                }
                int compareTo = thisLayer.CompareTo(otherLayer);
                if (compareTo != 0)
                {
                    return compareTo;
                }
            }
            return 0;
        }

        public static bool operator ==(LayerDepth left, LayerDepth right)
        {
            if (left is null)
            {
                return right is null;
            }
            return left.Equals(right);
        }
        public static bool operator !=(LayerDepth left, LayerDepth right)
        {
            return !(left == right);
        }
        public static bool operator <(LayerDepth left, LayerDepth right)
        {
            return (left.CompareTo(right) < 0);
        }
        public static bool operator >(LayerDepth left, LayerDepth right)
        {
            return (left.CompareTo(right) > 0);
        }

        public override bool Equals(object obj)
        {
            if (obj == null || obj.GetType() != typeof(LayerDepth))
            {
                return false;
            }
            return CompareTo((LayerDepth)obj) == 0;
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }
    }
}
