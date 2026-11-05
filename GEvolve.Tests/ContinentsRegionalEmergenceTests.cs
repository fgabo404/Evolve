using System.Globalization;
using System.Text.Json;
using GEvolve.Definitions.WorldGeneration.Configuration;
using GEvolve.Engine.Grid;
using GEvolve.Engine.World;
using GEvolve.Engine.World.LandmassGeneration;
using GEvolve.Engine.World.LandmassGeneration.Shared;
using GEvolve.Game;
using GEvolve.Game.Graphics;
using GEvolve.Game.Screens;
using GEvolve.State.Game;
using GEvolve.State.Persistence;

public static class ContinentsRegionalEmergenceTests
{
    public static void Run()
    {
        var service = new WorldGenerationService();
        foreach (GridSize size in Enum.GetValues<GridSize>())
        foreach (int seed in new[] { 2026, 42, -731 })
        {
            var config = LandmassProfiles.Create(size, LandmassType.Continents, seed);
            var experiment = service.CompareContinentsSurfacePreview(config);
            var world = experiment.Reference;
            var comparison = experiment.Comparison;
            var topology = new HexTopology(world.Grid);
            var plates = world.Landmass.PlateByCellId!;
            var motion = world.Landmass.TectonicMotion!;
            var assembly = experiment.Assembly;
            int range = Math.Max(2, (int)Math.Ceiling(motion.InfluenceRadiusHexes * 4));
            string inputs = JsonSerializer.Serialize(new { Plates = plates, Motion = motion, Assembly = assembly });
            var generator = new ContinentsSurfaceGenerator();
            var repeated = generator.CompareRegional(topology, config, plates, motion, assembly);
            Check(JsonSerializer.Serialize(comparison) == JsonSerializer.Serialize(repeated), "A/B deterministic diagnostics and fields");
            Check(inputs == JsonSerializer.Serialize(new { Plates = plates, Motion = motion, Assembly = assembly }),
                "A/B never mutates common plates, tectonics, cortex, provinces or links");
            Check(comparison.LocalizedSurface.SequenceEqual(service.GenerateContinentsSurfacePreview(config).Landmass.SurfaceByCellId),
                "Operational entry remains exactly A");
            Check(ReferenceEquals(world.Landmass.SurfaceByCellId, comparison.LocalizedSurface) &&
                plates.Distinct().Count() == 16 && assembly.Diagnostics.AcceptedGapHexes == range,
                "One official state, 16 plates and approved 4r");
            VerifyApprovedA(topology, plates, motion, assembly, comparison, range);
            VerifyRegional(topology, plates, motion, assembly, comparison, range);
            VerifySessionPersistence(world, comparison);
            var a = CoastMetrics.Measure(topology, comparison.LocalizedSurface);
            var b = CoastMetrics.Measure(topology, comparison.RegionalSurface);
            int gained = 0, lost = 0;
            for (int id = 0; id < topology.Count; id++)
            {
                Check(comparison.LocalizedSurface[id] <= 1 && comparison.RegionalSurface[id] <= 1,
                    "Both surfaces are complete 0/1 masks");
                if (assembly.Model.CrustByCellId[id] == 0)
                    Check(comparison.LocalizedSurface[id] == 0 && comparison.RegionalSurface[id] == 0 &&
                        comparison.RegionalUplift[id] == 0 && comparison.RegionalSubsidence[id] == 0,
                        "Oceanic crust never receives regional emergence");
                if (comparison.LocalizedSurface[id] == 0 && comparison.RegionalSurface[id] == 1) gained++;
                if (comparison.LocalizedSurface[id] == 1 && comparison.RegionalSurface[id] == 0) lost++;
            }
            Report("A", a); Report("B", b);
            string delta = string.Join(";", comparison.Budgets.Select(x => FormattableString.Invariant(
                $"{x.FragmentId}:+{x.PositiveB-x.PositiveA:G3}/-{x.NegativeB-x.NegativeA:G3}")));
            Console.WriteLine($"REGIONAL {size}/{seed}: sources={comparison.Sources.Length}, " +
                $"representatives={assembly.Evidence.Length}, gained={gained}, lost={lost}, budgetDelta=[{delta}], " +
                $"accretionContactsA/B={Contacts(comparison.LocalizedSurface)}/{Contacts(comparison.RegionalSurface)}, " +
                $"accretedPairsSameMassA/B={SameMass(a.Labels)}/{SameMass(b.Labels)}");
            foreach (var budget in comparison.Budgets)
                if (budget.PositiveStatus is not ("conserved" or "zero budget") ||
                    budget.NegativeStatus is not ("conserved" or "zero budget"))
                    Console.WriteLine($"REGIONAL BUDGET {size}/{seed}/{budget.FragmentId}: {budget}");
            if (size == GridSize.Small && seed == 2026)
            {
                var noLinks = assembly with { Model = assembly.Model with { AssemblyLinks = [] }, Evidence = [] };
                var empty = generator.CompareRegional(topology, config, plates, motion, noLinks);
                Check(empty.Sources.Length == 0 && empty.LocalizedSurface.SequenceEqual(empty.RegionalSurface),
                    "No links means no regional contribution");
                ExpectArgument(() => generator.CompareRegional(topology, config, plates,
                    motion with { Interactions = [..motion.Interactions, motion.Interactions[0]] }, assembly));
                var reversed = motion with { Interactions = motion.Interactions.Reverse().ToArray() };
                var reindexed = assembly with { Evidence = assembly.Evidence.Select(e => e with
                    { InteractionIndex = motion.Interactions.Length - 1 - e.InteractionIndex }).ToArray() };
                var reordered = generator.CompareRegional(topology, config, plates, reversed, reindexed);
                Check(comparison.RegionalSurface.SequenceEqual(reordered.RegionalSurface) &&
                    comparison.RegionalUplift.SequenceEqual(reordered.RegionalUplift) &&
                    comparison.RegionalSubsidence.SequenceEqual(reordered.RegionalSubsidence),
                    "Regional maximum kernels do not depend on source enumeration order");
            }

            void Report(string variant, CoastMetrics metrics)
            {
                string shares=string.Join(",",metrics.Areas.Take(4).Select(x=>(100.0*x/Math.Max(1,metrics.Areas.Sum())).ToString("F1",CultureInfo.InvariantCulture)));
                Console.WriteLine(FormattableString.Invariant($"REGIONAL {size}/{seed} {variant}: land={100.0*metrics.Areas.Sum()/topology.Count:F2}%, masses={metrics.Areas.Length}, mainShares=[{shares}], coast={metrics.Coast}, coast/sqrtArea={metrics.NormalizedCoast:F3}, compactP/sqrtArea(mean/max)={metrics.MeanCompactness:F3}/{metrics.MaxCompactness:F3}, orient120/0/60deg=[{string.Join(",",metrics.Orientations)}], straightExactMax={metrics.ExactStraightMax}, straight1hexMax={metrics.DigitalStraightMax:F1}, openNS={metrics.OpenEdges}, clippedMasses={metrics.ClippedMasses}"));
            }
            int Contacts(byte[] surface) => comparison.Sources.Count(s =>
                assembly.Evidence[s.LinkIndex].Kind == CorticalAssemblyKind.Accreted && s.OceanGapHexes == 0 &&
                surface[s.FirstSourceCellId] == 1 && surface[s.SecondSourceCellId] == 1);
            int SameMass(int[] labels)
            {
                var sets = assembly.Model.Fragments.Select(_ => new HashSet<int>()).ToArray();
                for (int id = 0; id < topology.Count; id++)
                    if (labels[id] >= 0) sets[assembly.Model.FragmentByCellId[id]].Add(labels[id]);
                return assembly.Model.AssemblyLinks.Count(link => link.Kind == CorticalAssemblyKind.Accreted &&
                    sets[link.FirstFragmentId].Overlaps(sets[link.SecondFragmentId]));
            }
        }
        VerifyMetricFixtures();
        Console.WriteLine("PASS: regional emergence A/B contracts; technical/visual adoption pending external audit");
    }

