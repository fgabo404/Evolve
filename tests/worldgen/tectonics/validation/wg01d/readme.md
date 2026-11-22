# WG-01-D evidence index

[Final report](../../../../../engine/worldgen/layers/tectonics/verification-d.md)
contains scope, requirements, results, metrics, observations and commands.
[Native owner matrix](native-validation.md): N01 PASS by owner declaration;
N02–N17 remain PENDING. [Audit follow-up](../../../../../engine/worldgen/layers/tectonics/audit-fix01.md).

Historical frames.csv columns named Median record the **upper middle** of thirty
sorted observations. Those original results are unchanged. The corrected
capture template uses a tested conventional median for future runs; new FIX01
measurements are stored separately, with individual samples retained.

- tectonics.txt: 16/16 groups, both fifteen-buffer sets, all 75 R/M cases,
  independent BFS and the D numeric predicate/midpoint oracle.
- laboratory.txt / grid.txt / menu.txt: retained regression results.
- engine-build.txt / game-build.txt / version.txt: Release and display metadata.
- core-performance.txt / core-summary.csv: final serialized array-copy phase
  and query measurements; all complete payloads checked against the orchestrator.
- core-performance-enumerable-probe.txt: initial diagnostic using an enumerable
  copy source; **excluded from final metrics**, retained to explain INFO-03.
- matched-area-summary.csv: descriptive summary of historical R areas whose
  production equivalence passed; no quotas or new acceptance thresholds.
- graphics/: actual OpenGL captures, CPU/GPU measurements, buffer transitions,
  exact inert capture source/project and execution environment/log.
- project-dependencies.txt / scope-review.txt / files.csv: dependency and change review.

Core raw CSV extraction in PowerShell (metadata and final checksum are excluded):

```powershell
$raw = Get-Content .\tests\worldgen\tectonics\validation\wg01d\core-performance.txt
$rows = $raw | Where-Object { $_ -match '^(Size,|Small,|Medium,|Large,)' } | ConvertFrom-Csv
$rows | Group-Object Size,Phase | Select-Object Name,Count
```

Measurements are environment-specific; offscreen captures and model calls are
not native interaction, independent audit or formal owner approval. Historical
FIX01/C/CAL01 files remain untouched. Do not overwrite committed evidence when
replaying commands; use a separate ignored output directory.
