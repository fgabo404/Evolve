// Adapts published tectonic values to static presentation meshes using the existing Grid outlines.
// Colors and arrows are display choices only; no partition, motion or boundary is regenerated here.
using System.Numerics;
using Evolve.Engine.WorldGeneration.Layers.Grid;
using Evolve.Engine.WorldGeneration.Layers.Grid.Geometry;
using Evolve.Engine.WorldGeneration.Layers.Tectonics;

namespace Evolve.Game;

internal static class TectonicVisualGeometry
{
    private static readonly Vector3[] Colors =
    [
        new(.25f,.60f,.81f), new(.88f,.47f,.28f), new(.38f,.72f,.44f), new(.77f,.47f,.75f),
        new(.88f,.73f,.30f), new(.30f,.73f,.72f), new(.79f,.35f,.43f), new(.52f,.55f,.83f),
        new(.64f,.72f,.30f), new(.87f,.57f,.64f), new(.52f,.72f,.87f), new(.66f,.49f,.31f),
        new(.61f,.78f,.63f), new(.69f,.63f,.82f)
    ];
    internal static Vector3 Color(byte plateId) => Colors[plateId];

    // Group triangles by PlateId for fourteen uniform-color draws without a per-vertex color buffer.
    internal static TectonicMesh Build(HexGridData grid, TectonicData data)
    {
        var positions = new float[checked(grid.CellCount * 36)];
        int[] starts = new int[14], counts = new int[14], cursor = new int[14];
        for (byte plate = 0; plate < 14; plate++)
        {
            starts[plate] = plate == 0 ? 0 : starts[plate - 1] + counts[plate - 1];
            counts[plate] = data.GetPlate(plate).CellCount * 18;
            cursor[plate] = starts[plate] * 2;
        }
        for (int cell = 0; cell < grid.CellCount; cell++)
        {
            byte owner = data.GetPlateId(cell);
            var center = FlatHexGeometry.Center(grid.GetOffset(cell));
            for (int corner = 0; corner < 6; corner++)
            {
                Write(positions, ref cursor[owner], center);
                Write(positions, ref cursor[owner], center + GridVisualGeometry.Corner(corner));
                Write(positions, ref cursor[owner], center + GridVisualGeometry.Corner((corner + 1) % 6));
            }
        }
        var borders = new List<float>();
        for (int index = 0; index < data.BoundaryCount; index++)
        {
            var boundary = data.GetBoundary(index);
            WriteEdge(boundary.FirstCellId, boundary.SecondCellId);
            // Draw both physical cuts of a logical seam edge; no extra cells, IDs or topological edges.
            if (Math.Abs(grid.GetOffset(boundary.FirstCellId).Column - grid.GetOffset(boundary.SecondCellId).Column) > 1)
                WriteEdge(boundary.SecondCellId, boundary.FirstCellId);
        }
        var arrows = new List<float>(14 * 12);
        for (byte plate = 0; plate < 14; plate++)
        {
            var motion = data.GetMotion(plate);
            var start = FlatHexGeometry.Center(grid.GetOffset(Anchor(data, plate)));
            var velocity = motion.Velocity;
            if (motion.SpeedCmPerYear == 0) continue;
            var direction = new Vector2((float)(velocity.X / motion.SpeedCmPerYear), (float)(velocity.Y / motion.SpeedCmPerYear));
            float length = (float)(grid.Rows * .04 * (.35 + .65 * motion.SpeedCmPerYear / 8));
            var end = start + direction * length;
            var side = new Vector2(-direction.Y, direction.X);
            AddLine(arrows, start, end);
            AddLine(arrows, end, end - direction * length * .35f + side * length * .22f);
            AddLine(arrows, end, end - direction * length * .35f - side * length * .22f);
        }
        return new(positions, starts, counts, borders.ToArray(), arrows.ToArray());

        void WriteEdge(int cell, int neighbor)
        {
            for (int direction = 0; direction < 6; direction++)
                if (grid.TryGetNeighbor(cell, (HexDirection)direction, out int next) && next == neighbor)
                {
                    int corner = (direction + 4) % 6;
                    var center = FlatHexGeometry.Center(grid.GetOffset(cell));
                    AddLine(borders, center + GridVisualGeometry.Corner(corner), center + GridVisualGeometry.Corner((corner + 1) % 6));
                    return;
                }
            throw new InvalidOperationException("Published boundary is not a Grid edge.");
        }
    }

    // The accepted origin is stable and guaranteed inside its plate, without an influence-distance field.
    internal static int Anchor(TectonicData data, byte plateId) => data.GetPlate(plateId).SeedCellId;
    private static void Write(float[] target, ref int index, Vector2 point) { target[index++] = point.X; target[index++] = point.Y; }
    private static void AddLine(List<float> target, Vector2 start, Vector2 end)
    {
        target.Add(start.X); target.Add(start.Y); target.Add(end.X); target.Add(end.Y);
    }
}

internal sealed record TectonicMesh(float[] Positions, int[] Starts, int[] Counts, float[] Boundaries, float[] Arrows);