    // Independent endpoint BFS, not the production regional source builder.
    private static int[] Distances(HexTopology topology, byte[] plates, SharedCorticalModel model,
        int fragment, IEnumerable<int> origins, int range)
    {
        var result = new int[topology.Count]; Array.Fill(result, -1);
        var pending = new Queue<int>();
        foreach (int origin in origins.Distinct()) { result[origin] = 0; pending.Enqueue(origin); }
        while (pending.TryDequeue(out int cell))
        {
            if (result[cell] == range) continue;
            foreach (int adjacent in topology.Neighbors(cell))
                if (result[adjacent] == -1 && plates[adjacent] == plates[cell] &&
                    (model.FragmentByCellId[adjacent] == -1 || model.FragmentByCellId[adjacent] == fragment))
                { result[adjacent] = result[cell] + 1; pending.Enqueue(adjacent); }
        }
        return result;
    }

    private static void VerifyApprovedA(HexTopology topology, byte[] plates, TectonicMotionState motion,
        ContinentsCorticalAssemblyResult assembly, ContinentsSurfaceComparison c, int range)
    {
        var model = assembly.Model;
        var positive = new double[topology.Count]; var negative = new double[topology.Count];
        foreach (var evidence in assembly.Evidence)
        {
            var edge = motion.Interactions[evidence.InteractionIndex];
            int[] af = Get(evidence.FirstFragmentId, edge.FirstCellId);
            int[] bs = Get(evidence.SecondFragmentId, edge.SecondCellId);
            int[] ar = Get(evidence.FirstFragmentId, edge.SecondCellId);
            int[] br = Get(evidence.SecondFragmentId, edge.FirstCellId);
            int forward = Nearest(af, evidence.FirstFragmentId) + Nearest(bs, evidence.SecondFragmentId);
            int reverse = Nearest(ar, evidence.FirstFragmentId) + Nearest(br, evidence.SecondFragmentId);
            for (int id = 0; id < topology.Count; id++)
            {
                int fragment = model.FragmentByCellId[id];
                int d = fragment == evidence.FirstFragmentId ? (forward <= reverse ? af[id] : ar[id]) :
                    fragment == evidence.SecondFragmentId ? (forward <= reverse ? bs[id] : br[id]) : -1;
                if (d < 0) continue;
                double weight = 1 - d / (range + 1.0);
                if (evidence.Kind == CorticalAssemblyKind.Accreted) positive[id] = Math.Max(positive[id], .10*weight);
                else negative[id] = Math.Max(negative[id], .18*weight);
            }
            int[] Get(int fragment, int source) => model.FragmentByCellId[source] >= 0 && model.FragmentByCellId[source] != fragment
                ? Enumerable.Repeat(-1, topology.Count).ToArray() : Distances(topology, plates, model, fragment, [source], range);
            int Nearest(int[] values, int fragment)
            {
                int best = 1_000_000;
                for (int id = 0; id < topology.Count; id++)
                    if (model.FragmentByCellId[id] == fragment && values[id] >= 0) best = Math.Min(best,values[id]);
                return best;
            }
        }
        // Independent global ocean-distance BFS freezes the approved coastal term.
        var oceanDistance = new int[topology.Count]; Array.Fill(oceanDistance,-1);
        var pending = new Queue<int>();
        for (int id=0;id<topology.Count;id++) if (model.CrustByCellId[id]==0) { oceanDistance[id]=0; pending.Enqueue(id); }
        while (pending.TryDequeue(out int cell)) foreach(int next in topology.Neighbors(cell))
            if(oceanDistance[next]<0) { oceanDistance[next]=oceanDistance[cell]+1; pending.Enqueue(next); }
        for(int id=0;id<topology.Count;id++)
        {
            double interior = Math.Clamp((oceanDistance[id]-1)/Math.Max(1,Math.Ceiling(motion.InfluenceRadiusHexes)),0,1);
            double potential = model.EmergenceAffinityByCellId[id]+.12*interior-.22*(1-interior);
            Check(potential==c.BasePotential[id] && positive[id]==c.LocalizedUplift[id] && negative[id]==c.LocalizedSubsidence[id],
                "A matches approved coastal potential, localized kernel, bounds and maximum combination");
            byte expected = (byte)(model.CrustByCellId[id]==1 && potential+positive[id]-negative[id]>.38 ? 1:0);
            Check(c.LocalizedSurface[id]==expected,"A preserves the approved 0.38 classification exactly");
        }
    }

