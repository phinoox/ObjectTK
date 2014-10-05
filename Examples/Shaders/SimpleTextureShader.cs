using DerpGL.Shaders;
using DerpGL.Shaders.Variables;
using DerpGL.Textures;
using OpenTK;
using OpenTK.Graphics.OpenGL;

namespace Examples.Shaders
{
    [VertexShaderSource("SimpleTexture.vs")]
    [FragmentShaderSource("SimpleTexture.fs")]
    public class SimpleTextureShader
        : Shader
    {
        [VertexAttrib(3, VertexAttribPointerType.Float)]
        public VertexAttrib InPosition { get; protected set; }
        [VertexAttrib(2, VertexAttribPointerType.Float)]
        public VertexAttrib InTexCoord { get; protected set; }

        public Uniform<Matrix4> ModelViewProjectionMatrix { get; protected set; }

        public TextureUniform<Texture2D> Texture { get; protected set; }
        public Uniform<bool> RenderTexCoords { get; protected set; }
    }
}