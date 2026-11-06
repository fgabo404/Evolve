using GEvolve.Definitions.WorldGeneration.Configuration;
using GEvolve.Engine.World.LandmassGeneration.Shared;
using GEvolve.State.Game;

namespace GEvolve.Engine.World.LandmassGeneration;

/// <summary>Builds land-only mountain axes from connected, convergent H04 contacts.</summary>
public sealed class PangaeaMountainRangeGenerator
{
    public const string Version = "pangaea-ranges-2";

    public LandmassState Apply(HexTopology topology, LandmassState terrain)
        => Apply(topology, terrain, null);

    public LandmassState Apply(HexTopology topology, LandmassState terrain,
        Action<PangaeaElevationDiagnostics>? captureElevations)
        => Apply(topology, terrain, captureElevations, default);

    public LandmassState Apply(HexTopology topology, LandmassState terrain,
        Action<PangaeaElevationDiagnostics>? captureElevations, ReadOnlyMemory<byte> previousBase)
        => Apply(topology, terrain, captureElevations, previousBase, default);

    public LandmassState Apply(HexTopology topology, LandmassState terrain,
        Action<PangaeaElevationDiagnostics>? captureElevations, ReadOnlyMemory<byte> previousBase,
        ReadOnlyMemory<byte> priorCalibrationBase)
        => Apply(topology, terrain, captureElevations, previousBase, priorCalibrationBase, null);

