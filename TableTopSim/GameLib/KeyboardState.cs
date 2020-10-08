using Microsoft.AspNetCore.Components.Web;
using System;
using System.Collections.Generic;
using System.Text;

namespace GameLib
{

    public class KeyboardState
    {
        Dictionary<string, KeyInfo> keyInfos;
        public bool AltKey { get; private set; }
        public bool CtrlKey { get; private set; }
        public bool ShiftKey { get; private set; }

        public KeyInfo this[string code]
        {
            get { return keyInfos.ContainsKey(code) ? keyInfos[code] : new KeyInfo(code, "", false, false, false, false); }
        }
        public KeyboardState()
        {
            keyInfos = new Dictionary<string, KeyInfo>();
            AltKey = false;
            ShiftKey = false;
            CtrlKey = false;
        }

        public KeyInfo KeyDown(KeyboardEventArgs args)
        {
            AltKey = args.AltKey;
            ShiftKey = args.ShiftKey;
            CtrlKey = args.CtrlKey;
            KeyInfo keyInfo = new KeyInfo(args.Code, args.Key, true, args.Repeat, false, false);
            if (keyInfos.ContainsKey(args.Code))
            {
                var prev = keyInfos[args.Code];
                keyInfo.LastDown = prev.Down;
                keyInfo.LastRepeat = prev.Repeat;
                keyInfos[args.Code] = keyInfo;
            }
            else
            {
                keyInfos.Add(args.Code, keyInfo);
            }
            return keyInfo;
        }
        public KeyInfo KeyUp(KeyboardEventArgs args)
        {
            AltKey = args.AltKey;
            ShiftKey = args.ShiftKey;
            CtrlKey = args.CtrlKey;

            KeyInfo keyInfo = new KeyInfo(args.Code, args.Key, false, false, true, false);
            if (keyInfos.ContainsKey(args.Code))
            {
                var prev = keyInfos[args.Code];
                keyInfo.LastDown = prev.Down;
                keyInfo.LastRepeat = prev.Repeat;
                keyInfos[args.Code] = keyInfo;
            }
            else
            {
                keyInfos.Add(args.Code, keyInfo);
            }
            return keyInfo;
        }

    }
    public struct KeyInfo
    {
        public string Code { get; set; }
        public string Key { get; set; }
        public bool Repeat { get; set; }
        public bool Down { get; set; }
        public bool LastDown { get; set; }
        public bool LastRepeat { get; set; }
        public KeyInfo(string code, string key, bool down, bool repeat, bool lastDown, bool lastRepeat)
        {
            Code = code;
            Key = key;
            Down = down;
            Repeat = repeat;
            LastDown = lastDown;
            LastRepeat = lastRepeat;
        }
    }
}
