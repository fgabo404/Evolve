using GEvolve.Engine.World.LandmassGeneration.Shared;
using GEvolve.State.Game;

namespace GEvolve.Engine.World.Tectonics;

/// <summary>Terrain deformation driven by the persisted velocities and real shared plate edges.</summary>
public sealed record TectonicReliefResult(
    byte[] ElevationByCellId, byte[] OceanFloorDepthByCellId, byte[] GeologicFeaturesByCellId);

public sealed class TectonicReliefGenerator
{
    public const string Version = "tectonic-relief-1";
    public const float FeatureThresholdCmPerYear = 1.0f;
    // The base elevation remains the terrain's low-frequency structure;
    // plate motion dominates the local change where active boundaries exist.
    private const double ReferenceSpeedCmPerYear = 8.0;

    public TectonicReliefResult Generate(HexTopology topology, ReadOnlySpan<byte> surface,
        ReadOnlySpan<byte> baseElevation, TectonicMotionState motion)
    {
        ArgumentNullException.ThrowIfNull(topology);
        ArgumentNullException.ThrowIfNull(motion);
        int count = topology.Count;
        if (surface.Length != count || baseElevation.Length != count ||
            motion.CompressionByCellId.Length != count ||
            motion.ExtensionByCellId.Length != count ||
            motion.ShearByCellId.Length != count ||
            motion.DistanceToBoundaryByCellId.Length != count ||
            !double.IsFinite(motion.InfluenceRadiusHexes) || motion.InfluenceRadiusHexes <= 0)
            throw new ArgumentException("Surface, elevations and motion fields must match the grid.");
        var elevation = new byte[count];
        var seafloor = new byte[count];
        var features = new byte[count];
        double reach = 2.5 * motion.InfluenceRadiusHexes;

        for (int id = 0; id < count; id++)
        {
            byte land = surface[id], original = baseElevation[id];
            float c = motion.CompressionByCellId[id], d = motion.ExtensionByCellId[id],
                s = motion.ShearByCellId[id];
            if (land is not 0 and not 1 || (land == 0 ? original != 0 : original > 100) ||
                !float.IsFinite(c) || !float.IsFinite(d) || !float.IsFinite(s) ||
                c < 0 || d < 0 || s < 0)
                throw new ArgumentException("Invalid surface, elevation or motion intensity.");

            // All three signed interactions came from actual relative plate velocities.
            // Unlike the old random pressure proxies this deformation cannot
            // occur far from a plate edge or in the absence of motion.
            bool nearBoundary = motion.DistanceToBoundaryByCellId[id] <= reach;
            double compression = nearBoundary
                ? Math.Sqrt(Math.Clamp(c / ReferenceSpeedCmPerYear, 0, 1)) : 0;
            double extension = nearBoundary
                ? Math.Sqrt(Math.Clamp(d / ReferenceSpeedCmPerYear, 0, 1)) : 0;
            double shear = nearBoundary
                ? Math.Sqrt(Math.Clamp(s / ReferenceSpeedCmPerYear, 0, 1)) : 0;
            float dominant = Math.Max(c, Math.Max(d, s));
            bool active = dominant >= FeatureThresholdCmPerYear && nearBoundary;

            if (land == 1)
            {
                // Convergence uplifts mountain belts; extension subsides rifts;
                // transform shear forms narrow fault valleys. Clamp above sea level
                // because coastline and land fraction belong to the preceding stage.
                int change = (int)Math.Round(36 * compression - 25 * extension - 16 * shear);
                elevation[id] = (byte)Math.Clamp(original + change, 1, 100);
                if (active)
                    features[id] = (byte)(c >= d && c >= s
                        ? GeologicFeature.MountainRange
                        : d >= s ? GeologicFeature.RiftValley : GeologicFeature.FaultValley);
            }
            else
            {
                // Separate relative bathymetry: ocean elevation remains exactly zero.
                // Convergent trenches deepen, divergent ridges shallow and transform
                // faults cut into the seabed. This is NOT depth in physical metres.
                seafloor[id] = (byte)Math.Clamp(
                    52 + (int)Math.Round(26 * compression - 23 * extension + 8 * shear), 1, 100);
                if (active)
                    features[id] = (byte)(c >= d && c >= s
                        ? GeologicFeature.OceanTrench
                        : d >= s ? GeologicFeature.MidOceanRidge
                            : GeologicFeature.OceanTransformFault);
            }
        }
        return new(elevation, seafloor, features);
    }
}
