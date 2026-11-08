using System.Globalization;
using System.Numerics;
using System.Security.Cryptography;
using GEvolve.Definitions.WorldGeneration.Configuration;
using GEvolve.Engine.Grid;
using GEvolve.Engine.World;
using GEvolve.Game.Graphics;

namespace GEvolve.Game;

/// <summary>Main-thread presentation controller. Workers own generation and snapshot copying, never GPU resources.</summary>
public sealed class PangaeaLaboratory : IDisposable
{
    private sealed record Completion(PangaeaGenerationSnapshot Snapshot, Exception? Error);
    private sealed record Work(Task<Completion> Task, CancellationTokenSource Cancellation, PangaeaGenerationStage Stage);
    private readonly WorldGenerationService _service = new();
    private PangaeaGenerationSession? _session;
    private Work? _work;
    private PangaeaGenerationSnapshot? _snapshot;
    private bool _motionQueued;
    public GridSize Size { get; private set; } = GridSize.Small;
    public string SeedText { get; private set; } = "2026";
    public string? Error { get; private set; }
    public string? Notice { get; private set; }
    public long PresentationRevision { get; private set; }
    public long SessionRevision { get; private set; }
    public int SnapshotCopies { get; private set; }
    public bool Busy => _work is not null;
    public PangaeaMapData? Map { get; private set; }
    public GridCell? SelectedCell { get; private set; }
    public MapViewMode View { get; private set; } = MapViewMode.Grid;
    public IReadOnlyList<PangaeaStageInfo> Stages { get; private set; } = [];

    public void ChooseSize(GridSize size)
    {
        _ = HexGridDefinitions.Get(size);
        if (size == Size) return;
        Size = size; ClearSession();
        if (int.TryParse(SeedText, out _)) RequestRestart();
    }
    public void SetSeedText(string text)
    {
        if (text.Length > 11 || text.Any(c => !char.IsAsciiDigit(c) && c != '-') ||
            text.Count(c => c == '-') > 1 || text.IndexOf('-') > 0) return;
        if (SeedText == text) return;
        SeedText = text; ClearSession();
        Notice = "SEMILLA CAMBIADA. INICIAR SESIÓN PARA CONTINUAR.";
    }
    public void RandomizeSeed()
    {
        bool hadSeed = int.TryParse(SeedText, out int previous);
        int seed;
        do { seed = BitConverter.ToInt32(RandomNumberGenerator.GetBytes(sizeof(int))); } while (hadSeed && seed == previous);
        SeedText = seed.ToString(CultureInfo.InvariantCulture);
        ClearSession(); RequestRestart();
    }
    public void ExecutePlatesAndMotion()
    {
        Execute(PangaeaGenerationStage.Plates);
        _motionQueued = _work?.Stage == PangaeaGenerationStage.Plates && Error is null;
    }
    public string PlatesAndMotionState()
    {
        var stages = Stages.Where(s => s.Stage is PangaeaGenerationStage.Plates or PangaeaGenerationStage.Motion).ToArray();
        if (stages.Length != 2) return "PENDIENTE";
        if (_motionQueued || stages.Any(s => s.Status == PangaeaStageStatus.Running)) return "EJECUTÁNDOSE";
        if (stages.Any(s => s.Status == PangaeaStageStatus.Failed)) return "ERROR";
        if (stages.All(s => s.Status == PangaeaStageStatus.Completed)) return "COMPLETADA";
        return CanExecute(PangaeaGenerationStage.Plates) ? "PENDIENTE" : "BLOQUEADA";
    }
    private void ClearSession()
    {
        AbandonWork(); _session = null; _snapshot = null; Stages = [];
        Map = null; SelectedCell = null; View = MapViewMode.Grid; Error = null;
        PresentationRevision++;
    }

    public void RequestRestart()
    {
        if (!int.TryParse(SeedText, NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out _))
        { Error = "LA SEMILLA DEBE SER UN ENTERO DE 32 BITS"; return; }
        Restart();
    }
    private void Restart()
    {
        var configuration = LandmassProfiles.Create(Size, LandmassType.Pangaea, int.Parse(SeedText, CultureInfo.InvariantCulture));
        AbandonWork();
        if (_session is null) _session = _service.CreatePangaeaSession(configuration);
        else _session.Reset(configuration);
        SessionRevision++; Error = null;
        SelectedCell = null; View = MapViewMode.Grid;
        SnapshotCopies++;
        Publish(_session.GetSnapshot());
        Execute(PangaeaGenerationStage.Grid);
    }

