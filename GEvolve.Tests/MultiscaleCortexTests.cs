using System.Diagnostics;
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

public static class MultiscaleCortexTests
{
    public static void Run()
    {
        var service = new WorldGenerationService();
        VerifyClassification();
        foreach (GridSize size in Enum.GetValues<GridSize>())
        foreach (int seed in new[] { 2026, 42, -731 })
        {
            var config = LandmassProfiles.Create(size, LandmassType.Continents, seed);
            var a = service.GenerateContinentsSurfacePreview(config); var topology = new HexTopology(a.Grid);
            var aCoast=CoastMetrics.Measure(topology,a.Landmass.SurfaceByCellId);
            var aClass=C1GeographicEvaluation.ClassifyAreas(topology.Count,aCoast.Areas);
            Console.WriteLine(FormattableString.Invariant($"C1 A {size}/{seed}: main={aClass.Main.Length}, ambiguous={aClass.Ambiguous.Length}, land={a.Statistics!.LandFraction:P1}, coast={aCoast.Coast}, compact={aCoast.MeanCompactness:F2}, orient=[{string.Join(",",aCoast.Orientations)}]"));
            byte[] plates = a.Landmass.PlateByCellId!; var motion = a.Landmass.TectonicMotion!;
            var reference = SharedCorticalPersistence.FromState(a.Landmass.SharedCortex!);
            string snapshot = JsonSerializer.Serialize(new { Reference = reference, World = a.Landmass });
            var generator = new ContinentsMultiscaleCorticalGenerator();
            using var first = generator.GenerateCandidates(topology, config, plates, motion, reference).GetEnumerator();
            using var second = generator.GenerateCandidates(topology, config, plates, motion, reference).GetEnumerator();
            int candidates = 0, technical = 0; long ticks = 0, allocated = 0;
            var outcomes = new List<string>();
            for (;;)
            {
                long started = Stopwatch.GetTimestamp(), before = GC.GetAllocatedBytesForCurrentThread();
                bool available = first.MoveNext();
                ticks += Stopwatch.GetTimestamp() - started; allocated += GC.GetAllocatedBytesForCurrentThread() - before;
                Check(available == second.MoveNext(), "Deterministic candidate count");
                if (!available) break;
                var candidate = first.Current;
                Check(candidate.Ordinal == candidates && candidate.PlannedDomains == 2 + candidates / 3,
                    "Exactly three candidates per K in 2,3,4");
                candidates++;
                Check(candidate.Solvers.All(r => r.Converged && double.IsFinite(r.RelativeResidual) &&
                    r.RelativeResidual <= C1Profile.SolverTolerance && r.Minimum >= -1e-10 && double.IsFinite(r.Maximum) && r.FieldHash.Length==64),
                    $"C1 numerical convergence: {size}/{seed}/{candidate.Ordinal}: {string.Join(";",candidate.Solvers)}");
                Check(JsonSerializer.Serialize(candidate) == JsonSerializer.Serialize(second.Current),
                    $"Complete C1 export/traces/reports deterministic: {size}/{seed}/{candidate.Ordinal}");
                if (candidate.Model is null)
                {
                    Check(candidate.Failure.Length > 0, "Unavailable geometry has an explicit diagnosis");
                    outcomes.Add($"{candidate.Ordinal}/K{candidate.PlannedDomains}:REJECTED({candidate.Failure})");
                    continue;
                }
                technical++;
                VerifyFormation(topology, plates, motion, reference, candidate);
                var assembly = new ContinentsCorticalAssemblyGenerator().Assemble(topology, config, plates, motion, candidate.Model);
                Check(assembly.Diagnostics.AcceptedGapHexes == Math.Max(2,(int)Math.Ceiling(motion.InfluenceRadiusHexes*4)), "C1 retains B2 4r");
                var surface = new ContinentsSurfaceGenerator().Generate(topology, config, plates, motion, assembly);
                var terrain = a.Landmass with { SurfaceByCellId = surface, SharedCortex = SharedCorticalPersistence.ToState(assembly.Model) };
                for(int id=0;id<topology.Count;id++)
                    Check(surface[id] <= 1 && (candidate.Model.CrustByCellId[id] != 0 || surface[id] == 0),
                        "B3 classifies C1 without oceanic land or future layers");
                var report = C1GeographicEvaluation.Evaluate(topology, terrain, candidate, assembly);
                var repeatedReport = C1GeographicEvaluation.Evaluate(topology, terrain, candidate, assembly);
                Check(JsonSerializer.Serialize(report) == JsonSerializer.Serialize(repeatedReport), "Read-only reproducible geographic evaluation");
                VerifyGeography(topology, terrain, candidate, report);
                string areas = string.Join(",",report.MainAreas.Select(v=>(100.0*v/topology.Count).ToString("F1",CultureInfo.InvariantCulture)));
                outcomes.Add(FormattableString.Invariant($"{candidate.Ordinal}/K{candidate.PlannedDomains}:{report.Status},main=[{areas}],dom={report.MainDominance:P0},amb={report.AmbiguousAreas.Length},def={candidate.Deficits.Sum()},it={candidate.Solvers.Max(s=>s.Iterations)},rr={candidate.Solvers.Max(s=>s.RelativeResidual):G2}"));
                if (candidate.Ordinal == 0)
                    Console.WriteLine(FormattableString.Invariant($"C1 SHAPE {size}/{seed}: land={100.0*report.LandCells/topology.Count:F1}%, secondary={report.SecondaryAreas.Length}, submerged={report.SubmergedContinentalCells}/{report.ContinentalCells}, ocean={report.OceanComponents}/{report.LargestOceanShare:P1}, cortexAreas=[{string.Join(",",report.CorticalFragmentAreas)}], provinces={report.ProvinceAreas.Length}, coast={report.Coast.Coast}, compact={report.Coast.MeanCompactness:F2}, orient=[{string.Join(",",report.Coast.Orientations)}], straight1hex={report.Coast.DigitalStraightMax:F1}, openNS={report.Coast.OpenEdges}, narrow={report.NarrowMainComponents}, accretionDirect/regional={report.DirectAccretions}/{report.RegionalAccretions}, emergedContacts={report.EmergedAccretionContacts}, rifts={report.RiftRelations}"));
            }
            Check(candidates == 9, "Frozen maximum includes all nine K/rank configurations");
            Check(snapshot == JsonSerializer.Serialize(new { Reference = reference, World = a.Landmass }),
                "C1 does not mutate A, official tectonics, affinity, surface or persistence");
            Console.WriteLine($"C1 {size}/{seed}: technical={technical}/9 | {string.Join(" | ",outcomes)}");
            Console.WriteLine(FormattableString.Invariant($"C1 COST {size}/{seed}: firstGeneratorMs={1000.0*ticks/Stopwatch.Frequency:F0}, allocatedMiB={allocated/1048576.0:F1}; excludes repeated validation and B2/B3; not retained memory"));
            if (size == GridSize.Small && seed == 2026)
            {
                VerifyOperator(topology, plates, motion);
                var incompatible=(float[])reference.EmergenceAffinityByCellId.Clone();incompatible[0]+=.01f;
                ExpectArgument(()=>generator.GenerateCandidates(topology,config,plates,motion,
                    reference with {EmergenceAffinityByCellId=incompatible}).Any());
                VerifyNarrowFixture(topology, a);
                var comparison = service.CompareContinentsGeometryPreview(config);
                Check(JsonSerializer.Serialize(comparison.Reference.Landmass)==JsonSerializer.Serialize(a.Landmass),"A/C entry preserves exact operational A");
                Check(comparison.Experiment.Candidates.Length==9,"A/C service reports every candidate");
                if(comparison.Experiment.Selected is { } selected)
                {
                    Check(ReferenceEquals(selected.Terrain.PlateByCellId,comparison.Reference.Landmass.PlateByCellId) &&
                        ReferenceEquals(selected.Terrain.TectonicMotion,comparison.Reference.Landmass.TectonicMotion),"A/C shares tectonics, not regenerated equivalents");
                    VerifySession(comparison.Reference,comparison.Experiment);
                }
                else Check(comparison.Experiment.Candidates.All(c=>c.Geography is null && c.Failure.Length>0),"No inspectable C has explicit reasons, never A relabelled as C");
            }
        }
        Console.WriteLine("PASS: C1 technical invariants; geographic rejection/review is an experimental outcome, not adoption");
    }

