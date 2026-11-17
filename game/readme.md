# game

Contiene la interfaz gráfica, la presentación del mundo, los menús y la interacción con el jugador.

No implementa reglas ni cálculos de simulación.

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

Build, runtime and visual acceptance remain pending Auditor validation and
project owner approval. The roadmap is unchanged.

## UI-02 — Main menu presentation

The menu displays `EVOLVE`, the assembly informational version and four static
buttons: `INICIAR`, `LAB`, `OPCIONES`, `SALIR`. No actions, hover states, keyboard
navigation or additional screens are implemented. Close using X or Alt+F4.

- `MainMenuPresentation.cs` composes the visual elements.
- `MainMenuStyle.cs` preserves the legacy muted blue-gray palette.
- `MainMenuLayout.cs` preserves the legacy button proportions and adapts short windows.
- `PixelMenuRenderer.cs` batches colored rectangles and bitmap text with OpenGL
  3.3 and releases its GPU resources when the window unloads.
- `LegacyBitmapFont.cs` contains the original custom 5x7 glyphs with six-pixel
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
restore and native close. Static buttons intentionally do not respond to input.

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
