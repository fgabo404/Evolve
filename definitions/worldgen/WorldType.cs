// Identifies the four approved world configurations consumed by generation requests.
// These selections describe geography preferences without implementing generation algorithms.

namespace Evolve.Definitions.WorldGeneration;

public enum WorldType
{
    Pangea = 0,
    Continents = 1,
    Archipelagos = 2,
    RealEarth = 3
}
