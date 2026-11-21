# CAL01 historical evidence archive

The owner selected profile R after CAL01. Current production uses only frozen
tectonic-partition-r-v2, revision 2. Historical P0/CAL01 evidence is retained;
its local-only/pending approval statements describe that earlier experiment and
are superseded by the final record in
[calibration.md](../../../../engine/worldgen/layers/tectonics/calibration.md).

## Preserved evidence

- [Original P0 measurements](../candidate-calibration.txt): unchanged archive.
- cal01/profiles.json and seeds.json: exact experiment definitions and samples.
- cal01/cases.json, cases.csv and areas.csv: all 375 cases and 5,250 plate rows.
- cal01/checks.txt: coverage, connectivity and full-replay evidence.
- cal01/summary.csv, summary.md and environment.json: statistical/method details.
- [image-index.md](cal01/image-index.md): all 23 actual PNG comparisons retained.
- cal01/local-files.csv: historical pre-finalization inventory, not a current
  delivery manifest; some source paths were archived and scene files removed.
- [archive/](archive/): original experiment code/profile/tool/readme snapshots
  with .txt suffixes, excluded from C# compilation and unavailable as runtime
  profile selectors. They document how historical outputs were produced.

The 23 bulky .scene files held regenerable centers/masks/owners used to make
the PNGs. They were removed during final cleanup; no original numeric result,
profile, PNG or compatibility evidence was deleted. No generated graphics are
bundled into Engine or Game. The text source snapshots have no build dependency
on Windows GDI+, old models, System.Drawing.Common or a dynamic profile factory.

Historical CLI options --compare-profiles/--export-control-maps and the
production experiment factory have been removed. Do not invoke archived
instructions against frozen production or overwrite historical data. Any
future comparative experiment needs separate authorization/workflow.

Current executable verification and measurement commands:
[tests readme](../readme.md).
The complete final delivery inventory is [delivery-files.csv](../delivery-files.csv).
