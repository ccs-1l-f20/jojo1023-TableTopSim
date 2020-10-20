using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace TableTopSim.Client
{
    public class GlobalData
    {
        public event Action<string> OnShowMessage;
        public event Func<Task> OnBrowserResize;
        public void ShowMessage(string message)
        {
            OnShowMessage?.Invoke(message);
        }
        public void Reset()
        {
            if (OnBrowserResize != null)
            {
                foreach (var d in OnBrowserResize.GetInvocationList())
                {
                    OnBrowserResize -= (d as Func<Task>);
                }
            }
        }
        public async Task BrowserResized()
        {
            if (OnBrowserResize != null)
            {
                await OnBrowserResize.Invoke();
            }
        }
    }
}
