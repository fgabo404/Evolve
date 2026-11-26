# WG-01-E: Integrated dynamics roadmap

**E1 is the accepted v0.0.43 baseline identified by the owner's E2-CAL01
authorization. The owner's E2-FINAL specification reports E2-CAL01 formally
accepted with observations and selects HISTORICAL for implementation/validation.
E2-FINAL is delivered pending architectural review, independent audit and owner
acceptance; no definitive profile freeze or normal activation is implied.
The owner subsequently authorized E3-CAL01 at v0.0.45, treating R/M/Influence
as frozen inputs. Only private Model A calibration is delivered; Mantle
selection/freezing, E3-FINAL and E4/E5 remain unauthorized.** The original
[four-increment A/B/C/D roadmap](roadmap.md) remains historical and active for
its outstanding acceptance gates. This separate extension neither closes
Layer 01 nor resolves GATE-01. No branch merge is authorized.

Design package: [history](e-history-review.md), [architecture](e-architecture.md),
[proposed schema](e-contracts.md), [documentary verification](e1-validation.md).
Current production remains schema 1/features 7, frozen R revision 2 and M
revision 1. All numeric Influence/mantle choices below need separate approval.

## E1 - Architecture and contracts

| Item | Requirement / delivered status |
| --- | --- |
| Scope | Documentary historical review, integrated atomic sequence, data proposal, propagation analysis, two mantle alternatives and future LAB/consumer boundaries. |
| Deliverables | This five-increment roadmap, schema-2 recommendation, analytical memory/work bounds, compatibility/test matrices and review evidence. |
| Invariants | No production, executable tests, vectors, profiles, CORE, Grid or UI modifications; GATE-01 partially open. |
| Planned checks | Links, scope inventory, arithmetic/coherence, VERSION and git diff --check; no runtime/GUI claims. |
| Acceptance | Independent architectural audit and express owner acceptance, including schema/model recommendations; delivery is not acceptance. |
| Dependencies | Approved R/M and Grid/CORE contracts; historical snapshot and D-FIX01 findings inspected. |
| Risks | New mantle is stylized; schema/API proposals and numeric policies remain subject to review. |

## E2 - Tectonic deformation

**E2-CAL01 historical delivery:** internal experimental builders and independent
checks were delivered for owner selection. See the
[calibration report](e2-calibration.md) for candidates, raw evidence, limitations
and the distinction between executed and future checks. E1 audit MINOR-02 is
clarified in [e-contracts.md](e-contracts.md): positive finite radius/cutoff,
checked integer reach and overflow rejection; no approved numeric range.
E1 audit MINOR-01 (schema-1/schema-2 reader compatibility) remains open for E4.
Neither entry concerns the separately numbered historical D-FIX01 findings.

**E2-FINAL delivery:** [exact selected-profile contract](influence-profile.md)
and [verification report](e2-final.md) specify proposed revision 1, complete
independent oracles, separate full-field review vectors and PERF-01 measurements.
TEST-01 and PERF-01 have developer evidence, pending independent review; E1
MINOR-01 stays open for E4. Normal publication remains schema 1/features 7,
without Influence or Mantle. GATE-01 remains partially open.

| Item | Requirement |
| --- | --- |
| Scope | Private source extraction, nearest-boundary distance and maximum-Gaussian C/E/S fields through real Grid topology. |
| Deliverables | Internal builders, independently verified fields, measured propagation work/memory and owner-calibrated radius/cutoff policy with frozen Influence revision and vectors. |
| Invariants | Frozen R/M values and streams; no crust filtering, summation or fake mantle; no production partial-feature publication or new user stage. |
| Planned tests | E-T01..04, E-T09..10 below; compare independent all-source results, source permutations, simultaneous/zero sources, final shell, seam and poles; full R/M regressions. |
| Acceptance | Independent audit plus owner approval of numerical profile and exact output vectors; all executed results reported without invented thresholds. |
| Dependencies | E1 accepted, explicit E2 authorization; approve numerical policy before freezing it. |
| Risks | Large cutoff approaches O(SN); float evaluation order and pruning can change values; measured costs must guide approval. |

## E3 - Mantle convection

