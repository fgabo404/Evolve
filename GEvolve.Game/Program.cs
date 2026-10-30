using System.Numerics;
using System.Diagnostics;
using GEvolve.Definitions.WorldGeneration.Configuration;
using GEvolve.Definitions.Versioning;
using GEvolve.Engine.Grid;
using GEvolve.Engine.World;
using GEvolve.Engine.World.LandmassGeneration;
using GEvolve.Engine.World.BiomeGeneration;
using GEvolve.State.Game;
using GEvolve.Game.Graphics;
using GEvolve.Game.Screens;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;
using OpenTK.Windowing.GraphicsLibraryFramework;
using Vector2i = OpenTK.Mathematics.Vector2i;

namespace GEvolve.Game;

internal sealed class EvolveWindow : GameWindow
{
    private static readonly Vector4 White = new(1, 1, 1, 1);
    private static readonly Vector4 Muted = new(.65f, .76f, .83f, 1);
    private static readonly Vector4 Accent = new(.38f, .76f, .89f, 1);
    private readonly GameSession _session = new();
    private readonly MapCamera2D _camera = new();
    private readonly Dictionary<UiAction, float> _hover = [];
    private UiRenderer? _ui;
    private HexGridRenderer? _gridRenderer;
    private UiAction? _pressed;
    private bool _dragging;
    private float _dragDistance;
    private Vector2 _lastPointer;
    private bool _dirty = true;
    private readonly bool _graphicsSmoke;
    private Task<GeneratedWorld>? _pendingGeneration;
    private bool _seedFocused;
    // A real, overlayed dropdown instead of four always-visible buttons.
    private UiAction? _openDropdown;
    private int _smokeScenario = -1;
    private bool _smokeZoomed;
    private MapViewMode _viewMode = MapViewMode.Biomes;
    private int _smokeViewStep;
    private long _generationStarted;
    private double _rendererPreparationMilliseconds;
    private static readonly (GridSize Size, LandmassType Type, int Seed)[] SmokeCases =
    [
        (GridSize.Small, LandmassType.Continents, 2026),
        (GridSize.Medium, LandmassType.Continents, 2026),
        (GridSize.Large, LandmassType.Continents, 2026),
        (GridSize.Large, LandmassType.Pangaea, 2026),
        (GridSize.Small, LandmassType.Pangaea, 42),
        (GridSize.Medium, LandmassType.Pangaea, -731),
        (GridSize.Large, LandmassType.SmallContinents, 2026),
        (GridSize.Large, LandmassType.Archipelagos, 2026),
        (GridSize.Enormous, LandmassType.Pangaea, 2026)
    ];

    public EvolveWindow(bool graphicsSmoke = false) : base(GameWindowSettings.Default, new NativeWindowSettings
    {
        ClientSize = new Vector2i(1280, 720),
        Title = "Evolve",
        APIVersion = new Version(3, 3),
        Profile = ContextProfile.Core
    }) { _graphicsSmoke = graphicsSmoke; }

