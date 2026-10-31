using System.Text.Json;
using GEvolve.Definitions.WorldGeneration.Configuration;
using GEvolve.Engine.World;
using GEvolve.Engine.World.LandmassGeneration;
using GEvolve.Engine.World.LandmassGeneration.Shared;
using GEvolve.State.Game;
using GEvolve.State.Persistence;

internal static class ContinentalElevationTests
{
    public static void Run(bool export = false)
    {
        ContinentalSurfaceBaseline.Verify();
        var service = new WorldGenerationService();
        int highTotal = 0, collisionTotal = 0, riftTotal = 0, riftLandTotal = 0,
            seamPairs = 0, lowBoundaryTotal = 0;
        foreach (var size in Enum.GetValues<GridSize>())
        foreach (int seed in new[] { 2026, 42, -731 })
        {
            var world = service.Generate(LandmassProfiles.Create(size, LandmassType.Continents, seed));
            var terrain = world.Landmass;
            var topology = new HexTopology(world.Grid);
            var heights = terrain.ElevationByCellId ?? throw new Exception("Missing continental elevations");
            var relief = terrain.ContinentalElevation ?? throw new Exception("Missing tectonic relief state");
            var surface = terrain.SurfaceByCellId;
            var motion = terrain.TectonicMotion!;
            int land = world.Statistics!.LandCells, high = 0, mountain = 0;
            for (int id = 0; id < topology.Count; id++)
            {
                Check(surface[id] == 0 ? heights[id] == 0 && relief.OceanDepthByCellId[id] > 0 :
                    heights[id] is >= 1 and <= 100 && relief.OceanDepthByCellId[id] == 0,
                    "Ocean has zero terrestrial height and land has valid 1..100 height");
                if (surface[id] == 1 && heights[id] >= 85) high++;
                if (surface[id] == 1 && relief.LandformByCellId[id] is
                    (byte)ContinentalReliefKind.Mountain or (byte)ContinentalReliefKind.HighRange) mountain++;
            }
            Check(high <= land * .08 && mountain <= land * .35,
                "High mountains must remain geographically limited");
            highTotal += high;
            collisionTotal += relief.Contacts.Count(c => c.Kind is GeologicalContactKind.ContinentalCollision or
                GeologicalContactKind.OceanContinentSubduction);
            riftTotal += relief.Contacts.Count(c => c.Kind == GeologicalContactKind.ContinentalRift);
            int[] upliftDistance = DistanceFromContacts(topology, motion, relief.Contacts,
                kind => kind is GeologicalContactKind.ContinentalCollision or
                    GeologicalContactKind.OceanContinentSubduction);
            int associatedHigh = Enumerable.Range(0, topology.Count).Count(id =>
                surface[id] == 1 && heights[id] >= 70 && upliftDistance[id] <= 9);
            int allHigh = Enumerable.Range(0, topology.Count).Count(id => surface[id] == 1 && heights[id] >= 70);
            Check(allHigh == 0 || associatedHigh >= allHigh * .75,
                "High ranges must lie near classified convergence or subduction");

            int[] riftDistance = DistanceFromContacts(topology, motion, relief.Contacts,
                kind => kind == GeologicalContactKind.ContinentalRift);
            int riftLand = 0, misplacedRift = 0, boundaryLand = 0, mountainBoundary = 0;
            for (int id = 0; id < topology.Count; id++)
            {
                if (surface[id] == 0) continue;
                if (relief.LandformByCellId[id] == (byte)ContinentalReliefKind.Rift)
                {
                    riftLand++;
                    if (riftDistance[id] > 8) misplacedRift++;
                }
                if (motion.DistanceToBoundaryByCellId[id] <= 2)
                {
                    boundaryLand++;
                    if (heights[id] >= 44) mountainBoundary++;
                }
            }
            Console.WriteLine($"RELIEF CHECK {size}/{seed}: rift {riftLand}, misplaced {misplacedRift}, " +
                $"boundary mountains {mountainBoundary}/{boundaryLand}");
            Check(misplacedRift == 0, "Rifts must follow classified continental extension");
            riftLandTotal += riftLand;
            lowBoundaryTotal += boundaryLand - mountainBoundary;
            int seamMax = 0;
            for (int row = 0; row < topology.Rows; row++)
            {
                int left = topology.Id(0, row), right = topology.Id(topology.Columns - 1, row);
                Check(topology.Neighbors(left).Contains(right), "Horizontal seam lost its hex adjacency");
                if (surface[left] == 1 && surface[right] == 1)
                {
                    seamPairs++;
                    seamMax = Math.Max(seamMax, Math.Abs(heights[left] - heights[right]));
                }
            }
            Check(seamMax <= 35, "Elevation breaks across the horizontal seam");

            var repeated = service.Generate(terrain.Configuration).Landmass;
            Check(repeated.SurfaceByCellId.SequenceEqual(surface) &&
                repeated.ElevationByCellId!.SequenceEqual(heights) &&
                JsonSerializer.Serialize(repeated.ContinentalElevation) == JsonSerializer.Serialize(relief),
                "Tectonic elevations are deterministic without changing surface");
            if (size == GridSize.Small && seed == 2026)
            {
                VerifyPersistence(world);
                var quietMotion = motion with
                {
                    Interactions = motion.Interactions.Select(edge => edge with
                        { Convergence = 0, Divergence = 0, Shear = 0, Kind = PlateInteractionKind.Quiet }).ToArray(),
                    CompressionByCellId = new float[topology.Count],
                    ExtensionByCellId = new float[topology.Count],
                    ShearByCellId = new float[topology.Count]
                };
                var quietTerrain = new ContinentalElevationGenerator().Generate(topology,
                    terrain with { TectonicMotion = quietMotion });
                var quietHeights = quietTerrain.ElevationByCellId!;
                Check(heights.Max() >= quietHeights.Max() + 20,
                    "Removing tectonic interactions must remove the principal mountain uplift");
                var riftInteractions = relief.Contacts.Where(c =>
                    c.Kind == GeologicalContactKind.ContinentalRift)
                    .Select(c => c.InteractionIndex).ToHashSet();
                var noRiftMotion = motion with
                {
                    Interactions = motion.Interactions.Select((edge, index) =>
                        riftInteractions.Contains(index) ? edge with { Divergence = 0 } : edge).ToArray()
                };
                var noRiftHeights = new ContinentalElevationGenerator().Generate(topology,
                    terrain with { TectonicMotion = noRiftMotion }).ElevationByCellId!;
                int depressedRift = Enumerable.Range(0, topology.Count).Count(id =>
                    relief.LandformByCellId[id] == (byte)ContinentalReliefKind.Rift &&
                    heights[id] < noRiftHeights[id]);
                Check(depressedRift >= riftLand * .6,
                    "Continental extension must lower the majority of classified rift cells");
            }
            if (export && (size, seed) is (GridSize.Small, 2026) or (GridSize.Medium, 42) or
                (GridSize.Large, 2026) or (GridSize.Large, -731))
            {
                string name = $"continental-elevation-{size.ToString().ToLowerInvariant()}-{seed}";
                ContinentalElevationDiagnostics.Export(name + ".svg", world);
                ContinentalElevationDiagnostics.ExportBitmap(name + ".bmp", world);
                Console.WriteLine($"CAPTURE: {name}.svg");
            }
            Console.WriteLine($"ELEVATION {size}/{seed}: land {land}, max {heights.Max()}, " +
                $"85+ {high}, mountain {mountain}, 70+ near convergence {associatedHigh}/{allHigh}, " +
                $"rifts {riftLand}, seam max delta {seamMax}");
        }
        Check(collisionTotal > 0 && riftTotal > 0 && riftLandTotal > 0 && seamPairs > 0 &&
            lowBoundaryTotal > 0,
            "Convergence and extension contribute without making every land boundary mountainous");
        Console.WriteLine($"PASS: continental tectonic relief; {highTotal} impassable summits across 12 worlds");
    }

