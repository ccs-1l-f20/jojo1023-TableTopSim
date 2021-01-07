using MathNet.Numerics.LinearAlgebra;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace MyCanvasLib
{
    public class WebGLWrapper
    {
        MyWebGLContext context;
        float red, green, blue, alpha;
        //WebGLProgram shaderProgram;
        //int vertexPosition;
        //WebGLUniformLocation projectionMatrix;
        //WebGLUniformLocation modelViewMatrix;
        float clientWidth, clientHeight;
        public WebGLWrapper(MyWebGLContext context, float red, float green, float blue, float alpha, float clientWidth, float clientHeight)
        {
            this.context = context;
            this.red = red;
            this.green = green;
            this.blue = blue;
            this.alpha = alpha;
            this.clientWidth = clientWidth;
            this.clientHeight = clientHeight;
        }
        public async Task Init()
        {
            //await context.ClearColorAsync(red, green, blue, alpha);
            //await context.ClearAsync(BufferBits.COLOR_BUFFER_BIT);
            string vsSource = @"
            attribute vec4 aVertexPosition;

            uniform mat4 uModelViewMatrix;
            uniform mat4 uProjectionMatrix;

            void main()
            {
                gl_Position = uProjectionMatrix * uModelViewMatrix * aVertexPosition;
            }
            ";

            string fsSource = @"
    void main()
            {
                gl_FragColor = vec4(1.0, 1.0, 1.0, 1.0);
            }
  ";
            var shaderProgram = await InitShaderProgram(vsSource, fsSource);
            var vertexPosition = await context.GetAttribLocationAsync(shaderProgram, "aVertexPosition");
            var projectionMatrix = await context.GetUniformLocationAsync(shaderProgram, "uProjectionMatrix");
            var modelViewMatrix = await context.GetUniformLocationAsync(shaderProgram, "uModelViewMatrix");

            var buffers = await InitBuffers();
            await DrawScene(shaderProgram, (uint)vertexPosition, projectionMatrix, modelViewMatrix, buffers);
        }
        async Task<WebGLBuffer> InitBuffers()
        {
            var positionBuffer = await context.CreateBufferAsync();
            await context.BindBufferAsync(BufferType.ARRAY_BUFFER, positionBuffer);

            float[] positions = new float[] {
            - 1, 1,
             1, 1,
            -1, -1,
             1, -1,
          };

            await context.BufferDataAsync<float>(BufferType.ARRAY_BUFFER, positions, BufferUsageHint.STATIC_DRAW);
            return positionBuffer;
        }

        public async Task DrawScene(WebGLProgram shaderProgram, uint vertexPosition, 
            WebGLUniformLocation programProjectionMatrix, 
            WebGLUniformLocation programModelViewMatrix,
            WebGLBuffer buffers)
        {
            await context.BeginBatchAsync();
            await context.ClearColorAsync(0, 0, 0, 1);  // Clear to black, fully opaque
            await context.ClearDepthAsync(1);                 // Clear everything
            await context.EnableAsync(EnableCap.DEPTH_TEST);           // Enable depth testing
            await context.DepthFuncAsync(CompareFunction.LEQUAL);            // Near things obscure far things

            // Clear the canvas before we start drawing on it.

            await context.ClearAsync(BufferBits.COLOR_BUFFER_BIT | BufferBits.DEPTH_BUFFER_BIT);

            // Create a perspective matrix, a special matrix that is
            // used to simulate the distortion of perspective in a camera.
            // Our field of view is 45 degrees, with a width/height
            // ratio that matches the display size of the canvas
            // and we only want to see objects between 0.1 units
            // and 100 units away from the camera.

            float fieldOfView = (float)(45 * Math.PI / 180);   // in radians
            float aspect = clientWidth / clientHeight;
            float zNear = 0.1f;
            float zFar = 100;

            // note: glmatrix.js always has the first argument
            // as the destination to receive the result.
            var projectionMatrix = CreatePerspectiveMatrix(fieldOfView, aspect, zNear, zFar);

            // Set the drawing position to the "identity" point, which is
            // the center of the scene.
            Matrix<float> modelViewMatrix = CreateMatrix.Dense<float>(4,4);// amount to translate
            // Now move the drawing position a bit to where we want to
            // start drawing the square.
            modelViewMatrix.SetDiagonal(new float[] { 0, 0, -6, 1 });
            // Tell WebGL how to pull out the positions from the position
            // buffer into the vertexPosition attribute.
            {
                int numComponents = 2;  // pull out 2 values per iteration

                DataType type = DataType.FLOAT;    // the data in the buffer is 32bit floats
                bool normalize = false;  // don't normalize
                int stride = 0;         // how many bytes to get from one set of values to the next
                                          // 0 = use type and numComponents above
                int offset = 0;         // how many bytes inside the buffer to start from
                await context.BindBufferAsync(BufferType.ARRAY_BUFFER, buffers);
                //gl.bindBuffer(gl.ARRAY_BUFFER, buffers.position);
                await context.VertexAttribPointerAsync(vertexPosition, numComponents, type, normalize, stride, offset);
                await context.EnableVertexAttribArrayAsync(vertexPosition);
            }

            // Tell WebGL to use our program when drawing
            await context.UseProgramAsync(shaderProgram);

            // Set the shader uniforms
            await context.UniformMatrixAsync(programProjectionMatrix, false, projectionMatrix.ToColumnMajorArray());
            await context.UniformMatrixAsync(programModelViewMatrix, false, modelViewMatrix.ToColumnMajorArray());

            {
                int offset = 0;
                int vertexCount = 4;
                await context.DrawArraysAsync(Primitive.TRINAGLE_STRIP, offset, vertexCount);
            }
            await context.EndBatchAsync();
        }


        async Task<WebGLProgram> InitShaderProgram(string vsSource, string fsSource)
        {
            var vertexShader = await LoadShader(ShaderType.VERTEX_SHADER, vsSource);
            var fragmentShader = await LoadShader(ShaderType.FRAGMENT_SHADER, fsSource);
            var shaderProgram = await context.CreateProgramAsync();

            await context.AttachShaderAsync(shaderProgram, vertexShader);
            await context.AttachShaderAsync(shaderProgram, fragmentShader);
            await context.LinkProgramAsync(shaderProgram);

            if (!(await context.GetProgramParameterAsync<bool>(shaderProgram, ProgramParameter.LINK_STATUS)))
            {
                throw new Exception("Unable to initialize the shader program");
            }
            return shaderProgram;
        }
        async Task<WebGLShader> LoadShader(ShaderType shaderType, string source)
        {

            var shader = await context.CreateShaderAsync(shaderType);

            await context.ShaderSourceAsync(shader, source);


            await context.CompileShaderAsync(shader);

            if (!(await context.GetShaderParameterAsync<bool>(shader, ShaderParameter.COMPILE_STATUS)))
            {
                await context.DeleteShaderAsync(shader);
                throw new Exception("An error occurred compiling the shaders");
            }

            return shader;
        }

        static Matrix<float> CreatePerspectiveMatrix(float fovy, float aspect, float near, float? far)
        {
            Matrix<float> matrix = CreateMatrix.Dense<float>(4, 4);
            float f = (float)(1.0 / Math.Tan(fovy / 2)),
              nf;
            matrix[0, 0] = f / aspect;
            matrix[0, 1] = 0;
            matrix[0, 2] = 0;
            matrix[0, 3] = 0;
            matrix[1, 0] = 0;
            matrix[1, 1] = f;
            matrix[1, 2] = 0;
            matrix[1, 3] = 0;
            matrix[2, 0] = 0;
            matrix[2, 1] = 0;
            matrix[2, 3] = -1;
            matrix[3, 0] = 0;
            matrix[3, 1] = 0;
            matrix[3, 3] = 0;
            if (far != null && !double.IsInfinity(far.Value))
            {
                nf = 1 / (near - far.Value);
                matrix[2, 2] = (far.Value + near) * nf;
                matrix[3, 2] = 2 * far.Value * near * nf;
            }
            else
            {
                matrix[2, 2] = -1;
                matrix[3, 2] = -2 * near;
            }
            return matrix;
        }
        //static Matrix<float> TranslateMatrix(Matrix<float> matrix, Vector<float> vector)
        //{
        //    Vector<float> vec4 = CreateVector.Dense<float>(4);
        //    vector.CopyTo(vec4);
        //    vec4[3] = 1;
        //    Matrix<float> retM = CreateMatrix.Dense<float>(4, 4);
        //    retM.SetColumn(0, matrix.Column(0).PointwiseMultiply(vec4));
        //    retM.SetColumn(1, matrix.Column(1).PointwiseMultiply(vec4));
        //    retM.SetColumn(2, matrix.Column(2).PointwiseMultiply(vec4));
        //    retM.SetColumn(3, matrix.Column(3).PointwiseMultiply(vec4));
        //    return retM;
        //}
    }
}

