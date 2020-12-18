using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Blazor.Extensions.Canvas.WebGL;
using Blazor.Extensions;
using System.Numerics;
using GlmNet;
using Microsoft.JSInterop;


namespace BlazorApp
{
    public class Vertex : ComponentBase
    {


        private WebGLContext _context;
        protected BECanvasComponent _canvasReference;
        float angle;
        mat4 worldMatrix = new mat4();
        mat4 viewMatrix = new mat4();
        mat4 projMatrix = new mat4();

        mat4 xRotationMatrix = new mat4();
        mat4 yRotationMatrix = new mat4();

        WebGLTexture boxTexture;

        [Inject]
        internal IJSRuntime JSRuntime { get; set; }

        private const string VS_SOURCE = @"attribute vec3 aPos;
                                         attribute vec2 vertTexCoord;
                                         varying vec2 fragTexCoord;
                                         uniform mat4 mWorld;
                                         uniform mat4 mView;
                                         uniform mat4 mProj;

                                         void main() {
                                            fragTexCoord = vertTexCoord;
                                            gl_Position = mProj * mView * mWorld * vec4(aPos, 1.0);
                                         }";

        private const string FS_SOURCE = @"precision mediump float;
                                         varying vec2 fragTexCoord;
                                         uniform sampler2D sampler;

                                         void main() {
                                            gl_FragColor = texture2D(sampler, fragTexCoord);
                                         }";

        float[] boxVertices = new[] // Using an index buffer to avoid overlap isnt working for some reason ...
                {
                //    X     Y    Z          U     V
                	-1.0f, 1.0f, -1.0f,    0.0f, 0.0f,
                    -1.0f, 1.0f, 1.0f,     0.0f, 1.0f,
                    1.0f, 1.0f, 1.0f,      1.0f, 1.0f,
                    1.0f, 1.0f, -1.0f,     1.0f, 0.0f,
                                           
		            // Left                
		            -1.0f, 1.0f, 1.0f,     0.0f, 0.0f,
                    -1.0f, -1.0f, 1.0f,    1.0f, 0.0f,
                    -1.0f, -1.0f, -1.0f,   1.0f, 1.0f,
                    -1.0f, 1.0f, -1.0f,    0.0f, 1.0f,
                                           
		            // Right               
		            1.0f, 1.0f, 1.0f,      1.0f, 1.0f,
                    1.0f, -1.0f, 1.0f,     0.0f, 1.0f,
                    1.0f, -1.0f, -1.0f,    0.0f, 0.0f,
                    1.0f, 1.0f, -1.0f,     1.0f, 0.0f,
                                           
		            // Front               
		            1.0f, 1.0f, 1.0f,      1.0f, 1.0f,
                    1.0f, -1.0f, 1.0f,     1.0f, 0.0f,
                    -1.0f, -1.0f, 1.0f,    0.0f, 0.0f,
                    -1.0f, 1.0f, 1.0f,     0.0f, 1.0f,
                                           
		            // Back                
		            1.0f, 1.0f, -1.0f,     0.0f, 0.0f,
                    1.0f, -1.0f, -1.0f,    0.0f, 1.0f,
                    -1.0f, -1.0f, -1.0f,   1.0f, 1.0f,
                    -1.0f, 1.0f, -1.0f,    1.0f, 0.0f,

		            // Bottom
		            -1.0f, -1.0f, -1.0f,   1.0f, 1.0f,
                    -1.0f, -1.0f, 1.0f,    1.0f, 0.0f,
                    1.0f, -1.0f, 1.0f,     0.0f, 0.0f,
                    1.0f, -1.0f, -1.0f,    0.0f, 1.0f
                };

