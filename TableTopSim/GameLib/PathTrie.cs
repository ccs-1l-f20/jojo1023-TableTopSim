using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace GameLib
{
    public class PathTrie<T>//:IEnumerable<KeyValuePair<IEnumerable<int>, T>>
    {
        TrieNode<T> root;
        HashSet<TrieNode<T>> endNodes;
        public int Count { get => endNodes.Count; }
        public PathTrie()
        {
            root = new TrieNode<T>();
            endNodes = new HashSet<TrieNode<T>>();
        }
        public T this[IEnumerable<int> key]
        {
            get
            {
                var node = FindNode(key);
                if(node == null || node.Key == null) { throw new KeyNotFoundException(); }
                return node.Value;
            }
            set
            {
                var node = FindNode(key);
                if (node == null || node.Key == null) { throw new KeyNotFoundException(); }
                node.SetValue(node.Key, value);
            }
        }
        public void InsertTrie(PathTrie<T> other, bool overideIfDup)
        {
            foreach(var endN in other.endNodes)
            {
                Insert(endN.Key, endN.Value, overideIfDup);
            }
        }
        public void Insert(IEnumerable<int> key, T value, bool overideIfDup = false)
        {
            TrieNode<T> currentNode = root;
            foreach(var k in key)
            {
                if (currentNode.Children.ContainsKey(k))
                {
                    currentNode = currentNode.Children[k];
                }
                else
                {
                    TrieNode<T> newNode = new TrieNode<T>();
                    currentNode.Children.Add(k, newNode);
                    currentNode = newNode;
                }
            }

            if (currentNode.Key != null && !overideIfDup) { throw new ArgumentException("Duplicate Key"); }

            currentNode.SetValue(key, value);
            endNodes.Add(currentNode);
        }

        TrieNode<T> FindNode(IEnumerable<int> key)
        {
            TrieNode<T> currentNode = root;
            foreach (var k in key)
            {
                if (currentNode.Children.ContainsKey(k))
                {
                    currentNode = currentNode.Children[k];
                }
                else
                {
                    return null;
                }
            }
            return currentNode;
        }
        public bool ContainsKey(IEnumerable<int> key)
        {
            TrieNode<T> node = FindNode(key);
            return node!= null && node.Key != null;
        }
        bool ContainsNode(IEnumerable<int> key)
        {
            TrieNode<T> node = FindNode(key);
            return node != null;
        }
        public HashSet<int> NextPathKeys(IEnumerable<int> key)
        {
            TrieNode<T> node = FindNode(key);
            if(node == null)
            {
                return null;
            }
            return new HashSet<int>(node.Children.Keys);
        }
        public void Clear()
        {
            root = new TrieNode<T>();
            endNodes.Clear();
        }
        public void ClearNodeChildren(IEnumerable<int> key)
        {
            TrieNode<T> node = FindNode(key);
            if(node != null)
            {
                node.Children.Clear();
            }
        }
        //public IEnumerator<KeyValuePair<IEnumerable<int>, T>> GetEnumerator()
        //{
        //    return new MyEnumerator<T>(endNodes);
        //}

        //IEnumerator IEnumerable.GetEnumerator()
        //{
        //    return new MyEnumerator<T>(endNodes);
        //}

        //private class MyEnumerator<U> : IEnumerator<KeyValuePair<IEnumerable<int>, U>>
        //{
        //    public KeyValuePair<IEnumerable<int>, U> Current => current;

        //    object IEnumerator.Current => current;


        //    HashSet<TrieNode<U>> endNodes;
        //    IEnumerator<TrieNode<U>> hashEnum;
        //    KeyValuePair<IEnumerable<int>, U> current;
        //    public MyEnumerator(HashSet<TrieNode<U>> endNodes)
        //    {
        //        this.endNodes = endNodes; 
        //        Reset();
        //    }

        //    public void Dispose()
        //    {
        //        hashEnum.Dispose();
        //    }

        //    public bool MoveNext()
        //    {
        //        if (hashEnum.MoveNext())
        //        {
        //            current = new KeyValuePair<IEnumerable<int>, U>(hashEnum.Current.Key, hashEnum.Current.Value);
        //            return true;
        //        }
        //        return false;
        //    }

        //    public void Reset()
        //    {
        //        hashEnum = endNodes.GetEnumerator();
        //        current = new KeyValuePair<IEnumerable<int>, U>(hashEnum.Current.Key, hashEnum.Current.Value);
        //    }
        //}
    }

    class TrieNode<T>
    {
        public T Value { get; private set; }
        public IEnumerable<int> Key { get; private set; }
        public Dictionary<int, TrieNode<T>> Children { get; private set; }

        public TrieNode()
        {
            Key = null;
            Children = new Dictionary<int, TrieNode<T>>();
        }

        public void SetValue(IEnumerable<int> key, T value)
        {
            Value = value;
            Key = key;
        }
    }
}