    private static readonly PangaeaGenerationStage[] OrderedStages = [PangaeaGenerationStage.Grid,
        PangaeaGenerationStage.Plates, PangaeaGenerationStage.Motion, PangaeaGenerationStage.Surface,
        PangaeaGenerationStage.Base, PangaeaGenerationStage.MountainSystems, PangaeaGenerationStage.Final];
    private IEnumerable<PangaeaGenerationStage> MissingPredecessors(PangaeaGenerationStage stage) => OrderedStages
        .TakeWhile(s => s != stage).Where(s => !Stages.Any(info => info.Stage == s && info.Status == PangaeaStageStatus.Completed));
    public bool CanExecute(PangaeaGenerationStage stage) => _session is not null && !Busy &&
        Stages.Any(s => s.Stage == stage && s.CanExecute) && !MissingPredecessors(stage).Any();
    public string StageState(PangaeaStageInfo stage) => stage.Status switch
    {
        PangaeaStageStatus.Running => "EJECUTÁNDOSE",
        PangaeaStageStatus.Completed => "COMPLETADA",
        PangaeaStageStatus.Failed => "ERROR",
        _ => CanExecute(stage.Stage) ? "PENDIENTE" : "BLOQUEADA"
    };
    public string UnavailableReason(PangaeaGenerationStage stage)
    {
        if (_session is null) return "INICIA UNA SESIÓN";
        if (Busy) return "ESPERA LA OPERACIÓN ACTIVA";
        var missing = MissingPredecessors(stage);
        return "FALTA: " + string.Join(", ", missing.Select(d => d switch
        { PangaeaGenerationStage.Grid => "CUADRÍCULA AUTOMÁTICA", PangaeaGenerationStage.Plates => "PLACAS",
            PangaeaGenerationStage.Motion => "MOVIMIENTO", PangaeaGenerationStage.Surface => "SUPERFICIE",
            PangaeaGenerationStage.Base => "BASE", _ => "SISTEMAS" }));
    }