    private static void VerifyRegional(HexTopology topology, byte[] plates, TectonicMotionState motion,
        ContinentsCorticalAssemblyResult assembly, ContinentsSurfaceComparison c, int range)
    {
        var model = assembly.Model;
        var distances = model.Fragments.Select(f => Distances(topology, plates, model, f.Id,
            Enumerable.Range(0,topology.Count).Where(id=>model.FragmentByCellId[id]==f.Id),range)).ToArray();
        for(int link=0;link<assembly.Evidence.Length;link++)
        {
            var evidence=assembly.Evidence[link]; var representative=motion.Interactions[evidence.InteractionIndex];
            var eligible = new HashSet<int>();
            for(int index=0;index<motion.Interactions.Length;index++)
            {
                var edge=motion.Interactions[index];
                bool samePlates = Math.Min(edge.FirstPlateId,edge.SecondPlateId)==Math.Min(representative.FirstPlateId,representative.SecondPlateId) &&
                    Math.Max(edge.FirstPlateId,edge.SecondPlateId)==Math.Max(representative.FirstPlateId,representative.SecondPlateId);
                bool normal = evidence.Kind==CorticalAssemblyKind.Accreted
                    ? edge.Convergence>=.5 && edge.Convergence>edge.Divergence && edge.Convergence>=.4*edge.Shear
                    : edge.Divergence>=.5 && edge.Divergence>edge.Convergence && edge.Divergence>=.4*edge.Shear;
                if(samePlates && normal && Math.Min(Gap(edge.FirstCellId,edge.SecondCellId),Gap(edge.SecondCellId,edge.FirstCellId))<=range)
                    eligible.Add(index);
            }
            var byCell = new Dictionary<int,List<int>>();
            foreach(int index in eligible)
            {
                var e=motion.Interactions[index];
                foreach(int cell in new[]{e.FirstCellId,e.SecondCellId})
                { if(!byCell.TryGetValue(cell,out var list)) byCell[cell]=list=[]; list.Add(index); }
            }
            var expected=new HashSet<int>{evidence.InteractionIndex}; var queue=new Queue<int>(); queue.Enqueue(evidence.InteractionIndex);
            while(queue.TryDequeue(out int reachableEdge))
            {
                var e=motion.Interactions[reachableEdge];
                foreach(int cell in new[]{e.FirstCellId,e.SecondCellId}) foreach(int adjacent in byCell[cell])
                    if(expected.Add(adjacent)) queue.Enqueue(adjacent);
            }
            var actual=c.Sources.Where(s=>s.LinkIndex==link).ToArray();
            Check(expected.SetEquals(actual.Select(s=>s.InteractionIndex)) && actual.Length==expected.Count,
                "All and only admissible contacts on the representative connected boundary segment");
            var seen=new HashSet<int>();
            foreach(var source in actual)
            {
                var edge=motion.Interactions[source.InteractionIndex];
                Check(topology.Neighbors(edge.FirstCellId).Contains(edge.SecondCellId) && plates[edge.FirstCellId]!=plates[edge.SecondCellId] &&
                    (source.FirstSourceCellId==edge.FirstCellId && source.SecondSourceCellId==edge.SecondCellId ||
                     source.FirstSourceCellId==edge.SecondCellId && source.SecondSourceCellId==edge.FirstCellId) &&
                    source.OceanGapHexes==Gap(source.FirstSourceCellId,source.SecondSourceCellId) && source.OceanGapHexes<=range,
                    "Every source has two independent fragment-side paths and a real inter-plate edge");
                Check(source.ParentInteractionIndex==-1 ? source.InteractionIndex==evidence.InteractionIndex :
                    seen.Contains(source.ParentInteractionIndex) && Shares(edge,motion.Interactions[source.ParentInteractionIndex]),
                    "Source provenance traces to its original representative without disconnected jumps");
                seen.Add(source.InteractionIndex);
            }
            int Gap(int first,int second)
            { int a=distances[evidence.FirstFragmentId][first],b=distances[evidence.SecondFragmentId][second]; return a<0||b<0?int.MaxValue:a+b; }
        }
        Check(c.Budgets.Length==model.Fragments.Length,"One signed budget pair per fragment");
        foreach(var fragment in model.Fragments)
        {
            var cells=Enumerable.Range(0,topology.Count).Where(id=>model.FragmentByCellId[id]==fragment.Id).ToArray();
            var budget=c.Budgets[fragment.Id];
            Check(budget.FragmentId==fragment.Id,"Budget retains fragment identity");
            foreach(bool positive in new[]{true,false})
            {
                var seeds = c.Sources.Where(s=>assembly.Evidence[s.LinkIndex].Kind==
                    (positive?CorticalAssemblyKind.Accreted:CorticalAssemblyKind.RiftSeparated))
                    .SelectMany(s=>new[]{(Fragment:assembly.Evidence[s.LinkIndex].FirstFragmentId,Cell:s.FirstSourceCellId),
                        (Fragment:assembly.Evidence[s.LinkIndex].SecondFragmentId,Cell:s.SecondSourceCellId)})
                    .Where(s=>s.Fragment==fragment.Id).Select(s=>s.Cell).Distinct();
                var support=Distances(topology,plates,model,fragment.Id,seeds,range);
                var localized=positive?c.LocalizedUplift:c.LocalizedSubsidence;
                var regional=positive?c.RegionalUplift:c.RegionalSubsidence;
                double cap=positive ? .10 : .18, target=cells.Sum(id=>localized[id]),actual=cells.Sum(id=>regional[id]);
                Check(Close(target,positive?budget.PositiveA:budget.NegativeA) && Close(actual,positive?budget.PositiveB:budget.NegativeB),
                    "Reported budgets equal independently summed fields");
                Check(Close(target,actual),"Representative support makes exact signed budget redistribution feasible");
                foreach(int id in cells)
                    Check(double.IsFinite(regional[id]) && regional[id]>=0 && regional[id]<=cap &&
                        (support[id]<0?regional[id]==0:regional[id]<=cap*(1-support[id]/(range+1.0))+1e-12),
                        "Regional support remains inside verified 4r paths and the bounded maximum kernel");
            }
        }
        for(int id=0;id<topology.Count;id++)
            Check(c.RegionalSurface[id]==(byte)(model.CrustByCellId[id]==1 && c.BasePotential[id]+c.RegionalUplift[id]-c.RegionalSubsidence[id]>.38?1:0),
                "B replaces localized effects and retains the identical base potential and sea level");
        static bool Shares(PlateInteraction a,PlateInteraction b) => a.FirstCellId==b.FirstCellId||a.FirstCellId==b.SecondCellId||
            a.SecondCellId==b.FirstCellId||a.SecondCellId==b.SecondCellId;
    }

