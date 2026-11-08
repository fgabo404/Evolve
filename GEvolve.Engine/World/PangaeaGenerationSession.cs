using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Security.Cryptography;
using System.Text.Json;
using GEvolve.Definitions.WorldGeneration.Configuration;
using GEvolve.Engine.Grid;
using GEvolve.Engine.World.LandmassGeneration;
using GEvolve.Engine.World.LandmassGeneration.Shared;
using GEvolve.Engine.World.Tectonics;
using GEvolve.Engine.World.BiomeGeneration;
using GEvolve.Engine.World.Climate;
using GEvolve.State.Game;

namespace GEvolve.Engine.World;

/// <summary>One temporary Pangaea pipeline. No incomplete LandmassState is exported.</summary>
public sealed class PangaeaGenerationSession
{
    public static IReadOnlyDictionary<PangaeaGenerationStage, IReadOnlyList<PangaeaGenerationStage>> Dependencies { get; }
        = new ReadOnlyDictionary<PangaeaGenerationStage, IReadOnlyList<PangaeaGenerationStage>>(
            new Dictionary<PangaeaGenerationStage, IReadOnlyList<PangaeaGenerationStage>>
            {
                [PangaeaGenerationStage.Grid] = Array.AsReadOnly<PangaeaGenerationStage>([]),
                [PangaeaGenerationStage.Plates] = Array.AsReadOnly(new[] { PangaeaGenerationStage.Grid }),
                [PangaeaGenerationStage.Motion] = Array.AsReadOnly(new[] { PangaeaGenerationStage.Plates }),
                [PangaeaGenerationStage.Surface] = Array.AsReadOnly(new[] { PangaeaGenerationStage.Plates }),
                [PangaeaGenerationStage.Base] = Array.AsReadOnly(new[] { PangaeaGenerationStage.Surface }),
                [PangaeaGenerationStage.MountainSystems] = Array.AsReadOnly(new[] { PangaeaGenerationStage.Surface, PangaeaGenerationStage.Motion }),
                [PangaeaGenerationStage.Final] = Array.AsReadOnly(new[] { PangaeaGenerationStage.Base, PangaeaGenerationStage.MountainSystems }),
                [PangaeaGenerationStage.Temperature] = Array.AsReadOnly(new[] { PangaeaGenerationStage.Final }),
                [PangaeaGenerationStage.Precipitation] = Array.AsReadOnly(new[] { PangaeaGenerationStage.Temperature }),
                [PangaeaGenerationStage.Biomes] = Array.AsReadOnly(new[] { PangaeaGenerationStage.Precipitation })
            });

    public static IReadOnlyList<PangaeaGenerationStage> GeologicalStages { get; } = Array.AsReadOnly(
        Enum.GetValues<PangaeaGenerationStage>().Where(s => (int)s <= (int)PangaeaGenerationStage.Final).ToArray());

    private sealed record Data(HexGrid? Grid = null, HexTopology? Topology = null, byte[]? Plates = null,
        TectonicMotionState? Motion = null, PangaeaSurfaceWork? Surface = null, LandmassStatistics? Statistics = null,
        PangaeaSurfaceAttempt[]? Attempts = null, byte[]? Base = null, PangaeaElevationSources? Sources = null,
        PangaeaMountainPlan? Plan = null, GeneratedWorld? World = null, PangaeaMountainRangeAudit? FinalAudit = null,
        LandmassState? Temperature = null, LandmassState? Precipitation = null, LandmassState? Biomes = null);
    private sealed record Operation(long Id, PangaeaGenerationStage Stage, LandmassConfiguration Configuration,
        Data Inputs, CancellationToken Cancellation);
    private readonly object _gate = new();
    private readonly bool _recordDiagnostics, _captureAudit;
    private readonly PangaeaFromPlatesGenerator _pangaea = new();
    private readonly PangaeaMountainRangeGenerator _mountains = new();
    private LandmassConfiguration _configuration;
    private long _configurationRevision = 1, _sequence;
    private long? _active;
    private Data _data = new();
    private Dictionary<PangaeaGenerationStage, PangaeaStageInfo> _stages = NewStages();

    internal PangaeaGenerationSession(LandmassConfiguration configuration, bool recordDiagnostics = true,
        bool captureAudit = true)
    {
        Validate(configuration);
        _configuration = configuration;
        _recordDiagnostics = recordDiagnostics;
        _captureAudit = captureAudit;
    }

    public void Reset(LandmassConfiguration configuration)
    {
        Validate(configuration);
        lock (_gate)
        {
            _configuration = configuration;
            _configurationRevision++;
            _sequence++;
            _active = null;
            _data = new();
            _stages = NewStages();
        }
    }

    public IReadOnlyList<PangaeaStageInfo> GetStages()
    {
        lock (_gate) return StageViews();
    }

