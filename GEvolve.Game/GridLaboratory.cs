using System.Globalization;
using System.Security.Cryptography;
using GEvolve.Definitions.WorldGeneration.Configuration;
using GEvolve.Engine.Grid;

namespace GEvolve.Game;

/// <summary>Temporary spatial data only. No terrain, world configuration or save state.</summary>
public sealed class GridLaboratory
{
    public GridSize Size { get; private set; } = GridSize.Small;
    public int Seed { get; private set; } = 2026;
    public string SeedText { get; private set; } = "2026";
    public string? Error { get; private set; }
    public HexGrid Grid { get; private set; } = HexGrid.Generate(HexGridDefinitions.Get(GridSize.Small));

    public void ChooseSize(GridSize size)
    {
        var definition = HexGridDefinitions.Get(size);
        if (size == Size) return;
        var grid = HexGrid.Generate(definition);
        Grid = grid; Size = size;
    }

    // The seed is session metadata. Empty geometry is deliberately independent of it.
    public void SetSeedText(string text)
    {
        if (text.Length > 11 || text.Any(c => !char.IsAsciiDigit(c) && c != '-') ||
            text.Count(c => c == '-') > 1 || text.IndexOf('-') > 0) return;
        if (text.Length > 0 && text != "-" &&
            !int.TryParse(text, NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out _)) return;
        SeedText = text;
        if (int.TryParse(text, NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out int seed))
        { Seed = seed; Error = null; }
        else Error = "COMPLETA UNA SEMILLA INT32";
    }

    public void RandomizeSeed()
    {
        int next;
        do { next = BitConverter.ToInt32(RandomNumberGenerator.GetBytes(sizeof(int))); } while (next == Seed);
        SetSeedText(next.ToString(CultureInfo.InvariantCulture));
    }
}