**E3-CAL02 delivery:** the owner authorized complementary experiments from the
accepted v0.0.46 base, keeping BALANCED as a conceptual direction only.
[Declared design](e3-cal02-design.md) and [results](e3-calibration-02.md) compare
operators/units, coordinate interpretation, sampling and data-derived Vref.
All alternatives are test-only; canonical CAL01 numerical components remain
unchanged. E3 MINOR-01 corrects only the old double-zero documentation claim,
preserving raw evidence. No Mantle profile, E3-FINAL, E4/E5 or closure is approved.

**E3-CAL01 delivery:** [historical reuse review](e3-history-review.md) and
[calibration report](e3-calibration.md) document unchanged sampler/RNG/Grid/
normal reuse, eight experimental Model A candidates, complete independent
potential/edge checks, numerical balance budgets and raw distributions/timings.
No candidate is selected or frozen. Normal schema 1/features 7 is unchanged;
E3-FINAL/E4/E5 require separate authorization. GATE-01 remains partially open.

| Item | Requirement |
| --- | --- |
| Scope | Private deterministic broad/regional mantle builder, preferably model A, consuming completed deformation without changing M. |
| Deliverables | Calibrated model/frequencies/weights/reference value, seed policy, fixed numeric order and error budgets, immutable value records and vectors. |
| Invariants | No Landmasses/Elevation dependency, geophysical-unit claim or feedback into R/M; no production placeholder field. |
| Planned tests | E-T05..06, E-T09..10; independent paired-edge oracle, periodic seams, polar no-flux, bounded potential, balance budget, controlled forcing and intraplate anomalies, all sizes/world types/seeds. |
| Acceptance | Owner selects/calibrates the model after independent review; conservation and deterministic claims match executed evidence. |
| Dependencies | Accepted E1/E2 and express E3 authorization. Selecting model B first requires a revised solver/profile contract review. |
| Risks | Stylized circulation, aliasing, cancellation/rounding and misleading physical interpretation; no solver tolerance is silently approved. |

## E4 - Integration and Laboratory

| Item | Requirement |
| --- | --- |
| Scope | Enable one complete schema-2 result within existing Tectonics.Execute; add independent Game views of that same result. |
| Deliverables | Full checked codec/accessors and atomic pipeline; legacy decoding; C/E/S, convection and approved optional flow views with legends; separate new full-result vectors. |
| Invariants | One Success/publication; CORE controls states/failures; no new stages or orchestrator changes; Grid always visible; same camera/selection; INICIAR unavailable. |
| Planned tests | E-T07..12; inject failures at every private module/validation boundary; retries/rebuild/historical snapshots/concurrency; view changes cannot regenerate; old full vectors compared through explicit legacy projections. |
| Acceptance | Independent integration audit and owner visual review; no claim that offscreen tests alone close native GATE-01. |
| Dependencies | E2/E3 accepted with frozen numeric policies and explicit E4 authorization; data proposal approved before codec changes. |
| Risks | Decoder corruption/overflow, transient memory copies, legacy compatibility and GPU/native input lifecycle. |

## E5 - Comprehensive verification

| Item | Requirement |
| --- | --- |
| Scope | Integrated correctness, compatibility, performance, native LAB evidence and remaining observations; no new algorithms/features. |
| Deliverables | Full test/benchmark mapping, reproducible raw evidence and environment details, independent audit submission, owner acceptance checklist. |
| Invariants | Historical evidence retained; no hidden parameter changes, fabricated timings, hardware-independent thresholds or automatic Layer 01 closure. |
| Planned tests | Entire E-T matrix, existing T01-T12 and D-FIX01 suites, CORE/Grid/menu/LAB regressions, Release Engine/Game, malformed data and real GPU/native checks where available. |
| Acceptance | Independent audit and express owner decision; unresolved native cases stay pending. Any closure/merge needs its own authorization. |
| Dependencies | E4 accepted; E5 expressly authorized; available hardware/runtime declared for each measurement. |
| Risks | Environment-limited native evidence, worst-case source density, simultaneous builders/copies and cross-runtime floating-point differences. |

## Compatibility matrix

