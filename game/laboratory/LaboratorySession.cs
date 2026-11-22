// Owns Laboratory-only navigation and real generation-session inspection.
// Advances only registered production capabilities through the existing orchestrator, without finalization or State integration.
using Evolve.Definitions.WorldGeneration;
using Evolve.Engine.WorldGeneration;
using Evolve.Engine.WorldGeneration.Layers.Grid;
using Evolve.Engine.WorldGeneration.Layers.Tectonics;

namespace Evolve.Game;

internal sealed class LaboratorySession
{
    private readonly WorldConfiguration configuration = new();
    private readonly Dictionary<GenerationStage, IWorldGenerationLayer> available;
    private readonly WorldGenerationOrchestrator orchestrator;
    public bool IsOpen { get; private set; }
    public MapSize Size => configuration.Size;
    public WorldType WorldType => configuration.WorldType;
    public string SeedText => configuration.SeedText;
    public GenerationContext? Context { get; private set; }
    public HexGridData? Grid { get; private set; }
    public TectonicData? Tectonics { get; private set; }
    public bool ShowBoundaries { get; private set; } = true;
    public bool ShowMotion { get; private set; } = true;
    public int? SelectedCell { get; private set; }
    public GenerationStage? SelectedView { get; private set; }
    public string Notice { get; private set; } = "CONFIGURE EL MUNDO PARA COMENZAR.";
    public GenerationStageStatus? GridStatus => Context?.GetProgress().GetStatus(GenerationStage.Grid);
    public bool CanRunGrid => Context?.GetProgress().NextStage == GenerationStage.Grid && CanAdvance;
    public bool CanAdvance => Context?.GetProgress().NextStage is GenerationStage stage && IsImplemented(stage);

    // This inventory is the single extension point for future real executors; missing stages are capability guards.
    public LaboratorySession(IEnumerable<IWorldGenerationLayer>? availableLayers = null)
    {
        available = (availableLayers ?? new IWorldGenerationLayer[] { new HexGridLayer(), new TectonicPlatesLayer() }).ToDictionary(layer => layer.Stage);
        if (available.Keys.Any(stage => !Enum.IsDefined(stage)))
            throw new ArgumentException("Unknown generation capability.", nameof(availableLayers));
        var layers = Enum.GetValues<GenerationStage>().Select(stage =>
            available.TryGetValue(stage, out var layer) ? layer : new UnavailableLayer(stage));
        orchestrator = new WorldGenerationOrchestrator(new WorldGenerationLayerRegistry(layers));
    }

    public void Open() => IsOpen = true;
    public void ReturnToMenu() => IsOpen = false;
    public bool OpenFromMenu(MainMenuButton? button)
    {
        if (button != MainMenuButton.Laboratory) return false;
        Open();
        return true;
    }
    public bool IsImplemented(GenerationStage stage) => available.ContainsKey(stage);

    // Availability is a display projection; the context remains the sole writable source of stage progress.
    public string StageStatus(GenerationStage stage) => !IsImplemented(stage) ? "NO DISPONIBLE"
        : (Context?.GetProgress().GetStatus(stage) ?? GenerationStageStatus.Pending) switch
        {
            GenerationStageStatus.Pending => "PENDIENTE",
            GenerationStageStatus.Running => "EN CURSO",
            GenerationStageStatus.Completed => "COMPLETADA",
            GenerationStageStatus.Failed => "FALLIDA",
            _ => "INVALIDADA"
        };

    public bool IsCompleted(GenerationStage stage) =>
        Context?.GetProgress().GetStatus(stage) == GenerationStageStatus.Completed;

    public void SetSize(MapSize value) { configuration.SetSize(value); ClearSession(); }
    public void SetWorldType(WorldType value) { configuration.SetWorldType(value); ClearSession(); }
    public void SetSeedText(string value) { configuration.SetSeedText(value); ClearSession(); }
    public void RandomizeSeed() { configuration.RandomizeSeed(); ClearSession(); }

    public void CycleSize()
    {
        var values = Enum.GetValues<MapSize>();
        SetSize(values[(Array.IndexOf(values, Size) + 1) % values.Length]);
    }
    public void CycleWorldType()
    {
        var values = Enum.GetValues<WorldType>();
        SetWorldType(values[(Array.IndexOf(values, WorldType) + 1) % values.Length]);
    }