    private static void VerifyFormation(HexTopology topology,byte[] plates,TectonicMotionState motion,
        SharedCorticalModel reference,C1FormationCandidate candidate)
    {
        var model=candidate.Model!; var trace=candidate.Trace!;
        var interactions=motion.Interactions.ToDictionary(edge=>(edge.FirstCellId,edge.SecondCellId));
        SharedCorticalModelValidator.Validate(topology,model,plates);
        Check(candidate.Origins.Length==8 && candidate.Origins.Select(o=>o.AnchorCellId).Distinct().Count()==8 && candidate.Solvers.Length==8,
            "Eight distinct sources with complete solver diagnostics");
        int total=reference.CrustByCellId.Count(c=>c==1);
        Check(candidate.CorticalBudget==total && candidate.Origins.Sum(o=>o.Budget)==total,"Global cortex budget equals A exactly");
        Check(candidate.Origins.Where(o=>o.Role==C1OriginRole.Principal).Sum(o=>o.Budget)==(int)Math.Round(.8*total,MidpointRounding.AwayFromZero),"Frozen 80/20 split");
        var owned=new int[8]; var fractionCount=new int[model.Fragments.Length];var provinceCount=new int[model.Provinces.Length];
        for(int id=0;id<topology.Count;id++)
        {
            int origin=trace.OriginByCellId[id];
            float affinity=reference.EmergenceAffinityByCellId[id];
            double expected=reference.CrustByCellId[id]==model.CrustByCellId[id] ? affinity :
                (float)((double)affinity+.70*(model.CrustByCellId[id]-reference.CrustByCellId[id]));
            Check(float.IsFinite(model.EmergenceAffinityByCellId[id]) && model.EmergenceAffinityByCellId[id]==expected,
                "C1 preserves A residual without new cortical properties or noise");
            if(origin<0)
            { Check(model.CrustByCellId[id]==0 && trace.ParentByCellId[id]==-1 && trace.RoundByCellId[id]==-1 && trace.InteractionByCellId[id]==-1,"Ocean has no false ancestry"); continue; }
            Check(origin<8 && model.CrustByCellId[id]==1,"Origin references continental material"); owned[origin]++;
            int fragment=model.FragmentByCellId[id]; fractionCount[fragment]++;
            provinceCount[model.ProvinceByCellId[id]]++;
            Check(candidate.OriginByFragmentId[fragment]==origin,"Physical fragment maps explicitly to ancestry");
            int predecessor=trace.ParentByCellId[id];
            if(predecessor<0)
                Check(id==candidate.Origins[origin].AnchorCellId && trace.RoundByCellId[id]==0,"Only declared roots have no predecessor");
            else
            {
                Check(topology.Neighbors(id).Contains(predecessor) && trace.OriginByCellId[predecessor]==origin &&
                    trace.RoundByCellId[predecessor]<trace.RoundByCellId[id],"Growth is adjacent, contiguous and acyclic by previous rounds");
                if(plates[id]==plates[predecessor]) Check(trace.InteractionByCellId[id]==-1,"Intraplate step invents no interaction");
                else
                {
                    int interaction=trace.InteractionByCellId[id]; Check((uint)interaction<(uint)motion.Interactions.Length,"Crossing has a real interaction index");
                    var edge=motion.Interactions[interaction];
                    Check(edge.FirstCellId==Math.Min(id,predecessor) && edge.SecondCellId==Math.Max(id,predecessor) &&
                        edge.Convergence>=.5 && edge.Convergence>edge.Divergence && edge.Convergence>=.4*edge.Shear,"Crossing has approved convergence, not just plate proximity");
                }
            }
            foreach(int adjacent in topology.Neighbors(id))
                if(trace.OriginByCellId[adjacent]==origin && plates[adjacent]!=plates[id])
                {
                    var edge=interactions[(Math.Min(id,adjacent),Math.Max(id,adjacent))];
                    Check(edge.Convergence>=.5 && edge.Convergence>edge.Divergence && edge.Convergence>=.4*edge.Shear,"Whole final same-origin interface is admissible");
                }
        }
        foreach(var origin in candidate.Origins)
            Check(origin.Budget==owned[origin.Id]+candidate.Deficits[origin.Id] && candidate.Deficits[origin.Id]>=0,
                "Every missing cortical cell is reported per origin");
        for(int fragment=0;fragment<model.Fragments.Length;fragment++)
        {
            var reached=new HashSet<int>{model.Fragments[fragment].CoreCellId}; var queue=new Queue<int>(); queue.Enqueue(reached.Single());
            while(queue.TryDequeue(out int cell)) foreach(int adjacent in topology.Neighbors(cell))
                if(model.FragmentByCellId[adjacent]==fragment && reached.Add(adjacent)) queue.Enqueue(adjacent);
            Check(reached.Count==fractionCount[fragment],"Each physical fragment is one actual connected component");
        }
        Check(candidate.ProvinceRoles.Length==model.Provinces.Length,"Provinces keep separate temporary roles");
        foreach(var province in model.Provinces)
        {
            var reached=new HashSet<int>{province.CoreCellId};var queue=new Queue<int>();queue.Enqueue(province.CoreCellId);
            while(queue.TryDequeue(out int cell))foreach(int adjacent in topology.Neighbors(cell))
                if(model.ProvinceByCellId[adjacent]==province.Id&&reached.Add(adjacent))queue.Enqueue(adjacent);
            Check(reached.Count==provinceCount[province.Id]&&Enum.IsDefined(candidate.ProvinceRoles[province.Id]),"Independent province BFS and explicit role");
        }
        foreach(var origin in candidate.Origins.Where(o=>o.ParentInteractionIndex>=0))
        {
            Check(origin.Role==C1OriginRole.Secondary&&origin.ProspectiveDomainId>=0,"Only associated secondary origins have prospective contacts");
            int root=candidate.Origins[candidate.Domains[origin.ProspectiveDomainId].PrincipalOriginId].AnchorCellId;
            var edge=motion.Interactions[origin.ParentInteractionIndex];
            int ownSide=edge.FirstCellId==origin.AnchorCellId?edge.SecondCellId:edge.FirstCellId;
            Check((edge.FirstCellId==origin.AnchorCellId||edge.SecondCellId==origin.AnchorCellId)&&plates[ownSide]==plates[root],"Prospective source is on the other side of a real contact");
            var distances=new Dictionary<int,int>{{root,0}};var queue=new Queue<int>();queue.Enqueue(root);
            int range=Math.Max(2,(int)Math.Ceiling(motion.InfluenceRadiusHexes*4));
            while(queue.TryDequeue(out int cell))
            { if(distances[cell]>=range)continue;foreach(int adjacent in topology.Neighbors(cell))
                if(plates[adjacent]==plates[root]&&distances.TryAdd(adjacent,distances[cell]+1))queue.Enqueue(adjacent); }
            Check(distances.ContainsKey(ownSide)&&edge.Convergence>=.5&&edge.Convergence>edge.Divergence&&edge.Convergence>=.4*edge.Shear,
                "Prospective association has a bounded same-plate path, without claiming a completed B2 link");
        }
    }

