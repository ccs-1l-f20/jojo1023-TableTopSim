using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace GameLib.Sprites
{
    public class SpriteRefrenceManager
    {
        public Dictionary<int, Sprite> SpriteRefrences { get; set; }
        public Dictionary<Sprite, int> SpriteAddresses { get; set; }

        public SpriteRefrenceManager()
        {
            SpriteRefrences = new Dictionary<int, Sprite>();
            SpriteAddresses = new Dictionary<Sprite, int>();
        }
        public void Reset()
        {
            SpriteRefrences.Clear();
            SpriteAddresses.Clear();
        }

        public void UpdateSpriteAddresses()
        {
            SpriteAddresses = SpriteRefrences.ToDictionary(kv => kv.Value, kv => kv.Key);
        }
    }
}
