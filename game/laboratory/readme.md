# Shared World Generation Interface

**Status:** LAB-GRID-02 implemented, pending manual visual verification, independent audit and owner acceptance.
**Branch:** `world-grid`.
**Baseline:** `v0.0.32`, `db1900966bf49f22aa1c91131ef3cb4d2519d2d8`.
**Delivery:** `v0.0.33`.

LAB-GRID-01 was delivered but not visually accepted. LAB-GRID-02 is the
owner-authorized corrective redesign, based on the supplied second reference
image: left configuration/stage/information sidebar and a large map on the right.
The owner's latest clarification retains Evolve's existing blue colors; the
white palette in the reference is not adopted. No new UI framework or font.
This is independent of WG-00-D/E. Accepted WG-CORE and WG-00-A/B/C remain unchanged.

## Shared screens and controls

INICIAR opens world-creation configuration; LAB opens manual generation.
Both use the same sidebar, dropdowns, configuration, camera, renderer, cell card
and session model inside the existing EvolveWindow. There is no second window.
OPCIONES retains its notice and SALIR retains native shutdown.

The sidebar contains world type, map size, editable seed with R randomization,
view selection, primary action, configuration/fit controls, actual stage status,
all eight canonical stages, cell information and a pinned back action.
It scrolls with the wheel when the pointer is over it; back remains visible.
The right canvas consumes remaining space and shows useful instructions until
published Grid data exists. The camera scales uniformly, never stretching hexes
to the canvas aspect ratio. DPI scaling uses framebuffer/client ratios.

- Open dropdowns with the mouse; select an option with the mouse or Up/Down and
  Enter. Escape closes a popup/edit before returning to the menu. Scrolling,
  focus loss and resizing close popups and cancel pending presses.
- Sizes/types come from MapSize and WorldType, without duplicate catalogs.
- Click the seed field, use digits and a leading + or -, Backspace or Delete.
  Empty, incomplete and overflowing values cannot create a request.
- R explicitly chooses a signed 64-bit seed using framework cryptographic bytes;
  this UI action does not consume deterministic engine stage streams.
- In LAB, CREAR SESION (N or Enter in seed editing) creates a context only.
  Click the next available stage, or press G, to invoke RunNext once.
- In INICIAR, CREAR MUNDO creates the context and advances implemented
  capabilities in canonical order, stopping at the first unavailable stage or
  failure. Currently only Grid runs. The result is explicitly a partial map,
  not a playable world. No FinalizeWorld, State or save action occurs.
- CONFIGURACION clears the partial session/map but preserves editable settings.
  SALIR AL MENU or Escape returns to the menu. Reopening the same mode keeps
  its temporary inspection state; switching LAB/INICIAR clears the old context
  and shares the selected configuration.
- A completed Grid stage or Grid view selects real published descriptor data.
  Other views cannot be selected until implemented. Pending/unavailable stages
  never display placeholder geographic results.
- Wheel over the map zooms at the pointer; right drag/arrows pan; AJUSTAR/F fits.
  Click a hex for CellId, column, row and geographic coordinates. Selection is
  yellow and actual engine neighbors blue, including opposite-edge seam IDs.

## Configuration, session and presentation boundaries

`WorldConfiguration` owns the only editable size/type/seed values and constructs
existing WorldGenerationSettings/GenerationRequest after invariant validation.
Its changes discard the session rather than modifying immutable engine metadata.

`LaboratorySession` shares both modes, discovers implemented executors from one
capability inventory (currently HexGridLayer only), and builds the existing
complete eight-stage registry with unavailable-stage guards elsewhere.
Guards throw NotSupportedException and produce no fake candidates. CanAdvance
checks the context's next eligible stage against real capabilities. Manual and
automatic entry modes share RunNextStage, which calls the existing RunNext.
To integrate a future real executor, extend the available inventory; automatic
progression does not hardcode a successful Grid-only stopping point.
A visualization for any newly implemented layer still requires real display code.

`StageStatus` projects Pending/Running/Completed/Failed/Invalidated from
GenerationProgress for implemented stages, and Unavailable for missing
capabilities. The unavailable engine slots remain Pending; they are never marked
Completed to satisfy the UI. Before a context exists, available Grid is shown
as pending configuration, not executed. The notice explains the missing session.
There is no second mutable progress table.

`LaboratoryUiState` owns only dropdown/edit/scroll state. `LaboratoryLayout`
supplies identical drawing/hit bounds for both modes, scaled in framebuffer
space. Short windows intentionally scroll the sidebar instead of clipping
unreachable controls or overlapping the map. The pinned back action remains
outside the scrolling clip. `LaboratoryPresentation` draws compact labels,
dropdowns, thin-bordered blue cards and the empty-map state using existing
bitmap glyphs and PixelMenuRenderer.

