# Laboratory headless checks

Run from the repository root:

```powershell
dotnet run --project .\tests\laboratory\Evolve.Laboratory.Checks.csproj --configuration Release
```

This .NET 10 console executable links the actual production LaboratorySession,
LaboratoryLayout, GridCamera and GridVisualGeometry sources. It references the
existing engine/definitions, loads no OpenTK and returns nonzero on any failure.

Eleven checks cover navigation, enum cycling, signed-seed validation, real Grid
generation across 24 size/type/seed combinations, unavailable downstream stages,
session replacement, canonical selection, seam neighbors, responsive/DPI layout,
fit/zoom/pan/inverse transforms, unit-radius outlines for all sizes and picking.
No copied simulation or test-only orchestrator is used.

These checks do not deliver native events, run GLSL, measure GPU performance,
validate screenshots or observe resource cleanup. Use the
[manual checklist](../../game/laboratory/readme.md#requested-manual-visual-validation)
for owner/Auditor validation. Independent audit and formal acceptance remain pending.