        UInt16[] boxIndices = new UInt16[]
        {
                    0, 1, 2,
                    0, 2, 3,

		            // Left
		            5, 4, 6,
                    6, 4, 7,

		            // Right
		            8, 9, 10,
                    8, 10, 11,

		            // Front
		            13, 12, 14,
                    15, 14, 12,

		            // Back
		            16, 17, 18,
                    16, 18, 19,

		            // Bottom
		            21, 20, 22,
                    22, 20, 23
                };

        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            if (firstRender)
            {
                var dotNetReference = DotNetObjectReference.Create(this);
                this._context = await this._canvasReference.CreateWebGLAsync();

                await _context.ClearColorAsync(0.75f, 0.85f, 0.8f, 0.0f);
                await _context.ClearAsync(BufferBits.COLOR_BUFFER_BIT | BufferBits.DEPTH_BUFFER_BIT);

                // Depth testing and backface culling
                await _context.EnableAsync(EnableCap.DEPTH_TEST);
                await _context.EnableAsync(EnableCap.CULL_FACE);
                await _context.CullFaceAsync(Face.BACK);
                await _context.FrontFaceAsync(FrontFaceDirection.CCW);

                var program = await InitProgramAsync(this._context, VS_SOURCE, FS_SOURCE);

                var vertexBuffer = await this._context.CreateBufferAsync(); // Create a buffer to pass variables to the GPU RAM.
                await this._context.BindBufferAsync(BufferType.ARRAY_BUFFER, vertexBuffer);

                var indexBuffer = await _context.CreateBufferAsync();
                await _context.BindBufferAsync(BufferType.ELEMENT_ARRAY_BUFFER, indexBuffer);

                // Everything below here is where drawing an object is done
                await this._context.BufferDataAsync(BufferType.ARRAY_BUFFER, boxVertices, BufferUsageHint.STATIC_DRAW); // Specifying which array to pass to the GPU 
                await this._context.BufferDataAsync(BufferType.ELEMENT_ARRAY_BUFFER, boxIndices, BufferUsageHint.STATIC_DRAW); // index buffer

                var positionAttribLocation = await _context.GetAttribLocationAsync(program, "aPos");
                var texCoordAttribLocation = await _context.GetAttribLocationAsync(program, "vertTexCoord");


                await this._context.VertexAttribPointerAsync((uint)positionAttribLocation, 3, DataType.FLOAT, false, 5 * sizeof(float), 0); // index, size of the vertex, type, normalized, how much info in a vertex
                await this._context.VertexAttribPointerAsync((uint)texCoordAttribLocation, 2, DataType.FLOAT, false, 5 * sizeof(float), 3 * sizeof(float)); //offset here because the color is on the 4th value
                await this._context.EnableVertexAttribArrayAsync((uint)positionAttribLocation);
                await this._context.EnableVertexAttribArrayAsync((uint)texCoordAttribLocation);


                //
                // Create texture
                //
                boxTexture = await _context.CreateTextureAsync();
                await _context.BindTextureAsync(TextureType.TEXTURE_2D, boxTexture);
                await _context.TexParameterAsync(TextureType.TEXTURE_2D, TextureParameter.TEXTURE_WRAP_S, (float)TextureParameterValue.CLAMP_TO_EDGE);
                await _context.TexParameterAsync(TextureType.TEXTURE_2D, TextureParameter.TEXTURE_WRAP_T, (float)TextureParameterValue.CLAMP_TO_EDGE);
                await _context.TexParameterAsync(TextureType.TEXTURE_2D, TextureParameter.TEXTURE_MIN_FILTER, (float)TextureParameterValue.LINEAR);
                await _context.TexParameterAsync(TextureType.TEXTURE_2D, TextureParameter.TEXTURE_MAG_FILTER, (float)TextureParameterValue.LINEAR);

                await JSRuntime.InvokeVoidAsync("anim.loadTexture", "/img/create.png");
                await _context.BindTextureAsync(TextureType.TEXTURE_2D, null);

                await this._context.UseProgramAsync(program); // Tell OpenGl which program should be active

                var matWorldUniformLocation = await _context.GetUniformLocationAsync(program, "mWorld");
                var matViewUniformLocation = await _context.GetUniformLocationAsync(program, "mView");
                var matProjUniformLocation = await _context.GetUniformLocationAsync(program, "mProj");

                await JSRuntime.InvokeVoidAsync("anim.start", dotNetReference, matWorldUniformLocation);

                // Camera is setup here
                worldMatrix = mat4.identity();
                viewMatrix = glm.lookAt(new vec3(0, 0, -8), new vec3(0, 0, 0), new vec3(0, 1, 0));
                projMatrix = glm.perspective(glm.radians(45), _canvasReference.Width / _canvasReference.Height, 0.1f, 1000.0f);

                await _context.UniformMatrixAsync(matWorldUniformLocation, false, worldMatrix.to_array());
                await _context.UniformMatrixAsync(matViewUniformLocation, false, viewMatrix.to_array());
                await _context.UniformMatrixAsync(matProjUniformLocation, false, projMatrix.to_array());

                // Here is where we WOULD do a render loop, like while (true)

                //await this._context.DrawArraysAsync(Primitive.TRIANGLES, 0, 3); // type, how many to skip, how many vertices
            }
        }

        [JSInvokable("eventRequestAnimationFrame")]
        public async Task eventRequestAnimationFrame(float time, WebGLUniformLocation location)
        {
            // MAIN RENDER LOOP
            //Console.WriteLine("test");
            // TODO: Double buffer to remedy the flickering because of ClearAsync
            //await _context.ClearColorAsync(0, 0, 0, 1);

            angle = time / 1000 / 6 * 2 * MathF.PI; // not a good idea to create new variables here, this is a loop.
            yRotationMatrix = glm.rotate(mat4.identity(), angle, new vec3(0, 1, 0));
            xRotationMatrix = glm.rotate(mat4.identity(), angle / 4, new vec3(1, 0, 0));
            worldMatrix = yRotationMatrix * xRotationMatrix;


            await _context.UniformMatrixAsync(location, false, worldMatrix.to_array());

            await _context.BindTextureAsync(TextureType.TEXTURE_2D, boxTexture);
            await _context.ActiveTextureAsync(Texture.TEXTURE0);
            //await _context.ClearAsync(BufferBits.COLOR_BUFFER_BIT);
            await this._context.DrawElementsAsync(Primitive.TRIANGLES, boxIndices.Length, DataType.UNSIGNED_SHORT, 0); // type, how many to skip, how many vertices
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
