// Validates and decodes the ownership-only schema-1 tectonic result without exposing mutable storage.
// Packs one copied LayerData<uint>; immutable views remain stable independently of GenerationContext rebuilding.
using Evolve.Definitions.WorldGeneration;
using Evolve.Engine.WorldGeneration.Layers.Grid;
using Evolve.Engine.WorldGeneration.Layers.Grid.Geometry;

namespace Evolve.Engine.WorldGeneration.Layers.Tectonics;

public sealed class TectonicData
{
    private const uint Magic = 0x54454354;
    private const int HeaderWords = 32, PlateWords = 8, OwnerOffset = 144;
    private readonly LayerData<uint> words;
    public int CellCount => checked((int)words[4]);
    public int PlateCount => 14;
    public MapSize Size => (MapSize)words[3];
    public uint SchemaVersion => words[1];
    public uint PartitionRevision => words[16];
    public uint MotionRevision => words[17];
    public uint InfluenceRevision => words[18];
    public bool HasMotion => false;
    public bool HasBoundaries => false;
    public bool HasInfluence => false;
    public int BoundaryCount => 0;

    private TectonicData(LayerData<uint> words) => this.words = words;

    // Validate once at the trust boundary; accessors require no regeneration or retained scratch.
    public static TectonicData FromLayerData(LayerData data, HexGridData grid)
    {
        ArgumentNullException.ThrowIfNull(data);
        ArgumentNullException.ThrowIfNull(grid);
        if (data.Stage != GenerationStage.Tectonics || data is not LayerData<uint> packed)
            throw new ArgumentException("Expected a packed Tectonics result.", nameof(data));
        Validate(packed, grid);
        return new(packed);
    }

    public byte GetPlateId(int cellId)
    {
        if ((uint)cellId >= (uint)CellCount) throw new ArgumentOutOfRangeException(nameof(cellId));
        return Owner(words, cellId);
    }

    public TectonicPlate GetPlate(byte plateId)
    {
        if (plateId >= PlateCount) throw new ArgumentOutOfRangeException(nameof(plateId));
        return Plate(words, plateId);
    }

    // A single codec owns all offsets; publication never contains draft arrays or reader references.
    internal static LayerData<uint> CreateCandidate(HexGridData grid, TectonicDraft draft, TectonicGenerationProfile profile)
    {
        ArgumentNullException.ThrowIfNull(grid);
        ArgumentNullException.ThrowIfNull(draft);
        ArgumentNullException.ThrowIfNull(profile);
        if (draft.Owners.Length != grid.CellCount || draft.Plates.Length != 14)
            throw new ArgumentException("Draft dimensions do not match Grid.", nameof(draft));
        uint[] buffer = new uint[checked(OwnerOffset + (grid.CellCount + 3) / 4)];
        buffer[0] = Magic; buffer[1] = 1; buffer[2] = 1;
        buffer[3] = (uint)SizeOf(grid); buffer[4] = (uint)grid.CellCount; buffer[5] = 14;
        buffer[6] = HeaderWords; buffer[7] = PlateWords; buffer[8] = OwnerOffset;
        buffer[9] = (uint)((grid.CellCount + 3) / 4); buffer[10] = (uint)buffer.Length;
        buffer[12] = 5; buffer[15] = (uint)buffer.Length; buffer[16] = TectonicGenerationProfile.PartitionRevision;
        for (byte plateId = 0; plateId < 14; plateId++)
        {
            var plate = draft.Plates[plateId];
            if (plate.PlateId != plateId) throw new ArgumentException("Draft plate rows must be ordered by identity.", nameof(draft));
            int start = HeaderWords + plateId * PlateWords;
            buffer[start] = checked((uint)plate.SeedCellId); buffer[start + 1] = checked((uint)plate.CellCount);
            buffer[start + 2] = BitConverter.SingleToUInt32Bits(plate.GrowthCost);
        }
        for (int cell = 0; cell < draft.Owners.Length; cell++)
            buffer[OwnerOffset + cell / 4] |= (uint)draft.Owners[cell] << (8 * (cell % 4));
        Validate(buffer, grid);
        return new LayerData<uint>(GenerationStage.Tectonics, buffer);
    }

    private static MapSize SizeOf(HexGridData grid)
    {
        // The accepted Grid constructor remains the sole dimensions table; this is only its reverse lookup.
        foreach (var size in Enum.GetValues<MapSize>())
        {
            var preset = new HexGridData(size);
            if (grid.Columns == preset.Columns && grid.Rows == preset.Rows) return size;
        }
        throw new ArgumentException("Unsupported Grid dimensions.", nameof(grid));
    }