    protected override void OnLoad()
    {
        base.OnLoad();
        GL.Enable(EnableCap.Blend);
        GL.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);
        _ui = new UiRenderer();
    }

    protected override void OnResize(ResizeEventArgs e)
    {
        base.OnResize(e);
        if (_session.Grid is { } grid)
            _camera.Fit(grid.Definition.Columns, grid.Definition.Rows, ClientSize.X, ClientSize.Y);
        _dirty = true;
    }

    protected override void OnUpdateFrame(FrameEventArgs args)
    {
        base.OnUpdateFrame(args);
        if (_pendingGeneration is { IsCompleted: true } completed)
        {
            _pendingGeneration = null;
            if (_session.Screen == GameScreen.Generating)
            {
                try
                {
                    GeneratedWorld result = completed.GetAwaiter().GetResult();
                    _session.CompleteGeneration(result);
                    var preparation = Stopwatch.StartNew();
                    _gridRenderer?.Dispose();
                    _gridRenderer = new HexGridRenderer(result.Grid, result.Landmass);
                    _viewMode = result.Landmass.TectonicPreviewOnly
                        ? MapViewMode.Tectonic : MapViewMode.Biomes;
                    preparation.Stop();
                    _rendererPreparationMilliseconds = preparation.Elapsed.TotalMilliseconds;
                    _camera.Fit(result.Grid.Definition.Columns, result.Grid.Definition.Rows,
                        ClientSize.X, ClientSize.Y);
                    if (_graphicsSmoke)
                        Console.WriteLine($"Generate {result.Grid.Definition.Size} " +
                            $"{result.Landmass.Configuration.Type}: " +
                            $"{Stopwatch.GetElapsedTime(_generationStarted).TotalMilliseconds:F1} ms total, " +
                            $"{_rendererPreparationMilliseconds:F1} ms GPU preparation, " +
                            $"{result.Statistics.LandFraction:P1} land, " +
                            $"{result.Statistics.ComponentCount} components");
                }
                catch (Exception error)
                {
                    Console.Error.WriteLine(error);
                    _gridRenderer?.Dispose();
                    _gridRenderer = null;
                    _session.GenerationFailed("NO SE PUDO GENERAR EL MUNDO");
                }
            }
            else if (completed.IsFaulted) _ = completed.Exception;
            _dirty = true;
        }
        var mouse = MousePosition;
        UiAction? hovered = Hit(mouse.X, mouse.Y);
        foreach (var button in VisibleControls())
        {
            _hover.TryGetValue(button.Action, out float value);
            float target = hovered == button.Action ? 1 : 0;
            float next = value + (target - value) *
                (1 - MathF.Exp(-10f * (float)args.Time));
            if (MathF.Abs(next - target) < .005f) next = target;
            if (next != value) { _hover[button.Action] = next; _dirty = true; }
        }
    }

    protected override void OnRenderFrame(FrameEventArgs args)
    {
        base.OnRenderFrame(args);
        if (!_dirty || _ui is null) return;
        _dirty = false;
        long started = Stopwatch.GetTimestamp();
        GL.Viewport(0, 0, FramebufferSize.X, FramebufferSize.Y);
        GL.ClearColor(.96f, .98f, .99f, 1f);
        GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);
        _ui.Clear();
        if (_session.Screen == GameScreen.World) RenderWorld();
        else RenderMenu();
        _ui.Flush(ClientSize.X, ClientSize.Y);
        if (_graphicsSmoke && _session.Screen == GameScreen.World && !_smokeZoomed)
            VerifyBiomePixels();
        SwapBuffers();
        if (_graphicsSmoke) AdvanceGraphicsSmoke(started);
    }

    private void VerifyBiomePixels()
    {
        var terrain = _session.State.Landmass!;
        var checkedBiomes = new HashSet<string>(StringComparer.Ordinal);
        var visible = _camera.VisibleRange();
        var pixels = new byte[4];
        int columns = _session.Grid!.Definition.Columns;
        for (int row = visible.FirstRow; row <= visible.LastRow; row++)
        for (int column = visible.FirstColumn; column <= visible.LastColumn; column++)
        {
            int id = row * columns + ((column % columns) + columns) % columns;
            if (_session.SelectedCell?.Id == id) continue; // selection overlay differs from terrain pixels
            string biome = terrain.Biomes?.Get(id).BiomeId ?? (terrain.IsLand(id) ? "land" : "ocean");
            if (_viewMode == MapViewMode.Elevation && terrain.ElevationByCellId is { } levels)
                biome = $"height {levels[id]}";
            if (terrain.IsImpassableSummit(id)) biome = "impassable summit";
            if (_viewMode == MapViewMode.Infrared)
                biome = $"thermal {terrain.Climate!.CurrentCelsius(id):F0}";
            if (_viewMode == MapViewMode.Rainfall)
                biome = $"rainfall {terrain.Climate!.BasePrecipitationMmByCellId![id]}";
            if (_viewMode == MapViewMode.Tectonic && terrain.PlateByCellId is { } plates)
                biome = $"plate {plates[id]}";
            if (checkedBiomes.Contains(biome)) continue;
            Vector2 screen = _camera.WorldToScreen(FlatHexGeometry.Center(new(column, row)));
            if (_viewMode == MapViewMode.Tectonic && terrain.TectonicMotion is { } tectonic)
            {
                float period = FlatHexGeometry.PeriodWidth(columns) * _camera.Scale;
                bool underArrow = tectonic.ArrowCellByPlateId.Any(anchor =>
                {
                    Vector2 point = _camera.WorldToScreen(FlatHexGeometry.Center(new(anchor % columns, anchor / columns)));
                    float dx = screen.X - point.X;
                    dx -= MathF.Round(dx / period) * period;
                    return dx * dx + MathF.Pow(screen.Y - point.Y, 2) < 45 * 45;
                });
                if (underArrow) continue; // arrow overlay is intentionally not the plate fill
            }
            // Evitar las tarjetas, los botones y el texto de la interfaz.
            if (screen.X < 310 || screen.X > ClientSize.X - 20 ||
                screen.Y < 150 || screen.Y > ClientSize.Y - 170) continue;
            int x = (int)(screen.X * FramebufferSize.X / ClientSize.X);
            int y = FramebufferSize.Y - 1 - (int)(screen.Y * FramebufferSize.Y / ClientSize.Y);
            GL.ReadPixels(x, y, 1, 1, PixelFormat.Rgba, PixelType.UnsignedByte, pixels);
            if (_viewMode is MapViewMode.Biomes or MapViewMode.Elevation &&
                terrain.IsImpassableSummit(id) &&
                (pixels[0] != 255 || pixels[1] != 255 || pixels[2] != 255))
                throw new InvalidOperationException($"Summit {id} is not pure white.");
            Vector3 expected = MapViewColors.ForCell(terrain, id, _viewMode) * 255;
            if (Math.Abs(pixels[0] - expected.X) > 2 || Math.Abs(pixels[1] - expected.Y) > 2 ||
                Math.Abs(pixels[2] - expected.Z) > 2)
                throw new InvalidOperationException($"Rendered biome color mismatch: {biome}, cell {id}.");
            checkedBiomes.Add(biome);
        }
        if (checkedBiomes.Count < 2)
            throw new InvalidOperationException("Insufficient visible biome colors in graphics smoke.");
        Console.WriteLine($"OpenGL {(_viewMode.ToString())} pixels verified: {string.Join(", ", checkedBiomes)}");
    }

    private void AdvanceGraphicsSmoke(long started)
    {
        GL.Finish();
        var error = GL.GetError();
        if (error != OpenTK.Graphics.OpenGL4.ErrorCode.NoError)
            throw new InvalidOperationException($"OpenGL smoke test failed: {error}");
        if (_session.Screen == GameScreen.World)
            Console.WriteLine($"OpenGL {_session.Grid!.Definition.Size} " +
                $"{_session.State.Landmass!.Configuration.Type}: " +
                $"{_gridRenderer!.VisibleInstanceCount} visible instances, " +
                $"{Stopwatch.GetElapsedTime(started).TotalMilliseconds:F1} ms frame");
        if (_session.Screen == GameScreen.MainMenu && _smokeScenario == -1)
        {
            _smokeScenario = 0;
            Activate(UiAction.Create);
            return;
        }
        if (_session.Screen == GameScreen.CreateWorld)
        {
            if (_session.Error is { } generationError)
                throw new InvalidOperationException($"OpenGL smoke generation failed: {generationError}");
            var item = SmokeCases[_smokeScenario];
            _session.ChooseSize(item.Size);
            _session.ChooseType(item.Type);
            _session.SetSeedText(item.Seed.ToString(System.Globalization.CultureInfo.InvariantCulture));
            StartGeneration();
            return;
        }
        if (_session.Screen != GameScreen.World) return;
        UiAction[] viewActions =
        [
            UiAction.ToggleTectonic, UiAction.ToggleSurface,
            UiAction.ToggleGrayscale, UiAction.ToggleSurface,
            UiAction.ToggleTectonic, UiAction.StressCompression, UiAction.StressExtension,
            UiAction.StressShear, UiAction.ToggleGeology, UiAction.ToggleSurface
        ];
        if (_smokeViewStep < viewActions.Length)
        {
            var terrain = _session.State.Landmass;
            if (_smokeViewStep == 0)
                _session.SelectAt(_camera.ScreenToWorld(new Vector2(ClientSize.X * .6f, ClientSize.Y * .55f)));
            var beforeClimate = terrain?.Climate?.BaseTemperatureTenthsByCellId.ToArray();
            var selected = _session.SelectedCell;
            int cameraVersion = _camera.Version;
            var action = viewActions[_smokeViewStep];
            var expectedMode = action == UiAction.StressCompression ? MapViewMode.Compression
                : action == UiAction.StressExtension ? MapViewMode.Extension
                : action == UiAction.StressShear ? MapViewMode.Shear
                : action == UiAction.ToggleGeology ? MapViewMode.Geology
                : action == UiAction.ToggleSurface
                ? MapViewMode.Biomes
                : action == UiAction.ToggleTectonic &&
                terrain?.PlateByCellId is not null
                ? MapViewMode.Tectonic
                : action == UiAction.ToggleRainfall &&
                terrain?.Climate?.BasePrecipitationMmByCellId is not null
                ? _viewMode == MapViewMode.Rainfall ? MapViewMode.Biomes : MapViewMode.Rainfall
                : action == UiAction.ToggleInfrared && terrain?.Climate is not null
                ? _viewMode == MapViewMode.Infrared ? MapViewMode.Biomes : MapViewMode.Infrared
                : action == UiAction.ToggleGrayscale && terrain?.ElevationByCellId is not null
                    ? _viewMode == MapViewMode.Elevation ? MapViewMode.Biomes : MapViewMode.Elevation
                    : _viewMode;
            var button = ScreenLayout.Buttons(GameScreen.World, ClientSize.X, ClientSize.Y)
                .Single(item => item.Action == action);
            if (Hit(button.Bounds.X + 5, button.Bounds.Y + 5) != action)
                throw new InvalidOperationException("View button hit testing failed.");
            Activate(action);
            if (_viewMode != expectedMode)
                throw new InvalidOperationException("View button did not select the expected mode.");
            if (!ReferenceEquals(terrain, _session.State.Landmass) ||
                selected != _session.SelectedCell || cameraVersion != _camera.Version ||
                _pendingGeneration is not null)
                throw new InvalidOperationException("Changing map view altered the world or camera.");
            if (beforeClimate is not null && !beforeClimate.SequenceEqual(
                terrain!.Climate!.BaseTemperatureTenthsByCellId))
                throw new InvalidOperationException("Changing map view altered climate.");
            _smokeViewStep++;
            return;
        }
        if (_smokeScenario < SmokeCases.Length - 1)
        {
            _smokeViewStep = 0;
            ReturnToMenu();
            _smokeScenario++;
            Activate(UiAction.Create);
        }
        else if (!_smokeZoomed)
        {
            _camera.Zoom(8, new Vector2(ClientSize.X / 2f, ClientSize.Y / 2f));
            _camera.Pan(120, 20);
            _session.SelectAt(_camera.ScreenToWorld(
                new Vector2(ClientSize.X / 2f, ClientSize.Y / 2f)));
            if (_session.SelectedCell is null)
                throw new InvalidOperationException("OpenGL smoke selection failed.");
            _smokeZoomed = true;
            _dirty = true;
        }
        else Close();
    }

    private void RenderMenu()
    {
        if (_ui is null) return;
        float w = ClientSize.X, h = ClientSize.Y;
        _ui.Rect(0, 0, w, h, new Vector4(.055f, .10f, .16f, 1));
        _ui.Rect(0, h * .72f, w, h * .28f, new Vector4(.075f, .16f, .21f, 1));
        _ui.Rect(0, h * .72f, w, 2, new Vector4(.24f, .42f, .48f, 1));
        float center = w / 2;
        float titlePixel = Math.Clamp(w / 145f, 5, 9);
        if (_session.Screen == GameScreen.MainMenu)
        {
            float versionPixel = Math.Clamp(w / 205f, 4, 6.5f);
            _ui.Text($"{EvolveVersion.GameName} {EvolveVersion.Display}",
                center, h * .17f, versionPixel, White);
            _ui.Text("Civilization Simulator", center, h * .17f + versionPixel * 9,
                Math.Clamp(w / 480f, 1.5f, 2.5f), Muted);
        }
        else if (_session.Screen == GameScreen.CreateWorld)
        {
            _ui.Text("Crear nuevo mundo", center, MathF.Max(32, h * .10f),
                Math.Clamp(w / 420f, 2, 3.2f), White);
            _ui.Text("Elige el tamaño del grid", center, MathF.Max(66, h * .16f),
                1.8f, Muted);
            var buttons = ScreenLayout.Buttons(GameScreen.CreateWorld, ClientSize.X, ClientSize.Y);
            var size = buttons.First(button => button.Action == UiAction.SizeSelect).Bounds;
            var type = buttons.First(button => button.Action == UiAction.TypeSelect).Bounds;
            var seed = buttons.First(button => button.Action == UiAction.SeedField).Bounds;
            _ui.Text("Tamaño", size.X, size.Y - 24, 1.8f, Muted, centered: false);
            _ui.Text("Tipo de mundo", type.X, type.Y - 24, 1.8f, Muted, centered: false);
            // Both dropdowns choose only the size/type of the next world;
            // stage 01 plates continue to depend exclusively on size and seed.
            _ui.Text("Semilla", seed.X, seed.Y - 24, 1.8f, Muted, centered: false);
            _ui.Text(EvolveVersion.Display, 16, h - 27, 1.7f, Muted, centered: false);
        }
        else if (_session.Screen == GameScreen.Generating)
        {
            _ui.Text("Generando mundo", center, h * .39f, 4, White);
            _ui.Text("Distribuyendo tierra y océanos", center, h * .52f, 2, Muted);
        }
        else
        {
            _ui.Text("Opciones", center, h * .24f, 5, White);
            _ui.Text("Opciones avanzadas pendientes", center, h * .42f, 2, Muted);
        }
        DrawButtons();
        if (_session.Screen == GameScreen.CreateWorld && _session.Error is { } error)
            _ui.Text(error, center, h - 32, 1.6f, new Vector4(1, .61f, .59f, 1));
    }

    private void RenderWorld()
    {
        if (_ui is null || _gridRenderer is null || _session.Grid is not { } grid) return;
        int scissorHeight = Math.Max(0,
            (int)(FramebufferSize.Y * (ClientSize.Y - MapCamera2D.TopBarHeight) /
                (float)Math.Max(ClientSize.Y, 1)));
        GL.Enable(EnableCap.ScissorTest);
        GL.Scissor(0, 0, FramebufferSize.X, scissorHeight);
        _gridRenderer.Draw(_camera, ClientSize.X, ClientSize.Y, _viewMode);
        GL.Disable(EnableCap.ScissorTest);
        if (_viewMode == MapViewMode.Tectonic)
        {
            _gridRenderer.DrawPlateBoundaries(_ui, _camera);
            _gridRenderer.DrawPlateMotions(_ui, _camera);
        }

        if (_session.SelectedCell is { } selected)
        {
            float right = _camera.X + _camera.ViewWidth / _camera.Scale;
            int first = (int)MathF.Floor((_camera.X - 2) / 1.5f);
            int last = (int)MathF.Ceiling((right + 1) / 1.5f);
            for (int column = first; column <= last; column++)
            {
                if (((column % grid.Definition.Columns) + grid.Definition.Columns) %
                    grid.Definition.Columns != selected.Address.Column) continue;
                Vector2 world = FlatHexGeometry.Center(new GridAddress(column, selected.Address.Row));
                Vector2 screen = _camera.WorldToScreen(world);
                if (screen.Y >= MapCamera2D.TopBarHeight && screen.Y <= ClientSize.Y)
                    _ui.Hex(screen.X, screen.Y, _camera.Scale,
                        new Vector4(.54f, .79f, .89f, .9f),
                        new Vector4(.15f, .43f, .59f, 1));
            }
        }

        _ui.Rect(0, 0, ClientSize.X, MapCamera2D.TopBarHeight,
            new Vector4(.06f, .13f, .20f, 1));
        _ui.Rect(0, MapCamera2D.TopBarHeight - 2, ClientSize.X, 2,
            new Vector4(.28f, .54f, .63f, 1));
        _ui.Text($"EVOLVE {EvolveVersion.Display}", 18, 19, 2.4f, White,
            centered: false);
        string size = grid.Definition.Size switch
        {
            GridSize.Small => "PEQUEÑO", GridSize.Medium => "MEDIANO",
            GridSize.Large => "GRANDE", _ => "ENORME"
        };
        if (ClientSize.X >= 600)
            _ui.Text($"{size}  {grid.Definition.Columns} X {grid.Definition.Rows}",
                ClientSize.X / 2f, 22, 1.8f, Muted);
        DrawButtons();
        // Etiquetas de altura solo en zoom: evita miles de textos a escala global.
        if (_session.State.Landmass?.ElevationByCellId is { } elevations &&
            _camera.Scale >= 13f)
        {
            var visible = _camera.VisibleRange();
            int cols = grid.Definition.Columns;
            for(int row=visible.FirstRow;row<=visible.LastRow;row++)
            for(int col=visible.FirstColumn;col<=visible.LastColumn;col++)
            {
                int wrapped=((col%cols)+cols)%cols;
                var center=FlatHexGeometry.Center(new GridAddress(col,row));
                Vector2 position=_camera.WorldToScreen(center);
                if(position.Y < MapCamera2D.TopBarHeight+10 || position.Y > ClientSize.Y-10 ||
                    position.X<10 || position.X>ClientSize.X-10)continue;
                _ui.Text(elevations[row*cols+wrapped].ToString(),position.X,position.Y-5,
                    1.3f, _viewMode != MapViewMode.Infrared &&
                        _session.State.Landmass.IsImpassableSummit(row * cols + wrapped)
                        ? new Vector4(.08f, .08f, .08f, 1) : White);
            }
        }
        if (_session.State.Landmass is { } worldInfo)
        {
            string kind = worldInfo.TectonicPreviewOnly ? "PLACAS TECTÓNICAS" :
                worldInfo.Configuration.Type switch
                {
                    LandmassType.Pangaea => "PANGEA",
                    LandmassType.Continents => "CONTINENTES",
                    LandmassType.SmallContinents => "CONTINENTES PEQUEÑOS",
                    _ => "ARCHIPIÉLAGOS"
                };
            float infoWidth = MathF.Min(270, ClientSize.X - 24);
            _ui.Rect(12, MapCamera2D.TopBarHeight + 12, infoWidth, 63,
                new Vector4(.06f, .13f, .20f, .91f));
            _ui.Text($"CAPA: {kind}", 22, MapCamera2D.TopBarHeight + 22,
                1.6f, White, centered: false);
            _ui.Text($"SEMILLA: {worldInfo.Configuration.Seed}",
                22, MapCamera2D.TopBarHeight + 48, 1.8f, Accent, centered: false);
        }
        if (_session.SelectedCell is { } cell && _session.State.Landmass is { } landmass)
        {
            if (landmass.TectonicPreviewOnly)
            {
                var boundaries = _gridRenderer.PlateBoundaries!;
                string line1 = $"CELDA {cell.Id}  C:{cell.Address.Column}  F:{cell.Address.Row}";
                string line2 = $"PLACA: {landmass.PlateByCellId![cell.Id]}  LÍMITE: " +
                    (boundaries.BoundaryByCellId[cell.Id] ? "SÍ" : "NO") +
                    $"  DIST: {boundaries.DistanceToBoundaryByCellId[cell.Id]} HEX";
                line2 += MotionLabel(landmass, cell.Id);
                float pixel = MathF.Min(1.6f, (ClientSize.X - 48) /
                    (6f * Math.Max(line1.Length, line2.Length)));
                float panelWidth = Math.Max(line1.Length, line2.Length) * 6 * pixel + 20;
                _ui.Rect(12, ClientSize.Y - 66, panelWidth, 56,
                    new Vector4(.07f, .16f, .23f, .9f));
                _ui.Text(line1, 22, ClientSize.Y - 57, pixel, White, centered: false);
                _ui.Text(line2, 22, ClientSize.Y - 35, pixel, White, centered: false);
            }
            else
            {
            string surface = landmass.IsLand(cell.Id) ? "TIERRA" : "OCÉANO";
            string label = $"CELDA {cell.Id}  C:{cell.Address.Column}  F:{cell.Address.Row}  {surface}";
            var environment = BiomeClassifier.GetCell(landmass, cell.Id);
            string height = environment.Elevation?.ToString() ?? "PENDIENTE";
            string details = $"ALT: {height}  LAT: {environment.Latitude:F1}";
            details += $"  PEND: {environment.Slope?.ToString() ?? "PENDIENTE"} NIV/HEX";
            if (environment.IsImpassableSummit) details += "  CUMBRE INFRANQUEABLE";
            if (landmass.GeologicFeaturesByCellId is not null)
            {
                string featureLabel = landmass.FeatureAt(cell.Id) switch
                {
                    GeologicFeature.MountainRange => "CORDILLERA",
                    GeologicFeature.RiftValley => "VALLE RIFT",
                    GeologicFeature.FaultValley => "FALLA TRANSFORMANTE",
                    GeologicFeature.OceanTrench => "FOSA OCEÁNICA",
                    GeologicFeature.MidOceanRidge => "DORSAL OCEÁNICA",
                    GeologicFeature.OceanTransformFault => "FALLA OCEÁNICA",
                    _ => "SIN ACCIDENTE"
                };
                details += $"  GEO: {featureLabel}";
                if (!landmass.IsLand(cell.Id) && landmass.OceanFloorDepthByCellId is { } depths)
                    details += $"  PROF. RELATIVA: {depths[cell.Id]}/100";
            }
            string temperature = landmass.Climate is { } climate
                ? FormattableString.Invariant($"TEMPERATURA: {climate.CurrentCelsius(cell.Id):F1} °C")
                : "TEMPERATURA: PENDIENTE";
            string rainfall = landmass.Climate?.BasePrecipitationMmByCellId is { } rain
                ? $"LLUVIA: {rain[cell.Id]} MM/AÑO  HUMEDAD: {landmass.Climate.BaseMoistureByCellId![cell.Id]}/100"
                : "LLUVIA: PENDIENTE";
            string biome = environment.Biome.Status switch
            {
                BiomeClassificationStatus.Assigned =>
                    $"{environment.Biome.BiomeId} / {environment.Biome.SubbiomeId}",
                BiomeClassificationStatus.MissingElevation => "BIOMA: ELEVACIÓN PENDIENTE",
                BiomeClassificationStatus.NotGenerated => "BIOMA: CLASIFICACIÓN PENDIENTE",
                _ => "BIOMA: SIN COINCIDENCIA EN CATÁLOGO"
            };
            string? plateInfo = landmass.PlateByCellId is { } plateIds &&
                _gridRenderer.PlateBoundaries is { } boundaries
                ? $"PLACA: {plateIds[cell.Id]}  LÍMITE: {(boundaries.BoundaryByCellId[cell.Id] ? "SÍ" : "NO")}  DIST: {boundaries.DistanceToBoundaryByCellId[cell.Id]} HEX"
                : null;
            if (plateInfo is not null) plateInfo += MotionLabel(landmass, cell.Id);
            string[] lines = plateInfo is null
                ? [label, details, biome, temperature, rainfall]
                : [label, details, biome, temperature, rainfall, plateInfo];
            int maxLength = lines.Max(line => line.Length);
            float pixel = MathF.Min(1.6f, (ClientSize.X - 48) / (6f * maxLength));
            float labelWidth = maxLength * 6 * pixel;
            int panelHeight = lines.Length * 20 + 11;
            _ui.Rect(12, ClientSize.Y - panelHeight - 12, labelWidth + 20, panelHeight,
                new Vector4(.07f, .16f, .23f, .9f));
            for (int index = 0; index < lines.Length; index++)
                _ui.Text(lines[index], 22, ClientSize.Y - panelHeight - 3 + 20 * index,
                    pixel, White, centered: false);
            }
        }
        DrawDropdown(); // Last overlay: above height labels, selected cells and buttons.
    }

    private static string MotionLabel(LandmassState terrain, int id)
    {
        if (terrain.TectonicMotion is not { } motion || terrain.PlateByCellId is not { } plates) return "";
        var plate = motion.Motions[plates[id]];
        return FormattableString.Invariant($"  DIR: {plate.DirectionDegrees:F1}°  V: {plate.SpeedCmPerYear:F2} CM/AÑO");
    }

    private void DrawButtons()
    {
        if (_ui is null) return;
        foreach (var button in ScreenLayout.Buttons(_session.Screen, ClientSize.X, ClientSize.Y))
        {
            var r = button.Bounds;
            _hover.TryGetValue(button.Action, out float hover);
            bool sizeAction = button.Action is UiAction.Small or UiAction.Medium or UiAction.Large or UiAction.Enormous;
            bool typeAction = button.Action is UiAction.Continents or UiAction.Pangaea or
                UiAction.SmallContinents or UiAction.Archipelagos;
            bool selected = sizeAction && ScreenLayout.SizeFor(button.Action) == _session.SelectedSize ||
                typeAction && ScreenLayout.TypeFor(button.Action) == _session.SelectedType ||
                button.Action == _openDropdown ||
                button.Action == UiAction.SeedField && _seedFocused ||
                button.Action == UiAction.ToggleSurface && _viewMode == MapViewMode.Biomes ||
                button.Action == UiAction.ToggleGrayscale && _viewMode == MapViewMode.Elevation ||
                button.Action == UiAction.ToggleInfrared && _viewMode == MapViewMode.Infrared ||
                button.Action == UiAction.ToggleRainfall && _viewMode == MapViewMode.Rainfall ||
                button.Action == UiAction.ToggleTectonic && _viewMode == MapViewMode.Tectonic ||
                button.Action == UiAction.StressCompression && _viewMode == MapViewMode.Compression ||
                button.Action == UiAction.StressExtension && _viewMode == MapViewMode.Extension ||
                button.Action == UiAction.StressShear && _viewMode == MapViewMode.Shear ||
                button.Action == UiAction.ToggleGeology && _viewMode == MapViewMode.Geology;
            _ui.Rect(r.X + 3, r.Y + 4, r.Width, r.Height,
                new Vector4(.02f, .05f, .09f, .7f));
            Vector4 baseColor = button.Action == UiAction.Generate
                ? new(.12f, .35f, .45f, 1) : new(.16f, .24f, .31f, 1);
            Vector4 color = Vector4.Lerp(baseColor, new Vector4(.27f, .44f, .52f, 1), hover);
            _ui.Rect(r.X, r.Y, r.Width, r.Height, color);
            _ui.Border(r.X, r.Y, r.Width, r.Height, selected ? 2.5f : 1.5f,
                selected ? Accent : new Vector4(.37f, .51f, .59f, 1));
            if (selected || hover > .05f)
                _ui.Rect(r.X, r.Y, 4, r.Height, Accent);
            if (button.Action is UiAction.SizeSelect or UiAction.TypeSelect)
            {
                string choice = button.Action == UiAction.SizeSelect
                    ? $"TAMAÑO: {ScreenLayout.SizeLabel(_session.SelectedSize)}"
                    : $"MUNDO: {ScreenLayout.TypeLabel(_session.SelectedType)}";
                float pixel = MathF.Min(1.9f, (r.Width - 48) / (choice.Length * 6f));
                _ui.Text(choice, r.X + 12, r.Y + (r.Height - 7 * pixel) / 2,
                    pixel, White, centered: false);
                _ui.Text(_openDropdown == button.Action ? "^" : "V",
                    r.X + r.Width - 18, r.Y + (r.Height - 14) / 2, 2, Accent);
            }
            else if (button.Action == UiAction.SeedField)
            {
                const float pixel = 2.2f;
                _ui.Text(_session.SeedText, r.X + 13,
                    r.Y + (r.Height - 7 * pixel) / 2, pixel, White, centered: false);
                if (_seedFocused)
                    _ui.Rect(r.X + 13 + _session.SeedText.Length * 6 * pixel,
                        r.Y + 9, 2, r.Height - 18, Accent);
            }
            else
            {
                float pixel = _session.Screen == GameScreen.World ? 1.9f :
                    typeAction || button.Action == UiAction.RandomSeed ?
                        MathF.Min(2.1f, (r.Width - 16) / (button.Text.Length * 6f)) : 2.5f;
                string text = button.Action == UiAction.ToggleGrayscale
                    ? _session.State.Landmass?.ElevationByCellId is null ? "Alturas pendientes"
                        : _viewMode == MapViewMode.Elevation ? "Ver colores" : button.Text
                    : button.Action == UiAction.ToggleInfrared
                        ? _session.State.Landmass?.Climate is null ? "CLIMA PENDIENTE"
                            : _viewMode == MapViewMode.Infrared ? "VER BIOMAS" : button.Text
                        : button.Text;
                _ui.Text(text, r.X + r.Width / 2,
                    r.Y + (r.Height - 7 * pixel) / 2, pixel, White);
            }
        }

        if (_session.Screen == GameScreen.CreateWorld) DrawDropdown();
    }

    private void DrawDropdown()
    {
        if (_ui is null) return;
        if (_openDropdown is not { } selector) return;
        foreach (var option in ScreenLayout.DropdownOptions(
            _session.Screen, ClientSize.X, ClientSize.Y, selector))
        {
            UiRect r = option.Bounds;
            bool chosen = selector == UiAction.SizeSelect
                ? ScreenLayout.SizeFor(option.Action) == _session.SelectedSize
                : ScreenLayout.TypeFor(option.Action) == _session.SelectedType;
            _hover.TryGetValue(option.Action, out float hover);
            Vector4 baseColor = chosen
                ? new Vector4(.17f, .38f, .46f, 1)
                : new Vector4(.12f, .22f, .29f, 1);
            _ui.Rect(r.X + 3, r.Y + 4, r.Width, r.Height,
                new Vector4(.02f, .05f, .09f, .9f));
            _ui.Rect(r.X, r.Y, r.Width, r.Height,
                Vector4.Lerp(baseColor, new Vector4(.26f, .45f, .53f, 1), hover));
            _ui.Border(r.X, r.Y, r.Width, r.Height, 1.5f,
                chosen ? Accent : new Vector4(.37f, .51f, .59f, 1));
            string label = option.Text;
            if (selector == UiAction.SizeSelect)
            {
                var definition = HexGridDefinitions.Get(ScreenLayout.SizeFor(option.Action));
                label += $"  {definition.Columns} X {definition.Rows}";
            }
            float px = MathF.Min(1.9f, (r.Width - 38) / (label.Length * 6f));
            _ui.Text(label, r.X + 12, r.Y + (r.Height - 7 * px) / 2,
                px, White, centered: false);
            if (chosen)
                _ui.Text("V", r.X + r.Width - 16,
                    r.Y + (r.Height - 14) / 2, 2, Accent);
        }
    }

    private IEnumerable<UiButton> VisibleControls()
    {
        foreach (var button in ScreenLayout.Buttons(_session.Screen, ClientSize.X, ClientSize.Y))
            yield return button;
        if (_openDropdown is not { } selector) yield break;
        foreach (var option in ScreenLayout.DropdownOptions(
            _session.Screen, ClientSize.X, ClientSize.Y, selector))
            yield return option;
    }

    private UiAction? Hit(float x, float y)
    {
        // The open dropdown overlays the map and any controls below it.
        if (_openDropdown is { } selector)
            foreach (var option in ScreenLayout.DropdownOptions(
                _session.Screen, ClientSize.X, ClientSize.Y, selector))
                if (option.Bounds.Contains(x, y)) return option.Action;
        foreach (var button in ScreenLayout.Buttons(_session.Screen, ClientSize.X, ClientSize.Y))
            if (button.Bounds.Contains(x, y)) return button.Action;
        return null;
    }

    protected override void OnMouseDown(MouseButtonEventArgs e)
    {
        base.OnMouseDown(e);
        if (e.Button != MouseButton.Left) return;
        Vector2 position = new(MousePosition.X, MousePosition.Y);
        _pressed = Hit(position.X, position.Y);
        if (_openDropdown is not null &&
            _pressed is not (UiAction.SizeSelect or UiAction.TypeSelect or
                UiAction.Small or UiAction.Medium or UiAction.Large or UiAction.Enormous or
                UiAction.Continents or UiAction.Pangaea or UiAction.SmallContinents or
                UiAction.Archipelagos))
        {
            _openDropdown = null;
            _pressed = null;
            _hover.Clear();
            _dirty = true;
            return; // Dismiss outside click; do not pan or select a world hex.
        }
        if (_session.Screen == GameScreen.World &&
            position.Y >= MapCamera2D.TopBarHeight && _pressed is null)
        {
            _dragging = true;
            _dragDistance = 0;
            _lastPointer = position;
        }
    }

    protected override void OnMouseMove(MouseMoveEventArgs e)
    {
        base.OnMouseMove(e);
        if (!_dragging) return;
        Vector2 next = new(e.Position.X, e.Position.Y);
        Vector2 delta = next - _lastPointer;
        _dragDistance += delta.Length();
        _lastPointer = next;
        _camera.Pan(delta.X, delta.Y);
        _dirty = true;
    }

    protected override void OnMouseUp(MouseButtonEventArgs e)
    {
        base.OnMouseUp(e);
        if (e.Button != MouseButton.Left) return;
        Vector2 position = new(MousePosition.X, MousePosition.Y);
        if (_dragging)
        {
            _dragging = false;
            if (_dragDistance < 4)
                _session.SelectAt(_camera.ScreenToWorld(position));
        }
        else if (_pressed is { } action && Hit(position.X, position.Y) == action)
            Activate(action);
        else if (_session.Screen == GameScreen.CreateWorld)
            _seedFocused = false;
        _pressed = null;
        _dirty = true;
    }

    protected override void OnMouseWheel(MouseWheelEventArgs e)
    {
        base.OnMouseWheel(e);
        if (_session.Screen != GameScreen.World || MousePosition.Y < MapCamera2D.TopBarHeight) return;
        _camera.Zoom(e.OffsetY, new Vector2(MousePosition.X, MousePosition.Y));
        _dirty = true;
    }

    protected override void OnKeyDown(KeyboardKeyEventArgs e)
    {
        base.OnKeyDown(e);
        if (_session.Screen == GameScreen.CreateWorld && _seedFocused &&
            e.Key == Keys.Backspace)
        {
            if (_session.SeedText.Length > 0)
                _session.SetSeedText(_session.SeedText[..^1]);
            _dirty = true;
            return;
        }
        if (e.Key == Keys.Escape && _openDropdown is not null)
        {
            _openDropdown = null;
            _hover.Clear();
            _dirty = true;
            return;
        }
        if (e.Key == Keys.Escape && _session.Screen != GameScreen.MainMenu)
        {
            _seedFocused = false;
            ReturnToMenu();
            _dirty = true;
        }
    }

    protected override void OnTextInput(TextInputEventArgs e)
    {
        base.OnTextInput(e);
        if (_session.Screen != GameScreen.CreateWorld || !_seedFocused) return;
        _session.SetSeedText(_session.SeedText + e.AsString);
        _dirty = true;
    }

    private void Activate(UiAction action)
    {
        switch (action)
        {
            case UiAction.SizeSelect or UiAction.TypeSelect:
                _openDropdown = _openDropdown == action ? null : action;
                break;
            case UiAction.Create: _session.OpenCreateWorld(); break;
            case UiAction.Options: _session.OpenOptions(); break;
            case UiAction.Exit: Close(); break;
            case UiAction.Back: ReturnToMenu(); break;
            case UiAction.ToggleSurface:
                if (_session.Screen == GameScreen.World &&
                    _session.State.Landmass?.TectonicPreviewOnly == false)
                    _viewMode = MapViewMode.Biomes;
                break;
            case UiAction.StressCompression or UiAction.StressExtension or UiAction.StressShear:
                if (_session.Screen == GameScreen.World && _session.State.Landmass?.TectonicMotion is not null)
                    _viewMode = action switch
                    {
                        UiAction.StressCompression => MapViewMode.Compression,
                        UiAction.StressExtension => MapViewMode.Extension,
                        _ => MapViewMode.Shear
                    };
                break;
            case UiAction.ToggleGeology:
                if (_session.Screen == GameScreen.World &&
                    _session.State.Landmass?.GeologicFeaturesByCellId is not null)
                    _viewMode = _viewMode == MapViewMode.Geology ? MapViewMode.Biomes : MapViewMode.Geology;
                break;
            case UiAction.ToggleGrayscale:
                if (_session.Screen == GameScreen.World &&
                    _session.State.Landmass?.ElevationByCellId is not null)
                    _viewMode = _viewMode == MapViewMode.Elevation ? MapViewMode.Biomes : MapViewMode.Elevation;
                break;
            case UiAction.ToggleTectonic:
                if (_session.Screen == GameScreen.World &&
                    _session.State.Landmass?.PlateByCellId is not null)
                    _viewMode = _session.State.Landmass?.TectonicPreviewOnly == true
                        ? MapViewMode.Tectonic
                        : _viewMode == MapViewMode.Tectonic ? MapViewMode.Biomes : MapViewMode.Tectonic;
                break;
            case UiAction.ToggleRainfall:
                if (_session.Screen == GameScreen.World &&
                    _session.State.Landmass?.Climate?.BasePrecipitationMmByCellId is not null)
                    _viewMode = _viewMode == MapViewMode.Rainfall ? MapViewMode.Biomes : MapViewMode.Rainfall;
                break;
            case UiAction.ToggleInfrared:
                if (_session.Screen == GameScreen.World && _session.State.Landmass?.Climate is not null)
                    _viewMode = _viewMode == MapViewMode.Infrared ? MapViewMode.Biomes : MapViewMode.Infrared;
                break;
            case UiAction.Small or UiAction.Medium or UiAction.Large or UiAction.Enormous:
                if (_openDropdown != UiAction.SizeSelect) break;
                _openDropdown = null;
                GridSize newSize = ScreenLayout.SizeFor(action);
                if (_session.Screen == GameScreen.World)
                {
                    if (_session.SelectedSize != newSize)
                        StartGeneration(changeSize: newSize);
                }
                else _session.ChooseSize(newSize);
                break;
            case UiAction.Continents or UiAction.Pangaea or UiAction.SmallContinents or
                UiAction.Archipelagos:
                if (_openDropdown != UiAction.TypeSelect) break;
                _openDropdown = null;
                LandmassType newType = ScreenLayout.TypeFor(action);
                if (_session.Screen == GameScreen.World)
                {
                    if (_session.SelectedType != newType)
                        StartGeneration(changeType: newType);
                }
                else _session.ChooseType(newType);
                break;
            case UiAction.SeedField: _seedFocused = true; break;
            case UiAction.RandomSeed: _session.RandomizeSeed(); break;
            case UiAction.Generate: StartGeneration(); break;
            case UiAction.Regenerate: StartGeneration(regenerate: true); break;
        }
        if (action != UiAction.SeedField) _seedFocused = false;
        _hover.Clear();
        _dirty = true;
    }

    private void StartGeneration(bool regenerate = false,
        GridSize? changeSize = null, LandmassType? changeType = null)
    {
        if (_pendingGeneration is not null) return;
        try
        {
            LandmassConfiguration configuration =
                changeSize is not null || changeType is not null
                    ? _session.BeginReconfigure(
                        changeSize ?? _session.SelectedSize,
                        changeType ?? _session.SelectedType)
                    : regenerate ? _session.BeginRegeneration() : _session.BeginGeneration();
            _openDropdown = null;
            _generationStarted = Stopwatch.GetTimestamp();
            _pendingGeneration = Task.Run(() =>
                new WorldGenerationService().Generate(configuration));
        }
        catch (FormatException)
        {
            // GameSession ya mantiene un mensaje de validación legible.
        }
        catch (Exception error)
        {
            Console.Error.WriteLine(error);
            _session.GenerationFailed("NO SE PUDO GENERAR EL MUNDO");
        }
    }

    private void ReturnToMenu()
    {
        _gridRenderer?.Dispose();
        _gridRenderer = null;
        _session.ReturnToMenu();
        _viewMode = MapViewMode.Biomes;
        _seedFocused = false;
        _hover.Clear();
    }

    protected override void OnUnload()
    {
        _gridRenderer?.Dispose();
        _ui?.Dispose();
        base.OnUnload();
    }
}

internal static class Program
{
    private static void Main(string[] args)
    {
        using var window = new EvolveWindow(args.Contains("--graphics-smoke"));
        window.Run();
    }
}