    public void Execute(PangaeaGenerationStage stage, CancellationToken cancellationToken = default)
        => Run(Begin(stage, cancellationToken));

    public Task ExecuteAsync(PangaeaGenerationStage stage, CancellationToken cancellationToken = default)
    {
        var operation = Begin(stage, cancellationToken);
        // Always run cleanup even if cancellation is requested after Begin.
        return Task.Run(() => Run(operation));
    }

    public PangaeaGenerationSnapshot GetSnapshot()
    {
        Data data; LandmassConfiguration configuration; long revision; IReadOnlyList<PangaeaStageInfo> stages;
        lock (_gate) { data = _data; configuration = _configuration; revision = _configurationRevision; stages = StageViews(); }
        // Published inputs are never mutated internally; copying outside the lock is safe.
        var environment = data.Biomes ?? data.Precipitation ?? data.Temperature;
        return new(configuration, revision, stages, data.Grid?.Copy(), data.Plates?.ToArray(), Clone(data.Motion),
            data.Surface?.Attempt, Array.AsReadOnly((data.Attempts ?? []).Select(a => a with
            { Statistics = CopyStatistics(a.Statistics)! }).ToArray()), data.Surface?.Surface.ToArray(),
            CopyStatistics(data.Statistics), data.Base?.ToArray(), PangaeaElevationSources.Copy(data.Sources),
            CopySelection(data.Plan), data.World?.PangaeaElevations?.Copy())
        { Slopes = environment?.SlopeByCellId?.ToArray(), Climate = Clone(environment?.Climate), Biomes = Clone(environment?.Biomes) };
    }

    public GeneratedWorld GetCompletedWorld()
    {
        var world = GetOwnedCompletedWorld();
        return new(world.Grid.Copy(), Clone(world.Landmass)!, CopyStatistics(world.Statistics))
            { PangaeaElevations = world.PangaeaElevations!.Copy() };
    }

    internal GeneratedWorld GetOwnedCompletedWorld()
    {
        lock (_gate)
        {
            if (_stages[PangaeaGenerationStage.Final].Status != PangaeaStageStatus.Completed || _data.World is not { } world)
                throw new InvalidOperationException("A current completed FINAL stage is required.");
            return world with { Landmass = _data.Biomes ?? _data.Precipitation ?? _data.Temperature ?? world.Landmass };
        }
    }

    internal PangaeaMountainRangeAudit? GetFinalAudit() { lock (_gate) return _data.FinalAudit; }

    private Operation Begin(PangaeaGenerationStage stage, CancellationToken cancellation)
    {
        cancellation.ThrowIfCancellationRequested();
        lock (_gate)
        {
            if (!Dependencies.TryGetValue(stage, out var dependencies)) throw new ArgumentOutOfRangeException(nameof(stage));
            if (_active.HasValue) throw new InvalidOperationException("Another generation stage is running.");
            if (dependencies.Any(d => _stages[d].Status != PangaeaStageStatus.Completed))
                throw new InvalidOperationException($"Stage {stage} requires completed dependencies: {string.Join(", ", dependencies)}.");
            Invalidate(stage);
            long id = ++_sequence;
            _active = id;
            _stages[stage] = new(stage, PangaeaStageStatus.Running, false, id, null, null, null,
                ReadOnly(dependencies.ToDictionary(d => d, d => _stages[d].Revision)),
                ReadOnly(dependencies.Where(d => _stages[d].Hash is not null).ToDictionary(d => d, d => _stages[d].Hash!)));
            return new(id, stage, _configuration, _data, cancellation);
        }
    }

    private void Run(Operation operation)
    {
        var timer = Stopwatch.StartNew();
        try
        {
            operation.Cancellation.ThrowIfCancellationRequested();
            var (result, failure) = Compute(operation);
            operation.Cancellation.ThrowIfCancellationRequested();
            string? hash = _recordDiagnostics && failure is null ? ResultHash(operation.Stage, result) : null;
            timer.Stop();
            lock (_gate)
            {
                if (_active != operation.Id) throw new OperationCanceledException("Generation configuration was reset; result discarded.");
                operation.Cancellation.ThrowIfCancellationRequested();
                _data = result;
                _active = null;
                _stages[operation.Stage] = _stages[operation.Stage] with
                {
                    Status = failure is null ? PangaeaStageStatus.Completed : PangaeaStageStatus.Failed,
                    Hash = hash, Duration = timer.Elapsed, Error = failure?.Message
                };
            }
            if (failure is not null) throw failure;
        }
        catch (Exception error)
        {
            timer.Stop();
            lock (_gate)
                if (_active == operation.Id)
                {
                    _active = null;
                    _stages[operation.Stage] = _stages[operation.Stage] with
                    {
                        Status = error is OperationCanceledException ? PangaeaStageStatus.Invalidated : PangaeaStageStatus.Failed,
                        Hash = null, Duration = timer.Elapsed, Error = error.Message
                    };
                }
            throw;
        }
    }

