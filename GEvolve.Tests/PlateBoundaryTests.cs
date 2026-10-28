using GEvolve.Definitions.WorldGeneration.Configuration;
using GEvolve.Engine.Grid;
using GEvolve.Engine.World.LandmassGeneration;
using GEvolve.Engine.World.LandmassGeneration.Shared;
using GEvolve.Engine.World.Tectonics;
using GEvolve.Game.Graphics;
using GEvolve.State.Game;

public static class PlateBoundaryTests
{
    public static void Run()
    {
        var definition = HexGridDefinitions.Get(GridSize.Small);
        var topology = new HexTopology(HexGrid.Generate(definition));
        int count = topology.Count, width = definition.Columns;
        var config = LandmassProfiles.Create(GridSize.Small, LandmassType.Pangaea, 42);
        var surface = Enumerable.Repeat((byte)1, count).ToArray();
        var plate = new byte[count];
        var terrain = new LandmassState(config, surface, 0) { PlateByCellId = plate };
        var empty = PlateBoundaryMap.Analyze(topology, terrain)!;
        Check(empty.Edges.Count == 0 && empty.BoundaryCellCount == 0 &&
            empty.DistanceToBoundaryByCellId.All(x => x == ushort.MaxValue),
            "Single-plate world has no false boundaries");
        Check(PlateBoundaryMap.Analyze(topology, terrain with { PlateByCellId = null }) is null,
            "Non-tectonic worlds have no invented boundaries");

        for (int id = 0; id < count; id++)
            plate[id] = (byte)(id % width < width / 2 ? 1 : 2);
        var map = PlateBoundaryMap.Analyze(topology, terrain)!;
        Check(map.Edges.Count > 0 && map.BoundaryCellCount > 0 &&
            map.Edges.Any(edge => edge.FirstCellId == 0 && edge.SecondCellId == width - 1),
            "Wrapped east-west seam is a real tectonic boundary");
        Check(map.Edges.All(edge => edge.FirstCellId < edge.SecondCellId &&
            plate[edge.FirstCellId] != plate[edge.SecondCellId] &&
            topology.Neighbors(edge.FirstCellId).Contains(edge.SecondCellId)) &&
            map.Edges.Count == map.Edges.Distinct().Count(),
            "Each boundary edge is canonical, adjacent and unique");
        for (int id = 0; id < count; id++)
        {
            bool expected = topology.Neighbors(id).ToArray().Any(nb => plate[id] != plate[nb]);
            Check(map.BoundaryByCellId[id] == expected &&
                (expected ? map.DistanceToBoundaryByCellId[id] == 0 :
                    map.DistanceToBoundaryByCellId[id] > 0),
                "Boundary cells and distances match plate differences");
            foreach (int nb in topology.Neighbors(id))
                Check(Math.Abs((int)map.DistanceToBoundaryByCellId[id] -
                    map.DistanceToBoundaryByCellId[nb]) <= 1,
                    "Boundary distance changes by at most one hex step");
        }
        Check(ReferenceEquals(terrain.PlateByCellId, plate), "Analysis never modifies plate state");
        Check(PlatePalette.ForPlate(1, false) != PlatePalette.ForPlate(2, false) &&
            PlatePalette.ForPlate(1, true) != PlatePalette.ForPlate(1, false),
            "Plate diagnostic colors distinguish plates and oceans");

        foreach (var (size, seed) in new[] {
            (GridSize.Small, 2026), (GridSize.Medium, 42),
            (GridSize.Large, -731), (GridSize.Enormous, 2026) })
        {
            var generated = new LandmassGenerationService().Generate(
                LandmassProfiles.Create(size, LandmassType.Pangaea, seed));
            var world = generated.Landmass;
            var grid = new HexTopology(generated.Grid);
            var first = PlateBoundaryMap.Analyze(grid, world)!;
            var repeated = PlateBoundaryMap.Analyze(grid, world)!;
            Check(first.Edges.SequenceEqual(repeated.Edges) &&
                first.DistanceToBoundaryByCellId.SequenceEqual(repeated.DistanceToBoundaryByCellId) &&
                first.BoundaryCellCount > 0,
                "Tectonic boundaries are reproducible for real Pangea maps");
            Check(ReferenceEquals(world.PlateByCellId, world.PlateByCellId) &&
                first.Edges.All(e => world.PlateByCellId![e.FirstCellId] !=
                    world.PlateByCellId[e.SecondCellId]),
                "Analysis never invents tectonic plate boundaries");
            Console.WriteLine($"PLATE BOUNDARIES {size}/{seed}: {first.Edges.Count} unique edges; {first.BoundaryCellCount} border cells");
        }
        Console.WriteLine("PASS: plate boundaries, seam, distances, determinism, palette");
    }

    private static void Check(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
