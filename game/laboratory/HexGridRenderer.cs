// Owns static Grid/tectonic GPU meshes and a small selection overlay from published immutable results.
// Reuses the existing pixel renderer's shader; only camera uniforms change during normal frames.
using Evolve.Engine.WorldGeneration.Layers.Grid;
using Evolve.Engine.WorldGeneration;
using Evolve.Engine.WorldGeneration.Layers.Tectonics;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;

namespace Evolve.Game;

internal sealed class HexGridRenderer : IDisposable
{
    private int vertexArray, vertexBuffer, overlayArray, overlayBuffer;
    private int vertexCount, overlayCount;
    private int fillArray, fillBuffer, boundaryArray, boundaryBuffer, arrowArray, arrowBuffer;
    private int boundaryCount, arrowCount;
    private int[] starts = [], counts = [];
    private TectonicData? tectonics;
    private HexGridData? grid;
    private int? selection;

    public HexGridRenderer()
    {
        try
        {
            CreateBuffer(out vertexArray, out vertexBuffer);
            CreateBuffer(out overlayArray, out overlayBuffer);
            CreateBuffer(out fillArray, out fillBuffer);
            CreateBuffer(out boundaryArray, out boundaryBuffer);
            CreateBuffer(out arrowArray, out arrowBuffer);
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
        if (!ReferenceEquals(tectonics, session.Tectonics))
        {
            tectonics = session.Tectonics;
            boundaryCount = arrowCount = 0;
            starts = counts = [];
            if (tectonics is not null && session.Grid is { } publishedGrid)
            {
                var mesh = TectonicVisualGeometry.Build(publishedGrid, tectonics);
                Upload(fillBuffer, mesh.Positions);
                Upload(boundaryBuffer, mesh.Boundaries);
                Upload(arrowBuffer, mesh.Arrows);
                starts = mesh.Starts; counts = mesh.Counts;
                boundaryCount = mesh.Boundaries.Length / 2; arrowCount = mesh.Arrows.Length / 2;
            }
        }
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

    public void Draw(PixelMenuRenderer renderer, GridCamera camera, int width, int height, LaboratorySession session)
    {
        if (vertexCount == 0) return;
        var view = camera.Viewport;
        renderer.UseWorldView(width, height, camera);
        GL.Enable(EnableCap.ScissorTest);
        try
        {
            GL.Scissor((int)view.X, (int)(height - view.Y - view.Height), (int)view.Width, (int)view.Height);
            GL.LineWidth(1);
            if (session.SelectedView == GenerationStage.Tectonics && tectonics is not null)
            {
                GL.BindVertexArray(fillArray);
                for (byte plate = 0; plate < 14; plate++)
                {
                    var color = TectonicVisualGeometry.Color(plate);
                    Tint(new Color4(color.X, color.Y, color.Z, 1));
                    GL.DrawArrays(PrimitiveType.Triangles, starts[plate], counts[plate]);
                }
                if (session.ShowBoundaries) DrawLines(boundaryArray, boundaryCount, new Color4(.09f,.12f,.17f,1));
                if (session.ShowMotion) DrawLines(arrowArray, arrowCount, new Color4(1f,1f,1f,1));
            }
            else DrawLines(vertexArray, vertexCount, new Color4(0.38f, 0.47f, 0.51f, 1));
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

    private static void Upload(int buffer, float[] data)
    {
        GL.BindBuffer(BufferTarget.ArrayBuffer, buffer);
        GL.BufferData(BufferTarget.ArrayBuffer, data.Length * sizeof(float), data, BufferUsageHint.StaticDraw);
    }
    private static void DrawLines(int array, int count, Color4 color)
    {
        GL.BindVertexArray(array); Tint(color);
        GL.DrawArrays(PrimitiveType.Lines, 0, count);
    }

    // The owning EvolveWindow disposes these buffers before releasing its graphics context.
    public void Dispose()
    {
        if (vertexBuffer != 0) GL.DeleteBuffer(vertexBuffer);
        if (overlayBuffer != 0) GL.DeleteBuffer(overlayBuffer);
        if (vertexArray != 0) GL.DeleteVertexArray(vertexArray);
        if (overlayArray != 0) GL.DeleteVertexArray(overlayArray);
        if (fillBuffer != 0) GL.DeleteBuffer(fillBuffer);
        if (boundaryBuffer != 0) GL.DeleteBuffer(boundaryBuffer);
        if (arrowBuffer != 0) GL.DeleteBuffer(arrowBuffer);
        if (fillArray != 0) GL.DeleteVertexArray(fillArray);
        if (boundaryArray != 0) GL.DeleteVertexArray(boundaryArray);
        if (arrowArray != 0) GL.DeleteVertexArray(arrowArray);
        fillBuffer = boundaryBuffer = arrowBuffer = fillArray = boundaryArray = arrowArray = 0;
        vertexBuffer = overlayBuffer = vertexArray = overlayArray = 0;
    }
}
