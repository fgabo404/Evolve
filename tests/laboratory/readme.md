# Laboratory-only Interface Checks

```powershell
dotnet run --project .\tests\laboratory\Evolve.Laboratory.Checks.csproj --configuration Release
```

This .NET 10 executable links production WorldConfiguration, LaboratorySession,
LaboratoryUiState, LaboratoryLayout, GridCamera, GridVisualGeometry and menu
button identities. It references actual Engine/Definitions and loads no OpenTK.
All 18 named checks must pass for exit code zero.

Coverage retains numeric seeds, enum cycling, real Grid across 24 configurations,
metadata replacement, canonical selection, camera/picking/outlines and headless
dependencies. The old toolbar-layout expectation is replaced by the authorized
sidebar contract: all controls and stages remain reachable by scrolling, back
stays fixed, popup hit bounds agree, and physical sizes correctly reflect
logical client dimensions at 100%, 125% and 200% DPI.

FIX01 checks exercise production LAB-only routing and the original INICIAR
notice before generation and after leaving a generated LAB session. They verify
that INICIAR opens no screen and changes no context/results, automatic creation
APIs are absent, and LAB advances once per explicit request. Other checks retain
capability discovery, atomic failure/invalid-candidate retry, seed validation,
dropdown navigation/scroll cancellation and completed-data inspection. The suite
links the actual production menu interaction model and activation keys.
The historical FIX01 inventory contained real Grid only. WG-01-C now registers
real Grid and Tectonics; test wrappers count calls/inject failures around the
actual executor, not fake tectonic geography or an alternate orchestrator.

These tests do not run native events/GLSL, inspect screenshots, measure GPU
performance or observe GPU cleanup. Use the
[manual verification checklist](../../game/laboratory/readme.md#requested-manual-visual-verification).
The original Laboratory/FIX01 design is accepted. WG-01-C visual acceptance
remains pending; headless checks provide no new native visual evidence.

## WG-01-C — Production tectonic inspection

The suite now passes 19/19 checks, preserving previous assertions and adapting
manual advancement to the newly authorized second real stage. It links the
actual TectonicVisualGeometry, with no OpenTK dependency, and tests published
flags-7 data for Small/Medium/Large and signed seeds. Exhaustive fill-to-owner
mapping and regular corners, all fourteen colors/anchors, arrow direction,
physical seam cuts, scrollable plate information, overlay toggles, camera reuse,
reopening and stale-session replacement are checked. The earlier capability/
retry assertions now use real TectonicPlatesLayer output.

Run the same Release command above. Final logs and limits are linked from
[WG-01-C delivery](../../engine/worldgen/layers/tectonics/delivery-c.md).
Native rendering/input/DPI, minimized windows and GPU retention/disposal still
require the owner's manual evidence and independent audit.
