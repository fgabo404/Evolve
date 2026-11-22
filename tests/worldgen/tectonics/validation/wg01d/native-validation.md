# WG-01-D — Pending owner native interaction matrix

**All owner rows remain PENDING.** No new row-by-row owner/native evidence was
supplied in D. Prior FIX01 baseline acceptance is retained, but does not prove
this complete matrix. Headless checks, reflection/model calls and offscreen
captures do not constitute native mouse/keyboard or desktop validation.

## Record before execution

- Tester/date; exact full Git SHA; root VERSION and displayed version.
- Windows/runtime, GPU/driver, monitor/resolution and scaling for each run.
- Exact steps, expected/observed result, PASS/FAIL, screenshot/video/log link.
- For a failure, record seed/type/size, input sequence and reproduction rate.
- Only change PENDING after actual evidence; independent audit is a separate gate.

```powershell
git branch --show-current
git rev-parse HEAD
Get-Content .\VERSION
dotnet build .\game\Evolve.Game.csproj --configuration Release
dotnet run --project .\game\Evolve.Game.csproj --configuration Release --no-build
```

Use Small, Medium and Large; seeds 0,1,-1 and at least one signed extreme.
Cover Pangea, Continents, Archipelagos and RealEarth; no later geographic layer
should execute. At each configuration, create a session using N or the button,
run Grid with G/a stage click, then Tectonics with a second action.

## Native checklist

| ID | Reproducible action and expected result | Existing Codex evidence | Owner/native status |
| --- | --- | --- | --- |
| N01 | Click LAB; configure size/type/seed; create session. No generation before an explicit Grid action | Real model/headless PASS | PENDING |
| N02 | Click Grid, then Tectonics. One stage per action; all later stages unavailable | Real API/GL scenario PASS | PENDING |
| N03 | Use N/G through native keyboard, including repeated/held keys; do not accidentally execute future layers | Headless API checks; no native events | PENDING |
| N04 | In every size inspect all 14 regions. Stable PlateId colors; regular hex lines always visible | Fill/owner tests + actual framebuffer sampling PASS | PENDING |
| N05 | Press B while V remains on, then V while B remains on; change view and return. Grid must stay visible | Independent model assertions/captures PASS | PENDING |
| N06 | Click interior, seam and polar hexes; scroll sidebar to read PlateId, cell count, percentage, direction and cm/year against the selected cell | Picking/metadata/mesh tests PASS | PENDING |
| N07 | Wheel zoom at cursor, right-drag and arrow-key pan, F/button fit; no distorted hexes or shifted hit bounds | Camera/model and actual frame sampling PASS | PENDING |
| N08 | Switch completed Grid/Tectonics views; return to menu and reopen LAB; same context/selection/camera inspection, no regeneration | Model/GL API checks PASS | PENDING |
| N09 | Activate INICIAR before/after LAB. Unavailable notice; no session replacement or new creation screen | Production routing/headless and actual menu frame PASS | PENDING |
| N10 | Repeat at 800x600, 1280x720 and 1920x1080 where supported; sidebar scroll/back reachable, controls and selection align | Headless layout coverage; actual GL evidence only 1280x720 | PENDING |
| N11 | Repeat at 100%,125%,150%,200% DPI where supported; move across monitors of different scale; click known cells after changes | Headless framebuffer transforms only | PENDING |
| N12 | Maximize, restore and continuously resize with pending pointer/keyboard input. No stale press, clipping inconsistency or camera distortion | Existing model/coordinate checks only | PENDING |
| N13 | Minimize/restore with a generated Large map; no crash, invalid size drawing or stale input; same inspection | Source guard, not native execution evidence | PENDING |
| N14 | Alt-Tab/focus loss while dragging or pressing controls; return and release. No unintended activation or stuck panning | Model cancellation/source checks only | PENDING |
| N15 | Launch on the owner's driver. Actual GLSL compilation/link and rendering succeed; record any diagnostics | Actual production shaders compiled/rendered on recorded NVIDIA driver | PENDING on owner environment |
| N16 | Generate Large, clear configuration, create Small. Observe expected storage policy with suitable tools; no unsupported physical-memory leak conclusion | GL logical buffer-size transitions PASS | PENDING for native/driver observation |
| N17 | Exit using SALIR and normal window close; repeat with LAB open. Process terminates normally; resource lifecycle/tooling evidence attached | Explicit OnUnload + diagnostic unbinding deletes logical GL names; no native close dispatch tested | PENDING |

GPU physical storage may be cached by the driver after logical deletion. Record
tools and interpretation; a memory counter alone is not a proof of a leak or
successful reclamation. Do not mark N17 passed solely from the offscreen test.

## Gate

Outstanding native rows and independent audit prevent an unconditional closure
of Layer 01. They do not invalidate the separately documented developer test
results. The owner supplies acceptance; Codex does not substitute its verdict.
