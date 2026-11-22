# WG-01-E1 — Historical source review

**Status:** documentation only, proposals pending independent audit and owner
acceptance. No historical classes imported, executed or claimed newly tested.
**Reviewed historical commit:** ef35e9f8865b02081c8f532a298a314e01091a8d,
the same pinned worldgeneration snapshot cited by the accepted A design.
**Current baseline:** v0.0.42 / 0e98ae43e3bca53f2104a87f7ca6f754d7fa0645.

The requested conceptual paths differ from this commit's physical layout:
Engine implementations are under World/worldgenerationold/, and the palette
is under Game/Legacy/Graphics/. Namespace names alone are not file locations.

## Inspected source and disposition

Paths below are relative to that historical Git tree, not current files.

| Historical path | Actual behavior | Recover / exclude |
| --- | --- | --- |
| GEvolve.Engine/World/worldgenerationold/Tectonics/TectonicMotionGenerator.cs | Project computes relative convergence/divergence/shear. Generate takes incident-edge maxima at both endpoints; Spread applies maximum Gaussian envelopes with bounded BFS | Recover source aggregation and attenuation semantics. Keep current approved M projections instead of replacing them with historical doubles/defaults. Reject old 16-plate/State/Landmass dependencies |
| GEvolve.Engine/World/worldgenerationold/Tectonics/PlateBoundaryMap.cs | Canonical unlike-owner pairs, boundary flags, multi-source BFS distance | Reuse concept via actual HexGridData. Reject exposed mutable bool/ushort arrays and ushort distance assumptions; use checked uint publication |
| GEvolve.Definitions/WorldGeneration/Configuration/TectonicMotionSettings.cs | Legacy speed [1,8], activity .15, Mixed .5; radius fraction .02 and cutoff 3; validation caps | These are historical proposals, not new approved parameters. Current M Mixed=.75 stays frozen. No mutable legacy settings or competing MapSize definitions |
| GEvolve.Tests/TectonicMotionTests.cs | Hand projections, endpoint reversal, seam normals, single-source Gaussian/oracle, channel separation, repeatability, plate invariance, official-size/seed matrix and terrain/save compatibility checks | Adapt mathematical cases to current 14-plate/64-bit/headless contracts. Old tests often reuse Project as expected-value logic; add independent oracles. Do not import old services, persistence tests or mutable fixtures |
| GEvolve.Engine/World/worldgenerationold/LandmassGeneration/ContinentalReliefGenerator.cs | Consumes crust/surface/motion, classifies collision/rift/ridge/subduction, spreads several channels, computes relief/volcanoes and sometimes changes surface | Recover only evidence that downstream geology consumes tectonic context. All crust, land/sea, age, relief, volcano and State mutation algorithms are excluded from E1/Layer 01 |
| GEvolve.Game/Legacy/Graphics/TectonicStressPalette.cs | Fixed diagnostic scale 16 and square-root color response for three channels | UI legend/transfer-function reference only, not physics or an approved E palette/default. Never normalize or change Engine values in a renderer |

## Exact historical propagation semantics

Each boundary contributes C=max(0,-normal), E=max(0,normal), S=abs(tangent).
At a cell incident to several boundaries each source channel is the **maximum**,
not the sum. The three source channels share a traversal; a cell can carry
several channels simultaneously without equating convergence with crust type.

Historical radius is max(1,min(columns,rows)*radiusFraction). Spread uses
limit=min(cellCount,ceil(radius*cutoffRadii)), includes that final integer shell,
precomputes float weights from exp(-(distance/radius)^2), then multiplies source
float by weight float before maximum reduction. It is not a continuous-distance
cutoff test or double-only kernel. BFS uses the actual graph, clips at poles,
crosses the horizontal seam and reuses stamped visited/distance/queue arrays.
Order-independent maxima do not license replacing the graph metric or Gaussian.

The historical Generate chooses an interior-distance arrow anchor. Current C
uses accepted seed anchors and is frozen; E1 does not authorize replacing it.
Old settings validate radius fraction <=.1 and cutoff in [1,5], among other
caps; those values/limits are not automatically adopted for E. Legacy stress
is a kinematic proxy, not force, pressure, strain or physical elevation.

## Mantle search: no implementation found in this snapshot

A whole-tree filename review and case-insensitive search of **all tracked text**
(not only the six files) found no matches for the following mantle/convection
terms. Git grep exit code 1 means no matches, not a failed source inspection.

```powershell
git ls-tree -r --name-only ef35e9f8865b02081c8f532a298a314e01091a8d
git grep -n -i -I -E 'convec|mantle|mantel|manto|upwell|downwell|astheno|buoyan|plume' ef35e9f8865b02081c8f532a298a314e01091a8d --
```

ThermalPalette.cs was additionally inspected: it maps **annual climate Celsius**
and is not mantle convection. No mantle implementation was identified in the
reviewed tracked source. This bounded finding is not a claim about unavailable
commits, external projects, binaries or uncommitted historical work. The mantle
alternatives in [e-architecture.md](e-architecture.md) are **new design proposals**,
not recovered historical algorithms or already calibrated physical models.
