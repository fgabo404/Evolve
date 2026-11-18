# Main menu audit preparation — UI-04

The preparation sections below preserve the historical UI-04 development
record. The official closure and independent audit outcome are recorded in the
final section of this document.

## Scope and responsibilities

The project owner accepted UI-01, UI-02 and UI-03. This delivery prepares the
existing menu for independent review. It does not redesign the approved menu,
add screens or simulation, or modify the architecture and roadmap documents.

| File | Responsibility |
| --- | --- |
| `EvolveEntryPoint.cs` | Start the native loop and dispose the window. |
| `EvolveWindow.cs` | Translate native input, convert client coordinates to framebuffer pixels, manage graphics lifetime and close requests. |
| `menu/MainMenuInteraction.cs` | Track mouse/keyboard ownership, selection, matching activation-key releases, cancellation, notices and exit intent. |
| `menu/MainMenuActivationKey.cs`, `menu/MainMenuButton.cs` | Identify activation keys and the four existing menu actions without OpenTK. |
| `menu/MainMenuLayout.cs` | Calculate approved geometry and shared button row positions for rendering and hit detection. |
| `menu/MainMenuPresentation.cs`, `menu/MainMenuStyle.cs` | Compose the approved design, input feedback and footer notice. |
| `rendering/LegacyBitmapFont.cs`, `rendering/PixelMenuRenderer.cs` | Render the existing local 5x7 glyphs and colored triangles in one OpenGL batch. |
| `GameVersion.cs`, `Evolve.Game.csproj`, root `VERSION` | Preserve exact display metadata while deriving normalized numeric versions. |
| `../tests/menu/` | Exercise linked production interaction and layout sources without OpenGL or third-party test packages. |

Activation-key ownership moved from the native window to the existing input
model so that Enter and Space, mismatched releases, repeat suppression and
cancellation can be tested directly. The window retains the same key mapping
and close lifecycle. No appearance, input semantics or gameplay scope was added.

## Build, run and automated checks

Prerequisites: .NET 10 SDK; running the graphical game additionally requires a
desktop and an OpenGL 3.3-capable graphics driver. Run from the repository root:

```powershell
dotnet restore .\game\Evolve.Game.csproj
dotnet build .\game\Evolve.Game.csproj --configuration Release --no-restore
dotnet run --project .\game\Evolve.Game.csproj --configuration Release --no-build
dotnet run --project .\tests\menu\Evolve.Menu.Checks.csproj --configuration Release
```

The console test suite returns `0` only if all named checks pass, otherwise `1`.
It compiles the same production `.cs` files used by the game, rather than a
test-only rewrite. It does not reference the graphical executable or load OpenTK.
`dotnet test` is not its runner. No additional test framework, NuGet packages,
assembly visibility changes or GPU fixtures are needed.

## Recorded execution

Environment: Windows x64, .NET SDK 10.0.402. Evidence collected on 2026-11-17.

The following validation commands were executed:

```powershell
dotnet --info
dotnet run --project .\tests\menu\Evolve.Menu.Checks.csproj --configuration Release
dotnet build .\game\Evolve.Game.csproj --configuration Release
git diff --check
```

The first test and game-build attempts stopped during restore because the
restricted environment could not read the user's `NuGet.Config`. The same two
commands were retried with authorized access to the normal NuGet configuration:

- Automated suite: **16/16 named checks passed**, exit code `0`.
- Game Release build: **0 warnings, 0 errors**, exit code `0`.
- Whitespace review: no errors.

Metadata was checked without launching the game using this exact PowerShell:

```powershell
$expectedVersion = (Get-Content .\VERSION -Raw).Trim()
$gameAssemblyPath = (Resolve-Path .\game\bin\Release\net10.0\Evolve.Game.dll).Path
$versionInfo = [System.Diagnostics.FileVersionInfo]::GetVersionInfo($gameAssemblyPath)
if ($versionInfo.ProductVersion -cne $expectedVersion) {
    throw 'Built informational version does not match VERSION.'
}
Write-Output "Display version: $($versionInfo.ProductVersion)"
Write-Output "File version: $($versionInfo.FileVersion)"
Write-Output "Assembly version: $([System.Reflection.AssemblyName]::GetAssemblyName($gameAssemblyPath).Version)"
```

Result: display version `v0.0.06`; file and assembly versions `0.0.6.0`; exit
code `0`. The test executable's dependency manifest was also inspected and
contains only its own project, with no OpenTK or third-party runtime dependencies.

## Technical review

- The game project retains only its existing OpenTK package references and no
  project references to simulation modules. The test project has no package or
  project references.
- Interaction and layout contain no graphics calls. Presentation uses the
  unchanged row geometry through `ButtonY`; `HitTest` excludes gaps, shadows and
  right/bottom edges. Automated checks cover all buttons at five framebuffer sizes.
- The window guards invalid dimensions and uses each axis's current framebuffer
  to client ratio before hit detection. Native DPI event delivery remains manual.
- The normal style, font, approved geometry and footer presentation are unchanged.
- Static resource review: the entry point uses `using` for the window;
  `OnUnload` disposes menu GPU resources before the base unload; the renderer
  clears handles on disposal and cleans up partial initialization; shader compile
  and link paths delete failed and temporary objects. This is a code review,
  not a measured GPU resource or native shutdown test.
