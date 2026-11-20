# World Generation Laboratory

**Status:** LAB-GRID-01 implemented, pending manual visual validation, independent audit and owner acceptance.
**Working branch:** `world-grid`
**Accepted engine baseline:** `v0.0.31`, `4f0049dba50f68b426c99bb7bafb749648c16d42`.

The owner explicitly authorized this first visual integration. It is independent
of the mathematical WG-00 roadmap: D01–D11, WG-00-A/B/C and WG-CORE remain
unchanged. WG-00-D/E, other geographic layers, WorldState and saves are excluded.
See the [LAB-GRID-01 delivery roadmap](roadmap.md).

## Navigation and controls

LAB uses the existing EvolveWindow and original bitmap font. VOLVER or Escape
returns to the menu; the temporary session remains available on reopening.
INICIAR and OPCIONES retain their unavailable behavior. SALIR/native close use
the existing window lifecycle.

- Click TAMANO / TIPO to cycle the real MapSize / WorldType enums; S / T do the
  same outside seed editing.
- Click SEED to edit a signed 64-bit decimal integer. Backspace removes one
  character, Delete clears it. ASCII digits and a leading + or - are supported.
  Empty, incomplete or overflowing values cannot create a session.
- CREAR SESION / N (or Enter while editing seed) creates only the context.
- GRID: RUNNEXT / G invokes only RunNext while Grid is Pending or Failed.
  The control is disabled after completion.
- Wheel zoom anchors at the pointer; right drag or arrow keys pan. AJUSTAR / F
  restores the initial fit. Resize refits an untouched camera and preserves a
  camera the user has moved.
- Click a cell to inspect CellId, column, row and geographic center. Yellow marks
  selection, blue its engine neighbors, including opposite-edge seam neighbors.

Changing metadata discards visible data and selection; create a fresh request
and context. Focus loss, pointer exit and client/framebuffer resizing cancel
pending mouse actions.

## Engine boundary and unavailable stages

LaboratorySession constructs existing WorldGenerationSettings and
GenerationRequest, calls CreateGeneration, then RunNext. Grid status comes from
Context.GetProgress, not a second progress table. Completed output is read as
one LayerData<HexGridDescriptor>, checked against the immutable request and
converted using ToGridData. No direct Grid execution bypasses orchestration.

The registry requires all eight canonical stages. It receives the real
HexGridLayer and seven explicit unavailable-stage capability guards. A guard
throws NotSupportedException and never produces fake candidate data. The UI
prevents all forward calls beyond Grid, so guards are not executed in the
exposed workflow. Remaining engine slots stay Pending and the screen labels
01–07 unavailable. No RunAll, arbitrary-stage runner, finalizer or rebuilder is
exposed by the Laboratory.

Game references the existing world-generation engine project, with definitions
available transitively. Engine remains UI/OpenTK independent.

## Rendering and selection responsibilities

- LaboratoryLayout supplies shared framebuffer control/hit bounds, DPI conversion
  and a responsive 2:1 letterboxed viewport. A uniform camera scale preserves
  regular hexagons rather than stretching the grid to fill the frame.
- GridVisualGeometry queries FlatHexGeometry.Center for cell centers. Six unit
  vectors at 60-degree intervals form presentation-only flat-top outlines.
  Rendering and polygon hit testing share those outlines; Game duplicates no
  axial inverse, neighbor topology or geographic calculation.
- GridCamera handles fit, pan, zoom and screen/world conversion without OpenGL.
- HexGridRenderer uploads a static position VBO once per grid replacement.
  Normal frames update camera uniforms and draw lines; a small overlay changes
  only when selection changes. PixelMenuRenderer supplies its existing shader
  and bitmap text. Its default identity transform preserves menu rendering.
- Click picking scans nearest engine centers, then rejects points outside the
  shared polygon. Ties select the first canonical ID. Identities, neighbors and
  geographic information come from HexGridData.

