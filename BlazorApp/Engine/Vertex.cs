using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Blazor.Extensions.Canvas.WebGL;
using Blazor.Extensions;
using System.Numerics;
using GlmNet;


namespace BlazorApp
{
    public class Vertex : ComponentBase
    {
        private WebGLContext _context;
        protected BECanvasComponent _canvasReference;

        private const string VS_SOURCE = "attribute vec2 aPos;" +
                                         "attribute vec3 aColor;" +
                                         "varying vec3 vColor;" +

                                         "void main() {" +
                                            "vColor = aColor;" +
                                            "gl_Position = vec4(aPos, 0.0, 1.0);" +
                                         "}";

        private const string FS_SOURCE = "precision mediump float;" +
                                         "varying vec3 vColor;" +

                                         "void main() {" +
                                            "gl_FragColor = vec4(vColor, 1.0);" +
                                         "}";

        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            this._context = await this._canvasReference.CreateWebGLAsync();

            await this._context.ClearColorAsync(1, 1, 1, 1);
            await this._context.ClearAsync(BufferBits.COLOR_BUFFER_BIT | BufferBits.DEPTH_BUFFER_BIT);
            var program = await this.InitProgramAsync(this._context, VS_SOURCE, FS_SOURCE);

            var vertexBuffer = await this._context.CreateBufferAsync(); // Create a buffer to pass variables to the GPU RAM.
            await this._context.BindBufferAsync(BufferType.ARRAY_BUFFER, vertexBuffer);

            var vertices = new[] // Using an index buffer to avoid overlap isnt working for some reason ...
            {
                // X     Y          R    G    B
                 0.0f,  0.5f,     1.0f, 1.0f, 0.0f,     // top right
                 -0.5f, -0.5f,     0.7f, 0.0f, 1.0f,    // bottom right
                 0.5f,  -0.5f,    0.1f, 1.0f, 0.6f      // top left 
            };

            // Everything below here is where drawing an object is done
            await this._context.BufferDataAsync(BufferType.ARRAY_BUFFER, vertices, BufferUsageHint.STATIC_DRAW); // Specifying which array to pass to the GPU 
            //await this._context.BufferDataAsync(BufferType.ELEMENT_ARRAY_BUFFER, indices, BufferUsageHint.STATIC_DRAW); // index buffer

            var positionAttribLocation = await _context.GetAttribLocationAsync(program, "aPos");
            var colorAttribLocation = await _context.GetAttribLocationAsync(program, "aColor");


            await this._context.VertexAttribPointerAsync((uint)positionAttribLocation, 2, DataType.FLOAT, false, 5 * sizeof(float), 0); // index, size of the vertex, type, normalized, how much info in a vertex
            await this._context.VertexAttribPointerAsync((uint)colorAttribLocation, 3, DataType.FLOAT, false, 5 * sizeof(float), 2 * sizeof(float)); //offset here because the color is on the 4th value
            await this._context.EnableVertexAttribArrayAsync((uint)positionAttribLocation);
            await this._context.EnableVertexAttribArrayAsync((uint)colorAttribLocation);


            // Here is where we WOULD do a render loop, like while (true)

            await this._context.UseProgramAsync(program);

            await this._context.DrawArraysAsync(Primitive.TRIANGLES, 0, 3); // type, how many to skip, how many vertices
        }

        private async Task<WebGLProgram> InitProgramAsync(WebGLContext gl, string vsSource, string fsSource)
        {
            var vertexShader = await this.LoadShaderAsync(gl, ShaderType.VERTEX_SHADER, vsSource); // compiling shaders
            var fragmentShader = await this.LoadShaderAsync(gl, ShaderType.FRAGMENT_SHADER, fsSource); // compiling shaders

            var program = await gl.CreateProgramAsync();
            await gl.AttachShaderAsync(program, vertexShader);
            await gl.AttachShaderAsync(program, fragmentShader);
            await gl.LinkProgramAsync(program);

            await gl.DeleteShaderAsync(vertexShader);
            await gl.DeleteShaderAsync(fragmentShader);

            if (!await gl.GetProgramParameterAsync<bool>(program, ProgramParameter.LINK_STATUS))
            {
                string info = await gl.GetProgramInfoLogAsync(program);
                throw new Exception("An error occured while linking the program: " + info);
            }

            // Might want to gl.ValidateProgramAsync here

            return program;
        }

        // Shader compiler stuff
        private async Task<WebGLShader> LoadShaderAsync(WebGLContext gl, ShaderType type, string source)
        {
            var shader = await gl.CreateShaderAsync(type);

            await gl.ShaderSourceAsync(shader, source);
            await gl.CompileShaderAsync(shader);

            if (!await gl.GetShaderParameterAsync<bool>(shader, ShaderParameter.COMPILE_STATUS))
            {
                string info = await gl.GetShaderInfoLogAsync(shader);
                await gl.DeleteShaderAsync(shader);
                throw new Exception("An error occured while compiling the shader: " + info);
            }

            return shader;
        }
    }

}
