using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text.Json;
using GEvolve.Definitions.WorldGeneration.Configuration;
using GEvolve.Engine.World;
using GEvolve.Engine.World.BiomeGeneration;
using GEvolve.Engine.World.LandmassGeneration;
using GEvolve.Engine.World.LandmassGeneration.Shared;
using GEvolve.State.Game;

public static class PangaeaTectonicAuditTests
{
    private sealed record Shape(int SpanProxyCells, double Elongation, int Endpoints, int Cycles,
        int BranchCells, bool Connected);
    private sealed record Segment(int Id, int Pair, int[] Cells, int[] Contacts, Shape Geometry,
        int[] CandidateIds, int[] SelectedIds, int EligibleEdges, int WinnerCells);
    private sealed record Visibility(int AxisCells, double MeanBase, double MeanFinal, double MeanGain,
        double LocalProminence, double? OutsideProminence, int Axis75, int Axis85);
    private sealed class Region
    {
        public int Land, Raised, Base75, Base85, Final75, Final85, New85, High75Unraised, Base75Outside;
        public double BaseSum, FinalSum, GainSum, CompressionSum;
        public HashSet<int> Contacts = [], Candidates = [], AcceptedAxes = [], InfluenceSystems = [];
    }

    public static void Run()
    {
        Console.WriteLine("AUDIT_ENV " + JsonSerializer.Serialize(new
        {
            Utc = DateTime.UtcNow.ToString("O"), Framework = RuntimeInformation.FrameworkDescription,
            OS = RuntimeInformation.OSDescription, Architecture = RuntimeInformation.ProcessArchitecture.ToString(),
            Grid = "Large 300x200 flat-top; E/W wrap; N/S open", Audit = "P03-A.5 observational only"
        }));
        foreach (var (seed, reference) in new[]
        {
            (-714216669, "Favorable"), (-2024691550, "Favorable"),
            (-174327693, "Unfavorable"), (-1979952984, "Unfavorable"), (2076374193, "Unfavorable")
        }) Audit(seed, reference);
        Console.WriteLine("AUDIT_PASS: five required seeds; traced/untraced complete geography identical; trace and persistence consistency verified.");
    }

