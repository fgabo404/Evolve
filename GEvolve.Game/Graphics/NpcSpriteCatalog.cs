using System.Text.Json;

namespace GEvolve.Game.Graphics;

/// <summary>Loads hex-direction NPC sprite metadata and raw PNG bytes from copied game assets.</summary>
public static class NpcSpriteCatalog
{
    public static string AssetRoot => Path.Combine(AppContext.BaseDirectory, "Assets", "NPC");

    public static NpcSpriteDefinition Load(string npcId)
    {
        if (string.IsNullOrWhiteSpace(npcId) || npcId.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0 ||
            npcId is "." or ".." || npcId.Contains('/') || npcId.Contains('\\'))
            throw new ArgumentException("Invalid NPC identifier.", nameof(npcId));

        var npcDir = Path.Combine(AssetRoot, npcId);
        var manifestPath = Path.Combine(npcDir, "sprite.json");
        using var stream = File.OpenRead(manifestPath);
        var definition = JsonSerializer.Deserialize<NpcSpriteDefinition>(stream,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true })
            ?? throw new InvalidDataException($"Empty NPC manifest: {manifestPath}");
        if (definition.Id != npcId || definition.FrameWidth <= 0 || definition.FrameHeight <= 0 ||
            definition.AnimationFps <= 0 || definition.Animations is null)
            throw new InvalidDataException($"Invalid NPC manifest: {manifestPath}");
        return definition;
    }

    public static byte[] ReadPng(string npcId, string animation, NpcSpriteDefinition definition)
    {
        if (!definition.Animations.TryGetValue(animation, out var entry) || entry.Frames <= 0)
            throw new ArgumentException($"Unknown NPC animation: {animation}", nameof(animation));
        if (string.IsNullOrWhiteSpace(entry.File) || Path.GetFileName(entry.File) != entry.File ||
            !entry.File.EndsWith(".png", StringComparison.OrdinalIgnoreCase))
            throw new InvalidDataException("NPC animation must reference a PNG in its own folder.");
        return File.ReadAllBytes(Path.Combine(AssetRoot, npcId, entry.File));
    }
}

public sealed class NpcSpriteDefinition
{
    public string Id { get; set; } = "";
    public string Name { get; set; } = "";
    public int FrameWidth { get; set; }
    public int FrameHeight { get; set; }
    public int AnimationFps { get; set; }
    public Dictionary<string, NpcAnimationDefinition> Animations { get; set; } = new();
}

public sealed class NpcAnimationDefinition
{
    public string File { get; set; } = "";
    public int Frames { get; set; }
}