    private static int[] DistanceFromContacts(HexTopology topology, TectonicMotionState motion,
        GeologicalContact[] contacts, Func<GeologicalContactKind, bool> include)
    {
        int[] distances = Enumerable.Repeat(int.MaxValue, topology.Count).ToArray();
        var queue = new int[topology.Count];
        int head = 0, tail = 0;
        foreach (var contact in contacts)
        {
            if (!include(contact.Kind)) continue;
            var edge = motion.Interactions[contact.InteractionIndex];
            foreach (int id in new[] { edge.FirstCellId, edge.SecondCellId })
                if (distances[id] == int.MaxValue)
                {
                    distances[id] = 0;
                    queue[tail++] = id;
                }
        }
        while (head < tail)
        {
            int id = queue[head++];
            foreach (int next in topology.Neighbors(id))
                if (distances[next] == int.MaxValue)
                {
                    distances[next] = distances[id] + 1;
                    queue[tail++] = next;
                }
        }
        return distances;
    }

    private static void VerifyPersistence(GeneratedWorld world)
    {
        string path = Path.Combine(Path.GetTempPath(), $"continental-elevation-{Guid.NewGuid():N}.json");
        try
        {
            var state = new GameState { HexGrid = new(world.Grid.Definition), Landmass = world.Landmass };
            GameSaveStore.Save(path, state);
            var loaded = GameSaveStore.Load(path).Landmass!;
            Check(loaded.ElevationByCellId!.SequenceEqual(world.Landmass.ElevationByCellId!) &&
                JsonSerializer.Serialize(loaded.ContinentalElevation) ==
                    JsonSerializer.Serialize(world.Landmass.ContinentalElevation),
                "Tectonic heights and contact classification survive save/load");
            // Saves made after the surface stage but before elevations remain readable.
            state.Landmass = world.Landmass with { ElevationByCellId = null, ContinentalElevation = null };
            GameSaveStore.Save(path, state);
            Check(GameSaveStore.Load(path).Landmass!.ContinentalSurface is not null,
                "Surface-only continental saves remain compatible");
        }
        finally { if (File.Exists(path)) File.Delete(path); }
    }

    private static void Check(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