    public void ClearSession()
    {
        Context = null;
        Grid = null;
        Tectonics = null;
        SelectedCell = null;
        SelectedView = null;
        Notice = "CONFIGURE Y CREE UNA NUEVA SESION.";
    }

    public bool CreateSession()
    {
        if (!configuration.TryCreateRequest(out var request))
        {
            Notice = "SEMILLA INVALIDA: ENTERO DE 64 BITS.";
            return false;
        }
        Context = orchestrator.CreateGeneration(request!);
        Grid = null;
        Tectonics = null;
        SelectedCell = null;
        SelectedView = null;
        Notice = "SESION CREADA. GRID PENDIENTE.";
        return true;
    }

    // Advance exactly one eligible Laboratory stage; unavailable capabilities never execute.
    public bool RunNextStage()
    {
        if (!CanAdvance)
        {
            Notice = Context is null ? "PRIMERO CREE UNA SESION." : "LA SIGUIENTE ETAPA NO ESTA DISPONIBLE.";
            return false;
        }
        GenerationStage stage = Context!.GetProgress().NextStage!.Value;
        var progress = orchestrator.RunNext(Context);
        if (progress.GetStatus(stage) != GenerationStageStatus.Completed)
        {
            Notice = progress.GetFailure(stage) ?? "LA ETAPA NO SE COMPLETO.";
            return false;
        }
        if (stage is GenerationStage.Grid or GenerationStage.Tectonics) SelectView(stage);
        Notice = $"{stage}: COMPLETADA. LAS CAPAS RESTANTES PUEDEN NO ESTAR DISPONIBLES.";
        return true;
    }

    // Retain the existing manual Grid action while enforcing its identity.
    public bool RunGrid() => CanRunGrid && RunNextStage();

    // Only completed data with a real visualization may be selected, never a placeholder image.
    public bool SelectView(GenerationStage stage)
    {
        if (stage is not (GenerationStage.Grid or GenerationStage.Tectonics) || !IsCompleted(stage)) return false;
        var data = Context!.GetLayerData(GenerationStage.Grid) as LayerData<HexGridDescriptor>;
        if (data is null || data.Count != 1 || data[0].Size != Context.Request.Settings.MapSize)
            throw new InvalidOperationException("Published Grid does not match the immutable request.");
        Grid ??= data[0].ToGridData();
        if (stage == GenerationStage.Tectonics)
            Tectonics ??= TectonicData.FromLayerData(Context.GetLayerData(stage)!, Grid);
        SelectedView = stage;
        return true;
    }

    public void ToggleBoundaries() => ShowBoundaries = !ShowBoundaries;
    public void ToggleMotion() => ShowMotion = !ShowMotion;

    // Presentation reads the already validated result; it never regenerates or guesses plate metadata.
    public string DescribeSelection()
    {
        if (SelectedCell is not int id || Grid is not { } grid) return "SELECCIONE UN HEXAGONO EN EL MAPA.";
        var offset = grid.GetOffset(id); var geo = grid.GetGeographicCenter(id);
        string text = FormattableString.Invariant($"CELLID: {id}\nCOLUMNA: {offset.Column}  FILA: {offset.Row}\nLONGITUD: {geo.Longitude:F3}\nLATITUD: {geo.Latitude:F3}");
        if (SelectedView == GenerationStage.Tectonics && Tectonics is { } tectonics)
        {
            byte owner = tectonics.GetPlateId(id);
            var plate = tectonics.GetPlate(owner); var motion = tectonics.GetMotion(owner);
            text += FormattableString.Invariant($"\nPLATEID: {owner}\nCELDAS: {plate.CellCount} ({100d * plate.CellCount / grid.CellCount:F2}%)\nDIRECCION: {motion.DirectionDegrees:F2} GRADOS\nVELOCIDAD: {motion.SpeedCmPerYear:F3} CM/ANO\nINFLUENCIAS: NO DISPONIBLES");
        }
        return text;
    }

    public void Select(int? cellId)
    {
        if (cellId is not null) _ = Grid?.GetOffset(cellId.Value) ?? throw new InvalidOperationException("No Grid to select.");
        SelectedCell = cellId;
    }

    private sealed class UnavailableLayer(GenerationStage stage) : IWorldGenerationLayer
    {
        public GenerationStage Stage => stage;
        public LayerExecutionResult Execute(GenerationLayerInputs inputs) =>
            throw new NotSupportedException($"Generation stage {Stage} is not implemented.");
    }
}
