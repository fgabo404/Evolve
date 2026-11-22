# WG-01-C-FIX01 — Grid overlay verification

**Branch:** world-tectonics. **Version:** v0.0.40.
**Baseline:** 7e71dee344d22c0ab5a2abee979fe72eff92c53d / v0.0.39.
**Status:** corrective delivery for independent/owner visual review; no WG-01-D
work or formal WG-01-C acceptance.

## Minimal production change

HexGridRenderer.Draw now draws active layer fill first, then always draws the
existing static Grid line buffer. Tectonic boundaries, white motion arrows and
selection follow, in that order. Grid line width remains one pixel. Its RGB
over plates is (.20,.25,.29), versus boundary (.09,.12,.17); Grid-only color
(.38,.47,.51) is unchanged. B/V retain their existing meanings; no Grid toggle.
Synchronize, BuildLines, GPU buffer ownership, camera, configuration and all
generation/publication code are unchanged. The extra work is one existing-buffer
draw per Tectonics frame, without new geometry or per-frame mesh construction.

## Actual visual evidence

These PNGs are lossless RGBA readbacks from a real OpenGL 3.3 framebuffer using
the production EvolveWindow, PixelMenuRenderer, HexGridRenderer and shaders.
The temporary harness creates one actual EvolveWindow, hides it, invokes its
real load/render/unload methods and renders to an RGBA8 framebuffer attachment.
It uses the real LaboratorySession and real Grid/Tectonics generation through
RunNextStage. No replacement renderer, copied geometry algorithm, production
test hook, additional game window or geographical generator is introduced.
The capture project is temporary under ignored output/, not a production or
permanent automated test project. Its exact source/project templates are
retained as inert .txt files for reproduction below.

Seed 0, Pangea, framebuffer 1280x720:

| Size | Grid before Tectonics | Tectonics + Grid | Zoom/pan/selection | B/V overlays off, Grid retained |
| --- | --- | --- | --- | --- |
| Small | [Grid](Small-grid.png) | [Plates](Small-tectonics.png) | [Detail](Small-tectonics-zoom.png) | [Grid retained](Small-grid-with-overlays-off.png) |
| Medium | [Grid](Medium-grid.png) | [Plates](Medium-tectonics.png) | [Detail](Medium-tectonics-zoom.png) | [Grid retained](Medium-grid-with-overlays-off.png) |
| Large | [Grid](Large-grid.png) | [Plates](Large-tectonics.png) | [Detail](Large-tectonics-zoom.png) | [Grid retained](Large-grid-with-overlays-off.png) |

Developer visual inspection covered all three fitted plate views and Large
zoom/overlay-off detail: regular hex lines remain visible, colors are retained,
boundaries stay darker, white arrows and final yellow/blue selection remain.
Large at fit has dense substructures only a few pixels wide; zoom resolves
individual regular hexagons. This sampling is not proof of every resolution,
seed, driver or native screen/DPI configuration.

The harness asserts the same Grid object and VBO are retained after Tectonics,
and checks exact buffer sizes 1,920,000 / 4,320,000 / 7,680,000 bytes. It captures
zoom, pan, selection and overlay toggles through actual model/camera APIs, not
native keyboard/mouse events. OpenGL reported NoError after all captures for
each size. Developer GPU execution does not substitute for owner visual review,
native input/DPI/minimize tests, driver resource accounting or the independent
Auditor's own .NET 10 execution limitation.

## Rendering measurements

[Raw run](capture-run.txt), [per-view measurements](render-performance.csv).
NVIDIA GeForce RTX 3050 Laptop GPU/PCIe/SSE2, driver/OpenGL version
3.3.0 NVIDIA 592.00; NVIDIA Corporation. Windows 10.0.26200, .NET 10.0.13.
Release, default tiering, VSync disabled, hidden window, RGBA8 offscreen target,
1280x720, one-pixel lines, seed 0/Pangea, center cell selected. Ten warmup frames
and thirty serial samples per size/view. No simultaneous builds/tests ran.