    private (Data Result, Exception? Failure) Compute(Operation operation)
    {
        var data = operation.Inputs;
        var configuration = operation.Configuration;
        switch (operation.Stage)
        {
            case PangaeaGenerationStage.Grid:
                var grid = HexGrid.Generate(HexGridDefinitions.Get(configuration.Size));
                return (data with { Grid = grid, Topology = new HexTopology(grid) }, null);
            case PangaeaGenerationStage.Plates:
                return (data with { Plates = TectonicPlateGenerator.Generate(data.Topology!, configuration.Seed) }, null);
            case PangaeaGenerationStage.Motion:
                var preview = new LandmassState(configuration, [], 0) { PlateByCellId = data.Plates, TectonicPreviewOnly = true };
                return (data with { Motion = new TectonicMotionGenerator().Generate(data.Topology!, preview) }, null);
            case PangaeaGenerationStage.Surface:
                var attempts = new List<PangaeaSurfaceAttempt>();
                LandmassStatistics? last = null;
                for (int attempt = 0; attempt < 4; attempt++)
                {
                    var work = _pangaea.GenerateSurface(data.Topology!, configuration, data.Plates!, attempt);
                    last = LandmassAnalysis.Analyze(data.Topology!, work.Surface);
                    bool accepted = LandmassProfileValidator.MeetsProfile(configuration.Type, last);
                    attempts.Add(new(attempt, accepted, last, _recordDiagnostics ? Hash(work.Surface) : null));
                    if (accepted) return (data with { Surface = work, Statistics = last, Attempts = attempts.ToArray() }, null);
                }
                return (data with { Attempts = attempts.ToArray() }, new InvalidOperationException(
                    $"Stage 02 {configuration.Type} did not meet the profile after four attempts " +
                    $"(land={last?.LandFraction:P1}, components={last?.ComponentCount}, dominant={last?.DominantLandShare:P1})."));
            case PangaeaGenerationStage.Base:
                PangaeaElevationSources? sources = null;
                var initial = _pangaea.GenerateElevation(data.Surface!, value => sources = value);
                return (data with { Base = initial, Sources = sources }, null);
            case PangaeaGenerationStage.MountainSystems:
                return (data with { Plan = _mountains.Prepare(data.Topology!, configuration, data.Surface!.Surface, data.Motion!, _captureAudit) }, null);
            case PangaeaGenerationStage.Final:
                var terrain = new LandmassState(configuration, data.Surface!.Surface, data.Surface.Attempt)
                { PlateByCellId = data.Plates, TectonicMotion = data.Motion, ElevationByCellId = data.Base };
                PangaeaElevationDiagnostics? diagnostics = null;
                PangaeaMountainRangeAudit? audit = null;
                terrain = _mountains.ApplyPrepared(terrain, data.Plan!, value => diagnostics = value,
                    data.Sources!.PreviousBaseByCellId, data.Sources.PriorCalibrationBaseByCellId,
                    _captureAudit ? value => audit = value : null);
                diagnostics!.Sources = data.Sources;
                return (data with { World = new(data.Grid!, terrain, data.Statistics) { PangaeaElevations = diagnostics }, FinalAudit = audit }, null);
            case PangaeaGenerationStage.Temperature:
                var geology = data.World!.Landmass;
                var slopes = TerrainSlope.Calculate(data.Topology!, geology);
                return (data with { Temperature = new TemperatureGenerator().Apply(data.Topology!, geology with { SlopeByCellId = slopes }) }, null);
            case PangaeaGenerationStage.Precipitation:
                return (data with { Precipitation = new PrecipitationGenerator().Apply(data.Topology!, data.Temperature!) }, null);
            case PangaeaGenerationStage.Biomes:
                return (data with { Biomes = new BiomeClassifier().Apply(data.Precipitation!, data.Topology!) }, null);
            default: throw new ArgumentOutOfRangeException(nameof(operation));
        }
    }

