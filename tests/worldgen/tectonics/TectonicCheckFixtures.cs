// Registers real Grid/Tectonics executors and independent Offset traversal for headless checks.
// Downstream fixtures remain test-only; no second runner or geographic implementation is introduced.
using Evolve.Definitions.WorldGeneration;
using Evolve.Engine.WorldGeneration;
using Evolve.Engine.WorldGeneration.Layers.Grid;
using Evolve.Engine.WorldGeneration.Layers.Grid.Geometry;
using Evolve.Engine.WorldGeneration.Layers.Tectonics;

namespace Evolve.WorldGeneration.Tectonics.Checks;

internal static class TectonicCheckFixtures
{
    internal static WorldGenerationOrchestrator Orchestrator(IWorldGenerationLayer? tectonics = null, IWorldGenerationLayer? grid = null) =>
        new(new WorldGenerationLayerRegistry(Enum.GetValues<GenerationStage>().Select(stage => stage switch
        {
            GenerationStage.Grid => grid ?? new HexGridLayer(),
            GenerationStage.Tectonics => tectonics ?? new TectonicPlatesLayer(),
            _ => new TestLayer(stage, inputs => LayerExecutionResult.Success(new LayerData<int>(inputs.Stage, [1])))
        })));

    internal static GenerationRequest Request(MapSize size, long seed = 0, WorldType type = WorldType.Pangea) =>
        new(seed, new WorldGenerationSettings(type, size));

    internal static LayerData<uint> Published(GenerationContext context) =>
        context.GetLayerData(GenerationStage.Tectonics) as LayerData<uint> ?? throw new InvalidOperationException("Tectonic data missing.");

    internal static (HexGridData Grid, LayerData<uint> Data) Generate(MapSize size, long seed = 0, WorldType type = WorldType.Pangea)
    {
        var orchestrator = Orchestrator();
        var context = orchestrator.CreateGeneration(Request(size, seed, type));
        orchestrator.RunUntil(context, GenerationStage.Tectonics);
        Require(context.GetProgress().GetStatus(GenerationStage.Tectonics) == GenerationStageStatus.Completed,
            context.GetProgress().GetFailure(GenerationStage.Tectonics) ?? "Tectonics failed");
        return (new HexGridData(size), Published(context));
    }

    // Explicit odd-column Offset deltas are independent from production axial/wrap/neighbor code.
    internal static IEnumerable<int> Neighbors(int cell, int columns, int rows)
    {
        int column = cell % columns, row = cell / columns, up = column % 2 == 0 ? -1 : 0;
        (int X, int Y)[] deltas = [(0, -1), (1, up), (1, up + 1), (0, 1), (-1, up + 1), (-1, up)];
        foreach (var (dx, dy) in deltas)
        {
            int nextRow = row + dy;
            if (nextRow < 0 || nextRow >= rows) continue;
            yield return nextRow * columns + (column + dx + columns) % columns;
        }
    }

    internal static void CheckRegions(HexGridData grid, TectonicData data)
    {
        int[] histogram = new int[14];
        for (int cell = 0; cell < grid.CellCount; cell++)
        {
            byte owner = data.GetPlateId(cell);
            Require(owner < 14, "Owner outside 0..13");
            histogram[owner]++;
            Require(Neighbors(cell, grid.Columns, grid.Rows).ToHashSet().SetEquals(grid.GetNeighbors(cell)), "Independent seam/pole topology differs");
        }
        bool[] visited = new bool[grid.CellCount];
        int[] queue = new int[grid.CellCount];
        for (byte plate = 0; plate < 14; plate++)
        {
            var record = data.GetPlate(plate);
            Require(record.PlateId == plate && record.CellCount > 0 && record.CellCount == histogram[plate] &&
                data.GetPlateId(record.SeedCellId) == plate && record.GrowthCost > 0, "Inconsistent plate metadata");
            int head = 0, tail = 1; queue[0] = record.SeedCellId; visited[record.SeedCellId] = true;
            while (head < tail)
                foreach (int next in Neighbors(queue[head++], grid.Columns, grid.Rows))
                    if (!visited[next] && data.GetPlateId(next) == plate) { visited[next] = true; queue[tail++] = next; }
            Require(tail == histogram[plate], "Independent owner BFS found disconnected plate");
        }
        Require(histogram.Sum() == grid.CellCount && visited.All(value => value), "Incomplete ownership coverage");
    }

    internal static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }

    internal static void Expect<T>(Action action) where T : Exception
    {
        try { action(); } catch (T) { return; }
        throw new InvalidOperationException($"Expected {typeof(T).Name}");
    }

    internal sealed class TestLayer(GenerationStage stage, Func<GenerationLayerInputs, LayerExecutionResult> execute) : IWorldGenerationLayer
    {
        public GenerationStage Stage => stage;
        public LayerExecutionResult Execute(GenerationLayerInputs inputs) => execute(inputs);
    }
}
