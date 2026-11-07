using System.Diagnostics;
using System.Security.Cryptography;
using System.Text.Json;
using GEvolve.Definitions.WorldGeneration.Configuration;
using GEvolve.Engine.World;
using GEvolve.Engine.World.LandmassGeneration;

public static class PangaeaStagedGenerationTests
{
    private sealed record Reference(string Size, string Type, int Seed, int Attempt, string Grid,
        string State, string Statistics, string Surface, string Plates, string Motion,
        string? Base, string? Contribution, string? Final, string? Systems);

    private static IEnumerable<(GridSize Size, LandmassType Type, int Seed)> Cases()
    {
        foreach (var size in Enum.GetValues<GridSize>())
        {
            foreach (int seed in new[] { -1387338765, -132790047, -714216669, -2024691550,
                -174327693, -1979952984, 2076374193 })
                yield return (size, LandmassType.Pangaea, seed);
            foreach (var type in new[] { LandmassType.Continents, LandmassType.Archipelagos, LandmassType.SmallContinents })
                yield return (size, type, 2026);
        }
    }

    public static void CaptureReference()
    {
        foreach (var (size, type, seed) in Cases())
        {
            var timer = Stopwatch.StartNew();
            var world = new WorldGenerationService().Generate(size, type, seed);
            timer.Stop();
            Console.WriteLine("STAGED_REFERENCE " + JsonSerializer.Serialize(Signature(world)));
            Console.WriteLine($"REFERENCE_TIME {size}/{type}/{seed}: {timer.Elapsed.TotalMilliseconds:F3} ms");
        }
        Console.WriteLine("STAGED_REFERENCE_PASS: 28 acceptance worlds across four sizes, 12 other-type controls; original automatic pipeline");
    }

    private static Reference Signature(GeneratedWorld world)
    {
        var terrain = world.Landmass;
        var fields = world.PangaeaElevations;
        return new(terrain.Configuration.Size.ToString(), terrain.Configuration.Type.ToString(),
            terrain.Configuration.Seed, terrain.DeterministicAttempt, Hash(world.Grid.Definition),
            Hash(terrain), Hash(world.Statistics), Hash(terrain.SurfaceByCellId), Hash(terrain.PlateByCellId!),
            Hash(terrain.TectonicMotion), fields is null ? null : Hash(fields.BaseByCellId.ToArray()),
            fields is null ? null : Hash(fields.ContributionByCellId.ToArray()),
            terrain.ElevationByCellId is null ? null : Hash(terrain.ElevationByCellId),
            terrain.PangaeaMountainRanges is null ? null : Hash(terrain.PangaeaMountainRanges));
    }

    private static string Hash<T>(T value) => Convert.ToHexString(SHA256.HashData(JsonSerializer.SerializeToUtf8Bytes(value)));
    private static string Hash(byte[] value) => Convert.ToHexString(SHA256.HashData(value));
    private static void Check(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
}