    private void Invalidate(PangaeaGenerationStage root)
    {
        var invalid = new HashSet<PangaeaGenerationStage> { root };
        foreach (var stage in Enum.GetValues<PangaeaGenerationStage>())
            if (Dependencies[stage].Any(invalid.Contains)) invalid.Add(stage);
        foreach (var stage in invalid)
        {
            var current = _stages[stage];
            _stages[stage] = Empty(stage, current.Status == PangaeaStageStatus.Pending ? PangaeaStageStatus.Pending : PangaeaStageStatus.Invalidated);
            _data = stage switch
            {
                PangaeaGenerationStage.Grid => _data with { Grid = null, Topology = null },
                PangaeaGenerationStage.Plates => _data with { Plates = null },
                PangaeaGenerationStage.Motion => _data with { Motion = null },
                PangaeaGenerationStage.Surface => _data with { Surface = null, Statistics = null, Attempts = null },
                PangaeaGenerationStage.Base => _data with { Base = null, Sources = null },
                PangaeaGenerationStage.MountainSystems => _data with { Plan = null },
                PangaeaGenerationStage.Final => _data with { World = null, FinalAudit = null },
                PangaeaGenerationStage.Temperature => _data with { Temperature = null },
                PangaeaGenerationStage.Precipitation => _data with { Precipitation = null },
                PangaeaGenerationStage.Biomes => _data with { Biomes = null },
                _ => throw new ArgumentOutOfRangeException(nameof(root))
            };
        }
    }

    private IReadOnlyList<PangaeaStageInfo> StageViews() => Array.AsReadOnly(_stages.Values.Select(info => info with
    { CanExecute = !_active.HasValue && Dependencies[info.Stage].All(d => _stages[d].Status == PangaeaStageStatus.Completed) }).ToArray());
    private static Dictionary<PangaeaGenerationStage, PangaeaStageInfo> NewStages() => Enum.GetValues<PangaeaGenerationStage>()
        .ToDictionary(stage => stage, stage => Empty(stage, PangaeaStageStatus.Pending));
    private static PangaeaStageInfo Empty(PangaeaGenerationStage stage, PangaeaStageStatus status) =>
        new(stage, status, false, 0, null, null, null, ReadOnly(new Dictionary<PangaeaGenerationStage, long>()),
            ReadOnly(new Dictionary<PangaeaGenerationStage, string>()));
    private static IReadOnlyDictionary<K, V> ReadOnly<K, V>(Dictionary<K, V> values) where K : notnull => new ReadOnlyDictionary<K, V>(values);
    private static void Validate(LandmassConfiguration configuration)
    {
        WorldGenerationService.Validate(configuration);
        if (configuration.Type != LandmassType.Pangaea) throw new ArgumentException("The staged session requires Pangaea.", nameof(configuration));
    }

    private static string ResultHash(PangaeaGenerationStage stage, Data data) => stage switch
    {
        PangaeaGenerationStage.Grid => Hash(data.Grid!.Definition),
        PangaeaGenerationStage.Plates => Hash(data.Plates!),
        PangaeaGenerationStage.Motion => Hash(data.Motion),
        PangaeaGenerationStage.Surface => Hash(new { data.Surface!.Attempt, Surface = Hash(data.Surface.Surface) }),
        PangaeaGenerationStage.Base => Hash(data.Base!),
        PangaeaGenerationStage.MountainSystems => Hash(new { data.Plan!.Ranges, data.Plan.Owner }),
        PangaeaGenerationStage.Final => Hash(data.World!.Landmass),
        PangaeaGenerationStage.Temperature => Hash(new { data.Temperature!.SlopeByCellId, data.Temperature.Climate }),
        PangaeaGenerationStage.Precipitation => Hash(data.Precipitation!.Climate),
        PangaeaGenerationStage.Biomes => Hash(data.Biomes!.Biomes),
        _ => throw new ArgumentOutOfRangeException(nameof(stage))
    };
    private static string Hash<T>(T value) => Convert.ToHexString(SHA256.HashData(JsonSerializer.SerializeToUtf8Bytes(value)));
    private static string Hash(byte[] value) => Convert.ToHexString(SHA256.HashData(value));
    private static T? Clone<T>(T? value) where T : class => value is null ? null : JsonSerializer.Deserialize<T>(JsonSerializer.SerializeToUtf8Bytes(value));
    private static LandmassStatistics? CopyStatistics(LandmassStatistics? statistics) => statistics is null ? null :
        statistics with { ComponentSizes = statistics.ComponentSizes.ToArray() };
    private static PangaeaMountainSelection? CopySelection(PangaeaMountainPlan? plan) => plan is null ? null : new(
        plan.Ranges.Select(r => new PangaeaMountainAxis(r.Id, r.AxisCellIds.ToArray(), r.WidthByAxisCell.ToArray(),
            r.FirstPlateId, r.SecondPlateId, r.PeakConvergence, r.MeanConvergence, r.Elongation,
            r.ContactEndpoints, r.ContactCycles, r.BranchingCells)).ToArray(), plan.Owner.ToArray(),
        ReadOnly((plan.Audit?.Contacts ?? []).GroupBy(c => c.Decision).ToDictionary(g => g.Key, g => g.Count())),
        ReadOnly((plan.Audit?.Candidates ?? []).GroupBy(c => c.Decision).ToDictionary(g => g.Key, g => g.Count())), plan.Samples.Length);
}
