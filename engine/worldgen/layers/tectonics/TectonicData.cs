// Validates and decodes schema-1 R ownership and optional approved M kinematics without exposing mutable storage.
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
    private readonly HexGridData grid;
    public int CellCount => checked((int)words[4]);
    public int PlateCount => 14;
    public MapSize Size => (MapSize)words[3];
    public uint SchemaVersion => words[1];
    public uint PartitionRevision => words[16];
    public uint MotionRevision => words[17];
    public uint InfluenceRevision => words[18];
    public bool HasMotion => words[2] == 7;
    public bool HasBoundaries => words[2] == 7;
    public bool HasInfluence => false;
    public int BoundaryCount => checked((int)words[11]);

    private TectonicData(LayerData<uint> words, HexGridData grid) => (this.words, this.grid) = (words, grid);

    // Validate once at the trust boundary; accessors require no regeneration or retained scratch.
    public static TectonicData FromLayerData(LayerData data, HexGridData grid)
    {
        ArgumentNullException.ThrowIfNull(data);
        ArgumentNullException.ThrowIfNull(grid);
        if (data.Stage != GenerationStage.Tectonics || data is not LayerData<uint> packed)
            throw new ArgumentException("Expected a packed Tectonics result.", nameof(data));
        Validate(packed, grid);
        return new(packed, grid);
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

    public PlateMotion GetMotion(byte plateId)
    {
        if (plateId >= PlateCount) throw new ArgumentOutOfRangeException(nameof(plateId));
        if (!HasMotion) throw new InvalidOperationException("Motion is unavailable in Ownership-only data.");
        return Motion(words, plateId);
    }

    public TectonicBoundary GetBoundary(int boundaryIndex)
    {
        if ((uint)boundaryIndex >= (uint)BoundaryCount) throw new ArgumentOutOfRangeException(nameof(boundaryIndex));
        int start = checked((int)words[10] + boundaryIndex * 5);
        int first = (int)words[start], second = (int)words[start + 1];
        return new(first, second, GetPlateId(first), GetPlateId(second), PlateBoundaryBuilder.Normal(grid, first, second),
            BitConverter.UInt32BitsToSingle(words[start + 2]), BitConverter.UInt32BitsToSingle(words[start + 3]),
            (TectonicInteractionKind)words[start + 4]);
    }

    // A single codec owns all offsets; publication never contains draft arrays or reader references.
    internal static LayerData<uint> CreateCandidate(HexGridData grid, TectonicDraft draft, TectonicGenerationProfile profile)
    {
        ArgumentNullException.ThrowIfNull(grid);
        ArgumentNullException.ThrowIfNull(draft);
        ArgumentNullException.ThrowIfNull(profile);
        if (draft.Owners.Length != grid.CellCount || draft.Plates.Length != 14)
            throw new ArgumentException("Draft dimensions do not match Grid.", nameof(draft));
        if ((draft.Motions is null) != (draft.Boundaries is null) || draft.Motions is { Length: not 14 })
            throw new ArgumentException("Motion and boundaries must be present together.", nameof(draft));
        int boundaryOffset = checked(OwnerOffset + (grid.CellCount + 3) / 4);
        int boundaryCount = draft.Boundaries?.Length ?? 0;
        uint[] buffer = new uint[checked(boundaryOffset + boundaryCount * 5)];
        buffer[0] = Magic; buffer[1] = 1; buffer[2] = draft.Motions is null ? 1u : 7u;
        buffer[3] = (uint)SizeOf(grid); buffer[4] = (uint)grid.CellCount; buffer[5] = 14;
        buffer[6] = HeaderWords; buffer[7] = PlateWords; buffer[8] = OwnerOffset;
        buffer[9] = (uint)((grid.CellCount + 3) / 4); buffer[10] = (uint)boundaryOffset;
        buffer[11] = (uint)boundaryCount;
        buffer[12] = 5; buffer[15] = (uint)buffer.Length; buffer[16] = TectonicGenerationProfile.PartitionRevision;
        if (draft.Motions is not null)
        {
            buffer[17] = PlateMotionGenerator.Revision;
            buffer[19] = BitConverter.SingleToUInt32Bits(PlateMotionGenerator.Activity);
            buffer[20] = BitConverter.SingleToUInt32Bits(PlateMotionGenerator.MixedRatio);
        }
        for (byte plateId = 0; plateId < 14; plateId++)
        {
            var plate = draft.Plates[plateId];
            if (plate.PlateId != plateId) throw new ArgumentException("Draft plate rows must be ordered by identity.", nameof(draft));
            int start = HeaderWords + plateId * PlateWords;
            buffer[start] = checked((uint)plate.SeedCellId); buffer[start + 1] = checked((uint)plate.CellCount);
            buffer[start + 2] = BitConverter.SingleToUInt32Bits(plate.GrowthCost);
            if (draft.Motions is { } motions)
            {
                if (motions[plateId].PlateId != plateId) throw new ArgumentException("Motion identities must match their rows.");
                WriteDouble(buffer, start + 3, motions[plateId].DirectionDegrees);
                WriteDouble(buffer, start + 5, motions[plateId].SpeedCmPerYear);
            }
        }
        for (int cell = 0; cell < draft.Owners.Length; cell++)
            buffer[OwnerOffset + cell / 4] |= (uint)draft.Owners[cell] << (8 * (cell % 4));
        for (int index = 0; index < boundaryCount; index++)
        {
            var boundary = draft.Boundaries![index];
            int start = boundaryOffset + index * 5;
            buffer[start] = checked((uint)boundary.FirstCellId); buffer[start + 1] = checked((uint)boundary.SecondCellId);
            buffer[start + 2] = BitConverter.SingleToUInt32Bits(boundary.SignedNormal);
            buffer[start + 3] = BitConverter.SingleToUInt32Bits(boundary.SignedTangent);
            buffer[start + 4] = (uint)boundary.Kind;
        }
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

    private static double ReadDouble(IReadOnlyList<uint> data, int start) =>
        BitConverter.UInt64BitsToDouble(data[start] | ((ulong)data[start + 1] << 32));
    private static void WriteDouble(uint[] data, int start, double value)
    {
        ulong bits = BitConverter.DoubleToUInt64Bits(value);
        data[start] = (uint)bits; data[start + 1] = (uint)(bits >> 32);
    }
    private static PlateMotion Motion(IReadOnlyList<uint> data, byte plate) =>
        new(plate, ReadDouble(data, HeaderWords + plate * PlateWords + 3), ReadDouble(data, HeaderWords + plate * PlateWords + 5));

    private static void Validate(IReadOnlyList<uint> data, HexGridData grid)
    {
        int count = grid.CellCount;
        long ownerWords = ((long)count + 3) / 4, boundaryOffset = OwnerOffset + ownerWords;
        if (data.Count < boundaryOffset) throw new ArgumentException("Truncated tectonic schema.");
        bool motion = data[2] == 7;
        long expectedWords = boundaryOffset + (motion ? (long)data[11] * 5 : 0);
        // Validate exact lengths and fixed offsets before reading untrusted metadata or allocating traversal buffers.
        if (data.Count != expectedWords || data[0] != Magic || data[1] != 1 || (data[2] != 1 && !motion) ||
            data[3] != (uint)SizeOf(grid) || data[4] != count || data[5] != 14 ||
            data[6] != HeaderWords || data[7] != PlateWords || data[8] != OwnerOffset ||
            data[9] != ownerWords || data[10] != boundaryOffset || (!motion && data[11] != 0) || data[12] != 5 ||
            data[13] != 0 || data[14] != 0 || data[15] != expectedWords || data[16] != TectonicGenerationProfile.PartitionRevision)
            throw new ArgumentException("Invalid ownership-only tectonic schema.");
        if (motion && (data[17] != PlateMotionGenerator.Revision ||
            data[19] != BitConverter.SingleToUInt32Bits(PlateMotionGenerator.Activity) ||
            data[20] != BitConverter.SingleToUInt32Bits(PlateMotionGenerator.MixedRatio)))
            throw new ArgumentException("Unknown motion profile or thresholds.");
        for (int index = 17; index < HeaderWords; index++)
        {
            if (motion && index is 17 or 19 or 20) continue;
            if (data[index] != 0) throw new ArgumentException("Unexpected optional feature or reserved word.");
        }
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
            if (motion)
            {
                var value = Motion(data, id);
                // Re-encoding canonical values rejects negative zero and noncanonical stationary angles.
                if (BitConverter.DoubleToUInt64Bits(value.DirectionDegrees) != (data[start + 3] | ((ulong)data[start + 4] << 32)) ||
                    BitConverter.DoubleToUInt64Bits(value.SpeedCmPerYear) != (data[start + 5] | ((ulong)data[start + 6] << 32)))
                    throw new ArgumentException("Noncanonical motion zero.");
            }
            for (int field = motion ? 7 : 3; field < PlateWords; field++)
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
        if (motion) ValidateBoundaries(data, grid);
    }

    // A bounded six-ID scratch scan proves completeness, order and geometry without retaining another edge set.
    private static void ValidateBoundaries(IReadOnlyList<uint> data, HexGridData grid)
    {
        int index = 0, count = checked((int)data[11]), offset = checked((int)data[10]);
        Span<int> neighbors = stackalloc int[6];
        for (int cell = 0; cell < grid.CellCount; cell++)
        {
            int length = 0;
            for (int direction = 0; direction < 6; direction++)
                if (grid.TryGetNeighbor(cell, (HexDirection)direction, out int next) && cell < next && Owner(data, cell) != Owner(data, next))
                    neighbors[length++] = next;
            neighbors[..length].Sort();
            for (int neighbor = 0; neighbor < length; neighbor++)
            {
                if (index >= count) throw new ArgumentException("Missing tectonic boundary.");
                int start = offset + index++ * 5, next = neighbors[neighbor];
                if (data[start] != cell || data[start + 1] != next) throw new ArgumentException("Noncanonical or incomplete boundary set.");
                var expected = PlateBoundaryBuilder.Project(grid, cell, next, Motion(data, Owner(data, cell)), Motion(data, Owner(data, next)));
                if (data[start + 2] != BitConverter.SingleToUInt32Bits(expected.SignedNormal) ||
                    data[start + 3] != BitConverter.SingleToUInt32Bits(expected.SignedTangent) || data[start + 4] != (uint)expected.Kind)
                    throw new ArgumentException("Boundary projection or classification mismatch.");
            }
        }
        if (index != count) throw new ArgumentException("Unexpected extra tectonic boundaries.");
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

// Mutable build buffers remain private to one attempt; optional influences are deliberately absent.
internal sealed record TectonicDraft(byte[] Owners, TectonicPlate[] Plates, TectonicGenerationMetrics Metrics,
    PlateMotion[]? Motions = null, TectonicBoundary[]? Boundaries = null);
internal readonly record struct TectonicGenerationMetrics(double SeedMilliseconds, double NoiseMilliseconds,
    double GrowthMilliseconds, int PeakFrontier, long AllocatedBytes);
