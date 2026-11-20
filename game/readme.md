# game

Contiene la interfaz gráfica, la presentación del mundo, los menús y la interacción con el jugador.

No implementa reglas ni cálculos de simulación.

## Source organization

- The root contains `Evolve.Game.csproj`, `EvolveEntryPoint.cs`, `EvolveWindow.cs`
  and `GameVersion.cs`: project configuration, startup, native window/event
  coordination and assembly version access.
- `menu/` contains button/key identities, interaction state, shared layout,
  presentation and styling. `EvolveWindow` connects these components to native
  input and the renderer.
- `rendering/` contains `PixelMenuRenderer.cs` and `LegacyBitmapFont.cs`: the
  shared OpenGL shader/batch and local 5x7 font data used by menu and Laboratory presentation.
- `laboratory/` contains the independently authorized LAB-GRID-01 session,
  layout, camera, visual outlines, static line renderer and presentation.
  LAB-GRID-02 adds shared world configuration and transient dropdown/scroll
  state for both manual LAB and automatic INICIAR.
  Its [readme](laboratory/readme.md) and [roadmap](laboratory/roadmap.md) document
  engine boundaries, controls and pending visual acceptance.
- `readme.md` and `audit.md` stay in the root of `game/`.
- `../tests/menu/` links production sources from `menu/` for GPU-independent
  verification; it does not contain a duplicate menu implementation.

All C# namespaces remain unchanged. The existing SDK project discovers the
relocated sources automatically. Every C# source file begins with an English
`//` purpose and responsibility header, as required by root `AGENTS.md`.

Reorganization validation: the 16 menu checks passed, the Release build completed
with zero warnings and errors, and `git diff --check` reported no whitespace
errors. A source comparison confirmed that all 13 C# files in `game/` and
`tests/menu/` retain their non-comment contents. Built display metadata matches
root `VERSION`. No GUI or visual validation was performed for this structural
change; native input, DPI behavior and GPU cleanup remain manual checks.

## Approved interface decisions

- Evolve uses C# / .NET 10 and OpenTK 4.9.4 for the graphical application.
- The entire application runs within one main window, `EvolveWindow`. Future
  menus, laboratories and game screens must be presented inside that same
  window. LAB-GRID-01 now connects the menu and Laboratory within that window.
- `game/` owns presentation and player interaction, independently of simulation
  rules. Its `menu/` and `rendering/` responsibilities are described above.
- C# source files begin with English `//` documentation according to root
  `AGENTS.md`. Root `VERSION` remains the only source of game version metadata.

These decisions preserve the approved interface structure only; they do not
define the physical organization of other modules.

## UI-01 — Proyecto gráfico mínimo

Proyecto ejecutable C# / .NET 10 con OpenTK 4.9.4. Abre una ventana
redimensionable titulada `Evolve`, de 1280 × 720 píxeles iniciales, con fondo
azul oscuro sólido. Requiere el SDK de .NET 10 y un controlador gráfico con
soporte para OpenGL 3.3.

`EvolveEntryPoint.cs` starts the application and disposes the window after the
loop ends. `EvolveWindow.cs` owns the window and menu graphics lifecycle.
Native close (X or Alt+F4 on Windows) ends the loop; menu resources are released
before OpenTK disposes the window and context. UI-02 introduces no references
to other modules.

The root `VERSION` file supplies all game version metadata through
`Evolve.Game.csproj`. `GameVersion.Display` reads `AssemblyInformationalVersion`
to preserve the exact prefix and leading zeros; numeric assembly and file
versions are normalized independently. Rebuild after changing `VERSION`.
UI-02 displays this metadata directly below the title.

### Validación del responsable / Auditor

Desde la raíz del repositorio, en PowerShell:

```powershell
dotnet restore .\game\Evolve.Game.csproj
dotnet build .\game\Evolve.Game.csproj --configuration Release --no-restore
dotnet run --project .\game\Evolve.Game.csproj --configuration Release --no-build
```

