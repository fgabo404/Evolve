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
        var systemCounts = new int[Enum.GetValues<GeologicalContactKind>().Length];
        int extendedCollisions = 0, elevatedCollisionAxes = 0, longMountainBelts = 0, coastalArcs = 0,
            oceanTrenches = 0, islandArcs = 0, riftAxes = 0, ridgeAxes = 0;
        bool existingIslandArcChecked = false;
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
            foreach (var system in VerifySystems(topology, terrain))
            {
                systemCounts[(int)system.Kind]++;
                var edges = system.InteractionIndices.Select(index => motion.Interactions[index]).ToArray();
                if (system.Kind == GeologicalContactKind.ContinentalCollision &&
                    system.InteractionIndices.Length >= 8 && system.Intensity >= 1)
                {
                    extendedCollisions++;
                    int landAxis = system.AxisCellIds.Count(id => terrain.SurfaceByCellId[id] == 1);
                    int mountainAxis = system.AxisCellIds.Count(id => terrain.SurfaceByCellId[id] == 1 &&
                        terrain.ElevationByCellId![id] >= 44);
                    if (mountainAxis > 0) elevatedCollisionAxes++;
                    if (landAxis >= 8 && mountainAxis >= landAxis * .35) longMountainBelts++;
                }
                if (system.Kind == GeologicalContactKind.OceanContinentSubduction)
                    foreach (var edge in edges)
                    {
                        int upper = edge.FirstPlateId == system.OverridingPlateId
                            ? edge.FirstCellId : edge.SecondCellId;
                        int lower = upper == edge.FirstCellId ? edge.SecondCellId : edge.FirstCellId;
                        if (terrain.SurfaceByCellId[upper] == 1 &&
                            relief.UpliftByCellId[upper] > 0) coastalArcs++;
                        if (terrain.SurfaceByCellId[lower] == 0 &&
                            relief.OceanDepthByCellId[lower] > 58) oceanTrenches++;
                    }
                if (system.Kind == GeologicalContactKind.OceanOceanSubduction)
                    foreach (var edge in edges)
                    {
                        int upper = edge.FirstPlateId == system.OverridingPlateId
                            ? edge.FirstCellId : edge.SecondCellId;
                        if (terrain.SurfaceByCellId[upper] == 1 &&
                            relief.UpliftByCellId[upper] > 0) islandArcs++;
                        if (!existingIslandArcChecked && terrain.SurfaceByCellId[upper] == 0)
                        {
                            int lower = upper == edge.FirstCellId ? edge.SecondCellId : edge.FirstCellId;
                            var hypotheticalSurface = terrain.SurfaceByCellId.ToArray();
                            hypotheticalSurface[upper] = 1;
                            var hypothetical = new ContinentalElevationGenerator().Generate(topology,
                                terrain with { SurfaceByCellId = hypotheticalSurface });
                            Check(hypothetical.ElevationByCellId![upper] > 0 &&
                                hypothetical.ContinentalElevation!.UpliftByCellId[upper] > 0 &&
                                hypothetical.ElevationByCellId[lower] == 0,
                                "Ocean-ocean upper plate can raise existing islands without creating land");
                            existingIslandArcChecked = true;
                        }
                    }
                if (system.Kind == GeologicalContactKind.ContinentalRift &&
                    system.AxisCellIds.Any(id => terrain.SurfaceByCellId[id] == 1 &&
                        relief.SubsidenceByCellId[id] > 0)) riftAxes++;
                if (system.Kind == GeologicalContactKind.OceanRidge &&
                    system.AxisCellIds.Any(id => terrain.SurfaceByCellId[id] == 0 &&
                        relief.OceanDepthByCellId[id] < 56)) ridgeAxes++;
            }
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
                string name = $"continental-orogeny-{size.ToString().ToLowerInvariant()}-{seed}";
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
        Console.WriteLine("TECTONIC SYSTEMS: " + string.Join(", ",
            Enum.GetValues<GeologicalContactKind>().Select(kind => $"{kind}={systemCounts[(int)kind]}")));
        Console.WriteLine($"PROCESS EVIDENCE: extended collisions {elevatedCollisionAxes}/{extendedCollisions}, long mountain belts {longMountainBelts}, " +
            $"coastal arc cells {coastalArcs}, trench cells {oceanTrenches}, island arc cells {islandArcs}, " +
            $"rift axes {riftAxes}, ridge axes {ridgeAxes}");
        Check(extendedCollisions > 0 && elevatedCollisionAxes > 0 && longMountainBelts > 0 && coastalArcs > 0 &&
            oceanTrenches > 0 && existingIslandArcChecked && riftAxes > 0 && ridgeAxes > 0,
            "Connected collisions, subduction, rifts and ocean ridges must leave geological evidence");
        Console.WriteLine($"PASS: continental tectonic relief; {highTotal} impassable summits across 12 worlds");
    }

    private static TectonicReliefSystem[] VerifySystems(HexTopology topology, LandmassState terrain)
    {
        var relief = terrain.ContinentalElevation!;
        var motion = terrain.TectonicMotion!;
        var systems = relief.Systems;
        Check(relief.Version == ContinentalElevationGenerator.Version &&
            relief.UpliftByCellId.Length == topology.Count &&
            relief.SubsidenceByCellId.Length == topology.Count,
            "Regional tectonic fields are present");
        int[] assigned = new int[relief.Contacts.Length];
        for (int systemId = 0; systemId < systems.Length; systemId++)
        {
            var system = systems[systemId];
            Check(system.Id == systemId && system.InteractionIndices.Length > 0 &&
                system.AxisCellIds.Length > 0 && system.WidthHexes is >= 3 and <= 13,
                "Stable nonempty tectonic system");
            var axis = system.AxisCellIds.ToHashSet();
            foreach (int index in system.InteractionIndices)
            {
                assigned[index]++;
                var contact = relief.Contacts[index];
                var edge = motion.Interactions[index];
                Check(contact.Kind == system.Kind &&
                    Math.Min(edge.FirstPlateId, edge.SecondPlateId) == system.FirstPlateId &&
                    Math.Max(edge.FirstPlateId, edge.SecondPlateId) == system.SecondPlateId &&
                    contact.OverridingPlateId == system.OverridingPlateId &&
                    axis.Contains(edge.FirstCellId) && axis.Contains(edge.SecondCellId),
                    "System contains only its classified contiguous plate margin");
            }
            var visited = new HashSet<int> { system.AxisCellIds[0] };
            var queue = new Queue<int>();
            queue.Enqueue(system.AxisCellIds[0]);
            while (queue.Count > 0)
                foreach (int neighbor in topology.Neighbors(queue.Dequeue()))
                    if (axis.Contains(neighbor) && visited.Add(neighbor)) queue.Enqueue(neighbor);
            Check(visited.Count == axis.Count, "Tectonic axis must be connected on the real hex topology");
        }
        for (int index = 0; index < assigned.Length; index++)
            Check(assigned[index] == (relief.Contacts[index].Kind == GeologicalContactKind.Quiet ? 0 : 1),
                "Each active contact belongs to exactly one tectonic system");
        return systems;
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
            // Previous elevation-stage saves used the local contact model and had no systems.
            state.Landmass = world.Landmass with
            {
                ContinentalElevation = world.Landmass.ContinentalElevation! with
                {
                    Version = "continental-elevation-1", Systems = [],
                    UpliftByCellId = [], SubsidenceByCellId = []
                }
            };
            GameSaveStore.Save(path, state);
            Check(GameSaveStore.Load(path).Landmass!.ContinentalElevation!.Version ==
                "continental-elevation-1", "Previous elevation-stage saves remain readable");
        }
        finally { if (File.Exists(path)) File.Delete(path); }
    }

    private static void Check(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
