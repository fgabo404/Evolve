# Frozen production R vectors

Algorithm: tectonic-partition-r-v2; partition revision 2; schema version 1.
Generated from real HexGridLayer and TectonicPlatesLayer via WG-CORE following
explicit owner selection of CAL01 R. No fixture is a P0 result relabeled as R.

Fifteen complete .words payloads cover Small/Medium/Large and seeds 0, 1, -1,
long.MinValue, long.MaxValue. Encoding is little-endian uint32, including every
header, plate and packed ownership word. Total payload bytes: 733,640.
[manifest.json](manifest.json) supplies per-case file names, canonical textual
SHA-256, origins, areas and growth-cost bits. Textual digest encoding is ASCII
comma-separated uppercase eight-digit uint hex values, without newline/BOM.

Normal regression checks read and compare every word. Hashes are supplementary.
The source exporter refuses an existing manifest; approved algorithm changes
must explicitly review and regenerate vectors together with the revision.

These are deterministic compatibility fixtures, not physical geology evidence
or independent Auditor approval. No movement/boundary/influence data is present.