Comprobar que la ventana se abre con el título y el fondo indicados, que el
fondo cubre el área al redimensionar, minimizar y restaurar, y que X y Alt+F4
cierran la aplicación limpiamente (en ejecuciones separadas). Tras cada cierre,
`$LASTEXITCODE` debe ser `0` y no debe quedar un proceso de Evolve activo.

The project owner has accepted UI-01 through UI-04 and officially closed the
base-interface roadmap. The independent audit returned **APPROVE WITH
OBSERVATIONS** for `v0.0.06`; the owner subsequently approved the `v0.0.07`
structural reorganization. The completed roadmap has been retired without a
replacement; its history remains available in Git. Approved interface decisions
are recorded above, and `audit.md` retains closure evidence and outstanding
manual checks. Future independent roadmaps require separate owner authorization.

## UI-02 — Main menu presentation

The menu displays `EVOLVE`, the assembly informational version and four buttons:
`INICIAR`, `LAB`, `OPCIONES`, `SALIR`. UI-03 adds input feedback and unavailable
notices to this presentation. Close using `SALIR`, X or Alt+F4.

- `menu/MainMenuPresentation.cs` composes the visual elements.
- `menu/MainMenuStyle.cs` preserves the legacy muted blue-gray palette.
- `menu/MainMenuLayout.cs` preserves the legacy button proportions and adapts short windows.
- `rendering/PixelMenuRenderer.cs` batches colored rectangles and bitmap text with OpenGL
  3.3 and releases its GPU resources when the window unloads.
- `rendering/LegacyBitmapFont.cs` contains the original custom 5x7 glyphs with six-pixel
  character advance, including Spanish accented capitals and uppercase
  normalization. Unsupported characters keep their advance but draw no pixels,
  matching the original renderer.

The initial client size remains 1280 x 720. A 400 x 300 minimum keeps the layout
readable. Glyphs are solid rectangles in the same batch as the buttons, without
textures, font rasterization or antialiasing. The original fractional pixel sizes
are retained at normal window sizes; small windows scale the composition to fit.
No external font assets, font installation or runtime download are required.

The visual reference is `worldgeneration` commit
`ef35e9f8865b02081c8f532a298a314e01091a8d`:

- `GEvolve.Game/Graphics/UiRenderer.cs`: glyph shapes, spacing and text centering.
- `GEvolve.Game/Program.cs`: background band, button fill, border and shadow.
- `GEvolve.Game/Screens/GridLaboratoryLayout.cs`: main-menu button width,
  height, spacing and placement.

At 1280 x 720 the original formulas yield 360-pixel-wide buttons, approximately
51-pixel height and 14-pixel gaps. Borders use the original 1.5-pixel thickness;
shadows retain the 3 x 4 offset. The title is only `EVOLVE`; the assembly version
occupies the former subtitle position. No legacy screens or navigation are imported.

Use the PowerShell restore, build and run commands above. The Auditor should
check the title, exact version against root `VERSION`, button order, white text,
thin blue-gray borders, subtle shadows, centering and absence of overlap at 400 x 300, 800 x 600,
1280 x 720 and maximized size, including high-DPI displays. Verify minimize /
restore and native close. Interaction validation is described below.

After building, compare metadata without relying on numeric assembly versions:

```powershell
$expectedVersion = (Get-Content .\VERSION -Raw).Trim()
$actualVersion = [System.Diagnostics.FileVersionInfo]::GetVersionInfo(
    (Resolve-Path .\game\bin\Release\net10.0\Evolve.Game.dll).Path
).ProductVersion
$expectedVersion
$actualVersion
$actualVersion -ceq $expectedVersion
```

## UI-03 — Main menu interaction

