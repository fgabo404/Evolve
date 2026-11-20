// Owns Laboratory navigation, editable configuration and inspection of a temporary generation session.
// Uses only the existing engine entry points; unavailable stages never produce candidate data.
using System.Globalization;
using Evolve.Definitions.WorldGeneration;
using Evolve.Engine.WorldGeneration;
using Evolve.Engine.WorldGeneration.Layers.Grid;

namespace Evolve.Game;

internal sealed class LaboratorySession
{
    private readonly WorldGenerationOrchestrator orchestrator;
    public bool IsOpen { get; private set; }
    public MapSize Size { get; private set; } = MapSize.Small;
    public WorldType WorldType { get; private set; } = WorldType.Pangea;
    public string SeedText { get; private set; } = "0";
    public GenerationContext? Context { get; private set; }
    public HexGridData? Grid { get; private set; }
    public int? SelectedCell { get; private set; }
    public string Notice { get; private set; } = "CREE UNA SESION Y EJECUTE GRID.";
    public GenerationStageStatus? GridStatus => Context?.GetProgress().GetStatus(GenerationStage.Grid);
    public bool CanRunGrid => Context is not null && GridStatus is GenerationStageStatus.Pending or GenerationStageStatus.Failed;

    public LaboratorySession()
    {
        IWorldGenerationLayer[] layers = Enum.GetValues<GenerationStage>()
            .Select(stage => stage == GenerationStage.Grid ? (IWorldGenerationLayer)new HexGridLayer() : new UnavailableLayer(stage)).ToArray();
        orchestrator = new WorldGenerationOrchestrator(new WorldGenerationLayerRegistry(layers));
    }

    public void Open() => IsOpen = true;
    public void ReturnToMenu() => IsOpen = false;

    // Cycling uses the existing enums; no second configuration catalog or geographic defaults exist.
    public void CycleSize()
    {
        var sizes = Enum.GetValues<MapSize>();
        Size = sizes[(Array.IndexOf(sizes, Size) + 1) % sizes.Length];
        ClearSession();
    }

    public void CycleWorldType()
    {
        var types = Enum.GetValues<WorldType>();
        WorldType = types[(Array.IndexOf(types, WorldType) + 1) % types.Length];
        ClearSession();
    }

    public void SetSeedText(string value)
    {
        SeedText = value;
        ClearSession();
    }

    // Metadata changes require a fresh context, so stale visible data is discarded immediately.
    private void ClearSession()
    {
        Context = null;
        Grid = null;
        SelectedCell = null;
        Notice = "CONFIGURACION CAMBIADA. CREE UNA SESION.";
    }

    public bool CreateSession()
    {
        if (!long.TryParse(SeedText, NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out long seed))
        {
            Notice = "SEMILLA INVALIDA: ENTERO DE 64 BITS.";
            return false;
        }
        Context = orchestrator.CreateGeneration(new GenerationRequest(seed, new WorldGenerationSettings(WorldType, Size)));
        Grid = null;
        SelectedCell = null;
        Notice = "SESION CREADA. GRID PENDIENTE.";
        return true;
    }

    public bool RunGrid()
    {
        if (!CanRunGrid)
        {
            Notice = Context is null ? "PRIMERO CREE UNA SESION." : "SOLO GRID ESTA DISPONIBLE.";
            return false;
        }
        var progress = orchestrator.RunNext(Context!);
        if (progress.GetStatus(GenerationStage.Grid) != GenerationStageStatus.Completed)
        {
            Notice = progress.GetFailure(GenerationStage.Grid) ?? "GRID NO COMPLETADA.";
            return false;
        }
        var data = Context!.GetLayerData(GenerationStage.Grid) as LayerData<HexGridDescriptor>;
        if (data is null || data.Count != 1 || data[0].Size != Context.Request.Settings.MapSize)
            throw new InvalidOperationException("Published Grid does not match the immutable request.");
        Grid = data[0].ToGridData();
        Notice = "GRID LISTA. CLIC: SELECCION. RUEDA: ZOOM. ARRASTRE DERECHO: MOVER.";
        return true;
    }

    public void Select(int? cellId)
    {
        if (cellId is not null) _ = Grid?.GetOffset(cellId.Value) ?? throw new InvalidOperationException("No Grid to select.");
        SelectedCell = cellId;
    }

    // Capability guards satisfy the fixed registry but do not simulate any unimplemented generator.
    // The UI never advances these stages; an accidental call fails instead of publishing fake results.
    private sealed class UnavailableLayer(GenerationStage stage) : IWorldGenerationLayer
    {
        public GenerationStage Stage => stage;
        public LayerExecutionResult Execute(GenerationLayerInputs inputs) =>
            throw new NotSupportedException($"Generation stage {Stage} is not implemented.");
    }
}