    private static void VerifyGeography(HexTopology topology,LandmassState terrain,C1FormationCandidate candidate,C1GeographicReport report)
    {
        var sizes=new List<int>(); var seen=new bool[topology.Count]; var queue=new Queue<int>();
        for(int start=0;start<topology.Count;start++)
        {
            if(seen[start]||!terrain.IsLand(start))continue;
            int area=0;seen[start]=true;queue.Enqueue(start);
            while(queue.TryDequeue(out int cell))
            { area++;foreach(int adjacent in topology.Neighbors(cell))if(!seen[adjacent]&&terrain.IsLand(adjacent)){seen[adjacent]=true;queue.Enqueue(adjacent);} }
            sizes.Add(area);
        }
        Check(report.MainAreas.SequenceEqual(sizes.Where(a=>(long)a*100>=3L*topology.Count).OrderDescending()) &&
            report.AmbiguousAreas.SequenceEqual(sizes.Where(a=>(long)a*100>=topology.Count&&(long)a*100<3L*topology.Count).OrderDescending()),
            "Independent land BFS counts every qualifying main and ambiguous component");
        if(report.Status==C1GeographicStatus.Accepted)
            Check(report.MainAreas.Length is >=2 and <=4 && report.MainDominance>=.60 && report.AmbiguousAreas.Length==0 &&
                report.NarrowMainComponents==0 && report.ShelfSeparatedMainPairs==0 && report.UnsupportedMainComposites==0 &&
                candidate.Deficits.Sum()==0 && report.ContinentalCells==candidate.CorticalBudget,"Accepted experimental criteria, never geological approval");
        if(candidate.Deficits.Sum()>0 || report.MainAreas.Length is <2 or >4 || report.MainDominance<.60)
            Check(report.Status==C1GeographicStatus.Rejected && report.Reasons.Length>0,"Failed geography is explicitly rejected without modifying the mask");
    }