`menu/MainMenuInteraction.cs` owns pointer presses, keyboard selection and footer
notices without OpenGL or window dependencies. `menu/MainMenuButton.cs` identifies
the four approved actions in display order. `menu/MainMenuActivationKey.cs` identifies
Enter and Space without referencing OpenTK. The interaction model tracks their
matching releases, repeats and cancellation; the window only translates native
key events. `MainMenuLayout.ButtonY` is shared
by drawing and hit detection; gaps and shadows do not count as button bounds.
`EvolveWindow.cs` converts cursor positions from client coordinates to framebuffer
pixels using the current size ratio on each axis, including high-DPI windows.

The initial selection is empty, preserving the approved idle appearance.
Hover or keyboard selection slightly lightens the button and its border. A held
left click, Enter or Space shows the darker pressed fill. The original normal
colors, typography, dimensions, title, version position and footer are unchanged.

- A left click activates only when released over the same button it pressed.
- Up and Down wrap through all four buttons. Without an existing hover or
  selection, Down begins at `INICIAR` and Up at `SALIR`.
- Enter and Space activate on release and ignore repeated key-down activation.
  Without a prior selection or hover, they select `INICIAR`.
- Actual pointer movement takes over highlighting and cancels keyboard presses;
  keyboard navigation cancels pointer presses. A stationary pointer does not
  override keyboard selection.
- Focus loss, pointer exit, client resize and framebuffer resize cancel pending
  presses. Releasing an old input after cancellation cannot activate a button.
- At UI-03 delivery, `INICIAR`, `LAB` and `OPCIONES` displayed unavailable
  notices. LAB-GRID-02 routes LAB/INICIAR to the shared generation interface
  before the legacy notice handler; OPCIONES retains its notice.
- `SALIR` requests the existing native close lifecycle. No other screens,
  simulation or settings functionality were introduced by UI-03.

Build and run with the PowerShell commands above, then manually validate:

1. With no hover or keyboard selection, compare the menu against approved UI-02;
   the 12% footer must remain empty before any unavailable action is activated.
2. Hover each button and hold the left button to inspect pressed feedback. Release
   on the same button to activate. Drag to another button or a gap before release
   and confirm no action occurs. A press begun outside must not activate on entry.
3. Use Up and Down in both directions through the wrap boundary. Activate each
   unavailable action with Enter and Space; hold either key to verify no repeated
   activation and that pressed feedback persists until release.
4. While pressing, move outside the window, change focus with Alt+Tab or resize;
   return and release, confirming no pending action fires. Change navigation mode
   while holding an input and confirm the old press is cancelled.
5. Repeat hover, click and keyboard checks at 400 x 300, 800 x 600, 1280 x 720,
   maximized size and across displays with different DPI settings. Confirm the
   footer notice stays readable and inside the existing footer.
6. In separate runs, activate `SALIR` with mouse, Enter and Space. Confirm clean
   termination, `$LASTEXITCODE` equal to `0` and no remaining Evolve process.

## UI-04 — Automated verification and audit preparation

Run the GPU-independent executable test suite from the repository root:

```powershell
dotnet run --project .\tests\menu\Evolve.Menu.Checks.csproj --configuration Release
$LASTEXITCODE
```

This suite links the actual production interaction and layout source files into
its test executable. It introduces no packages, GPU dependencies, public test
hooks or duplicate menu implementation. It is a console suite, so use `dotnet run`
rather than `dotnet test`; a failed check produces a nonzero exit code.

The checks cover pointer ownership, invalid releases, feedback, circular
navigation, Enter/Space release matching and repeat suppression, cancellation,
input-source changes, unavailable notices, exit requests and layout hit bounds.
Native event delivery, real DPI changes, OpenGL cleanup and visual output must
still be checked manually using the checklist above.

See `audit.md` for executed commands, results, technical review, the independent
audit verdict and accepted observation dispositions. The base-interface roadmap
is **CLOSED — Approved by the project owner.** DPI, native-event and real GPU
resource-release checks without sufficient evidence remain manual limitations;
closure does not imply that those checks have been executed.

## LAB-GRID-01 — Authorized Grid visualization

Historical delivery record: this implementation was not visually accepted.
Its initial layout is superseded by the corrective LAB-GRID-02 delivery below.

