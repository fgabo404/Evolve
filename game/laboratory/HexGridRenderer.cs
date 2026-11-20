// Owns static GPU line buffers for the generated grid and a small selection overlay.
// Reuses the existing pixel renderer's shader; only camera uniforms change during normal frames.
using Evolve.Engine.WorldGeneration.Layers.Grid;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;

namespace Evolve.Game;

internal sealed class HexGridRenderer : IDisposable
{
    private int vertexArray, vertexBuffer, overlayArray, overlayBuffer;
    private int vertexCount, overlayCount;
    private HexGridData? grid;
    private int? selection;

    public HexGridRenderer()
    {
        try
        {
            CreateBuffer(out vertexArray, out vertexBuffer);
            CreateBuffer(out overlayArray, out overlayBuffer);
        }
        catch { Dispose(); throw; }
    }

    private static void CreateBuffer(out int array, out int buffer)
    {
        array = GL.GenVertexArray();
        buffer = GL.GenBuffer();
        GL.BindVertexArray(array);
        GL.BindBuffer(BufferTarget.ArrayBuffer, buffer);
        GL.VertexAttribPointer(0, 2, VertexAttribPointerType.Float, false, 2 * sizeof(float), 0);
        GL.EnableVertexAttribArray(0);
        GL.DisableVertexAttribArray(1);
    }

    public void Synchronize(LaboratorySession session)
    {
        if (!ReferenceEquals(grid, session.Grid))
        {
            grid = session.Grid;
            selection = null;
            overlayCount = 0;
            vertexCount = 0;
            if (grid is not null)
            {
                var lines = GridVisualGeometry.BuildLines(grid);
                GL.BindBuffer(BufferTarget.ArrayBuffer, vertexBuffer);
                GL.BufferData(BufferTarget.ArrayBuffer, lines.Length * sizeof(float), lines, BufferUsageHint.StaticDraw);
                vertexCount = lines.Length / 2;
            }
        }
        if (grid is not null && selection != session.SelectedCell)
        {
            selection = session.SelectedCell;
            var lines = selection is int id
                ? GridVisualGeometry.BuildLines(grid, new[] { id }.Concat(grid.GetNeighbors(id))) : [];
            GL.BindBuffer(BufferTarget.ArrayBuffer, overlayBuffer);
            GL.BufferData(BufferTarget.ArrayBuffer, lines.Length * sizeof(float), lines, BufferUsageHint.DynamicDraw);
            overlayCount = lines.Length / 2;
        }
    }

    public void Draw(PixelMenuRenderer renderer, GridCamera camera, int width, int height)
    {
        if (vertexCount == 0) return;
        var view = camera.Viewport;
        renderer.UseWorldView(width, height, camera);
        GL.Enable(EnableCap.ScissorTest);
        try
        {
            GL.Scissor((int)view.X, (int)(height - view.Y - view.Height), (int)view.Width, (int)view.Height);
            GL.LineWidth(1);
            GL.BindVertexArray(vertexArray);
            Tint(new Color4(0.38f, 0.47f, 0.51f, 1));
            GL.DrawArrays(PrimitiveType.Lines, 0, vertexCount);
            if (overlayCount > 0)
            {
                GL.BindVertexArray(overlayArray);
                Tint(new Color4(0.35f, 0.8f, 1f, 1));
                GL.DrawArrays(PrimitiveType.Lines, 12, overlayCount - 12);
                Tint(new Color4(1f, 0.82f, 0.25f, 1));
                GL.DrawArrays(PrimitiveType.Lines, 0, 12);
            }
        }
        finally { GL.Disable(EnableCap.ScissorTest); }
    }

    private static void Tint(Color4 color) => GL.VertexAttrib4(1, color.R, color.G, color.B, color.A);

    // The owning EvolveWindow disposes these buffers before releasing its graphics context.
    public void Dispose()
    {
        if (vertexBuffer != 0) GL.DeleteBuffer(vertexBuffer);
        if (overlayBuffer != 0) GL.DeleteBuffer(overlayBuffer);
        if (vertexArray != 0) GL.DeleteVertexArray(vertexArray);
        if (overlayArray != 0) GL.DeleteVertexArray(overlayArray);
        vertexBuffer = overlayBuffer = vertexArray = overlayArray = 0;
    }
}
