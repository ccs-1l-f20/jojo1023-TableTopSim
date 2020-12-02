using Microsoft.AspNetCore.Components;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace GameLib.Sprites
{
    public class SpriteRefrenceManager
    {
        //Dictionary<int, Sprite> defaultSprites;
        //Dictionary<Sprite, int> defaultSpriteAddresses;
        public Dictionary<int, Sprite> SpriteRefrences { get; set; }
        public Dictionary<Sprite, int> SpriteAddresses { get; set; }
        public Dictionary<int, ElementReference> ImageElementRefs { get; private set; }
        public ElementReference ImageNotFound { get; private set; }

        public event Action<int> SpriteAdded;
        Random random;
        public SpriteRefrenceManager(Dictionary<int, ElementReference> imageElementRefs, ElementReference imageNotFound)
        {
            SpriteRefrences = new Dictionary<int, Sprite>();
            SpriteAddresses = new Dictionary<Sprite, int>();
            ImageElementRefs = imageElementRefs;
            ImageNotFound = imageNotFound;
            random = new Random();
        }
        public void Reset()
        {
            SpriteRefrences.Clear();
            SpriteAddresses.Clear();
        }
        public Sprite GetSprite(int address)
        {
            return SpriteRefrences[address];
        }
        public bool ContainsAddress(int address)
        {
            return SpriteRefrences.ContainsKey(address);
        }
        public int GetAddress(Sprite sprite)
        {
            return SpriteAddresses[sprite];
        }
        public int AddSprite(Sprite sprite)
        {
            int newAdd = random.Next();
            while (SpriteRefrences.ContainsKey(newAdd))
            {
                newAdd = random.Next();
            }
            AddSprite(newAdd, sprite);
            SpriteAdded?.Invoke(newAdd);
            return newAdd;
        }

        public void AddSprite(int address, Sprite sprite)
        {
            SpriteRefrences.Add(address, sprite);
            SpriteAddresses.Add(sprite, address);
        }
        public bool RemoveSprite(int address)
        {
            if (SpriteRefrences.ContainsKey(address))
            {
                Sprite s = SpriteRefrences[address];
                SpriteRefrences.Remove(address);
                SpriteAddresses.Remove(s);
                return true;
            }
            return false;
        }
        public bool RemoveSprite(Sprite sprite)
        {
            if (SpriteAddresses.ContainsKey(sprite))
            {
                int a = SpriteAddresses[sprite];
                SpriteAddresses.Remove(sprite);
                SpriteRefrences.Remove(a);
                return true;
            }
            return false;
        }
        public void UpdateSpriteAddresses()
        {
            SpriteAddresses = SpriteRefrences.ToDictionary(kv => kv.Value, kv => kv.Key);
        }
    }
}