`GridCamera`, `GridVisualGeometry` and `HexGridRenderer` are reused unchanged.
Centers, topology, IDs and geographic queries still come from accepted engine
APIs; rendering/picking share the same presentation outline. Static mesh buffers
upload once per grid replacement, and only a small overlay changes with selection.
The same shader uses identity transforms for the main menu and camera transforms
for the grid. One canonical strip is drawn; seam neighbors retain original IDs.
There are no duplicate padding cells or infinite repeated-image scrolling.

## Rendering limits retained from LAB-GRID-01

For Large, static positions use 960,000 vertices / 1,920,000 floats =
7,680,000 GPU position bytes, plus a temporary CPU upload array of that size.
Selection plus six neighbors needs at most 672 position bytes. Shared edges are
drawn twice. These are calculated buffer sizes, not measured driver totals.
Construction is O(cell count) per new grid; picking is O(cell count) per click.
Generation and first mesh upload run on the UI thread; future expensive
executors may need a separately designed scheduling/cancellation policy.

No FPS, GPU memory/latency or throughput budget is approved. Culling, edge
deduplication and faster picking remain evidence-driven rendering decisions.
Neighbor/distance query throughput remains pending WG-00-D; no WG-00-D/E work
is delivered here. The Auditor's previously recorded native .NET 10 limitation
is not resolved by these separate developer executions.

## Build and automated evidence

From the repository root with .NET 10:

```powershell
dotnet run --project .\tests\laboratory\Evolve.Laboratory.Checks.csproj --configuration Release
dotnet run --project .\tests\menu\Evolve.Menu.Checks.csproj --configuration Release
dotnet run --project .\tests\worldgen\grid\Evolve.WorldGeneration.Grid.Checks.csproj --configuration Release
dotnet build .\engine\worldgen\Evolve.Engine.WorldGeneration.csproj --configuration Release
dotnet build .\game\Evolve.Game.csproj --configuration Release
git diff --check
```

Developer results: 18/18 shared-screen checks; 16/16 existing menu checks;
complete Grid A/FIX01/B/C suite including 10/10 integration checks, all 145,000
cells and 2,320,000 retained BFS assertions. Engine and Game Release builds
reported zero warnings/errors. Compiled display version matched root VERSION.
The first updated DPI test used mixed logical/physical units; its setup was
corrected to construct physical dimensions from logical client size and scale,
then the complete 18-check suite passed. No native visual approval is claimed.

Checks link actual production configuration/session/layout/UI-state/camera/
outline sources and menu-button identities, with no OpenTK loaded. Test-only
later-stage executors verify capability discovery and fault/retry handling;
none are production geography. Retired CORE test files are not claimed present
or rerun; current public-core behavior is exercised by Grid and UI integration.
The main-menu interaction model retains historical notice paths; the window
calls tested OpenFromMenu routing for LAB/INICIAR before those paths.

## Requested manual visual verification

```powershell
dotnet run --project .\game\Evolve.Game.csproj --configuration Release --no-build
$LASTEXITCODE
```

Record screenshots, client/framebuffer sizes, DPI, OS/GPU/driver and pass/fail
observations. All following checks are pending owner/Auditor review:

1. Compare structure with the supplied image: compact left sidebar, cards and
   left-aligned controls, large right map, and retained blue palette. Confirm
   the old three-row full-width toolbar is gone in both LAB and INICIAR.
2. Test 400×300, 800×600, 1280×720, maximized, and 100%/125%/200% DPI.
   Scroll to every stage and the information card. Check pinned back navigation,
   popup placement, wheel routing, keyboard popup selection, labels and hits.
3. INICIAR: change type/size, edit invalid/overflow/signed seeds, randomize,
   then CREAR MUNDO. Grid must complete, Tectonics remain unavailable/Pending
   internally, and a partial-map notice appear. CONFIGURACION must allow another
   request. No playable-world success or active-state transition may occur.
4. LAB: create a session and observe pending Grid with empty-map guidance.
   Click Grid/G to advance once, inspect its completed view and verify all seven
   remaining stages are unavailable. Do not treat unavailable as completed.
5. For all three sizes and four world types, inspect regular flat-top outlines,
   fit, zoom anchor, right-drag pan, geographic selection and seam neighbors
   after transforms. Record Large-grid responsiveness without inventing budgets.
6. Minimize/restore, change focus while editing/clicking/dragging, move across
   DPI displays and return LAB/INICIAR → menu → either mode. No stale action or
   popup may survive cancellation.
7. Close with SALIR, X and Alt+F4 in separate runs. Confirm exit code zero,
   no lingering process and graphics resource deletion with appropriate tooling.

Runtime GLSL compilation, exact pixels, native-event delivery, GPU performance
and cleanup remain unverified by headless checks. Independent audit and formal
owner acceptance are required before LAB-GRID-02 is accepted.
