// Validates experimental Gaussian radius/cutoff values and checked Grid traversal reach.
// Defines no approved profile and is not used by the published tectonic executor.
using Evolve.Engine.WorldGeneration.Layers.Grid;

namespace Evolve.Engine.WorldGeneration.Layers.Tectonics;

internal readonly record struct TectonicInfluenceParameters
{
    internal float Radius { get; }
    internal float Cutoff { get; }

    internal TectonicInfluenceParameters(float radius, float cutoff)
    {
        RequirePositive(radius, nameof(radius));
        RequirePositive(cutoff, nameof(cutoff));
        Radius = radius;
        Cutoff = cutoff;
    }

    // Validate before clipping, so enormous inputs cannot hide a narrowing overflow.
    internal int GetReach(HexGridData grid)
    {
        ArgumentNullException.ThrowIfNull(grid);
        RequirePositive(Radius, nameof(Radius));
        RequirePositive(Cutoff, nameof(Cutoff));
        double ceiling = Math.Ceiling((double)Radius * Cutoff);
        if (!double.IsFinite(ceiling) || ceiling > int.MaxValue)
            throw new OverflowException("Influence reach cannot be represented as an integer.");
        return Math.Min(grid.CellCount - 1, checked((int)ceiling));
    }

    internal static TectonicInfluenceParameters Resolve(HexGridData grid, float minimumRadius, float fraction, float cutoff)
    {
        ArgumentNullException.ThrowIfNull(grid);
        RequirePositive(minimumRadius, nameof(minimumRadius));
        RequirePositive(fraction, nameof(fraction));
        RequirePositive(cutoff, nameof(cutoff));
        float radius = (float)Math.Max((double)minimumRadius, (double)fraction * Math.Min(grid.Columns, grid.Rows));
        if (!float.IsFinite(radius)) throw new OverflowException("Influence radius cannot be stored as a finite float.");
        var parameters = new TectonicInfluenceParameters(radius, cutoff);
        parameters.GetReach(grid);
        return parameters;
    }

    private static void RequirePositive(float value, string name)
    {
        if (!float.IsFinite(value) || value <= 0) throw new ArgumentOutOfRangeException(name, "Expected a finite positive value.");
    }
}