    private static TectonicPlate Plate(IReadOnlyList<uint> data, byte plate)
    {
        int start = HeaderWords + plate * PlateWords;
        return new(plate, checked((int)data[start]), checked((int)data[start + 1]), BitConverter.UInt32BitsToSingle(data[start + 2]));
    }

    private static byte Owner(IReadOnlyList<uint> data, int cell) => (byte)(data[OwnerOffset + cell / 4] >> (8 * (cell % 4)));

    private static void Validate(IReadOnlyList<uint> data, HexGridData grid)
    {
        int count = grid.CellCount;
        long ownerWords = ((long)count + 3) / 4, expectedWords = OwnerOffset + ownerWords;
        // Validate exact lengths and fixed offsets before reading untrusted metadata or allocating traversal buffers.
        if (data.Count != expectedWords || data[0] != Magic || data[1] != 1 || data[2] != 1 ||
            data[3] != (uint)SizeOf(grid) || data[4] != count || data[5] != 14 ||
            data[6] != HeaderWords || data[7] != PlateWords || data[8] != OwnerOffset ||
            data[9] != ownerWords || data[10] != expectedWords || data[11] != 0 || data[12] != 5 ||
            data[13] != 0 || data[14] != 0 || data[15] != expectedWords || data[16] != TectonicGenerationProfile.PartitionRevision)
            throw new ArgumentException("Invalid ownership-only tectonic schema.");
        for (int index = 17; index < HeaderWords; index++)
            if (data[index] != 0) throw new ArgumentException("Unexpected optional feature or reserved word.");
        int[] counts = new int[14];
        for (int cell = 0; cell < count; cell++)
        {
            byte plate = Owner(data, cell);
            if (plate >= 14) throw new ArgumentException("Cell ownership must be in 0..13.");
            counts[plate]++;
        }
        bool[] costRanks = new bool[14];
        for (byte id = 0; id < 14; id++)
        {
            int start = HeaderWords + id * PlateWords;
            if (data[start] >= count || data[start + 1] == 0 || data[start + 1] != counts[id])
                throw new ArgumentException("Invalid plate seed or cell count.");
            var plate = Plate(data, id);
            if (Owner(data, plate.SeedCellId) != id || !float.IsFinite(plate.GrowthCost) || plate.GrowthCost <= 0)
                throw new ArgumentException("Invalid plate origin or cost.");
            int rank = -1;
            for (int candidate = 0; candidate < 14; candidate++)
                if (plate.GrowthCost == TectonicGenerationProfile.Approved.GetCost(candidate)) rank = candidate;
            if (rank < 0 || costRanks[rank]) throw new ArgumentException("Growth costs must be a permutation of the revision profile.");
            costRanks[rank] = true;
            for (int field = 3; field < PlateWords; field++)
                if (data[start + field] != 0) throw new ArgumentException("Motion and reserved plate words must be absent.");
        }
        // One shared visited/queue pair traverses each cell once; no per-plate world or persistent neighbor cache.
        bool[] visited = new bool[count];
        int[] queue = new int[count];
        for (byte plate = 0; plate < 14; plate++)
        {
            int seed = Plate(data, plate).SeedCellId, head = 0, tail = 1;
            queue[0] = seed; visited[seed] = true;
            while (head < tail)
            {
                int cell = queue[head++];
                for (int direction = 0; direction < 6; direction++)
                {
                    if (grid.TryGetNeighbor(cell, (HexDirection)direction, out int next) &&
                        !visited[next] && Owner(data, next) == plate)
                    { visited[next] = true; queue[tail++] = next; }
                }
            }
            if (tail != counts[plate]) throw new ArgumentException("Each plate must form one connected region.");
        }
    }
}

// Exposes one plate's immutable ownership statistics and canonical growth cost as values.
public readonly record struct TectonicPlate
{
    public byte PlateId { get; }
    public int SeedCellId { get; }
    public int CellCount { get; }
    public float GrowthCost { get; }
    internal TectonicPlate(byte plateId, int seedCellId, int cellCount, float growthCost) =>
        (PlateId, SeedCellId, CellCount, GrowthCost) = (plateId, seedCellId, cellCount, growthCost);
}

// Mutable buffers never leave one layer attempt; optional C features are deliberately not implemented.
internal sealed record TectonicDraft(byte[] Owners, TectonicPlate[] Plates, TectonicGenerationMetrics Metrics);
internal readonly record struct TectonicGenerationMetrics(double SeedMilliseconds, double NoiseMilliseconds,
    double GrowthMilliseconds, int PeakFrontier, long AllocatedBytes);
