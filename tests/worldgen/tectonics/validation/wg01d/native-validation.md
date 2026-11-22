# WG-01-D — Pending owner native interaction matrix

**N01: PASS by owner declaration. N02–N17: PENDING.** The D-FIX01 authorization
records that the owner personally created a Pangea/Small/seed-0 session without
automatically executing Grid or Tectonics; observations: none. Provenance:
owner declaration communicated to the Architect, then supplied in the D-FIX01
request. This is not an independent Codex observation, and no owner-provided
capture is asserted. Separate Codex framebuffer images are not N01 owner
evidence. Exact test date/build/monitor metadata was not supplied.
Prior C + FIX01 acceptance does not prove this complete matrix. Headless,
model/reflection and offscreen results do not constitute native input approval.

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
| N01 | Click LAB; configure size/type/seed; create session. No generation before an explicit Grid action | Separate model/headless evidence; owner reports Pangea/Small/0, no automatic stages, no observations | PASS — owner declaration communicated to Architect |
| N02 | Click Grid, then Tectonics. One stage per action; all later stages unavailable | Real API/GL scenario PASS | PENDING |
| N03 | Use N/G through native keyboard, including repeated/held keys; do not accidentally execute future layers | Headless API checks; no native events | PENDING |
| N04 | In every size inspect all 14 regions. Stable PlateId colors; regular hex lines always visible | Fill/owner tests + actual framebuffer sampling PASS | PENDING |
| N05 | Press B while V remains on, then V while B remains on; change view and return. Grid must stay visible | Independent model assertions/captures PASS | PENDING |
| N06 | Click interior, seam and polar hexes; scroll sidebar to read PlateId, cell count, percentage, direction and cm/year against the selected cell | Picking/metadata/mesh tests PASS | PENDING |
| N07 | Wheel zoom at cursor, right-drag and arrow-key pan, F/button fit; no distorted hexes or shifted hit bounds | Camera/model and actual frame sampling PASS | PENDING |
| N08 | Switch completed Grid/Tectonics views; return to menu and reopen LAB; same context/selection/camera inspection, no regeneration | Model/GL API checks PASS | PENDING |
| N09 | Activate INICIAR before/after LAB. Unavailable notice; no session replacement or new creation screen | Production routing/headless and actual menu frame PASS | PENDING |
| N10 | Repeat at 800x600, 1280x720 and 1920x1080 where supported; sidebar scroll/back reachable, controls and selection align | FIX01 programmatic GL captures at 800x600, 1280x720 and actual 1920x1061; requested 1920x1080 not validated | PENDING |
| N11 | Repeat at 100%,125%,150%,200% DPI where supported; move across monitors of different scale; click known cells after changes | Headless framebuffer transforms only | PENDING |
| N12 | Maximize, restore and continuously resize with pending pointer/keyboard input. No stale press, clipping inconsistency or camera distortion | Model checks plus programmatic client/framebuffer resize; no native drag/maximize/pending-input execution | PENDING |
| N13 | Minimize/restore with a generated Large map; no crash, invalid size drawing or stale input; same inspection | Source guard, not native execution evidence | PENDING |
| N14 | Alt-Tab/focus loss while dragging or pressing controls; return and release. No unintended activation or stuck panning | Model cancellation/source checks only | PENDING |
| N15 | Launch on the owner's driver. Actual GLSL compilation/link and rendering succeed; record any diagnostics | Actual production shaders compiled/rendered on recorded NVIDIA driver | PENDING on owner environment |
| N16 | Generate Large, clear configuration, create Small. Observe expected storage policy with suitable tools; no unsupported physical-memory leak conclusion | GL logical buffer-size transitions PASS | PENDING for native/driver observation |
| N17 | Exit using SALIR and normal window close; repeat with LAB open. Process terminates normally; resource lifecycle/tooling evidence attached | Explicit OnUnload + diagnostic unbinding deletes logical GL names; no native close dispatch tested | PENDING |

GPU physical storage may be cached by the driver after logical deletion. Record
tools and interpretation; a memory counter alone is not a proof of a leak or
successful reclamation. Do not mark N17 passed solely from the offscreen test.

## Gate

Outstanding native rows and audit finding follow-up prevent an unconditional
closure of Layer 01 despite the reported favorable audit with observations.
They do not invalidate developer tests. The owner supplies acceptance; Codex
does not substitute its verdict.

## D-FIX01 owner procedure

```powershell
powershell -NoProfile -File .\tests\worldgen\tectonics\validation\wg01d-fix01\StartNativeValidation.ps1
```

This opt-in launcher records branch/SHA/version/runtime/build/exit output under
ignored output/ and copies this checklist for manual notes. It never marks rows
PASS. It is syntax-checked tooling; actual owner execution remains required.
If local script policy blocks it, run the explicit build/run commands above;
do not weaken the machine's script policy for this task.

For each run record actual resolution, Windows scale, monitor and driver, then:

1. Exercise N02–N09 with native pointer/keyboard. Use separate B/V toggles,
   wheel/right drag/arrows/F, sidebar scrolling to plate details and completed
   view switches. Return/reopen LAB and verify unavailable INICIAR.
2. Exercise N10/N12 at supported resolutions; manually maximize/restore and
   resize while a press is pending. A programmatic framebuffer capture does
   not prove drag input, maximize handling or selection under those events.
3. Exercise N11 on monitors/scales actually available. Record unsupported
   combinations as PENDING with the missing hardware/configuration; moving
   between differently scaled monitors requires that real setup.
4. Exercise N13/N14 by minimizing/restoring and changing focus during presses
   and camera dragging. Record stuck input, accidental actions or crashes.
5. Exercise N15 on the responsible person's normal driver/window environment.
   Developer shader success on one offscreen context is separate evidence.
6. Exercise N16/N17 using SALIR and the window close control, from menu and LAB.
   Record process termination and any available graphics-tool trace/counters.
   Logical GL object deletion and exit code do not alone establish physical
   driver-memory reclamation. Do not infer leak resolution from a single counter.

N02–N17 stay PENDING until equivalent native evidence or an explicit applicable
owner decision is supplied. See [audit-fix01.md](../../../../../engine/worldgen/layers/tectonics/audit-fix01.md)
for complementary automated results and environment limitations.
