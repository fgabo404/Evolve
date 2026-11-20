// Holds reusable editable configuration for the Laboratory's manual generation sessions.
// Validates only existing definition enums and signed seed input before constructing an engine request.
using System.Buffers.Binary;
using System.Globalization;
using System.Security.Cryptography;
using Evolve.Definitions.WorldGeneration;
using Evolve.Engine.WorldGeneration;

namespace Evolve.Game;

internal sealed class WorldConfiguration
{
    public MapSize Size { get; private set; } = MapSize.Small;
    public WorldType WorldType { get; private set; } = WorldType.Pangea;
    public string SeedText { get; private set; } = "0";

    public void SetSize(MapSize value)
    {
        if (!Enum.IsDefined(value)) throw new ArgumentOutOfRangeException(nameof(value));
        Size = value;
    }

    public void SetWorldType(WorldType value)
    {
        if (!Enum.IsDefined(value)) throw new ArgumentOutOfRangeException(nameof(value));
        WorldType = value;
    }

    public void SetSeedText(string value)
    {
        ArgumentNullException.ThrowIfNull(value);
        SeedText = value;
    }

    // Randomization is an explicit UI action, independent of deterministic engine stage streams.
    public void RandomizeSeed()
    {
        Span<byte> bytes = stackalloc byte[sizeof(long)];
        RandomNumberGenerator.Fill(bytes);
        SeedText = BinaryPrimitives.ReadInt64LittleEndian(bytes).ToString(CultureInfo.InvariantCulture);
    }

    public bool TryCreateRequest(out GenerationRequest? request)
    {
        request = null;
        if (!long.TryParse(SeedText, NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out long seed))
            return false;
        request = new GenerationRequest(seed, new WorldGenerationSettings(WorldType, Size));
        return true;
    }
}
