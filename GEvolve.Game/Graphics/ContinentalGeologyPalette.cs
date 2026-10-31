using System.Numerics;
using GEvolve.State.Game;

namespace GEvolve.Game.Graphics;

public static class ContinentalGeologyPalette
{
    public static Vector3 ForCell(LandmassState terrain, int id)
    {
        var geology = terrain.ContinentalGeology!;
        return (ContinentalLandform)geology.LandformByCellId[id] switch
        {
            ContinentalLandform.MountainBelt => new(.60f, .43f, .32f),
            ContinentalLandform.Rift => new(.55f, .32f, .65f),
            ContinentalLandform.Fault => new(.85f, .65f, .28f),
            ContinentalLandform.Trench => new(.06f, .10f, .29f),
            ContinentalLandform.Ridge => new(.16f, .70f, .78f),
            ContinentalLandform.VolcanicArc => new(.95f, .32f, .15f),
            _ => terrain.IsLand(id) ? new(.65f, .73f, .50f) :
                geology.CrustByCellId[id] == 1 ? new(.28f, .54f, .66f) : new(.12f, .32f, .50f)
        };
    }
}