- Version metadata continues to derive from root `VERSION`; the build preserves
  its prefix and zeros in informational metadata and disables Git SHA suffixes.

## Manual verification and limitations

The graphical game was not launched or visually validated during UI-04. The
project owner and independent Auditor must perform the interaction checklist in
`readme.md`, including:

1. Compare idle rendering with the approved UI-02 design: font, dark background,
   button proportions, colors, thin borders, title, version and 12% bottom footer.
2. Verify mouse/keyboard feedback, matching releases, invalid drags, key repeats,
   focus-loss cancellation and readable unavailable notices in the existing footer.
3. Repeat after resizing, minimizing/restoring and moving between DPI settings.
   Automated layout checks do not prove OS cursor scaling or event ordering.
4. Verify clean `SALIR`, X and Alt+F4 shutdown, exit code `0` and no lingering
   process. Logic tests prove exit intent, not native resource release.

The custom font intentionally uses the legacy uppercase mapping, supported
Spanish accented capitals and blank advance for unsupported glyphs. Fractional
pixel sizes and the 400 x 300 minimum client size are existing design choices.
Unavailable features remain unavailable; there is no world generation,
laboratory, settings implementation, simulation or persistence in the menu.

The execution and technical review above are historical development evidence,
not proof of checks performed by the independent Auditor. The independent audit
outcome and the project owner's subsequent acceptance are recorded below.

## Official closure — 2026-11-18

**Roadmap status:** CLOSED — Approved by the project owner.

**Independent audit verdict:** APPROVE WITH OBSERVATIONS.

**Audited commit:** `60b82a54025306367dcba40c8ba84c8ff529a5cc`

**Audited version:** `v0.0.06`.

The project owner supplied this audit outcome and authorized formal closure
after reviewing and approving the later `game/menu/` and `game/rendering/`
structure. The Auditor identified no confirmed critical or major defects.
This closure records that outcome; it does not perform a new independent audit
or claim that the Auditor executed the automated checks.

### Observation dispositions

| ID | Observation | Accepted treatment |
| --- | --- | --- |
| AUD-01 | Roadmap stage/status inconsistency. | Resolved by this documentary closure: UI-01 through UI-04 are recorded as completed and accepted. |
| AUD-02 | Grammatical observation in the unavailable-feature message. | Correction deferred by the project owner. The existing message, including OPCIONES, is unchanged. |
| AUD-03 | DPI validation pending. | Known limitation; real DPI and cross-display validation remain pending. No execution is asserted. |
| AUD-04 | Display prefix `v` rendered with the `V` glyph. | Accepted by the project owner. The current bitmap font and its mapping are retained. Exact informational metadata still comes from root `VERSION`. |
| AUD-05 | Native-event and real resource-release validation. | Manual verification remains pending wherever sufficient evidence is absent. Code review and exit-intent checks do not establish native/GPU cleanup. |

### Later structural approval and development evidence

The independent audit examined **`v0.0.06`**, not the later **`v0.0.07`**
reorganization at commit `9db15d1e614edf88b37137b112868eb32b58d0e9`.
The owner approved that subsequent structure. Development recorded:

- 16/16 automated checks passed after moving the files.
- Release build succeeded with zero warnings and errors.
- Whitespace verification passed.
- A comparison of all 13 C# source files preserved their non-comment contents;
  no functional changes were identified by the development review.
- Built informational metadata matched `v0.0.07`.

These are executed development checks and a development source review, not an
assertion that the Auditor independently tested or reviewed `v0.0.07`.
Historical evidence and the manual checklist above remain available. DPI,
native events and real resource release are not marked as verified by closure.

The `v0.0.08` closure changed only documentation and root `VERSION`. Approved
interface decisions, including the single `EvolveWindow` policy for future
screens, were recorded in the completed roadmap and are now maintained in
`readme.md`. The retired roadmap remains in Git history. No navigation, new
screen, source-code change or observation-related correction was implemented
in that closure.

### Closure validation performed by development

The following commands were executed on 2026-11-18 for the documentary closure:

```powershell
dotnet run --project .\tests\menu\Evolve.Menu.Checks.csproj --configuration Release
dotnet build .\game\Evolve.Game.csproj --configuration Release
git diff --check
```

Results: 16/16 checks passed; Release build succeeded with zero warnings and
errors; whitespace verification passed. Built informational metadata was read
from `Evolve.Game.dll` and matched root `VERSION` exactly: `v0.0.08`.
The change list was restricted to the interface roadmap (retired after closure),
`audit.md`, `readme.md` and root `VERSION`; no C# source, project or test code was
changed. These are development validation results, not a new Auditor execution.
No manual GPU, DPI or native
event verification was performed for this closure.

## Completed roadmap retirement

After official closure, the project owner authorized deletion of the completed
interface roadmap without moving it or retaining a working-tree copy. The
closure document remains available in Git at commit
`e55644686a83dec18be47078b23c978ae29b6305`.

The approved interface decisions are maintained in `readme.md`. All historical
audit evidence, the verdict and observation dispositions above remain valid;
retirement does not mark pending DPI, native-event or GPU resource-release
checks as completed. No replacement roadmap or new development is authorized
by this retirement.
