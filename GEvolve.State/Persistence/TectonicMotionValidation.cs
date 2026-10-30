using GEvolve.State.Game;

namespace GEvolve.State.Persistence;

internal static class TectonicMotionValidation
{
    public static void Validate(LandmassState terrain)
    {
        if (terrain.TectonicMotion is not { } motion) return;
        int count = terrain.SurfaceByCellId.Length;
        var plates = terrain.PlateByCellId;
        bool Nonnegative(double value) => double.IsFinite(value) && value >= 0;
        bool InvalidField(float[]? field) => field is null || field.Length != count || field.Any(v => !Nonnegative(v));
        if (plates is null || plates.Any(p => p >= 16) || string.IsNullOrWhiteSpace(motion.Version) ||
            motion.Motions is null || motion.Motions.Length != 16 || motion.Interactions is null ||
            InvalidField(motion.CompressionByCellId) || InvalidField(motion.ExtensionByCellId) || InvalidField(motion.ShearByCellId) ||
            motion.DistanceToBoundaryByCellId is null || motion.DistanceToBoundaryByCellId.Length != count ||
            motion.ArrowCellByPlateId is null || motion.ArrowCellByPlateId.Length != 16 ||
            !double.IsFinite(motion.InfluenceRadiusHexes) || motion.InfluenceRadiusHexes <= 0)
            throw new InvalidDataException("Invalid tectonic motion arrays.");
        for (int id = 0; id < 16; id++)
        {
            var plate = motion.Motions[id];
            int anchor = motion.ArrowCellByPlateId[id];
            if (plate is null || plate.PlateId != id || !Nonnegative(plate.DirectionDegrees) ||
                plate.DirectionDegrees >= 360 || !Nonnegative(plate.SpeedCmPerYear) || plate.SpeedCmPerYear <= 0 ||
                (uint)anchor >= (uint)count || plates[anchor] != id)
                throw new InvalidDataException("Invalid plate motion or anchor.");
        }
        foreach (var edge in motion.Interactions)
            if ((uint)edge.FirstCellId >= (uint)count || (uint)edge.SecondCellId >= (uint)count ||
                edge.FirstCellId >= edge.SecondCellId || edge.FirstPlateId == edge.SecondPlateId ||
                edge.FirstPlateId != plates[edge.FirstCellId] || edge.SecondPlateId != plates[edge.SecondCellId] ||
                !Nonnegative(edge.Convergence) || !Nonnegative(edge.Divergence) || !Nonnegative(edge.Shear) ||
                !double.IsFinite(edge.Normal.X) || !double.IsFinite(edge.Normal.Y) ||
                Math.Abs(edge.Normal.X * edge.Normal.X + edge.Normal.Y * edge.Normal.Y - 1) > 1e-6 ||
                !Enum.IsDefined(edge.Kind))
                throw new InvalidDataException("Invalid plate boundary interaction.");
    }
}
