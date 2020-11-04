using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
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
        //public Sprite GetSprite(int address)
        //{
        //    Sprite s = null;
        //    lock (SpriteRefrences)
        //    {
        //        if (SpriteRefrences.ContainsKey(address))
        //        {
        //            s = SpriteRefrences[address];
        //        }
        //    }
        //    return s;
        //}
        //public bool ContainsAddress(int address)
        //{
        //    bool b = false;
        //    lock (SpriteRefrences)
        //    {

        //    }
        //    return b;
        //}
        //public int GetAddress(Sprite sprite)
        //{
        //    int a;
        //    lock (SpriteRefrences)
        //    {
        //        a = SpriteAddresses[sprite];
        //    }
        //    return a;
        //}
        public void UpdateSpriteAddresses()
        {
            //lock (LockObject)
            //{
                SpriteAddresses = SpriteRefrences.ToDictionary(kv => kv.Value, kv => kv.Key);
            //}
        }
    }
}
