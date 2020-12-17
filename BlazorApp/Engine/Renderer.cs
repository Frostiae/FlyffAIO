using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using System.IO;
using System.Numerics;
using Blazor.Extensions.Canvas.WebGL;
using Blazor.Extensions;
using Blazor.Extensions.Canvas;


namespace BlazorApp
{
    public class Renderer
    {

        private BECanvasComponent _canvasReference;
        WebGLContext _context;
        public long canvasWidth = 0;
        public long canvasHeight = 0;

        byte[] byteArr;

        byte[] textureData = new byte[4] { 0, 0, 255, 0 };

        WebGLUniformLocation u_matrix_location;

        private const string VS_SOURCE =
            "attribute vec3 aPos;" +
            "attribute vec2 aTex;" +
            "varying vec2 vTex;" +
            "uniform mat4 u_matrix;" +

            "void main() {" +
                "gl_Position = u_matrix * vec4(aPos, 1.0);" +
                "vTex = aTex;" +
            "}";

        private const string FS_SOURCE = "precision mediump float;" +
                                         "varying vec2 vTex;" +
                                         "uniform sampler2D u_texture;" +

                                         "void main() {" +
                                            "gl_FragColor = texture2D(u_texture, vTex);" +
                                         "}";

        Vector3 transVector = new Vector3((float)-1, (float)-1, 0);

        WebGLBuffer vertexBuffer;

    }

}
