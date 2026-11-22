# WG-01-E1: Documentary verification and delivery

## Scope and baseline

Authorized branch: world-tectonics. Initial HEAD:
`0e98ae43e3bca53f2104a87f7ca6f754d7fa0645`, VERSION v0.0.42, clean tree.
Delivery VERSION: v0.0.43. E1 is architecture/documentation only; no production,
executable tests, vectors or active numeric parameters are changed. No builds,
headless suites or GUI sessions were executed for E1. Prior D/FIX01 runtime
evidence remains historical evidence, not a new E1 result.

The [history review](e-history-review.md) pins the legacy snapshot and actual
six source paths. Its whole-tree textual mantle search returned no matches
(expected Git grep exit 1); no historical mantle implementation was identified
in that snapshot. The [architecture](e-architecture.md) therefore labels both
mantle alternatives as new proposals.

## Inventory

| File relative to this directory, except VERSION | Change / responsibility |
| --- | --- |
| e-history-review.md | Added: historical formulas, exclusions and bounded mantle-search evidence. |
| e-architecture.md | Added: one atomic pipeline, Gaussian semantics/costs, two mantle models, determinism, future consumers/LAB. |
| e-contracts.md | Added: schema alternatives, recommended layout, numeric units, validation, legacy compatibility and memory. |
| e-roadmap.md | Added: E1-E5 scopes/gates, compatibility matrix, future test matrix and unresolved decisions. |
| e1-validation.md | Added: baseline, inventory, documentary review and reproducible checks. |
| readme.md | Modified: index the E1 proposals without enabling new behavior. |
| contracts.md | Modified: pointer distinguishing proposed schema 2 from active schema 1. |
| roadmap.md | Modified: pointer to the separate E extension; A/B/C/D retained. |
| Root VERSION | Modified once from v0.0.42 to v0.0.43. |

No files are deleted or renamed. Existing audit records, calibration reports,
vector files and all source/project files are unchanged.

## Documentary checks

- Initial branch, full baseline SHA, VERSION and clean-tree preconditions were
  inspected before edits.
- Local Markdown destinations in all eight affected Markdown documents were
  resolved against disk: **45 local targets, zero missing**. The new package
  uses file links, not unverified
  section-fragment links.
- Seed-zero B counts were read directly from header word 11 of each existing
  Small/Medium/Large motion vector: 1,870 / 2,818 / 3,944. No vectors were
  generated or modified. These counts support the analytical tables only.
- Payload arithmetic was checked: 624+33N+20B; seed-zero totals 698,024 /
  1,541,984 / 2,719,504 bytes. Conservative all-edge bounds are 1,852,624 /
  4,173,624 / 7,424,624 bytes. Gaussian visit bounds were checked from the
  documented integer-shell formula. These are calculations, not timings.
- Review separates current schema 1/features 7 from proposed schema 2/features
  31, retains R/M vectors and three root RNG draws, and gives all five E
  increments scope/deliverables/invariants/tests/acceptance/dependencies/risks.
- Scope inventory and whitespace are checked with Git, including the staged
  new files: **nine authorized files**, with no deletions/renames. Final
  commit/publication SHA is obtained from Git, not embedded
  recursively in the documents belonging to that commit.

## PowerShell documentary verification

From the repository root, review the published commit without building/running:

```powershell
git branch --show-current
Get-Content .\VERSION
git show --stat --oneline HEAD
git show --format= --name-status HEAD
git diff HEAD^ HEAD --check
git status --porcelain
git rev-parse HEAD
git ls-remote origin refs/heads/world-tectonics
```

Inspect the eight changed Markdown documents for local destinations:

```powershell
$documents = git diff HEAD^ HEAD --name-only -- '*.md'
$checkedLinks = 0
foreach ($document in $documents) {
    $content = Get-Content -LiteralPath $document -Raw -Encoding UTF8
    foreach ($match in [regex]::Matches($content, '\]\(([^)]+)\)')) {
        $target = $match.Groups[1].Value
        if ($target -match '^(https?://|#)') { continue }
        $path = ($target -split '#', 2)[0]
        $resolved = Join-Path (Split-Path $document -Parent) $path
        if (-not (Test-Path -LiteralPath $resolved)) { throw "Missing: $document -> $target" }
        $checkedLinks++
    }
}
"Local link targets checked: $checkedLinks"
```

## Remaining approval and evidence

E1 delivery is not independent Auditor approval. Schema 2, model A and all new
numeric/seed policies remain proposals for express owner acceptance. E2-E5
tests are planned, not implemented or passed. Gaussian full-field validation
cost and mantle floating-point budgets need later executable evidence.
GATE-01 remains partially open: N01 owner-declared PASS; N02-N17 PENDING.
No native/DPI/GPU or final Layer 01 acceptance claim is made. Stop after E1;
no E2, Layer 02 or merge is authorized by this delivery.