    private static void Audit(int seed, string reference)
    {
        var configuration = LandmassProfiles.Create(GridSize.Large, LandmassType.Pangaea, seed);
        var service = new WorldGenerationService();
        var timer = Stopwatch.StartNew();
        var control = service.Generate(configuration);
        double controlMs = timer.Elapsed.TotalMilliseconds;
        PangaeaMountainRangeAudit? captured = null;
        timer.Restart();
        var world = service.Generate(configuration, value => captured = value);
        double tracedMs = timer.Elapsed.TotalMilliseconds;
        var audit = captured ?? throw new Exception("Selection trace missing");
        var terrain = world.Landmass;
        var topology = new HexTopology(world.Grid);
        var motion = terrain.TectonicMotion!;
        var ranges = terrain.PangaeaMountainRanges!;
        var fields = world.PangaeaElevations!;
        byte[] initial = fields.BaseByCellId.ToArray(), final = fields.FinalByCellId.ToArray();
        byte[] gain = fields.ContributionByCellId.ToArray();
        string controlHash = Hash(control.Landmass), tracedHash = Hash(terrain);
        Check(controlHash == tracedHash && control.Grid.Definition == world.Grid.Definition &&
            Hash(control.Statistics) == Hash(world.Statistics), "Tracing changed generated geography");
        Check(control.PangaeaElevations!.BaseByCellId.Span.SequenceEqual(fields.BaseByCellId.Span) &&
            control.PangaeaElevations.ContributionByCellId.Span.SequenceEqual(fields.ContributionByCellId.Span),
            "Tracing changed the base/contribution fields");
        Check(audit.Contacts.Length == motion.Interactions.Length && audit.Contacts.Select(c => c.InteractionIndex)
            .SequenceEqual(Enumerable.Range(0, motion.Interactions.Length)), "Every contact must have one actual trace decision");
        var selected = audit.Candidates.Where(c => c.Decision == MountainCandidateDecision.Selected).ToArray();
        Check(selected.Select(c => c.SelectedRangeId).Order().SequenceEqual(ranges.Ranges.Select(r => r.Id)),
            "Selected candidates and persisted systems differ");
        var restored = JsonSerializer.Deserialize<PangaeaMountainRangeState>(JsonSerializer.Serialize(ranges))!;
        Check(Hash(restored) == Hash(ranges), "Range metadata changed in serialization");
        var candidateByCell = new Dictionary<int, MountainCandidateAudit>();
        foreach (var candidate in audit.Candidates)
            foreach (int id in candidate.ContactCellIds)
                Check(candidateByCell.TryAdd(id, candidate), "A candidate cell was assigned to multiple components");
        var eligible = audit.Contacts.Select(c => c.Decision == MountainContactDecision.Eligible).ToArray();
        var bothLand = motion.Interactions.Select(e => terrain.IsLand(e.FirstCellId) && terrain.IsLand(e.SecondCellId)).ToArray();
        var convergent = motion.Interactions.Select((e, i) => bothLand[i] && e.Convergence > 0 &&
            (e.Kind is PlateInteractionKind.Convergent or PlateInteractionKind.Mixed)).ToArray();
        int land = terrain.SurfaceByCellId.Count(value => value == 1);
        var eligibleByCell = new Dictionary<int, List<int>>();
        foreach (var contact in audit.Contacts.Where(c => c.Decision == MountainContactDecision.Eligible))
        {
            var e = motion.Interactions[contact.InteractionIndex];
            foreach (int id in new[] { e.FirstCellId, e.SecondCellId })
            {
                if (!eligibleByCell.TryGetValue(id, out var list)) eligibleByCell[id] = list = [];
                list.Add(contact.InteractionIndex);
            }
        }
        Check(candidateByCell.Keys.Order().SequenceEqual(eligibleByCell.Keys.Order()),
            "Every eligible endpoint must belong to exactly one actual candidate component");
        Check(Enumerable.Range(0, eligible.Length).All(i => !eligible[i] || convergent[i]),
            "Eligible contacts must be classified convergent land contacts");
        var influence = new int[ranges.Ranges.Length + 1];
        var actualRaised = new int[influence.Length];
        var visibilityByRange = MeasureVisibilities(topology, terrain, initial, final, gain);
        for (int id = 0; id < topology.Count; id++)
        {
            influence[ranges.RangeByCellId[id]]++;
            if (gain[id] > 0) actualRaised[ranges.RangeByCellId[id]]++;
        }
        Console.WriteLine($"AUDIT_CASE Large/{seed} reference={reference}");
        foreach (var c in audit.Candidates)
        {
            var shape = MeasureShape(topology, c.ContactCellIds);
            Check(shape.Connected, "Actual candidate component is disconnected");
            var edges = c.ContactCellIds.Where(eligibleByCell.ContainsKey).SelectMany(id => eligibleByCell[id])
                .Distinct().Where(i => Pair(motion.Interactions[i]) == c.FirstPlateId * 16 + c.SecondPlateId).ToArray();
            double mean = edges.Length == 0 ? 0 : edges.Average(i => motion.Interactions[i].Convergence);
            double maximum = edges.Length == 0 ? 0 : edges.Max(i => motion.Interactions[i].Convergence);
            Visibility? visibility = null;
            if (c.Decision == MountainCandidateDecision.Selected)
            {
                var range = ranges.Ranges[c.SelectedRangeId - 1];
                Check(c.FinalAxisCellIds.SequenceEqual(range.AxisCellIds) &&
                    c.WidthByAxisCell.SequenceEqual(range.WidthByAxisCell) && c.AffectedCells == actualRaised[range.Id],
                    "Trace differs from returned mountain geometry or raised area");
                for (int i = 0; i < c.FinalAxisCellIds.Length; i++)
                    Check(terrain.IsLand(c.FinalAxisCellIds[i]) && (i == 0 ||
                        topology.Neighbors(c.FinalAxisCellIds[i - 1]).Contains(c.FinalAxisCellIds[i])),
                        "Selected axis is not a connected land-only hex path");
                visibility = visibilityByRange[range.Id];
            }
            Console.WriteLine("AUDIT_CANDIDATE " + JsonSerializer.Serialize(new
            {
                Seed = seed, Id = c.CandidateId, Plates = $"{c.FirstPlateId}/{c.SecondPlateId}",
                Decision = c.Decision.ToString(), MinimumAxisCells = audit.MinimumAxisCells,
                ContactCells = c.ContactCellIds.Length, SpanProxyCells = shape.SpanProxyCells,
                EvaluatedAxisCells = c.ProposedAxisCellIds.Length == 0 ? (int?)null : c.ProposedAxisCellIds.Length,
                FinalAxisCells = c.FinalAxisCellIds.Length, Elongation = (double?)c.TestedElongation ?? shape.Elongation,
                ElongationTestedBySelection = c.TestedElongation.HasValue,
                shape.Connected, shape.Endpoints, shape.Cycles, shape.BranchCells,
                ContactConvergenceMean = mean, ContactConvergenceMax = maximum,
                WidthMin = c.WidthByAxisCell.Length == 0 ? (int?)null : c.WidthByAxisCell.Min(),
                WidthMean = c.WidthByAxisCell.Length == 0 ? (double?)null : c.WidthByAxisCell.Average(v => (double)v),
                WidthMax = c.WidthByAxisCell.Length == 0 ? (int?)null : c.WidthByAxisCell.Max(),
                c.SelectedRangeId, c.AffectedCells,
                InfluenceCells = c.SelectedRangeId == 0 ? 0 : influence[c.SelectedRangeId],
                Regions = c.ContactCellIds.Select(id => RegionId(topology, id)).Distinct().Order().ToArray(),
                AxisHash = c.FinalAxisCellIds.Length == 0 ? null : Hash(c.FinalAxisCellIds), Visibility = visibility
            }));
        }
        var segments = RawSegments(topology, terrain, convergent, eligible, candidateByCell);
        foreach (var s in segments)
            Console.WriteLine("AUDIT_SEGMENT " + JsonSerializer.Serialize(new
            {
                Seed = seed, s.Id, Plates = $"{s.Pair / 16}/{s.Pair % 16}", ContactEdges = s.Contacts.Length,
                ContactCells = s.Cells.Length, s.Geometry.SpanProxyCells, s.Geometry.Elongation,
                s.Geometry.Connected, s.EligibleEdges, s.WinnerCells, s.CandidateIds, s.SelectedIds,
                MeanConvergence = s.Contacts.Average(i => motion.Interactions[i].Convergence),
                PeakConvergence = s.Contacts.Max(i => motion.Interactions[i].Convergence),
                Regions = s.Cells.Select(id => RegionId(topology, id)).Distinct().Order().ToArray()
            }));
        var regionStats = MeasureRegions(topology, terrain, initial, final, gain, audit, convergent);
        foreach (var region in regionStats) Console.WriteLine("AUDIT_REGION " + JsonSerializer.Serialize(region));
        var axisCells = ranges.Ranges.SelectMany(r => r.AxisCellIds).ToArray();
        var visible = ranges.Ranges.Select(r => visibilityByRange[r.Id]).ToArray();
        double AxisMean(Func<Visibility, double> selector) => axisCells.Length == 0 ? 0 :
            visible.Sum(v => selector(v) * v.AxisCells) / axisCells.Length;
        int[] convIndices = Enumerable.Range(0, convergent.Length).Where(i => convergent[i]).ToArray();
        int internalCandidateEdges = 0, selectedEdges = 0;
        foreach (int i in convIndices.Where(i => eligible[i]))
        {
            var e = motion.Interactions[i];
            if (!candidateByCell.TryGetValue(e.FirstCellId, out var first) ||
                !candidateByCell.TryGetValue(e.SecondCellId, out var second) || first.CandidateId != second.CandidateId ||
                first.FirstPlateId * 16 + first.SecondPlateId != Pair(e)) continue;
            internalCandidateEdges++;
            if (first.Decision == MountainCandidateDecision.Selected) selectedEdges++;
        }
        var longUnselected = segments.Where(s => s.Geometry.SpanProxyCells >= audit.MinimumAxisCells && s.SelectedIds.Length == 0).ToArray();
        var qualified = audit.Candidates.Where(c => c.ProposedAxisCellIds.Length >= audit.MinimumAxisCells).ToArray();
        var slopes = TerrainSlope.Calculate(topology, terrain)!;
        var baseHigh75 = Enumerable.Range(0, topology.Count).Where(id => terrain.IsLand(id) && initial[id] >= 75).ToArray();
        var finalHigh75 = Enumerable.Range(0, topology.Count).Where(id => terrain.IsLand(id) && final[id] >= 75).ToArray();
        var white = Enumerable.Range(0, topology.Count).Where(id => terrain.IsLand(id) && final[id] >= 85).ToArray();
        var convCells = convIndices.SelectMany(i => new[] { motion.Interactions[i].FirstCellId, motion.Interactions[i].SecondCellId }).Distinct().ToArray();
        Console.WriteLine($"AUDIT_COUNTS seed={seed} reference={reference} land={land} convergentLand={convIndices.Length} " +
            $"eligible={eligible.Count(v => v)} candidates={audit.Candidates.Length} accepted={selected.Length} " +
            $"axes={axisCells.Length} influence={topology.Count - influence[0]} meanAxisFinal={AxisMean(v => v.MeanFinal):F3} " +
            $"meanLocalProminence={AxisMean(v => v.LocalProminence):F3}");
        Console.WriteLine("AUDIT_SUMMARY " + JsonSerializer.Serialize(new
        {
            Seed = seed, Reference = reference, LandCells = land, ControlMilliseconds = controlMs, TracedMilliseconds = tracedMs,
            Determinism = new { FullState = controlHash, Surface = Hash(terrain.SurfaceByCellId),
                Plates = Hash(terrain.PlateByCellId!), H04 = Hash(motion), Ranges = Hash(ranges), TracedEqualsControl = true },
            Contacts = new
            {
                Total = motion.Interactions.Length, BothLand = bothLand.Count(v => v),
                PositiveConvergenceLand = motion.Interactions.Where((e, i) => bothLand[i] && e.Convergence > 0).Count(),
                ClassifiedConvergenceLand = convIndices.Length, ConvergenceEdgesPer1000Land = 1000.0 * convIndices.Length / land,
                ConvergentPlates = convIndices.SelectMany(i => new[] { motion.Interactions[i].FirstPlateId, motion.Interactions[i].SecondPlateId }).Distinct().Count(),
                MeanConvergence = convIndices.Length == 0 ? 0 : convIndices.Average(i => motion.Interactions[i].Convergence),
                PeakConvergence = convIndices.Length == 0 ? 0 : convIndices.Max(i => motion.Interactions[i].Convergence),
                MeanCompressionAtConvergentCells = convCells.Length == 0 ? 0 : convCells.Average(id => (double)motion.CompressionByCellId[id]),
                Eligible = eligible.Count(v => v), EligiblePercent = Percent(eligible.Count(v => v), convIndices.Length),
                InternalCandidateEdges = internalCandidateEdges, InternalCandidatePercent = Percent(internalCandidateEdges, convIndices.Length),
                SelectedComponentEdges = selectedEdges, SelectedContactPercent = Percent(selectedEdges, convIndices.Length),
                RawSegments = segments.Length, LongUnselectedSegments = longUnselected.Length,
                LongestUnselectedSpan = longUnselected.Length == 0 ? 0 : longUnselected.Max(s => s.Geometry.SpanProxyCells),
                FirstRejections = audit.Contacts.GroupBy(c => c.Decision.ToString()).ToDictionary(g => g.Key, g => g.Count())
            },
            Selection = new
            {
                Candidates = audit.Candidates.Length, Accepted = selected.Length, Persisted = restored.Ranges.Length,
                Rejected = audit.Candidates.Length - selected.Length, RejectedPercent = Percent(audit.Candidates.Length - selected.Length, audit.Candidates.Length),
                LengthQualified = qualified.Length, CompactRejected = qualified.Count(c => c.Decision != MountainCandidateDecision.Selected),
                CompactRejectedPercent = Percent(qualified.Count(c => c.Decision != MountainCandidateDecision.Selected), qualified.Length),
                Rejections = audit.Candidates.Where(c => c.Decision != MountainCandidateDecision.Selected)
                    .GroupBy(c => c.Decision.ToString()).ToDictionary(g => g.Key, g => g.Count()),
                AxisCells = axisCells.Length, InfluenceCells = topology.Count - influence[0],
                InfluencePercentLand = Percent(topology.Count - influence[0], land), RaisedCells = gain.Count(v => v > 0),
                MeanAxisBase = AxisMean(v => v.MeanBase), MeanAxisFinal = AxisMean(v => v.MeanFinal),
                MeanAxisGain = AxisMean(v => v.MeanGain), MeanLocalProminence = AxisMean(v => v.LocalProminence),
                AxisWhitePercent = Percent(visible.Sum(v => v.Axis85), axisCells.Length),
                MeanCorridorSlope = ranges.RangeByCellId.Count(id => id > 0) == 0 ? 0 :
                    Enumerable.Range(0, topology.Count).Where(id => ranges.RangeByCellId[id] > 0).Average(id => slopes[id])
            },
            Relief = new
            {
                MeanBase = terrain.SurfaceByCellId.Select((s, i) => s == 1 ? (int?)initial[i] : null).Where(v => v.HasValue).Average(),
                MeanFinal = terrain.SurfaceByCellId.Select((s, i) => s == 1 ? (int?)final[i] : null).Where(v => v.HasValue).Average(),
                BaseHigh75 = baseHigh75.Length, FinalHigh75 = finalHigh75.Length,
                InheritedHigh75Percent = Percent(baseHigh75.Length, finalHigh75.Length),
                BaseHigh75OutsideRanges = baseHigh75.Count(id => ranges.RangeByCellId[id] == 0),
                FinalHigh75WithoutRise = finalHigh75.Count(id => gain[id] == 0),
                BaseWhite85 = initial.Count(v => v >= 85), FinalWhite85 = white.Length,
                NewWhite85 = white.Count(id => initial[id] < 85),
                InheritedWhitePercent = Percent(white.Count(id => initial[id] >= 85), white.Length)
            },
            Regions = regionStats
        }));
    }

