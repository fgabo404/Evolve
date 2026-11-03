using GEvolve.Definitions.WorldGeneration.Configuration;
using GEvolve.Engine.Grid;
using GEvolve.Engine.World.LandmassGeneration;
using GEvolve.Engine.World.LandmassGeneration.Shared;
using GEvolve.State.Game;

public static class SharedCorticalModelTests
{
    public static void Run()
    {
        var topology = new HexTopology(HexGrid.Generate(HexGridDefinitions.Get(GridSize.Small)));
        int count = topology.Count;
        int west = topology.Id(0, 20);
        int east = topology.Neighbors(west).ToArray()
            .First(id => id % topology.Columns == topology.Columns - 1);
        int second = topology.Id(12, 20);
        int third = topology.Id(14, 20);
        int oceanIsland = topology.Id(30, 20);
        var plates = new byte[count];
        plates[west] = 3; plates[east] = 7; // Same fragment spans distinct plates.
        plates[second] = 3; plates[third] = 3; // One plate hosts multiple fragments.
        var surface = new byte[count];
        surface[second] = 1;
        surface[oceanIsland] = 1; // Emerged oceanic crust is permitted.
        var crust = new byte[count];
        var fragments = Enumerable.Repeat(-1, count).ToArray();
        var provinces = Enumerable.Repeat(-1, count).ToArray();
        foreach (var (cell, fragment, province) in new[]
        {
            (west, 0, 0), (east, 0, 0), (second, 1, 1), (third, 2, 2)
        })
        {
            crust[cell] = (byte)CrustKind.Continental;
            fragments[cell] = fragment;
            provinces[cell] = province;
        }
        var model = new SharedCorticalModel(SharedCorticalModel.CurrentVersion,
            crust, fragments, provinces, new float[count],
            [new(0, west), new(1, second), new(2, third)],
            [new(0, 0, west), new(1, 1, second), new(2, 2, third)],
            [new(0, 1, CorticalAssemblyKind.Accreted),
             new(1, 2, CorticalAssemblyKind.RiftSeparated)]);

        void Valid(SharedCorticalModel candidate, byte[]? mask = null) =>
            SharedCorticalModelValidator.Validate(topology, candidate, plates, mask ?? surface);
        void Invalid(SharedCorticalModel candidate)
        {
            try { Valid(candidate); throw new Exception("Invalid cortical model was accepted."); }
            catch (ArgumentException) { }
        }

        Valid(model);
        Check(model.FragmentByCellId[west] == model.FragmentByCellId[east] &&
            plates[west] != plates[east] && plates[second] == plates[third] &&
            model.FragmentByCellId[second] != model.FragmentByCellId[third],
            "Cortical fragments are independent of tectonic plate IDs");
        Check(surface[west] == 0 && crust[west] == (byte)CrustKind.Continental &&
            surface[oceanIsland] == 1 && crust[oceanIsland] == (byte)CrustKind.Oceanic,
            "Continental crust may be submerged and oceanic crust may support emergent islands");
        Check(topology.Neighbors(west).Contains(east), "Cortical province spans east/west seam");

        Invalid(model with { CrustByCellId = new byte[count - 1] });
        Invalid(model with { ProvinceByCellId = new int[count - 1] });
        Invalid(model with { EmergenceAffinityByCellId = new float[count + 1] });
        Invalid(model with { CrustByCellId = Change(crust, west, (byte)2) });
        var nonfinite = new float[count]; nonfinite[west] = float.NaN;
        Invalid(model with { EmergenceAffinityByCellId = nonfinite });
        Invalid(model with { FragmentByCellId = Change(fragments, second, -1) });
        Invalid(model with { ProvinceByCellId = Change(provinces, second, 99) });
        Invalid(model with { Provinces = [new(0, 0, west), new(1, 0, second), new(2, 2, third)] });
        Invalid(model with { Fragments = [new(0, west), new(1, second), new(2, second)] });
        Invalid(model with { AssemblyLinks = [new(0, 1, CorticalAssemblyKind.Accreted),
            new(0, 1, CorticalAssemblyKind.RiftSeparated)] });
        Invalid(model with { AssemblyLinks = [new(1, 0, CorticalAssemblyKind.Accreted)] });
        Invalid(model with { AssemblyLinks = [new(0, 0, CorticalAssemblyKind.Accreted)] });
        Invalid(model with { AssemblyLinks = [new(0, 3, CorticalAssemblyKind.Accreted)] });
        Invalid(model with { AssemblyLinks = [new(0, 1, (CorticalAssemblyKind)255)] });

        // The top and bottom edges do NOT touch; their identical x coordinate is irrelevant.
        int top = topology.Id(5, 0), bottom = topology.Id(5, topology.Rows - 1);
        var disconnectedCrust = (byte[])crust.Clone();
        var disconnectedFragments = (int[])fragments.Clone();
        var disconnectedProvinces = (int[])provinces.Clone();
        disconnectedCrust[top] = disconnectedCrust[bottom] = (byte)CrustKind.Continental;
        disconnectedFragments[top] = disconnectedFragments[bottom] = 0;
        disconnectedProvinces[top] = disconnectedProvinces[bottom] = 0;
        Invalid(model with { CrustByCellId = disconnectedCrust,
            FragmentByCellId = disconnectedFragments, ProvinceByCellId = disconnectedProvinces });
        Check(!topology.Neighbors(top).Contains(bottom), "No north/south wrapping");
        try
        {
            SharedCorticalModelValidator.Validate(topology, model, new byte[count - 1], surface);
            throw new Exception("Mismatched plate array was accepted.");
        }
        catch (ArgumentException) { }
        try
        {
            SharedCorticalModelValidator.Validate(topology, model, plates, new byte[count - 1]);
            throw new Exception("Mismatched surface array was accepted.");
        }
        catch (ArgumentException) { }

        Console.WriteLine("PASS: H05-A1 cortical identities, references, topology and emergence independence");
    }

    private static T[] Change<T>(T[] input, int index, T value)
    {
        var clone = (T[])input.Clone(); clone[index] = value; return clone;
    }
    private static void Check(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
