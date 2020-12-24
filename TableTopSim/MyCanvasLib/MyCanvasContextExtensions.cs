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

        public static MyWebGLContext CreateWebGL(this MyCanvasClass canvas)
        {
            return new MyWebGLContext(canvas).InitializeAsync().GetAwaiter().GetResult() as MyWebGLContext;
        }

        public static async Task<MyWebGLContext> CreateWebGLAsync(this MyCanvasClass canvas)
        {
            return await new MyWebGLContext(canvas).InitializeAsync().ConfigureAwait(false) as MyWebGLContext;
        }

        public static MyWebGLContext CreateWebGL(this MyCanvasClass canvas, WebGLContextAttributes attributes)
        {
            return new MyWebGLContext(canvas, attributes).InitializeAsync().GetAwaiter().GetResult() as MyWebGLContext;
        }

        public static async Task<MyWebGLContext> CreateWebGLAsync(this MyCanvasClass canvas, WebGLContextAttributes attributes)
        {
            return await new MyWebGLContext(canvas, attributes).InitializeAsync().ConfigureAwait(false) as MyWebGLContext;
        }
    }
}