    private static double? Percent(int count, int total) => total == 0 ? null : count * 100.0 / total;
    private static int Pair(PlateInteraction edge) => Math.Min(edge.FirstPlateId, edge.SecondPlateId) * 16 + Math.Max(edge.FirstPlateId, edge.SecondPlateId);
    private static int RegionId(HexTopology topology, int id) => (id / topology.Columns * 3 / topology.Rows) * 3 + id % topology.Columns * 3 / topology.Columns;
    private static string Hash<T>(T value) => Convert.ToHexString(SHA256.HashData(JsonSerializer.SerializeToUtf8Bytes(value)));
    private static string Hash(byte[] value) => Convert.ToHexString(SHA256.HashData(value));
    private static void Check(bool value, string reason) { if (!value) throw new InvalidOperationException(reason); }

    private static Shape MeasureShape(HexTopology topology, int[] ids)
    {
        var cells = ids.ToHashSet();
        int origin = ids.Min(), endpoints = 0, branches = 0, edges = 0;
        double sx = 0, sy = 0, xx = 0, yy = 0, xy = 0;
        foreach (int id in ids)
        {
            int degree = 0;
            foreach (int next in topology.Neighbors(id)) if (cells.Contains(next)) degree++;
            if (degree <= 1) endpoints++; if (degree >= 4) branches++; edges += degree;
            int dx = id % topology.Columns - origin % topology.Columns;
            if (dx > topology.Columns / 2) dx -= topology.Columns;
            if (dx < -topology.Columns / 2) dx += topology.Columns;
            double x = 1.5 * dx, y = Math.Sqrt(3) * (id / topology.Columns + .5 * (id % topology.Columns & 1));
            sx += x; sy += y; xx += x * x; yy += y * y; xy += x * y;
        }
        xx = xx / ids.Length - Math.Pow(sx / ids.Length, 2);
        yy = yy / ids.Length - Math.Pow(sy / ids.Length, 2);
        xy = xy / ids.Length - sx * sy / (ids.Length * (double)ids.Length);
        double trace = xx + yy, discriminant = Math.Sqrt((xx - yy) * (xx - yy) + 4 * xy * xy);
        var first = Sweep(origin); var second = Sweep(first.Cell);
        return new(second.Distance + 1, Math.Sqrt((trace + discriminant + 2) / Math.Max(1, trace - discriminant + 2)),
            endpoints, Math.Max(0, edges / 2 - ids.Length + 1), branches, first.Visited == ids.Length);
        (int Cell, int Distance, int Visited) Sweep(int start)
        {
            var distances = new Dictionary<int, int> { [start] = 0 };
            var queue = new Queue<int>(); queue.Enqueue(start);
            int farthest = start;
            while (queue.TryDequeue(out int id))
            {
                if (distances[id] > distances[farthest] || distances[id] == distances[farthest] && id < farthest) farthest = id;
                foreach (int next in topology.Neighbors(id))
                    if (cells.Contains(next) && distances.TryAdd(next, distances[id] + 1)) queue.Enqueue(next);
            }
            return (farthest, distances[farthest], distances.Count);
        }
    }

