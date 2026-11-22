# Frozen WG-01-C M vectors

These fifteen complete little-endian uint32 buffers were exported explicitly
from production TectonicPlatesLayer through WG-CORE after the owner approved
the exact M policy. They include all schema-1 flags-7 sections, motion revision 1
(tectonic-motion-m-v1) and unchanged R partition revision 2.

All three sizes and seeds 0,1,-1,long.MinValue,long.MaxValue are represented.
Normal checks compare every word and supplementary canonical-text SHA-256.
The original ../vectors/ buffers remain unchanged and their original complete
B schema is compared using an explicit test-only Ownership projection. This
removes only the newly defined C fields; it never edits accepted fixture files.

The export command refuses a nonempty output directory. Normal tests never
update fixtures. Experiment fingerprints in ../experiments/motion-cal01/ are
historical comparative evidence, not substitutes for these production buffers.
