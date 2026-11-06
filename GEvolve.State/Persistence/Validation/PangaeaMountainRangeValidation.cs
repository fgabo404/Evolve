using GEvolve.Definitions.WorldGeneration.Configuration;
using GEvolve.State.Game;

namespace GEvolve.State.Persistence;

internal static class PangaeaMountainRangeValidation
{
    public static void Validate(LandmassState terrain)
    {
        if (terrain.PangaeaMountainRanges is not { } state) return;
        int count = terrain.SurfaceByCellId.Length;
        if (terrain.Configuration.Type != LandmassType.Pangaea || !terrain.HasSurface ||
            terrain.ElevationByCellId is null || terrain.TectonicMotion is null ||
            state.Version is not ("pangaea-ranges-1" or "pangaea-ranges-2") || state.Ranges is null ||
            state.RangeByCellId is null || state.RangeByCellId.Length != count ||
            state.Ranges.Length > ushort.MaxValue ||
            state.RangeByCellId.Any(value => value > state.Ranges.Length))
            throw new InvalidDataException("Invalid Pangaea mountain range state.");
        for (int index = 0; index < state.Ranges.Length; index++)
        {
            var range = state.Ranges[index];
            if (range is null || range.Id != index + 1 ||
                range.AxisCellIds is null || range.WidthByAxisCell is null ||
                range.AxisCellIds.Length < 2 ||
                range.AxisCellIds.Length != range.WidthByAxisCell.Length ||
                !float.IsFinite(range.PeakConvergence) || range.PeakConvergence < 1 ||
                range.WidthByAxisCell.Any(width => width is < 2 or > 12) ||
                range.AxisCellIds.Any(id => (uint)id >= (uint)count ||
                    terrain.SurfaceByCellId[id] != 1 || state.RangeByCellId[id] != range.Id) ||
                range.AxisCellIds.Distinct().Count() != range.AxisCellIds.Length ||
                state.Version == "pangaea-ranges-2" &&
                (range.FirstPlateId >= 16 || range.SecondPlateId >= 16 ||
                 range.FirstPlateId >= range.SecondPlateId ||
                 !float.IsFinite(range.MeanConvergence) || range.MeanConvergence < 1 ||
                 !float.IsFinite(range.Elongation) || range.Elongation < 1 ||
                 range.ContactEndpoints < 0 || range.ContactCycles < 0 ||
                 range.BranchingCells < 0 || range.AffectedCells < 0))
                throw new InvalidDataException("Invalid Pangaea mountain range geometry.");
        }
        for (int id = 0; id < count; id++)
            if (terrain.SurfaceByCellId[id] == 0 && state.RangeByCellId[id] != 0)
                throw new InvalidDataException("A mountain range reaches ocean.");
    }
}