    private static Segment[] RawSegments(HexTopology topology, LandmassState terrain, bool[] convergent,
        bool[] eligible, Dictionary<int, MountainCandidateAudit> candidateByCell)
    {
        var result = new List<Segment>();
        var interactions = terrain.TectonicMotion!.Interactions;
        foreach (var group in Enumerable.Range(0, interactions.Length).Where(i => convergent[i]).GroupBy(i => Pair(interactions[i])).OrderBy(g => g.Key))
        {
            var byCell = new Dictionary<int, List<int>>();
            foreach (int i in group)
                foreach (int id in new[] { interactions[i].FirstCellId, interactions[i].SecondCellId })
                { if (!byCell.TryGetValue(id, out var list)) byCell[id] = list = []; list.Add(i); }
            var visited = new HashSet<int>();
            foreach (int start in byCell.Keys.Order())
            {
                if (!visited.Add(start)) continue;
                var queue = new Queue<int>(); queue.Enqueue(start);
                var cells = new List<int>(); var contacts = new HashSet<int>();
                while (queue.TryDequeue(out int id))
                {
                    cells.Add(id); foreach (int i in byCell[id]) contacts.Add(i);
                    foreach (int next in topology.Neighbors(id))
                        if (byCell.ContainsKey(next) && visited.Add(next)) queue.Enqueue(next);
                }
                var candidateIds = cells.Where(candidateByCell.ContainsKey).Select(id => candidateByCell[id])
                    .Where(c => c.FirstPlateId * 16 + c.SecondPlateId == group.Key).DistinctBy(c => c.CandidateId).ToArray();
                var eligibleCells = contacts.Where(i => eligible[i]).SelectMany(i => new[] { interactions[i].FirstCellId, interactions[i].SecondCellId }).Distinct();
                int winners = eligibleCells.Count(id => candidateByCell.TryGetValue(id, out var c) && c.FirstPlateId * 16 + c.SecondPlateId == group.Key);
                result.Add(new(result.Count + 1, group.Key, cells.ToArray(), contacts.Order().ToArray(), MeasureShape(topology, cells.ToArray()),
                    candidateIds.Select(c => c.CandidateId).Order().ToArray(), candidateIds.Where(c => c.SelectedRangeId > 0).Select(c => c.SelectedRangeId).Order().ToArray(),
                    contacts.Count(i => eligible[i]), winners));
            }
        }
        return result.ToArray();
    }

