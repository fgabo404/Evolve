# Influence review references

These fixtures describe the owner-selected HISTORICAL candidate for E2-FINAL,
`tectonic-influence-historical-v1`, proposed revision 1. They are pinned for
exact regression review; definitive profile acceptance remains pending.
They do not enable Influence, a production data schema, Mantle or Laboratory views.

The [manifest](manifest.json) lists all 60 cases: Small/Medium/Large x seeds
0, 1, -1, long.MinValue, long.MaxValue x all four world types. Fifteen unique
lossless `.influence.br` files serve equivalent world types; the exporter and
normal verification actually generate and compare all sixty cases.

After Brotli decompression, each CellId in ascending order has exactly sixteen
bytes: uint32LE nearest-boundary distance, followed by binary32LE Compression,
Extension and Shear. No header is inserted and no production schema number is
assigned. Expected lengths are 320,000 / 720,000 / 1,280,000 bytes. There is no
mutable reference to these fixtures in production.

The manifest records raw and compressed SHA-256, exact parameter words, numeric
revision, dimensions and original B/M hashes. The tests decompress to a bounded
expected length, reject trailing decoded bytes, compare every stored byte with
fresh production internal fields and verify the unchanged historical R/M
buffers. A shared file across world types does not replace those executions.

Export is an explicit authoring operation and refuses a nonempty directory.
It first verifies policy/dense fixtures and, for every unique map, complete
independent source/distance/envelope oracles before writing a buffer. Do not
regenerate these merely to make a failing test pass. Follow the
[numeric contract](../../../../engine/worldgen/layers/tectonics/influence-profile.md)
and [test commands](../readme.md) for reviewed reproduction to a new directory.