    private static void VerifyOperator(HexTopology topology,byte[] plates,TectonicMotionState motion)
    {
        var graph=new C1GrowthField(topology,plates,motion); int n=topology.Count;
        var x=Enumerable.Range(0,n).Select(id=>Math.Sin(id*.17)).ToArray();
        var y=Enumerable.Range(0,n).Select(id=>Math.Cos(id*.11)).ToArray();var ax=new double[n];var ay=new double[n];
        graph.Apply(x,ax,128);graph.Apply(y,ay,128);
        double xay=0,yax=0,positive=0,energy=0;
        for(int id=0;id<n;id++)
        {
            Check(graph.Resistance[id]>=.5 && graph.Resistance[id]<=2.25 && graph.TensorXX[id]>0 &&
                graph.TensorYY[id]>0 && graph.TensorXX[id]*graph.TensorYY[id]-graph.TensorXY[id]*graph.TensorXY[id]>0,"Positive resistance and symmetric positive tensor");
            xay+=x[id]*ay[id];yax+=y[id]*ax[id];positive+=x[id]*ax[id];energy+=x[id]*x[id];
            var neighbors=topology.Neighbors(id);
            for(int slot=0;slot<neighbors.Length;slot++)
            {
                int adjacent=neighbors[slot],reverse=topology.Neighbors(adjacent).IndexOf(id);double g=graph.Conductances[id*6+slot];
                Check(double.IsFinite(g)&&g>=0&&g==graph.Conductances[adjacent*6+reverse],"Reciprocal nonnegative conductance, including seam");
                if(adjacent>id)energy+=128/Math.PI*g*(x[id]-x[adjacent])*(x[id]-x[adjacent]);
            }
        }
        Check(Close(xay,yax)&&positive>0&&Close(positive,energy),"Matrix symmetry and independent positive energy identity");
        int anchor=topology.Id(0,topology.Rows/2);var solved=graph.Solve(0,anchor,128);graph.Apply(solved.Values,ax,128);
        double error=0;for(int id=0;id<n;id++){double r=ax[id]-(id==anchor?1:0);error+=r*r;}
        Check(solved.Report.Converged && Math.Sqrt(error)<=1e-8,"True independently recomputed PCG residual");
        var quiet=motion with {Interactions=motion.Interactions.Select(e=>e with {Convergence=0,Divergence=0,Shear=0}).ToArray()};
        var isotropic=new C1GrowthField(topology,plates,quiet);
        Check(isotropic.TensorXX.All(v=>v==1)&&isotropic.TensorYY.All(v=>v==1)&&isotropic.TensorXY.All(v=>v==0),"No directional evidence means isotropic tensor");
        var invalidNormal=(PlateInteraction[])motion.Interactions.Clone();invalidNormal[0]=invalidNormal[0] with {Normal=new(double.NaN,0)};
        ExpectArgument(()=>new C1GrowthField(topology,plates,motion with {Interactions=invalidNormal}));
        var invalidField=(float[])motion.CompressionByCellId.Clone();invalidField[0]=float.PositiveInfinity;
        ExpectArgument(()=>new C1GrowthField(topology,plates,motion with {CompressionByCellId=invalidField}));
    }