    public void Execute(PangaeaGenerationStage stage)
    {
        if (!CanExecute(stage)) { Error = UnavailableReason(stage); return; }
        var session = _session!;
        var cancellation = new CancellationTokenSource();
        try
        {
            var task = session.ExecuteAsync(stage, cancellation.Token);
            // Begin has already invalidated descendants. Remove their renderable data immediately.
            Stages = session.GetStages();
            Publish(Prune(_snapshot!, Stages));
            _work = new(Complete(task, session), cancellation, stage);
            Error = null; Notice = "GENERANDO " + (stage switch
            { PangaeaGenerationStage.Grid => "CUADRÍCULA AUTOMÁTICA", PangaeaGenerationStage.Plates => "PLACAS",
                PangaeaGenerationStage.Motion => "MOVIMIENTO / H04", PangaeaGenerationStage.Surface => "SUPERFICIE",
                PangaeaGenerationStage.Base => "ELEVACIONES BASE", PangaeaGenerationStage.MountainSystems => "SISTEMAS MONTAÑOSOS",
                _ => "ELEVACIONES FINAL" });
        }
        catch (Exception error) { cancellation.Dispose(); Error = error.Message; }
    }
    private static async Task<Completion> Complete(Task task, PangaeaGenerationSession session)
    {
        Exception? failure = null;
        try { await task.ConfigureAwait(false); } catch (Exception error) { failure = error; }
        // Snapshot serialization/copying also stays off the window thread, even for an already completed task.
        return await Task.Run(() => new Completion(session.GetSnapshot(), failure)).ConfigureAwait(false);
    }
    public bool Update()
    {
        if (_work is not { Task.IsCompleted: true } work) return false;
        _work = null;
        bool runMotion = _motionQueued && work.Stage == PangaeaGenerationStage.Plates;
        _motionQueued = false;
        try
        {
            var result = work.Task.GetAwaiter().GetResult();
            SnapshotCopies++; Publish(result.Snapshot);
            if (result.Error is null)
                ChooseView(work.Stage switch
                {
                    PangaeaGenerationStage.Grid => MapViewMode.Grid,
                    PangaeaGenerationStage.Plates => MapViewMode.Tectonic,
                    PangaeaGenerationStage.Motion => MapViewMode.Motion,
                    PangaeaGenerationStage.Surface => MapViewMode.Surface,
                    PangaeaGenerationStage.Base => MapViewMode.ElevationBase,
                    PangaeaGenerationStage.MountainSystems => MapViewMode.Geology,
                    _ => MapViewMode.ElevationFinal
                });
            Error = result.Error is OperationCanceledException ? null : result.Error?.Message;
            Notice = result.Error is OperationCanceledException ? "RESULTADO CANCELADO Y DESCARTADO. PUEDES REPETIR." :
                result.Error is null ? "ETAPA COMPLETADA" : "ERROR. PUEDES REPETIR LA ETAPA.";
            if (runMotion && result.Error is null && !work.Cancellation.IsCancellationRequested)
                Execute(PangaeaGenerationStage.Motion);
        }
        catch (Exception error) { Error = error.Message; }
        finally { work.Cancellation.Dispose(); }
        return true;
    }
    public void Cancel()
    {
        _motionQueued = false;
        _work?.Cancellation.Cancel();
        Notice = "CANCELACIÓN SOLICITADA. EL CÁLCULO PUEDE TERMINAR ANTES DE DESCARTARSE.";
    }
    private void AbandonWork()
    {
        _motionQueued = false;
        if (_work is not { } work) return;
        _work = null; work.Cancellation.Cancel();
        _ = work.Task.ContinueWith(t => { _ = t.Exception; work.Cancellation.Dispose(); }, TaskScheduler.Default);
    }
    private void Publish(PangaeaGenerationSnapshot snapshot)
    {
        _snapshot = snapshot; Stages = snapshot.Stages; UpdateMap();
    }
    private void UpdateMap()
    {
        Map = _snapshot?.Grid is not null ? new(_snapshot) : null;
        if (Map is null) SelectedCell = null;
        if (Map is not null && !Map.AvailableViews.Contains(View)) View = Map.AvailableViews.Last();
        PresentationRevision++;
    }
    public bool ChooseView(MapViewMode view)
    {
        if (Map?.AvailableViews.Contains(view) != true) return false;
        View = view; PresentationRevision++; return true;
    }
    public GridCell? SelectAt(Vector2 worldPoint)
    {
        var grid = Map?.Snapshot.Grid;
        var address = grid is null ? null : FlatHexGeometry.CellAt(worldPoint, grid.Definition.Columns, grid.Definition.Rows);
        SelectedCell = grid is not null && address is { } cell && grid.TryGet(cell, out var found) ? found : null;
        return SelectedCell;
    }
    public IReadOnlyList<string> Inspect() => SelectedCell is { } cell && Map is { } map ? map.Inspect(cell.Id) : [];
    private static PangaeaGenerationSnapshot Prune(PangaeaGenerationSnapshot s, IReadOnlyList<PangaeaStageInfo> stages)
    {
        bool Has(PangaeaGenerationStage stage)
        {
            var current = stages.Single(i => i.Stage == stage);
            return current.Status == PangaeaStageStatus.Completed &&
                s.Stages.Single(i => i.Stage == stage).Revision == current.Revision;
        }
        return s with { Stages = stages,
            Grid = Has(PangaeaGenerationStage.Grid) ? s.Grid : null,
            Plates = Has(PangaeaGenerationStage.Plates) ? s.Plates : null,
            Motion = Has(PangaeaGenerationStage.Motion) ? s.Motion : null,
            Surface = Has(PangaeaGenerationStage.Surface) ? s.Surface : null,
            SelectedAttempt = Has(PangaeaGenerationStage.Surface) ? s.SelectedAttempt : null,
            SurfaceStatistics = Has(PangaeaGenerationStage.Surface) ? s.SurfaceStatistics : null,
            Attempts = Has(PangaeaGenerationStage.Surface) ? s.Attempts : [],
            Base = Has(PangaeaGenerationStage.Base) ? s.Base : null,
            BaseSources = Has(PangaeaGenerationStage.Base) ? s.BaseSources : null,
            Mountains = Has(PangaeaGenerationStage.MountainSystems) ? s.Mountains : null,
            Elevations = Has(PangaeaGenerationStage.Final) ? s.Elevations : null };
    }
    public void Dispose() => ClearSession();
}
