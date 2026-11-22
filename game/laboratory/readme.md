# World Generation Laboratory

**Status:** LAB-GRID-02 including FIX01 formally accepted by the owner, as confirmed in the WG-00-D authorization. Two non-blocking observations remain open below.
**Branch:** `world-grid`.
**FIX01 baseline:** `v0.0.33`, `ceaa289018b1a56eafe3417a0d64e6b4fb3c5b70`.
**FIX01 delivery:** `v0.0.34`.

**Current extension:** owner-authorized WG-01-C on world-tectonics, v0.0.39,
adds real Tectonics inspection while preserving this accepted Grid interface.
Its independent audit and owner visual acceptance remain pending; existing
non-blocking observations below are not closed by this extension.

LAB-GRID-01 was delivered but not visually accepted. LAB-GRID-02 is the
owner-authorized corrective redesign, based on the supplied second reference
image: left configuration/stage/information sidebar and a large map on the right.
The owner's latest clarification retains Evolve's existing blue colors; the
white palette in the reference is not adopted. No new UI framework or font.
This is independent of WG-00-D/E. Accepted WG-CORE and WG-00-A/B/C remain unchanged.

## Laboratory navigation and controls

Only LAB opens this interface inside the existing EvolveWindow. INICIAR retains
its original unavailable notice without opening a screen or generating data.
Normal game creation remains unimplemented. There is no second window.
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
- CONFIGURACION clears the temporary session/map but preserves editable settings.
  SALIR AL MENU or Escape returns to the menu. Reopening LAB retains temporary
  inspection state. Activating INICIAR after leaving LAB does not modify it.
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

`LaboratorySession` owns manual LAB generation, discovers implemented executors from one
capability inventory (HexGridLayer and TectonicPlatesLayer), and builds the existing
complete eight-stage registry with unavailable-stage guards elsewhere.
Guards throw NotSupportedException and produce no fake candidates. CanAdvance
checks the context's next eligible stage against real capabilities. CreateSession
only creates a pending context; each explicit RunNextStage calls RunNext once.
The normal-creation mode and automatic CreateWorld entry point were removed.
Reusable configuration, layout, camera and rendering components remain intact.
A visualization for any newly implemented layer still requires real display code.

WG-01-C supplies that display for published Tectonics only. After Grid, the
second explicit G/next-stage action runs real Tectonics; Landmasses and later
stages stay unavailable. The view dropdown contains Grid/Tectonics, with pending
results disabled. A completed-stage click also selects its existing output.
Changing a view preserves Grid/camera references and never invokes generation.

`StageStatus` projects Pending/Running/Completed/Failed/Invalidated from
GenerationProgress for implemented stages, and Unavailable for missing
capabilities. The unavailable engine slots remain Pending; they are never marked
Completed to satisfy the UI. Before a context exists, available Grid is shown
as pending configuration, not executed. The notice explains the missing session.
There is no second mutable progress table.

`LaboratoryUiState` owns only dropdown/edit/scroll state. `LaboratoryLayout`
supplies identical drawing/hit bounds for LAB, scaled in framebuffer
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

Developer results: 18/18 Laboratory checks; 16/16 existing menu checks;
complete Grid A/FIX01/B/C suite including 10/10 integration checks, all 145,000
cells and 2,320,000 retained BFS assertions. Engine and Game Release builds
reported zero warnings/errors. Compiled display version matched root VERSION.
These are headless/build results, not new native visual evidence. The owner has
accepted LAB-GRID-02 including FIX01 separately; acceptance does not imply
execution of every native validation below.

Checks link actual production configuration/session/layout/UI-state/camera/
outline sources and menu-button identities, with no OpenTK loaded. Test-only
later-stage executors verify capability discovery and fault/retry handling;
none are production geography. Retired CORE test files are not claimed present
or rerun; current public-core behavior is exercised by Grid and UI integration.
The main-menu interaction model retains historical notice paths; the window
calls tested OpenFromMenu routing for LAB only before those paths. Tests also
exercise the original INICIAR notice and verify that it preserves LAB data.

## Requested manual visual verification

```powershell
dotnet run --project .\game\Evolve.Game.csproj --configuration Release --no-build
$LASTEXITCODE
```

Record screenshots, client/framebuffer sizes, DPI, OS/GPU/driver and pass/fail
observations. All following checks are pending owner/Auditor review:

1. Confirm the approved LAB appearance remains unchanged. Return to the menu
   and activate INICIAR with mouse, Enter and Space: only the original unavailable
   notice must appear, without opening another screen or changing LAB data.
2. Test 400×300, 800×600, 1280×720, maximized, and 100%/125%/200% DPI.
   Scroll to every stage and the information card. Check pinned back navigation,
   popup placement, wheel routing, keyboard popup selection, labels and hits.
3. In LAB, change type/size, edit invalid/overflow/signed seeds and randomize.
   CONFIGURACION must allow another request. No playable-world success or
   active-state transition may occur.