| Protected baseline | Required evidence in later increments |
| --- | --- |
| R partition revision 2 | Same 14 owners/seeds/costs, connected regions, complete original B vectors and all 75 archived R cases. |
| M motion revision 1 | Same directions/speeds/thresholds, boundaries/classes and all 75 M cases; preserve fifteen complete M buffers. |
| Existing random policy | Exactly three original input draws; new mantle domain separated without shifting those draws; new stream vectors reviewed separately. |
| Legacy schema 1 | Ownership-only 1 and motion/boundary 7 remain readable; original thirty complete vector files retained unchanged. |
| New integrated schema | Explicit schema 2, full feature set, checked offsets and new vectors; legacy prefix projections compare all R/M words without pretending the complete new buffer is byte-identical. |
| Grid / WG-CORE | No algorithm/API change, same Grid IDs/topology/dimensions and existing BFS comparisons; same atomic lifecycle, progress and exclusivity tests. |
| Laboratory/menu | One window, unchanged navigation/camera/selection/always-visible Grid and unavailable INICIAR; no active world or saves. |
| Downstream layers | Read-only forward inputs only; no landmasses, height, crust, landforms or reverse dependencies implemented here. |
| Historical audit | Preserve D/FIX01 evidence and GATE-01: N01 owner-declared PASS, N02-N17 PENDING; E start does not resolve them. |

## Planned executable test matrix

These were **future requirements at E1**. E2-CAL01's actual executed subset
is identified in its calibration report; no E3/E4/E5 checks are claimed here.
Fixtures use approved real Grid sizes and controlled sparse source fields;
do not add tiny production MapSize values merely to simplify an oracle.

| ID | Check / independent oracle | Increment |
| --- | --- | --- |
| E-T01 | Endpoint source max from stored M components; coexisting channels, shared endpoints, zero-source fields and no crust/plate barrier. | E2 |
| E-T02 | All-source Gaussian reference with independent Grid BFS; exact float operation policy, final shell/cutoff and strongest-farther-source case. | E2 |
| E-T03 | Source permutations yield same complete field; seam routes/polar clipping, nearest-boundary distances independent of amplitude. | E2 |
| E-T04 | Invalid/nonfinite profile inputs, arithmetic overflow, radius metadata, exact distance and corruption rejection; no fake partial features. | E2/E4 |
| E-T05 | Model-A potential and paired flux reference, periodic noise, broad/regional scales, polar boundaries, ideal bounds and derived rounding budgets. | E3 |
| E-T06 | Controlled divergent/convergent forcing and intraplate anomalies; zero/noise-only cases; deterministic isolated stream and no M feedback. | E3 |
| E-T07 | One real CORE publication or none: failure injected at each module and codec, retry, rebuild, stable historical views and concurrent/reentrant rejection. | E4 |
| E-T08 | Exact layout/length/features/revisions, malicious offsets, NaN/infinity, reserved words and malformed rows; no mutable references escape. | E4 |
| E-T09 | R/M full legacy projection, all original vectors and archived cases, unchanged accepted Grid/CORE contracts. | E2-E5 |
| E-T10 | Repeated Small/Medium/Large runs and world types; raw elapsed/allocation/retained-memory evidence, warmup/batches/runtime/hardware; isolate operations and harness cost. | E2-E5 |
| E-T11 | Switch every LAB view without generation, preserve CellId/camera, visible Grid, correct units/legends, legacy absent-feature handling and unavailable INICIAR. | E4/E5 |
| E-T12 | Native input/DPI/resize/minimize/restore/close and GPU buffers across all sizes; distinguish offscreen evidence from owner/native confirmation. | E4/E5 |

## Outstanding decisions and observations

- Approve or revise schema 2 and its eight new per-cell words before E4 code.
- Calibrate Influence radius/minimum/cutoff and freeze exact float semantics in E2.
- Select mantle A or B; calibrate numeric/seed policies and error budgets in E3.
- OBS-01 crust classification remains downstream. OBS-02's approved M policy and
  OBS-03's approved R calibration stay frozen. OBS-04's optional propagation
  remains disabled in current production until separately implemented/accepted.
- GATE-01 remains partially open under [audit-fix01.md](audit-fix01.md).
  Existing native/DPI/GPU limitations are not erased by new design documents.

The E1 publication stop was followed by E2-CAL01 and then explicit E2-FINAL
authorization. Stop after E2-FINAL publication for architectural review,
independent audit and express owner acceptance of the proposed profile.
The later E3-CAL01 authorization permits only the separately recorded Model A
calibration. Stop after its publication for architectural review, independent
audit and owner parameter selection. No E3-FINAL/E4/E5, schema-2 publication or
LAB changes are authorized. GATE-01 is not resolved by this delivery.
