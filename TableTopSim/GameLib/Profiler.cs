using Microsoft.JSInterop;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace GameLib
{
    public class Profiler
    {
        public bool Enabled { get; set; }
        public Profiler(bool enabled)
        {
            Enabled = enabled;
        }

        public async Task ConsoleTime(IJSRuntime jsRuntime, string message)
        {
            if (Enabled)
            {
                await jsRuntime.InvokeVoidAsync("console.time", message);
            }
        }
        public async Task ConsoleTimeEnd(IJSRuntime jsRuntime, string message)
        {
            if (Enabled)
            {
                await jsRuntime.InvokeVoidAsync("console.timeEnd", message);
            }
        }
    }
}