GL_TIME_ELAPSED measures the actual complete EvolveWindow frame (including
sidebar/text/overlays), not Grid alone. CPU finish timing includes reflection,
render dispatch, SwapBuffers and GL.Finish; PNG encoding/readback are excluded.
Each result is a synchronized sample, not an interactive FPS estimate. No
before-fix baseline, CPU affinity/frequency lock, statistical speedup claim,
universal budget or numerical acceptance threshold is asserted.

| Size | Grid GPU median [min,max] ms | Tectonics + Grid GPU median [min,max] ms | Tectonics CPU finish median ms |
| --- | --- | --- | --- |
| Small | .657408 [.516096,1.231872] | .875520 [.717824,1.751040] | 3.839700 |
| Medium | .766976 [.698368,1.202176] | 1.209344 [1.049600,2.641920] | 5.833400 |
| Large | .841728 [.771072,1.085440] | 3.131392 [2.927616,3.996672] | 5.479900 |

Timings depend on driver, scheduling, thermal/clock state and scene/UI contents.
This exercises real GPU drawing; it does not establish minimized/restored
window handling, input latency, GPU lifetime, native presentation throughput
or formal visual acceptance.

## Regression commands and results

The final execution used the commands below with --no-restore, using already
restored packages. Logs retain each actual result:

```powershell
dotnet run --project .\tests\laboratory\Evolve.Laboratory.Checks.csproj --configuration Release --no-restore
dotnet run --project .\tests\worldgen\grid\Evolve.WorldGeneration.Grid.Checks.csproj --configuration Release --no-restore
dotnet run --project .\tests\worldgen\tectonics\Evolve.WorldGeneration.Tectonics.Checks.csproj --configuration Release --no-restore
dotnet run --project .\tests\menu\Evolve.Menu.Checks.csproj --configuration Release --no-restore
dotnet build .\engine\worldgen\Evolve.Engine.WorldGeneration.csproj --configuration Release --no-restore
dotnet build .\game\Evolve.Game.csproj --configuration Release --no-restore
git diff --check
```

Laboratory 19/19; menu 16/16; complete Grid suite, including 2,320,000 BFS
comparisons and 1,015,000 triangles; tectonic B/C regression suite with both
fifteen-buffer sets and all 75 approved M cases. Engine/Game Release compile
without warnings/errors. AssemblyInformationalVersion matches VERSION v0.0.40.
Source/diff review verifies that accepted engine code and fixtures are untouched.
No generation output, interaction logic or menu behavior changed.

## Reproduce the GPU capture

From repository root, using Windows/.NET 10 with an available OpenGL 3.3 driver:

```powershell
$captureDirectory = '.\output\wg01c-fix01-capture'
$evidenceDirectory = '.\tests\laboratory\validation\wg01c-fix01'
New-Item -ItemType Directory -Path $captureDirectory -Force | Out-Null
Copy-Item -LiteralPath "$evidenceDirectory\Capture.csproj.txt" -Destination "$captureDirectory\Capture.csproj"
Copy-Item -LiteralPath "$evidenceDirectory\GridCaptureEntryPoint.cs.txt" -Destination "$captureDirectory\GridCaptureEntryPoint.cs"
dotnet run --project "$captureDirectory\Capture.csproj" --configuration Release -- .\output\wg01c-fix01-replay
```

The recorded run used tests/laboratory/validation/wg01c-fix01 as its output;
use the separate output path above to preserve committed evidence. Native
interactive validation remains the owner's task:

```powershell
dotnet run --project .\game\Evolve.Game.csproj --configuration Release
```

Enter LAB, select each size, create a session (N), run Grid (G), then Tectonics
(G). Confirm Grid stays visible, toggle B/V, zoom with the wheel, pan with right
drag/arrows, select cells and use F to fit. Check resizing, DPI transitions,
minimize/restore, LAB reopening, INICIAR unavailable and clean shutdown.