    private static void VerifySessionPersistence(GeneratedWorld world,ContinentsSurfaceComparison comparison)
    {
        var session=new GameSession(); session.OpenCreateWorld(); session.ChooseSize(world.Landmass.Configuration.Size);
        session.ChooseType(LandmassType.Continents); session.SetSeedText(world.Landmass.Configuration.Seed.ToString(CultureInfo.InvariantCulture));
        session.BeginGeneration(); session.CompleteGeneration(world); session.AttachSurfaceComparison(comparison);
        session.SelectAt(FlatHexGeometry.Center(new GridAddress(0,world.Grid.Definition.Rows/2)));
        var selected=session.SelectedCell; string before=JsonSerializer.Serialize(session.State);
        var b=session.SelectSurfaceVariant(true);
        Check(session.IsRegionalSurface && ReferenceEquals(b.SurfaceByCellId,comparison.RegionalSurface) &&
            ReferenceEquals(b.SharedCortex,world.Landmass.SharedCortex) && selected is not null && session.SelectedCell==selected &&
            ReferenceEquals(session.Grid,world.Grid) && before==JsonSerializer.Serialize(session.State),
            "Selecting B preserves official A, grid, selection, seed and persistence state");
        for(int id=0;id<world.Grid.Cells.Count;id++)
            Check(MapViewColors.ForCell(b,id,MapViewMode.Surface)==(comparison.RegionalSurface[id]==1
                ? MapViewColors.SurfaceLandColor:MapViewColors.SurfaceOceanColor),"B uses the identical A land/ocean palette");
        string path=Path.Combine(Path.GetTempPath(),$"evolve-regional-{Guid.NewGuid():N}.json");
        try
        {
            GameSaveStore.Save(path,session.State);
            Check(JsonSerializer.Serialize(GameSaveStore.Load(path).Landmass)==JsonSerializer.Serialize(world.Landmass),
                "Saving while viewing B persists exact official A and cortex");
        }
        finally { if(File.Exists(path)) File.Delete(path); }
        Check(ReferenceEquals(session.SelectSurfaceVariant(false),world.Landmass),"Selecting A restores the original object");
        foreach(var dimensions in new[]{(1280,720),(640,480)})
        {
            var buttons=ScreenLayout.Buttons(GameScreen.World,dimensions.Item1,dimensions.Item2,true,true);
            Check(buttons.Single(b=>b.Action==UiAction.SurfaceLocalized).Text=="A — LOCALIZADA" &&
                buttons.Single(b=>b.Action==UiAction.SurfaceRegional).Text=="B — REGIONAL", "Explicit A/B controls at both layouts");
            foreach(var button in buttons)
                Check(button.Bounds.X>=0 && button.Bounds.Y>=0 && button.Bounds.X+button.Bounds.Width<=dimensions.Item1 &&
                    button.Bounds.Y+button.Bounds.Height<=dimensions.Item2,"Comparison buttons fit both supported layouts");
        }
        session.ReturnToMenu(); Check(!session.HasSurfaceComparison && !session.IsRegionalSurface,"Experiment lifetime is only the session world");
    }

