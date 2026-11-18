# Main menu audit preparation — UI-04

## Scope and responsibilities

The project owner accepted UI-01, UI-02 and UI-03. This delivery prepares the
existing menu for independent review. It does not redesign the approved menu,
add screens or simulation, or modify the architecture and roadmap documents.

| File | Responsibility |
| --- | --- |
| `EvolveEntryPoint.cs` | Start the native loop and dispose the window. |
| `EvolveWindow.cs` | Translate native input, convert client coordinates to framebuffer pixels, manage graphics lifetime and close requests. |
| `MainMenuInteraction.cs` | Track mouse/keyboard ownership, selection, matching activation-key releases, cancellation, notices and exit intent. |
| `MainMenuActivationKey.cs`, `MainMenuButton.cs` | Identify activation keys and the four existing menu actions without OpenTK. |
| `MainMenuLayout.cs` | Calculate approved geometry and shared button row positions for rendering and hit detection. |
| `MainMenuPresentation.cs`, `MainMenuStyle.cs` | Compose the approved design, input feedback and footer notice. |
| `LegacyBitmapFont.cs`, `PixelMenuRenderer.cs` | Render the existing local 5x7 glyphs and colored triangles in one OpenGL batch. |
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

These results are development evidence, not independent audit approval. Final
roadmap acceptance belongs to the project owner after the Auditor's review.