    public LandmassState Apply(HexTopology topology, LandmassState terrain,
        Action<PangaeaElevationDiagnostics>? captureElevations, ReadOnlyMemory<byte> previousBase,
        ReadOnlyMemory<byte> priorCalibrationBase, Action<PangaeaMountainRangeAudit>? captureAudit)
    {
        ArgumentNullException.ThrowIfNull(topology);
        ArgumentNullException.ThrowIfNull(terrain);
        if (terrain.Configuration.Type != LandmassType.Pangaea || !terrain.HasSurface ||
            terrain.ElevationByCellId is not { } original ||
            terrain.TectonicMotion is not { } motion || terrain.PlateByCellId is not { } plates ||
            original.Length != topology.Count || plates.Length != topology.Count ||
            terrain.SurfaceByCellId.Length != topology.Count)
            throw new ArgumentException("Complete Pangaea terrain and official H04 motion are required.", nameof(terrain));

        int count = topology.Count;
        List<MountainContactAudit>? contactAudit = captureAudit is null ? null : [];
        List<MountainCandidateAudit>? candidateAudit = captureAudit is null ? null : [];
        var strength = new float[count];
        var pair = new int[count];
        Array.Fill(pair, -1);
        // Require an emergent contact on both sides, appreciable convergence,
        // and convergence at least comparable to tangential shear.
        int interactionIndex = -1;
        foreach (var edge in motion.Interactions)
        {
            interactionIndex++;
            if (edge.Kind is not (PlateInteractionKind.Convergent or PlateInteractionKind.Mixed) ||
                edge.Convergence < 1 || edge.Convergence < edge.Shear * .65 ||
                terrain.SurfaceByCellId[edge.FirstCellId] != 1 ||
                terrain.SurfaceByCellId[edge.SecondCellId] != 1)
            {
                // Reasons describe the existing first-failed predicate; they do not select contacts.
                contactAudit?.Add(new(interactionIndex,
                    edge.Kind is not (PlateInteractionKind.Convergent or PlateInteractionKind.Mixed)
                        ? MountainContactDecision.KindNotConvergent
                        : edge.Convergence < 1 ? MountainContactDecision.ConvergenceBelowOne
                        : edge.Convergence < edge.Shear * .65 ? MountainContactDecision.ShearDominates
                        : MountainContactDecision.NotBothLand));
                continue;
            }
            contactAudit?.Add(new(interactionIndex, MountainContactDecision.Eligible));
            int key = Math.Min(edge.FirstPlateId, edge.SecondPlateId) * 16 +
                Math.Max(edge.FirstPlateId, edge.SecondPlateId);
            foreach (int id in new[] { edge.FirstCellId, edge.SecondCellId })
                if (edge.Convergence > strength[id] ||
                    edge.Convergence == strength[id] && key < pair[id])
                {
                    strength[id] = (float)edge.Convergence;
                    pair[id] = key;
                }
        }

        var visited = new bool[count];
        var stamp = new int[count];
        var distance = new int[count];
        var parent = new int[count];
        int search = 0;
        var queue = new Queue<int>();
        var ranges = new List<PangaeaMountainRange>();
        var longitudinal = new Dictionary<int, double[]>();
        var usedAxis = new bool[count];
        int minimumLength = Math.Max(7, topology.Rows / 20);
        for (int start = 0; start < count; start++)
        {
            if (pair[start] < 0 || visited[start]) continue;
            var component = new List<int>();
            visited[start] = true;
            queue.Enqueue(start);
            while (queue.TryDequeue(out int id))
            {
                component.Add(id);
                foreach (int neighbor in topology.Neighbors(id))
                    if (!visited[neighbor] && pair[neighbor] == pair[start])
                    {
                        visited[neighbor] = true;
                        queue.Enqueue(neighbor);
                    }
            }
            if (component.Count < minimumLength)
            {
                RecordCandidate(component, null, null, null, MountainCandidateDecision.ComponentBelowMinimum);
                continue;
            }
            // Two graph sweeps find a long, naturally bending axis. Each step is
            // a real hex edge, including edges across the horizontal seam.
            int endA = Farthest(component.Min(), pair[start]);
            int endB = Farthest(endA, pair[start]);
            var axis = new List<int>();
            for (int id = endB; id != -1; id = parent[id]) axis.Add(id);
            axis.Reverse();
            if (axis.Count < minimumLength)
            {
                RecordCandidate(component, axis, null, null, MountainCandidateDecision.AxisBelowMinimum);
                continue;
            }
            int[]? proposedAuditAxis = candidateAudit is null ? null : axis.ToArray();
            int endpoints = 0, branches = 0, edgeCount = 0;
            double meanX = 0, meanY = 0, xx = 0, yy = 0, xy = 0;
            int originX = start % topology.Columns;
            foreach (int id in component)
            {
                int degree = 0;
                foreach (int neighbor in topology.Neighbors(id))
                    if (pair[neighbor] == pair[start]) degree++;
                if (degree <= 1) endpoints++;
                if (degree >= 4) branches++;
                edgeCount += degree;
                int x = id % topology.Columns - originX;
                if (x > topology.Columns / 2) x -= topology.Columns;
                if (x < -topology.Columns / 2) x += topology.Columns;
                double px = x * 1.5, py = id / topology.Columns * Math.Sqrt(3);
                meanX += px; meanY += py;
                xx += px * px; yy += py * py; xy += px * py;
            }
            meanX /= component.Count; meanY /= component.Count;
            xx = xx / component.Count - meanX * meanX;
            yy = yy / component.Count - meanY * meanY;
            xy = xy / component.Count - meanX * meanY;
            double trace = xx + yy, discriminant = Math.Sqrt((xx - yy) * (xx - yy) + 4 * xy * xy);
            float elongation = (float)Math.Sqrt((trace + discriminant + 2) /
                Math.Max(1, trace - discriminant + 2));
            int cycles = Math.Max(0, edgeCount / 2 - component.Count + 1);
            // Large open arcs survive even if their covariance is nearly round.
            bool compact = axis.Count < topology.Rows * .12 && elongation < 2 ||
                axis.Count < topology.Rows * .20 && elongation < 1.35 && endpoints <= 2;
            if (compact)
            {
                RecordCandidate(component, axis, null, null,
                    axis.Count < topology.Rows * .12 && elongation < 2
                        ? MountainCandidateDecision.CompactShort : MountainCandidateDecision.CompactRoundFewEndpoints,
                    elongation, endpoints, cycles, branches);
                continue;
            }

            // Shift selected interior axis cells at most one hex into the nearby
            // compression field. Keep each replacement adjacent to both neighbors.
            var occupied = new HashSet<int>(axis);
            for (int i = 1; i < axis.Count - 1; i++)
            {
                int old = axis[i], replacement = old;
                foreach (int candidate in topology.Neighbors(axis[i - 1]))
                    if (candidate != old && !occupied.Contains(candidate) && !usedAxis[candidate] &&
                        topology.Neighbors(candidate).Contains(axis[i + 1]) &&
                        terrain.SurfaceByCellId[candidate] == 1 &&
                        motion.DistanceToBoundaryByCellId[candidate] is > 0 and <= 2 &&
                        motion.DistanceToBoundaryByCellId[candidate] >
                            motion.DistanceToBoundaryByCellId[old] &&
                        motion.CompressionByCellId[candidate] >= strength[old] * .5f)
                    { replacement = candidate; break; }
                if (replacement == old) continue;
                occupied.Remove(old); occupied.Add(replacement);
                strength[replacement] = Math.Max(strength[replacement], strength[old]);
                axis[i] = replacement;
            }
            int baseWidth = Math.Max(3, (int)Math.Round(topology.Rows * .025));
            var widths = new byte[axis.Count];
            float peak = 0, mean = 0;
            int span = Math.Max(6, axis.Count / 6);
            var random = new DeterministicRandom(terrain.Configuration.Seed,
                unchecked((uint)(0x52414E47 + component.Min())));
            int anchorCount = axis.Count / span + 2;
            var widthAnchors = new double[anchorCount];
            var heightAnchors = new double[anchorCount];
            for (int a = 0; a < anchorCount; a++)
            {
                widthAnchors[a] = .65 + random.NextDouble() * .85;
                heightAnchors[a] = .35 + random.NextDouble() * .65;
            }
            var profile = new double[axis.Count];
            for (int i = 0; i < axis.Count; i++)
            {
                peak = Math.Max(peak, strength[axis[i]]);
                mean += strength[axis[i]];
                int section = i / span;
                double t = (double)(i % span) / span;
                t = t * t * (3 - 2 * t);
                double widthFactor = widthAnchors[section] * (1 - t) + widthAnchors[section + 1] * t;
                profile[i] = heightAnchors[section] * (1 - t) + heightAnchors[section + 1] * t;
                widths[i] = (byte)Math.Clamp((int)Math.Round(baseWidth * widthFactor), 2, 12);
            }
            int key = pair[start];
            var mountain = new PangaeaMountainRange(0, axis.ToArray(), widths, peak,
                (byte)(key / 16), (byte)(key % 16), mean / axis.Count,
                elongation, endpoints, cycles, branches);
            ranges.Add(mountain);
            RecordCandidate(component, proposedAuditAxis, axis, widths, MountainCandidateDecision.Selected,
                elongation, endpoints, cycles, branches);
            longitudinal[axis[0]] = profile;
            foreach (int id in axis) usedAxis[id] = true;
        }

        ranges.Sort((a, b) => a.AxisCellIds.Min().CompareTo(b.AxisCellIds.Min()));
        var owner = new ushort[count];
        var best = new double[count];
        var final = (byte[])original.Clone();
        var localBest = new double[count];
        var touched = new List<int>();
        for (int index = 0; index < ranges.Count; index++)
        {
            var range = ranges[index] with { Id = index + 1 };
            ranges[index] = range;
            var frontier = new PriorityQueue<(int Cell, int Axis, int Distance, double Uplift), double>();
            var profile = longitudinal[range.AxisCellIds[0]];
            double scale = range.AxisCellIds.Length >= topology.Rows * .20 ? 1 : .65;
            for (int i = 0; i < range.AxisCellIds.Length; i++)
            {
                int cell = range.AxisCellIds[i];
                double uplift = Uplift(i, 0);
                if (uplift <= localBest[cell]) continue;
                if (localBest[cell] == 0) touched.Add(cell);
                localBest[cell] = uplift;
                frontier.Enqueue((cell, i, 0, uplift), -uplift);
            }
            while (frontier.TryDequeue(out var step, out _))
            {
                if (step.Uplift < localBest[step.Cell]) continue;
                if (step.Uplift > best[step.Cell])
                {
                    best[step.Cell] = step.Uplift;
                    owner[step.Cell] = checked((ushort)range.Id);
                    // Existing high terrain is preserved; new uplift cannot create
                    // broad saturated plateaus at the top of the 1..100 scale.
                    final[step.Cell] = ComposeElevation(original[step.Cell], step.Uplift);
                }
                if (step.Distance >= range.WidthByAxisCell[step.Axis]) continue;
                foreach (int neighbor in topology.Neighbors(step.Cell))
                    if (terrain.SurfaceByCellId[neighbor] == 1)
                    {
                        double next = Uplift(step.Axis, step.Distance + 1);
                        if (next <= localBest[neighbor]) continue;
                        if (localBest[neighbor] == 0) touched.Add(neighbor);
                        localBest[neighbor] = next;
                        frontier.Enqueue((neighbor, step.Axis, step.Distance + 1, next), -next);
                    }
            }
            foreach (int id in touched) localBest[id] = 0;
            touched.Clear();

            double Uplift(int axisIndex, int steps)
            {
                double width = range.WidthByAxisCell[axisIndex];
                double taper = Math.Pow(1 - steps / (width + 1), 2);
                double intensity = Math.Clamp(strength[range.AxisCellIds[axisIndex]] / 4, .35, 1);
                return 25 * scale * intensity * profile[axisIndex] * taper;
            }
        }
        // Axes have distinct eligible cells. Keep their own identity even where
        // another system's wider flank has the greater height contribution.
        foreach (var range in ranges)
            foreach (int id in range.AxisCellIds)
                owner[id] = checked((ushort)range.Id);
        var affected = new int[ranges.Count];
        for (int id = 0; id < count; id++)
            if (owner[id] != 0 && final[id] > original[id]) affected[owner[id] - 1]++;
        for (int i = 0; i < ranges.Count; i++)
            ranges[i] = ranges[i] with { AffectedCells = affected[i] };
        captureElevations?.Invoke(new PangaeaElevationDiagnostics(original, final, best, previousBase, priorCalibrationBase));
        if (captureAudit is not null)
        {
            var byStart = ranges.ToDictionary(range => range.AxisCellIds[0]);
            var candidates = candidateAudit!.Select(candidate =>
            {
                if (candidate.Decision != MountainCandidateDecision.Selected) return candidate;
                var selected = byStart[candidate.FinalAxisCellIds[0]];
                return candidate with { SelectedRangeId = selected.Id, AffectedCells = selected.AffectedCells };
            }).ToArray();
            captureAudit(new(contactAudit!.ToArray(), candidates, minimumLength));
        }
        return terrain with { ElevationByCellId = final,
            PangaeaMountainRanges = new(Version, ranges.ToArray(), owner) };

        int Farthest(int source, int key)
        {
            int currentStamp = ++search, farthest = source;
            queue.Enqueue(source);
            stamp[source] = currentStamp;
            distance[source] = 0;
            parent[source] = -1;
            while (queue.TryDequeue(out int id))
            {
                if (distance[id] > distance[farthest] ||
                    distance[id] == distance[farthest] && id < farthest)
                    farthest = id;
                foreach (int neighbor in topology.Neighbors(id))
                    if (pair[neighbor] == key && stamp[neighbor] != currentStamp)
                    {
                        stamp[neighbor] = currentStamp;
                        distance[neighbor] = distance[id] + 1;
                        parent[neighbor] = id;
                        queue.Enqueue(neighbor);
                    }
            }
            return farthest;
        }

        void RecordCandidate(List<int> cells, IEnumerable<int>? proposed, IEnumerable<int>? selected,
            byte[]? widths, MountainCandidateDecision decision, float? elongation = null,
            int? endpoints = null, int? cycles = null, int? branches = null)
        {
            if (candidateAudit is null) return;
            int key = pair[cells[0]];
            candidateAudit.Add(new(candidateAudit.Count + 1, (byte)(key / 16), (byte)(key % 16),
                cells.ToArray(), proposed?.ToArray() ?? [], selected?.ToArray() ?? [],
                widths?.ToArray() ?? [], decision, elongation, endpoints, cycles, branches));
        }
    }

    internal static byte ComposeElevation(byte original, double uplift) =>
        (byte)Math.Max(original, Math.Min(94, (int)Math.Round(original + uplift)));
}
