using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace GameLib.Sprites
{
    public class SpriteRefrenceManager
    {
        Dictionary<int, Sprite> defaultSprites;
        Dictionary<Sprite, int> defaultSpriteAddresses;
        public Dictionary<int, Sprite> SpriteRefrences { get; set; }
        public Dictionary<Sprite, int> SpriteAddresses { get; set; }
        public SpriteRefrenceManager(Dictionary<int, Sprite> defaultSprites)
        {
            this.defaultSprites = defaultSprites;
            defaultSpriteAddresses = defaultSprites.ToDictionary(kv => kv.Value, kv => kv.Key);
            SpriteRefrences = new Dictionary<int, Sprite>();
            SpriteAddresses = new Dictionary<Sprite, int>();
        }
        public void Reset()
        {
            SpriteRefrences.Clear();
            SpriteAddresses.Clear();
        }
        public Sprite GetSprite(int address)
        {
            if (defaultSprites.ContainsKey(address))
            {
                return defaultSprites[address];
            }
            return SpriteRefrences[address];
        }
        public bool ContainsAddress(int address)
        {
            return defaultSprites.ContainsKey(address) || SpriteRefrences.ContainsKey(address);
        }
        public int GetAddress(Sprite sprite)
        {
            if (defaultSpriteAddresses.ContainsKey(sprite))
            {
                return defaultSpriteAddresses[sprite];
            }
            return SpriteAddresses[sprite];
        }
        public void AddSprite(int address, Sprite sprite)
        {
            if (defaultSprites.ContainsKey(address) || defaultSpriteAddresses.ContainsKey(sprite))
            {
                throw new IndexOutOfRangeException();
            }
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
