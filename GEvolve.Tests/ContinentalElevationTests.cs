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
        int multiPlateCollisions = 0, collisionSutureCells = 0,
            variedCollisionBelts = 0, plateauCells = 0, plainCells = 0, lowHillCells = 0;
        int legacyCollisionSystems = 0, longestGain = 0;
        var longestCollisionBySize = new Dictionary<GridSize, int>();
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
            var legacySystems = TectonicReliefSystemAnalyzer.AnalyzeLegacy(topology,
                motion, relief.Contacts);
            int priorCollisions = legacySystems.Count(system =>
                system.Kind == GeologicalContactKind.ContinentalCollision);
            int mergedCollisions = relief.Systems.Count(system =>
                system.Kind == GeologicalContactKind.ContinentalCollision);
            int previousLongest = legacySystems.Where(system =>
                system.Kind == GeologicalContactKind.ContinentalCollision)
                .Select(system => system.LengthHexEdges).DefaultIfEmpty().Max();
            int currentLongest = relief.Systems.Where(system =>
                system.Kind == GeologicalContactKind.ContinentalCollision)
                .Select(system => system.LengthHexEdges).DefaultIfEmpty().Max();
            Check(mergedCollisions <= priorCollisions && currentLongest >= previousLongest,
                "Joining compatible sutures must not fragment or shorten collision belts");
            legacyCollisionSystems += priorCollisions;
            longestGain = Math.Max(longestGain, currentLongest - previousLongest);
            int principal = relief.Systems.Count(system =>
                system.MountainTier == TectonicMountainTier.Principal);
            int secondary = relief.Systems.Count(system =>
                system.MountainTier == TectonicMountainTier.Secondary);
            Console.WriteLine($"MOUNTAIN SYSTEMS {size}/{seed}: principal {principal}, secondary {secondary}");
            Check(principal <= 3 && relief.MountainTierByCellId.Length == topology.Count &&
                relief.Systems.Where(system => system.MountainTier == TectonicMountainTier.Principal)
                    .All(system => system.Kind is GeologicalContactKind.ContinentalCollision or
                        GeologicalContactKind.OceanContinentSubduction or
                        GeologicalContactKind.OceanOceanSubduction) &&
                Enumerable.Range(0, topology.Count).All(id =>
                    surface[id] == 1 || relief.MountainTierByCellId[id] == 0),
                "Principal systems are classified from tectonic land uplift only");
            foreach (var system in VerifySystems(topology, terrain))
            {
                systemCounts[(int)system.Kind]++;
                var edges = system.InteractionIndices.Select(index => motion.Interactions[index]).ToArray();
                if (system.Kind == GeologicalContactKind.ContinentalCollision)
                {
                    if (system.ParticipatingPlateIds.Length > 2) multiPlateCollisions++;
                    collisionSutureCells += system.AxisCellIds.Length;
                    longestCollisionBySize[size] = Math.Max(
                        longestCollisionBySize.GetValueOrDefault(size), system.LengthHexEdges);
                    Check(edges.All(edge => terrain.ContinentalSurface!.CrustByCellId[edge.FirstCellId] ==
                        (byte)CrustKind.Continental &&
                        terrain.ContinentalSurface.CrustByCellId[edge.SecondCellId] ==
                        (byte)CrustKind.Continental),
                        "Continental collision sutures must join two actual continental crust blocks");
                    var axialHeights = system.AxisCellIds.Where(id => surface[id] == 1)
                        .Select(id => (int)heights[id]).ToArray();
                    if (axialHeights.Length >= 20 &&
                        axialHeights.Max() - axialHeights.Min() >= 15)
                        variedCollisionBelts++;
                }
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
            plateauCells += relief.LandformByCellId.Count(kind =>
                kind == (byte)ContinentalReliefKind.Plateau);
            plainCells += relief.LandformByCellId.Count(kind =>
                kind is (byte)ContinentalReliefKind.CoastalPlain or
                    (byte)ContinentalReliefKind.InlandPlain);
            lowHillCells += relief.LandformByCellId.Count(kind => kind == (byte)ContinentalReliefKind.Hills);
            for (int id = 0; id < topology.Count; id++)
            {
                Check(surface[id] == 0 ? heights[id] == 0 && relief.OceanDepthByCellId[id] > 0 :
                    heights[id] is >= 1 and <= 100 && relief.OceanDepthByCellId[id] == 0,
                    "Ocean has zero terrestrial height and land has valid 1..100 height");
                if (surface[id] == 1 && heights[id] >= 85) high++;
                if (surface[id] == 1 && relief.LandformByCellId[id] is
                    (byte)ContinentalReliefKind.Mountain or (byte)ContinentalReliefKind.HighRange) mountain++;
            }
            Console.WriteLine($"RELIEF COVERAGE {size}/{seed}: land {land}, summits {high} ({(double)high / land:P1}), mountains {mountain} ({(double)mountain / land:P1})");
            Check(high <= land * .08 && mountain <= land * .35,
                "High mountains must remain geographically limited");
            highTotal += high;
            collisionTotal += relief.Contacts.Count(c => c.Kind is GeologicalContactKind.ContinentalCollision or
                GeologicalContactKind.OceanContinentSubduction);
            riftTotal += relief.Contacts.Count(c => c.Kind == GeologicalContactKind.ContinentalRift);
            int[] upliftDistance = DistanceFromContacts(topology, motion, relief.Contacts,
                kind => kind is GeologicalContactKind.ContinentalCollision or
                    GeologicalContactKind.OceanContinentSubduction,
                relief.Systems.Where(s => s.Kind is GeologicalContactKind.ContinentalCollision or
                    GeologicalContactKind.OceanContinentSubduction).SelectMany(s => s.CrestCellIds));
            int associatedHigh = Enumerable.Range(0, topology.Count).Count(id =>
                surface[id] == 1 && heights[id] >= 70 && upliftDistance[id] <= 9);
            int allHigh = Enumerable.Range(0, topology.Count).Count(id => surface[id] == 1 && heights[id] >= 70);
            Check(allHigh == 0 || associatedHigh >= allHigh * .75,
                "High ranges must lie near classified convergence or its attributed terrestrial crest");

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
                Check(quietTerrain.ContinentalElevation!.Systems.Length == 0 &&
                    quietTerrain.ContinentalElevation.MountainTierByCellId.All(value => value == 0),
                    "No active contact means no principal or secondary mountain system");
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
            if (export && (size, seed) is (GridSize.Small, 2026) or (GridSize.Small, 42) or
                (GridSize.Medium, 42) or (GridSize.Medium, -731) or (GridSize.Large, 42) or
                (GridSize.Large, -731) or (GridSize.Enormous, 2026))
            {
                string name = $"continental-belts-{size.ToString().ToLowerInvariant()}-{seed}";
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
        Console.WriteLine($"COLLISION BELTS: {legacyCollisionSystems} old segments -> " +
            $"{systemCounts[(int)GeologicalContactKind.ContinentalCollision]} regional systems; " +
            $"{multiPlateCollisions} span 3+ plates, longest gain {longestGain} edges, " +
            $"{collisionSutureCells} suture hexes; longest by size " +
            string.Join(", ", longestCollisionBySize.Select(item => $"{item.Key}={item.Value}")));
        Console.WriteLine($"RELIEF REGIONS: {variedCollisionBelts} collision belts vary by 15+ height, " +
            $"{plateauCells} plateau, {plainCells} plain and {lowHillCells} low-hill land hexes");
        Check(multiPlateCollisions > 0 && collisionSutureCells > 0 &&
            variedCollisionBelts > 0 &&
            longestCollisionBySize[GridSize.Enormous] >
                longestCollisionBySize[GridSize.Small] * 1.3,
            "Connected multi-plate collision belts should lengthen with world scale");
        // Broad plateaus are now intentional. Low hills (18..29) are also lowland,
        // not mountain, so include them when checking that stable interiors survive.
        Check(plateauCells > 0 && plainCells + lowHillCells > plateauCells,
            "Lowlands must remain extensive alongside regional collision plateaus");
        Console.WriteLine($"PROCESS EVIDENCE: extended collisions {elevatedCollisionAxes}/{extendedCollisions}, long mountain belts {longMountainBelts}, " +
            $"coastal arc cells {coastalArcs}, trench cells {oceanTrenches}, island arc cells {islandArcs}, " +
            $"rift axes {riftAxes}, ridge axes {ridgeAxes}");
        Check(extendedCollisions > 0 && elevatedCollisionAxes > 0 && longMountainBelts > 0 && coastalArcs > 0 &&
            oceanTrenches > 0 && existingIslandArcChecked && riftAxes > 0 && ridgeAxes > 0,
            "Connected collisions, subduction, rifts and ocean ridges must leave geological evidence");
        int sampledWorlds = 0, worldsWithPrincipal = 0, largeWithMultiple = 0;
        foreach (var size in Enum.GetValues<GridSize>())
        foreach (int seed in new[] { 7, 99, 12345, 314159, -2026, 8675309 })
        {
            var terrain = service.Generate(LandmassProfiles.Create(size, LandmassType.Continents, seed)).Landmass;
            int principal = terrain.ContinentalElevation!.Systems.Count(system =>
                system.MountainTier == TectonicMountainTier.Principal);
            int mountainCells = terrain.ContinentalElevation.LandformByCellId.Count(kind =>
                kind is (byte)ContinentalReliefKind.Mountain or (byte)ContinentalReliefKind.HighRange);
            int landCells = terrain.SurfaceByCellId.Count(kind => kind == 1);
            Check(principal <= 3 && mountainCells <= landCells * .35,
                "Tectonic opportunities must not make every continent mountainous");
            sampledWorlds++;
            if (principal > 0) worldsWithPrincipal++;
            if (size is GridSize.Large or GridSize.Enormous && principal >= 2) largeWithMultiple++;
            Console.WriteLine($"CALIBRATION {size}/{seed}: principal {principal}, " +
                $"mountain land {mountainCells * 100.0 / landCells:F1}%");
        }
        Check(worldsWithPrincipal >= sampledWorlds * .7 && largeWithMultiple >= 4,
            "Most seeds need a tectonically justified principal range, including multiple large-world systems");
        Console.WriteLine($"PASS: calibration {worldsWithPrincipal}/{sampledWorlds} worlds with principal systems; " +
            $"{largeWithMultiple}/12 large worlds with multiple systems");
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
                    system.ParticipatingPlateIds.Contains(edge.FirstPlateId) &&
                    system.ParticipatingPlateIds.Contains(edge.SecondPlateId) &&
                    (system.Kind == GeologicalContactKind.ContinentalCollision &&
                        system.ParticipatingPlateIds.Length > 2 ||
                     Math.Min(edge.FirstPlateId, edge.SecondPlateId) == system.FirstPlateId &&
                     Math.Max(edge.FirstPlateId, edge.SecondPlateId) == system.SecondPlateId) &&
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
        GeologicalContact[] contacts, Func<GeologicalContactKind, bool> include,
        IEnumerable<int>? terrestrialCrests = null)
    {
        int[] distances = Enumerable.Repeat(int.MaxValue, topology.Count).ToArray();
        var queue = new int[topology.Count];
        int head = 0, tail = 0;
        foreach (int id in terrestrialCrests ?? [])
            if (distances[id] == int.MaxValue)
            { distances[id] = 0; queue[tail++] = id; }
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
            var previousTerrain = new ContinentalElevationGenerator().Generate(
                new HexTopology(world.Grid), world.Landmass, useRegionalBelts: false);
            var oldSelection = previousTerrain.ContinentalSurface!.Selection!;
            var oldAttempts = oldSelection.Attempts.ToArray();
            oldAttempts[oldSelection.SelectedAttempt] = oldAttempts[oldSelection.SelectedAttempt] with
            {
                PrincipalSystems = previousTerrain.ContinentalElevation!.Systems.Count(s => s.MountainTier == TectonicMountainTier.Principal),
                SecondarySystems = previousTerrain.ContinentalElevation.Systems.Count(s => s.MountainTier == TectonicMountainTier.Secondary)
            };
            state.Landmass = previousTerrain with
            {
                ContinentalSurface = previousTerrain.ContinentalSurface! with
                {
                    Selection = previousTerrain.ContinentalSurface.Selection! with
                        { Attempts = oldAttempts, FinalPrincipalSystems = 0, FinalSecondarySystems = 0 }
                },
                ContinentalElevation = previousTerrain.ContinentalElevation! with
                {
                    Version = "continental-elevation-4",
                    Systems = previousTerrain.ContinentalElevation.Systems.Select(system =>
                        system with { ParticipatingPlateIds = [] }).ToArray()
                }
            };
            GameSaveStore.Save(path, state);
            Check(GameSaveStore.Load(path).Landmass!.ContinentalElevation!.Version ==
                "continental-elevation-4", "Prior orogenic saves remain readable");
            // Saves made after the surface stage but before elevations remain readable.
            state.Landmass = world.Landmass with
            {
                ElevationByCellId = null, ContinentalElevation = null,
                ContinentalSurface = world.Landmass.ContinentalSurface! with { Selection = null }
            };
            GameSaveStore.Save(path, state);
            Check(GameSaveStore.Load(path).Landmass!.ContinentalSurface is not null,
                "Surface-only continental saves remain compatible");
            state.Landmass = world.Landmass with
            {
                ContinentalSurface = world.Landmass.ContinentalSurface! with { Selection = null },
                ContinentalElevation = world.Landmass.ContinentalElevation! with
                {
                    Version = "continental-elevation-2", MountainTierByCellId = []
                }
            };
            GameSaveStore.Save(path, state);
            Check(GameSaveStore.Load(path).Landmass!.ContinentalElevation!.Version ==
                "continental-elevation-2", "Previous regional elevation saves remain readable");
            // Previous elevation-stage saves used the local contact model and had no systems.
            state.Landmass = world.Landmass with
            {
                ContinentalSurface = world.Landmass.ContinentalSurface! with { Selection = null },
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
