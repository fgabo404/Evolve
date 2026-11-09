using System.Globalization;
using System.Security.Cryptography;
using GEvolve.Definitions.WorldGeneration.Configuration;

namespace GEvolve.Game;

/// <summary>Menu form metadata only. No grid, generation service, world configuration or persisted state.</summary>
public sealed class WorldCreationForm
{
    public GridSize Size { get; private set; } = GridSize.Small;
    public LandmassType Type { get; private set; } = LandmassType.Pangaea;
    public string SeedText { get; private set; } = "2026";
    public bool CanGenerate => false;
    public void ChooseSize(GridSize size) { _ = HexGridDefinitions.Get(size); Size = size; }
    public void ChooseType(LandmassType type)
    {
        if (!SelectableWorldTypes.IsSelectable(type)) throw new ArgumentOutOfRangeException(nameof(type));
        Type = type;
    }
    public void SetSeedText(string text)
    {
        if (text.Length > 11 || text.Any(c => !char.IsAsciiDigit(c) && c != '-') ||
            text.Count(c => c == '-') > 1 || text.IndexOf('-') > 0) return;
        if (text.Length > 0 && text != "-" &&
            !int.TryParse(text, NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out _)) return;
        SeedText = text;
    }
    public void RandomizeSeed()
    {
        string next;
        do { next = BitConverter.ToInt32(RandomNumberGenerator.GetBytes(sizeof(int))).ToString(CultureInfo.InvariantCulture); } while (next == SeedText);
        SeedText = next;
    }
}