4. LAB: create a session and observe pending Grid with empty-map guidance.
   Click Grid/G to advance once, inspect its completed view and verify all seven
   remaining stages are unavailable. Do not treat unavailable as completed.
5. For all three sizes and four world types, inspect regular flat-top outlines,
   fit, zoom anchor, right-drag pan, geographic selection and seam neighbors
   after transforms. Record Large-grid responsiveness without inventing budgets.
6. Minimize/restore, change focus while editing/clicking/dragging, move across
   DPI displays and return LAB → menu → LAB. No stale action or
   popup may survive cancellation.
7. Close with SALIR, X and Alt+F4 in separate runs. Confirm exit code zero,
   no lingering process and graphics resource deletion with appropriate tooling.

Runtime GLSL compilation, exact pixels, native-event delivery, GPU performance
and cleanup remain unverified by headless checks. Formal owner acceptance of
LAB-GRID-02/FIX01 does not close these remaining observations.

## Non-blocking observations retained after acceptance

1. Native input, DPI transitions and actual GPU lifecycle/resource deletion still
   require recorded manual evidence. Headless checks and developer builds do not
   establish these properties, nor remove the Auditor's .NET 10 execution limit.
2. GPU buffer retention is unchanged: returning to the menu retains the LAB
   session/mesh, and clearing a session resets draw counts without immediately
   shrinking the existing buffer allocation. A later upload replaces storage;
   disposal deletes it at window shutdown. This source-level behavior is not a
   measured driver-memory/lifecycle guarantee or evidence of a leak correction.

WG-00-D only records these observations; it changes no Laboratory code or UI.

## WG-01-C — Actual published plate inspection

The owner approved M [1,8) cm/year, activity .15 and mixed ratio .75 with the
exact policy in the tectonic motion calibration. The session reads one validated
TectonicData view from the completed flags-7 publication; the renderer never
regenerates plate ownership or motion. Colors are stable by PlateId for all
fourteen regions. TectonicVisualGeometry builds grouped triangles, unique
boundary lines and arrows once per published result, reusing Grid corners and
the same camera. Both visible cuts of a seam interface are shown; IDs are not
duplicated. The plate's accepted origin anchors each arrow inside its region.

Click a hex to read actual PlateId, CellCount/area percentage, direction and
speed. The existing information card expands its scrollable height only for
this view. B toggles boundaries and V toggles arrows, with status in the map
hint. N creates a session, G advances one stage, F fits; wheel zooms, right drag
or arrows pan. Escape/back returns to the menu; reopening retains inspection.
INICIAR remains unavailable. No separate boundary-edge selection or native
visual/GPU approval is claimed. Influence fields remain unavailable.

Large adds 11,520,000 position bytes for static fills plus boundary/arrow data
to the existing 7,680,000-byte Grid line buffer, with temporary CPU upload
arrays. Fourteen uniform-color draws avoid per-vertex colors. The existing
buffer-retention policy extends to these meshes: clearing resets draw counts;
later uploads replace storage, shutdown deletes buffers/arrays while the
context is current. Actual driver retention/deletion still needs manual evidence.

Developer checks pass 19/19 including real Grid→Tectonics sessions for all
sizes, complete fill ownership/regular corners, seam lines, stable palette and
arrow anchors/directions, actual inspection text, toggles and LAB reopening.
[C delivery/evidence](../../engine/worldgen/layers/tectonics/delivery-c.md)
records all regressions, benchmarks and the remaining independent/visual gate.
Native input, DPI, minimize/restore, rendering and GPU lifecycle observations
remain open. Re-run the manual checklist above for Small/Medium/Large with
Tectonics, overlays and several seeds before owner visual acceptance.

## WG-01-C-FIX01 — Always-visible Grid (v0.0.40)

The owner requested that the existing static Grid lines remain visible over
Tectonics. Draw order is now layer fill, Grid lines, enabled tectonic boundaries,
enabled white motion arrows, then selection. Grid uses a discreet dark color
over plates; tectonic boundaries retain their darker, higher-contrast color.
Grid-only appearance and one-pixel line width are unchanged. B/V control only
their existing overlays; there is no Grid visibility toggle.

The same Grid VAO/VBO is reused. No geometry rebuild in Draw, new Grid data,
GPU position allocation, generator call, camera/input change or publication
change is introduced. Each Tectonics frame adds one draw of the existing line
buffer. Accepted Grid/R/M/CORE source and compatibility fixtures are unchanged.

[FIX01 evidence](../../tests/laboratory/validation/wg01c-fix01/readme.md)
contains actual OpenGL framebuffer captures for all sizes, isolated rendering
measurements, reproducible capture tooling and regression outputs. This is
developer offscreen GPU evidence, not owner interactive/visual acceptance or
proof of native input, DPI transitions, driver retention or shutdown behavior.
WG-01-C acceptance remains pending independent audit and owner review.