    private static void VerifySession(GeneratedWorld a,C1ExperimentResult experiment)
    {
        var session=new GameSession();session.OpenCreateWorld();session.ChooseSize(a.Landmass.Configuration.Size);session.ChooseType(LandmassType.Continents);
        session.SetSeedText(a.Landmass.Configuration.Seed.ToString(CultureInfo.InvariantCulture));session.BeginGeneration();session.CompleteGeneration(a);
        session.SelectAt(FlatHexGeometry.Center(new GridAddress(0,20)));var selected=session.SelectedCell;
        string before=JsonSerializer.Serialize(session.State);session.AttachGeometryComparison(experiment);var c=session.SelectGeometryVariant(true);
        Check(session.SelectedCell==selected&&selected is not null&&ReferenceEquals(session.Grid,a.Grid)&&
            ReferenceEquals(c.SharedCortex,experiment.Selected!.Terrain.SharedCortex)&&before==JsonSerializer.Serialize(session.State),"A/C preserves selection, grid and official A state");
        string path=Path.Combine(Path.GetTempPath(),$"evolve-c1-{Guid.NewGuid():N}.json");
        try{GameSaveStore.Save(path,session.State);Check(JsonSerializer.Serialize(GameSaveStore.Load(path).Landmass)==JsonSerializer.Serialize(a.Landmass),"Viewing C persists only A");}
        finally{if(File.Exists(path))File.Delete(path);}
        Check(ReferenceEquals(session.SelectGeometryVariant(false),a.Landmass),"A selector restores the original object");
        foreach(var dimensions in new[]{(1280,720),(640,480)})
        {
            var buttons=ScreenLayout.Buttons(GameScreen.World,dimensions.Item1,dimensions.Item2,true,false,true);
            Check(buttons.Single(b=>b.Action==UiAction.GeometryCurrent).Text=="A — CORTEZA ACTUAL"&&
                buttons.Single(b=>b.Action==UiAction.GeometryMultiscale).Text=="C — MULTIESCALA","Explicit geometry comparison, not surface-only B");
            foreach(var button in buttons)Check(button.Bounds.Y+button.Bounds.Height<=dimensions.Item2,"C1 controls fit the existing viewport");
        }
        session.ReturnToMenu();Check(session.GeometryExperiment is null&&!session.IsMultiscaleGeometry,"C1 is session-only");
    }

