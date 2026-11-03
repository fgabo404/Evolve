using GEvolve.Definitions.WorldGeneration.Configuration;

namespace GEvolve.State.Game;

public readonly record struct CorticalFragmentState(int Id, int CoreCellId);
public readonly record struct CorticalProvinceState(int Id, int FragmentId, int CoreCellId);
public enum CorticalAssemblyStateKind : byte { Accreted, RiftSeparated }
public readonly record struct CorticalAssemblyStateLink(int FirstFragmentId, int SecondFragmentId,
    CorticalAssemblyStateKind Kind);

/// <summary>Optional lossless cortical save DTO; never inferred from legacy ContinentalGeologyState.</summary>
public sealed record SharedCorticalState(string Version, byte[] CrustByCellId,
    int[] FragmentByCellId, int[] ProvinceByCellId, float[] EmergenceAffinityByCellId,
    CorticalFragmentState[] Fragments, CorticalProvinceState[] Provinces,
    CorticalAssemblyStateLink[] AssemblyLinks)
{
    public const string CurrentVersion = "shared-cortex-1";
}

public enum WorldGenerationStage { TectonicsOnly, CortexAvailable, SurfaceAvailable, Ready }

/// <summary>Validates persisted cortex using the official odd-q hex neighbors with E/W wrapping only.</summary>
public static class SharedCorticalStateValidator
{
    public static void Validate(HexGridDefinition grid, SharedCorticalState state, byte[] plates)
    {
        ArgumentNullException.ThrowIfNull(grid);
        ArgumentNullException.ThrowIfNull(state);
        int count = checked(grid.Columns * grid.Rows);
        if (grid != HexGridDefinitions.Get(grid.Size) || state.Version != SharedCorticalState.CurrentVersion ||
            state.CrustByCellId is null || state.CrustByCellId.Length != count ||
            state.FragmentByCellId is null || state.FragmentByCellId.Length != count ||
            state.ProvinceByCellId is null || state.ProvinceByCellId.Length != count ||
            state.EmergenceAffinityByCellId is null || state.EmergenceAffinityByCellId.Length != count ||
            state.Fragments is null || state.Provinces is null || state.AssemblyLinks is null ||
            plates is null || plates.Length != count)
            throw new InvalidDataException("Invalid cortical version, topology or array dimensions.");

        var fragmentCounts = new int[state.Fragments.Length];
        var provinceCounts = new int[state.Provinces.Length];
        for (int id = 0; id < count; id++)
        {
            byte crust = state.CrustByCellId[id];
            int fragment = state.FragmentByCellId[id], province = state.ProvinceByCellId[id];
            if (plates[id] >= 16 || crust > 1 || !float.IsFinite(state.EmergenceAffinityByCellId[id]))
                throw new InvalidDataException("Invalid cortical cell value.");
            if (crust == 0)
            {
                if (fragment != -1 || province != -1)
                    throw new InvalidDataException("Oceanic crust references a continental identity.");
                continue;
            }
            if ((uint)fragment >= (uint)fragmentCounts.Length ||
                (uint)province >= (uint)provinceCounts.Length ||
                state.Provinces[province].FragmentId != fragment)
                throw new InvalidDataException("Orphan cortical reference.");
            fragmentCounts[fragment]++;
            provinceCounts[province]++;
        }
        for (int id = 0; id < fragmentCounts.Length; id++)
            if (state.Fragments[id].Id != id || fragmentCounts[id] == 0 ||
                (uint)state.Fragments[id].CoreCellId >= (uint)count ||
                state.FragmentByCellId[state.Fragments[id].CoreCellId] != id)
                throw new InvalidDataException("Invalid cortical fragment or core.");
        for (int id = 0; id < provinceCounts.Length; id++)
            if (state.Provinces[id].Id != id || provinceCounts[id] == 0 ||
                (uint)state.Provinces[id].FragmentId >= (uint)fragmentCounts.Length ||
                (uint)state.Provinces[id].CoreCellId >= (uint)count ||
                state.ProvinceByCellId[state.Provinces[id].CoreCellId] != id ||
                state.FragmentByCellId[state.Provinces[id].CoreCellId] != state.Provinces[id].FragmentId)
                throw new InvalidDataException("Invalid cortical province or core.");

        var visited = new int[count];
        var queue = new int[count];
        for (int province = 0; province < provinceCounts.Length; province++)
        {
            int stamp = province + 1, head = 0, tail = 1;
            int core = state.Provinces[province].CoreCellId;
            queue[0] = core; visited[core] = stamp;
            while (head < tail)
            {
                int cell = queue[head++];
                int x = cell % grid.Columns, y = cell / grid.Columns;
                int shift = (x & 1) == 0 ? -1 : 0;
                Visit(x, y - 1);
                Visit(x, y + 1);
                Visit(x - 1, y + shift);
                Visit(x - 1, y + shift + 1);
                Visit(x + 1, y + shift);
                Visit(x + 1, y + shift + 1);
            }
            if (tail != provinceCounts[province])
                throw new InvalidDataException("Disconnected cortical province.");

            void Visit(int column, int row)
            {
                if ((uint)row >= (uint)grid.Rows) return;
                if (column < 0) column += grid.Columns;
                else if (column >= grid.Columns) column -= grid.Columns;
                int neighbor = row * grid.Columns + column;
                if (visited[neighbor] == stamp || state.ProvinceByCellId[neighbor] != province) return;
                visited[neighbor] = stamp;
                queue[tail++] = neighbor;
            }
        }

        var pairs = new HashSet<(int, int)>();
        foreach (var link in state.AssemblyLinks)
            if ((uint)link.FirstFragmentId >= (uint)fragmentCounts.Length ||
                (uint)link.SecondFragmentId >= (uint)fragmentCounts.Length ||
                link.FirstFragmentId >= link.SecondFragmentId ||
                !Enum.IsDefined(link.Kind) || !pairs.Add((link.FirstFragmentId, link.SecondFragmentId)))
                throw new InvalidDataException("Invalid cortical assembly link.");
    }
}
