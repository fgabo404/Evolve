using OpenTK.Graphics.OpenGL4;

namespace GEvolve.Game.Graphics;

internal sealed class ShaderProgram : IDisposable
{
    public int Handle { get; }

    public ShaderProgram(string vertexSource, string fragmentSource)
    {
        int vertex = Compile(ShaderType.VertexShader, vertexSource);
        int fragment = 0;
        try
        {
            fragment = Compile(ShaderType.FragmentShader, fragmentSource);
            Handle = GL.CreateProgram();
            try
            {
                GL.AttachShader(Handle, vertex);
                GL.AttachShader(Handle, fragment);
                GL.LinkProgram(Handle);
                GL.GetProgram(Handle, GetProgramParameterName.LinkStatus, out int linked);
                if (linked == 0)
                    throw new InvalidOperationException(
                        $"OpenGL program link failed: {GL.GetProgramInfoLog(Handle)}");
            }
            catch
            {
                GL.DeleteProgram(Handle);
                throw;
            }
        }
        finally
        {
            GL.DeleteShader(vertex);
            if (fragment != 0) GL.DeleteShader(fragment);
        }
    }

    public int Uniform(string name) => GL.GetUniformLocation(Handle, name);
    public void Use() => GL.UseProgram(Handle);
    public void Dispose() => GL.DeleteProgram(Handle);

    private static int Compile(ShaderType type, string source)
    {
        int shader = GL.CreateShader(type);
        GL.ShaderSource(shader, source);
        GL.CompileShader(shader);
        GL.GetShader(shader, ShaderParameter.CompileStatus, out int compiled);
        if (compiled != 0) return shader;
        string log = GL.GetShaderInfoLog(shader);
        GL.DeleteShader(shader);
        throw new InvalidOperationException($"OpenGL shader compile failed: {log}");
    }
}