The owner explicitly authorized this visual delivery on `world-grid`, based on
accepted engine version `v0.0.31`. It has its own
[Laboratory roadmap](laboratory/roadmap.md), independent of mathematical WG-00.
WG-CORE, accepted Grid geometry/data/integration and D01–D11 remain unchanged.
No WG-00-D/E, geographic algorithm, save or active WorldState work is included.

Activate LAB and return with VOLVER/Escape inside the existing EvolveWindow.
Configure the real MapSize/WorldType enums and a signed 64-bit seed, create a
context, then run only Grid through the existing RunNext operation. Inspect the
published descriptor and real progress. Unavailable stages cannot produce fake
results. The Game project now references the existing engine project; it contains
presentation/configuration actions, not simulation or generation algorithms.

The menu's layout, font, palette, INICIAR/OPCIONES notices and shutdown behavior
remain intact. MainMenuInteraction retains its base-interface tests; the native
window intercepts LAB activation before calling its historical unavailable
notice path. Camera/picking/session tests run without OpenTK. Static grid buffers
are released alongside menu buffers before the window's graphics context closes.

Additional verification:

```powershell
dotnet run --project .\tests\laboratory\Evolve.Laboratory.Checks.csproj --configuration Release
dotnet run --project .\tests\worldgen\grid\Evolve.WorldGeneration.Grid.Checks.csproj --configuration Release
dotnet build .\engine\worldgen\Evolve.Engine.WorldGeneration.csproj --configuration Release
dotnet build .\game\Evolve.Game.csproj --configuration Release
```

Developer checks passed (11/11 Laboratory, 16/16 menu, complete A/B/C/FIX01 Grid
suite and clean Release builds). This does not approve the visual result:
Small/Medium/Large, zoom/pan, transformed selection, high DPI/resizing,
minimize/restore, LAB → menu navigation and actual GPU resource cleanup require
the [requested manual validation](laboratory/readme.md#requested-manual-visual-verification).
LAB-GRID-01 has no visual acceptance; LAB-GRID-02 is the corrective current delivery.

## LAB-GRID-02 — Shared configuration and partial world creation

The owner supplied a sidebar/map reference image and clarified that Evolve's
original blue colors remain appropriate. The shared interface replaces the old
three-row toolbar with compact left configuration controls, dropdowns, seed
editing/randomization, all eight canonical stages and a cell-information card.
The map occupies the remaining right-hand space and preserves uniform scale.
The sidebar scrolls at short heights; back navigation stays pinned.

INICIAR now opens world-creation configuration. CREAR MUNDO creates a real
request/context and calls the same RunNext advancement path used by manual LAB,
stopping at the first unavailable capability or failure. Currently Grid alone
completes; Tectonics is unavailable. The result is clearly marked partial,
without finalization, active State, saves or a playable-world claim.
CONFIGURACION permits a new request; both modes can return to the menu.

WorldConfiguration is the sole editable settings/seed model. LaboratorySession
owns capability-aware engine interaction; LaboratoryUiState owns dropdown/edit/
scroll state only. The main window calls tested OpenFromMenu routing for LAB and
INICIAR before the legacy notice paths; menu appearance and OPCIONES/SALIR remain
unchanged. Existing cached Grid rendering and engine algorithms are preserved.

Developer evidence: 18/18 shared-screen checks, 16/16 menu checks, complete
A/FIX01/B/C Grid suite including 10/10 integration checks and 2,320,000 BFS
assertions; clean Engine and Game Release builds. Commands above still apply.
The current visual/manual checklist is in
[Laboratory documentation](laboratory/readme.md#requested-manual-visual-verification).
LAB-GRID-02 is pending reference-image comparison, native size/DPI/input/GPU
verification, independent audit and formal owner acceptance. WG-CORE, accepted
WG-00-A/B/C and D01–D11 are unchanged; no WG-00-D/E work or merge is authorized.
