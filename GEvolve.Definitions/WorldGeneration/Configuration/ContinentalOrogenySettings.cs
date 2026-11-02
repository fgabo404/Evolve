namespace GEvolve.Definitions.WorldGeneration.Configuration;

/// <summary>Resolution-aware acceptance rules for terrestrial mountain ranges.</summary>
public static class ContinentalOrogenySettings
{
    public const int MountainElevation = 44;
    public const double MinimumProminence = 12;
    public static double ResolutionScale(int rows) => rows / 133.0;
    public static int MinimumCrestLength(int rows) => (int)Math.Ceiling(rows * .10);

    public static bool MeetsPrincipalGeometry(int rows, int length, int connectedCells,
        double width, double prominence)
    {
        double scale = ResolutionScale(rows);
        int minimum = MinimumCrestLength(rows);
        return length >= minimum && connectedCells >= minimum * 2 * scale &&
            width >= 2 * scale && prominence >= MinimumProminence;
    }
}