    private static void VerifyMetricFixtures()
    {
        var topology=new HexTopology(HexGrid.Generate(HexGridDefinitions.Get(GridSize.Small)));
        var surface=new byte[topology.Count]; int row=topology.Rows/2;
        surface[topology.Id(0,row)]=1;
        var single=CoastMetrics.Measure(topology,surface);
        Check(single.Coast==6 && single.OpenEdges==0 && single.Orientations.SequenceEqual(new[]{2,2,2}),"Single hex coastline geometry");
        surface[topology.Id(topology.Columns-1,row)]=1;
        var seam=CoastMetrics.Measure(topology,surface);
        Check(seam.Coast==10 && seam.Areas.Length==1,"Seam edge is internal, never a false coast");
        Array.Clear(surface); surface[topology.Id(0,0)]=1;
        var pole=CoastMetrics.Measure(topology,surface);
        Check(pole.Coast==topology.Neighbors(topology.Id(0,0)).Length && pole.Coast+pole.OpenEdges==6 && pole.ClippedMasses==1,
            "Open polar edges are separate from true coastline");
    }

    private static bool Close(double a,double b)=>Math.Abs(a-b)<=1e-9*Math.Max(1,Math.Abs(a));
    private static void ExpectArgument(Action action)
    { try{action();}catch(ArgumentException){return;} throw new InvalidOperationException("Malformed regional contacts accepted"); }
    private static void Check(bool condition,string message)
    { if(!condition) throw new InvalidOperationException(message); }
}
