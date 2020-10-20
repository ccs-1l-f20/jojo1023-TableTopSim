using Microsoft.JSInterop;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace TableTopSim.Client
{
    public class BrowserResize
    {
        public static event Func<Task> OnResize;

        [JSInvokable]
        public static async Task OnBrowserResize()
        {
            await OnResize?.Invoke();
        }

        public static async Task<int> GetInnerHeight(IJSRuntime jSRuntime)
        {
            return await jSRuntime.InvokeAsync<int>("browserResize.getInnerHeight");
        }

        public static async Task<int> GetInnerWidth(IJSRuntime jSRuntime)
        {
            return await jSRuntime.InvokeAsync<int>("browserResize.getInnerWidth");
        }
    }
}
