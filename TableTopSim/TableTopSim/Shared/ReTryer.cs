using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace TableTopSim.Shared
{
    public static class ReTryer
    {
        public static async Task<T?> Try<T>(int delay, int amountOfTries, Func<Task<T?>> func) where T : struct
        {
            for (int i = 0; i < amountOfTries; i++)
            {
                T? output = await func.Invoke();
                if (output != null)
                {
                    return output;
                }
                if (i + 1 < amountOfTries)
                {
                    await Task.Delay(delay);
                }
            }
            return null;
        }
        public static async Task<T> Try<T>(int delay, int amountOfTries, Func<Task<T>> func) where T : class
        {
            for (int i = 0; i < amountOfTries; i++)
            {
                T output = await func.Invoke();
                if (output != null)
                {
                    return output;
                }
                if (i + 1 < amountOfTries)
                {
                    await Task.Delay(delay);
                }
            }
            return null;
        }
        public static async Task<bool> Try(int delay, int amountOfTries, Func<Task<bool>> func)
        {
            for (int i = 0; i < amountOfTries; i++)
            {
                bool success = await func.Invoke();
                if (success)
                {
                    return true;
                }
                if (i + 1 < amountOfTries)
                {
                    await Task.Delay(delay);
                }
            }
            return false;
        }
    }
}
