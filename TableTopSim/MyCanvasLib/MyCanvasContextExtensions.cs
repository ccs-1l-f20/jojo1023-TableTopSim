using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace MyCanvasLib
{
    public static class CanvasContextExtensions
    {
        public static MyCanvas2DContext CreateCanvas2D(this MyCanvasClass canvas)
        {
            return new MyCanvas2DContext(canvas).InitializeAsync().GetAwaiter().GetResult() as MyCanvas2DContext;
        }

        public static async Task<MyCanvas2DContext> CreateCanvas2DAsync(this MyCanvasClass canvas)
        {
            return await new MyCanvas2DContext(canvas).InitializeAsync().ConfigureAwait(false) as MyCanvas2DContext;
        }
    }
}