    private static Dictionary<int, Visibility> MeasureVisibilities(HexTopology topology, LandmassState terrain,
        byte[] initial, byte[] final, byte[] gain)
    {
        var state = terrain.PangaeaMountainRanges!;
        var owner = state.RangeByCellId;
        var outside = new double[state.Ranges.Length + 1];
        var outsideLinks = new int[outside.Length];
        var result = new Dictionary<int, Visibility>();
        for (int id = 0; id < topology.Count; id++)
            if (owner[id] != 0)
                foreach (int next in topology.Neighbors(id))
                    if (terrain.IsLand(next) && owner[next] != owner[id])
                    { outside[owner[id]] += final[next]; outsideLinks[owner[id]]++; }
        foreach (var range in state.Ranges) result.Add(range.Id, Measure(range));
        return result;

        Visibility Measure(PangaeaMountainRange range)
        {
            double baseSum = 0, finalSum = 0, gainSum = 0, local = 0;
            int above75 = 0, white = 0;
            foreach (int id in range.AxisCellIds)
            {
                baseSum += initial[id]; finalSum += final[id]; gainSum += gain[id];
                if (final[id] >= 75) above75++; if (final[id] >= 85) white++;
                int sum = 0, count = 0;
                foreach (int next in topology.Neighbors(id)) if (terrain.IsLand(next)) { sum += final[next]; count++; }
                if (count > 0) local += final[id] - (double)sum / count;
            }
            int length = range.AxisCellIds.Length;
            return new(length, baseSum / length, finalSum / length, gainSum / length, local / length,
                outsideLinks[range.Id] == 0 ? null : finalSum / length - outside[range.Id] / outsideLinks[range.Id], above75, white);
        }
    }

