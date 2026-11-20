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
Default production inventory contains real Grid only. Additional executors in
this test file are not production geography or an alternate orchestrator.

These tests do not run native events/GLSL, inspect screenshots, measure GPU
performance or observe GPU cleanup. Use the
[manual verification checklist](../../game/laboratory/readme.md#requested-manual-visual-verification).
The Laboratory design has owner visual approval. FIX01 awaits independent audit
and formal acceptance; headless checks provide no new native visual evidence.
