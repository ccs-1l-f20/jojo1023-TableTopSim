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

        BindingList<float> layers;
        [GameSerializableData(layersDataId)]
        public BindingList<float> Layers { get => layers; set { layers = value; OnLayersChanged?.Invoke(this, layersDataId); } }

        public float this[int index]
        {
            get => Layers[index];
            set { Layers[index] = value; }
        }

        static LayerDepth()
        {
            GameSerialize.AddType<LayerDepth>(GameSerialize.GenericSerializeFunc, GameSerialize.GenericDeserializeFunc, true);
        }
        public LayerDepth()
        {
            layers = new BindingList<float>();
            layers.AddingNew += Layers_AddingNew;
            layers.ListChanged += Layers_ListChanged;
        }
        public LayerDepth(float layer)
        {
            layers = new BindingList<float>();
            layers.Add(layer);
            layers.AddingNew += Layers_AddingNew;
            layers.ListChanged += Layers_ListChanged;
        }
        public LayerDepth(IList<float> layerList)
        {
            layers = new BindingList<float>(layerList);
            layers.AddingNew += Layers_AddingNew;
            layers.ListChanged += Layers_ListChanged;
        }
        public void AddTo(LayerDepth other)
        {
            for (int i = 0; i < other.layers.Count; i++)
            {
                layers.Add(other.layers[i]);
            }
        }
        private void Layers_ListChanged(object sender, ListChangedEventArgs e)
        {
            OnLayersChanged?.Invoke(this, layersDataId);
        }

        private void Layers_AddingNew(object sender, AddingNewEventArgs e)
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