    private static object[] MeasureRegions(HexTopology topology, LandmassState terrain, byte[] initial, byte[] final,
        byte[] gain, PangaeaMountainRangeAudit audit, bool[] convergent)
    {
        var regions = Enumerable.Range(0, 9).Select(_ => new Region()).ToArray();
        var owner = terrain.PangaeaMountainRanges!.RangeByCellId;
        for (int id = 0; id < topology.Count; id++)
        {
            if (!terrain.IsLand(id)) continue;
            var r = regions[RegionId(topology, id)]; r.Land++;
            r.BaseSum += initial[id]; r.FinalSum += final[id]; r.GainSum += gain[id];
            r.CompressionSum += terrain.TectonicMotion!.CompressionByCellId[id];
            if (gain[id] > 0) r.Raised++;
            if (initial[id] >= 75) { r.Base75++; if (owner[id] == 0) r.Base75Outside++; }
            if (initial[id] >= 85) r.Base85++;
            if (final[id] >= 75) { r.Final75++; if (gain[id] == 0) r.High75Unraised++; }
            if (final[id] >= 85) { r.Final85++; if (initial[id] < 85) r.New85++; }
            if (owner[id] > 0) r.InfluenceSystems.Add(owner[id]);
        }
        for (int i = 0; i < convergent.Length; i++)
            if (convergent[i])
                foreach (int r in new[] { RegionId(topology, terrain.TectonicMotion!.Interactions[i].FirstCellId),
                    RegionId(topology, terrain.TectonicMotion!.Interactions[i].SecondCellId) }.Distinct()) regions[r].Contacts.Add(i);
        foreach (var c in audit.Candidates)
        {
            foreach (int r in c.ContactCellIds.Select(id => RegionId(topology, id)).Distinct()) regions[r].Candidates.Add(c.CandidateId);
            foreach (int r in c.FinalAxisCellIds.Select(id => RegionId(topology, id)).Distinct()) regions[r].AcceptedAxes.Add(c.SelectedRangeId);
        }
        return regions.Select((r, id) => (object)new
        {
            Region = id, LatitudeThird = id / 3, LongitudeThird = id % 3, LandCells = r.Land,
            ConvergentContacts = r.Contacts.Count, ConvergentPer1000Land = r.Land == 0 ? (double?)null : 1000.0 * r.Contacts.Count / r.Land,
            Candidates = r.Candidates.Count, AcceptedAxes = r.AcceptedAxes.Count, InfluenceSystems = r.InfluenceSystems.Count,
            CandidatePer1000Land = r.Land == 0 ? (double?)null : 1000.0 * r.Candidates.Count / r.Land,
            AcceptedPer1000Land = r.Land == 0 ? (double?)null : 1000.0 * r.AcceptedAxes.Count / r.Land,
            RaisedCells = r.Raised, RaisedPercentLand = Percent(r.Raised, r.Land),
            MeanBase = r.Land == 0 ? (double?)null : r.BaseSum / r.Land,
            MeanFinal = r.Land == 0 ? (double?)null : r.FinalSum / r.Land,
            MeanGain = r.Land == 0 ? (double?)null : r.GainSum / r.Land,
            MeanCompression = r.Land == 0 ? (double?)null : r.CompressionSum / r.Land,
            r.Base75, r.Base85, r.Final75, r.Final85, r.New85, r.High75Unraised, r.Base75Outside,
            High75UnraisedPercentLand = Percent(r.High75Unraised, r.Land), Base75OutsidePercentLand = Percent(r.Base75Outside, r.Land)
        }).ToArray();
    }
}
