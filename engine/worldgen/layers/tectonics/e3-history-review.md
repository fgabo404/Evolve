# E3-CAL01: Historical reuse review before implementation

Reviewed snapshot: `ef35e9f8865b02081c8f532a298a314e01091a8d`, specifically
`GEvolve.Engine/World/worldgenerationold/`. This extends the E1 inspection;
it does not import historical projects or substitute another mantle model.

| Historical source | Relevant behavior | Model A disposition |
| --- | --- | --- |
| LandmassGeneration/Shared/PeriodicNoise.cs | Three X-periodic/open-Y value-noise octaves, cubic smoothing, amplitude halving and division by fixed amplitude sum | Directly reuse the existing modern PeriodicTectonicNoise sampler, already adapted for ulong seeds, explicit hash overflow and lattice limits. Do not copy the old int-seed hash or rewrite sampling. |
| LandmassGeneration/Shared/DeterministicRandom.cs | SplitMix64 with int seed/uint stream packing and modulo-bounded integers | Directly reuse current DeterministicStageRandom and DeriveSeed. Adapt only a private fresh WG01MNT1 domain; no old seed truncation or extra draw from R/M streams. |
| Tectonics/TectonicMotionGenerator.cs | Periodic unit normals, paired boundary projections and maximum Gaussian envelopes | Reuse approved PlateBoundaryBuilder.Normal for all real Grid edges, including same-plate edges; consume existing frozen Influence. No copied projection, propagation or legacy defaults. |
| LandmassGeneration/Shared/HexTopology.cs | Persistent six-neighbor cache over old mutable HexGrid | Reuse HexGridData.TryGetNeighbor, FlatHexGeometry.Center/PeriodWidth/MapHeight. No imported cache, second topology or Offset-column-as-physical-X shortcut. |
| LandmassGeneration/PlateSurfaceInfluence.cs | Per-plate preferences, boundary-distance BFS, coast/island growth biases | Boundary-distance concept already exists in frozen Influence. Exclude crust/coast/island logic, mutable arrays and ushort distance assumptions. |
| LandmassGeneration/PangaeaFromPlatesGenerator.cs and PangaeaBaseFields.cs | Velocity/pressure arrays, smoothed uplift, regional/local noise and a Potential converted to land elevation through quantile anchors | Useful comparison for fixed weighted noise composition only. These are surface/relief fields, not a mantle circulation implementation. Exclude land masks, smoothed pressure/uplift, per-map normalization, byte heights and later-stage dependencies. |
| LandmassGeneration/ContinentalReliefGenerator.cs | Influence-driven geological features and local periodic detail | Consume the accepted Influence implementation instead; no relief, surface mutation, faults, crust or volcanism is imported into Model A. |

Whole tracked text was searched again with `git grep -n -i -I -E
'convec|mantle|mantel|manto|upwell|downwell|astheno|buoyan|plume' <snapshot> --`:
zero matches (expected exit 1). Filename review and the additional Pangaea
Potential/Pressure inspection identified no mantle circulation model. This is
a bounded claim about that snapshot, not unavailable work or all Git history.
No newly discovered mantle model requires architectural substitution review.

Reuse existing pure samplers, RNG, coordinates, topology and normals unchanged.
Implement only the new bounded tectonic/noise potential, canonical paired graph
fluxes, conservation/error diagnostics and private calibration harness. Fixed
weight normalization by wt+wb+wr follows approved Model A; historical per-world
relief normalization does not. No numerical R/M/Influence contract is modified.

The [calibration report](e3-calibration.md) records the experimental parameters,
new logic, tests, costs and remaining selection decisions. No Mantle profile
is frozen and no field is enabled in normal Tectonics publication.