Only one canonical strip is rendered. No padding/repeated cells or new IDs
exist. Seam continuity is logical and visible through opposite-edge neighbor
highlighting; panning does not implement repeated periodic images or infinite
seamless scrolling.

At Large, the static buffer contains 960,000 position vertices / 1,920,000 floats:
7,680,000 GPU position bytes plus a temporary CPU upload array of that size.
Shared edges are intentionally drawn twice. Selection plus six neighbors needs
at most 672 position bytes. These are calculated buffer sizes, not measured
driver totals. CPU mesh construction is O(cell count) on grid replacement;
selection is O(cell count) on click. The complete mesh is not rebuilt per frame.

No framerate, GPU/driver limit, selection latency or query-throughput budget has
been measured or approved. Edge deduplication, culling and faster picking are
future rendering decisions requiring evidence. Pending WG-00-D/E performance
work is not implemented or accepted here.

## Build and headless checks

Run from the repository root with .NET 10:

```powershell
dotnet run --project .\tests\laboratory\Evolve.Laboratory.Checks.csproj --configuration Release
dotnet run --project .\tests\menu\Evolve.Menu.Checks.csproj --configuration Release
dotnet run --project .\tests\worldgen\grid\Evolve.WorldGeneration.Grid.Checks.csproj --configuration Release
dotnet build .\engine\worldgen\Evolve.Engine.WorldGeneration.csproj --configuration Release
dotnet build .\game\Evolve.Game.csproj --configuration Release
git diff --check
```

Developer evidence: 11/11 Laboratory and 16/16 menu checks passed. The complete
Grid suite passed A/FIX01/B checks and 10/10 C integration checks, including
2,320,000 retained geometry/grid-API BFS assertions. Both Release builds reported
zero warnings/errors. These are developer checks, not independent audit or
visual acceptance. The Auditor's previously recorded native .NET 10 execution
limitation is not resolved by this separate developer execution.

The Laboratory check executable links actual production session/layout/camera/
visual-geometry sources and references Engine. It loads no OpenTK and creates
no window or GPU context. Native events, runtime GLSL compilation, pixels, GPU
performance and actual graphics-resource cleanup remain manual checks.

## Requested manual visual validation

```powershell
dotnet run --project .\game\Evolve.Game.csproj --configuration Release --no-build
$LASTEXITCODE
```

Record OS, GPU/driver, DPI, viewport size, responsiveness/framerate, screenshots
and pass/fail evidence. All checks below remain **pending**, not approved:

1. Compare the unchanged menu and enter LAB by mouse/keyboard. INICIAR must
   remain unavailable; no second window may open.
2. Generate Small, Medium and Large using all world types and positive/negative
   seeds; reject invalid/overflow seeds. Creating a session leaves Grid Pending
   and no mesh; RunNext publishes its size and Completed state. Further forward
   attempts must never run Tectonics.
3. Inspect flat-top regular outlines, neutral background, visible lines and fit.
   Wheel zoom must preserve its cursor anchor; right drag/arrows pan; F restores
   fit. Record Large-grid responsiveness.
4. Select center, polar and seam cells after zoom/pan. Compare CellId/column/row/
   geographic information with engine queries. Opposite-edge blue neighbors
   must retain valid IDs; outside clicks clear selection.
5. Resize through 400×300, 800×600, 1280×720 and maximized/high-DPI displays.
   Confirm controls, hit detection and hex shapes remain consistent. At small
   sizes a full-grid fit is dense; zoom to inspect individual cells.
6. Minimize/restore, change focus during a press/drag, leave/re-enter the window.
   No delayed click/drag may fire; the temporary session must survive.
7. Navigate LAB → menu → LAB. Close via SALIR, X and Alt+F4 in separate runs.
   Confirm exit code zero/no lingering process. Use a graphics/debug tool to
   verify resource deletion before claiming GPU cleanup approval.

Manual visual evidence, independent audit and formal owner acceptance remain
required. Further visual/geographic development needs separate authorization.