    private static void VerifyClassification()
    {
        Check(C1Profile.MainAreaFraction==.03&&C1Profile.AmbiguousAreaFraction==.01&&C1Profile.MainDominance==.60,
            "Initial experimental geography thresholds remain frozen");
        var c=C1GeographicEvaluation.ClassifyAreas(1000,[30,29,10,9,500]);
        Check(c.Main.SequenceEqual(new[]{500,30})&&c.Ambiguous.SequenceEqual(new[]{29,10})&&c.Secondary.SequenceEqual(new[]{9}),"Frozen 3% and 1% boundaries");
        Check(C1GeographicEvaluation.ClassifyAreas(1000,[30,30,30,30,30]).Main.Length==5,"Never select only the four largest masses");
    }

    private static void VerifyNarrowFixture(HexTopology topology,GeneratedWorld a)
    {
        var mask=new byte[topology.Count];
        for(int row=40;row<70;row++)for(int col=20;col<100;col++)if(col<50||col>=70)mask[topology.Id(col,row)]=1;
        for(int col=50;col<70;col++)mask[topology.Id(col,55)]=1;
        int core=topology.Id(20,40);var ids=mask.Select(v=>v==1?0:-1).ToArray();
        var model=new SharedCorticalModel(SharedCorticalModel.CurrentVersion,mask,(int[])ids.Clone(),ids,new float[topology.Count],
            [new(0,core)],[new(0,0,core)],[]);
        var formation=new C1FormationCandidate(0,2,mask.Count(v=>v==1),[],[],Enumerable.Range(0,8).Select(i=>new C1SolverReport(i,true,1,0,0,1,"fixture")).ToArray(),
            new int[8],model,[0],[C1ProvinceRole.Margin],null,"");
        var terrain=a.Landmass with {SurfaceByCellId=mask,SharedCortex=SharedCorticalPersistence.ToState(model)};
        var report=C1GeographicEvaluation.Evaluate(topology,terrain,formation,new(model,[],[]));
        Check(report.NarrowMainComponents==1&&report.Reasons.Any(r=>r.Contains("narrow")),"One-cell isthmus does not silently qualify a compound continent");
    }
    private static bool Close(double a,double b)=>Math.Abs(a-b)<=1e-8*Math.Max(1,Math.Max(Math.Abs(a),Math.Abs(b)));
    private static void ExpectArgument(Action action){try{action();}catch(ArgumentException){return;}throw new InvalidOperationException("Malformed C1 input accepted");}
    private static void Check(bool condition,string message){if(!condition)throw new InvalidOperationException(message);}
}
