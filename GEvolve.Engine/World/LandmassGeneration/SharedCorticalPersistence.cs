using GEvolve.State.Game;

namespace GEvolve.Engine.World.LandmassGeneration;

/// <summary>Explicit lossless DTO conversion; no implicit migration from historical geology.</summary>
public static class SharedCorticalPersistence
{
    public static SharedCorticalState ToState(SharedCorticalModel model)
    {
        ArgumentNullException.ThrowIfNull(model);
        if (model.Version != SharedCorticalModel.CurrentVersion)
            throw new NotSupportedException("Unsupported cortical version.");
        return new SharedCorticalState(model.Version,
            (byte[])model.CrustByCellId.Clone(), (int[])model.FragmentByCellId.Clone(),
            (int[])model.ProvinceByCellId.Clone(), (float[])model.EmergenceAffinityByCellId.Clone(),
            model.Fragments.Select(f => new CorticalFragmentState(f.Id, f.CoreCellId)).ToArray(),
            model.Provinces.Select(p => new CorticalProvinceState(p.Id, p.FragmentId, p.CoreCellId)).ToArray(),
            model.AssemblyLinks.Select(l => new CorticalAssemblyStateLink(
                l.FirstFragmentId, l.SecondFragmentId, (CorticalAssemblyStateKind)l.Kind)).ToArray());
    }

    public static SharedCorticalModel FromState(SharedCorticalState state)
    {
        ArgumentNullException.ThrowIfNull(state);
        if (state.Version != SharedCorticalState.CurrentVersion)
            throw new NotSupportedException("Unsupported cortical version.");
        return new SharedCorticalModel(state.Version,
            (byte[])state.CrustByCellId.Clone(), (int[])state.FragmentByCellId.Clone(),
            (int[])state.ProvinceByCellId.Clone(), (float[])state.EmergenceAffinityByCellId.Clone(),
            state.Fragments.Select(f => new CorticalFragment(f.Id, f.CoreCellId)).ToArray(),
            state.Provinces.Select(p => new CorticalProvince(p.Id, p.FragmentId, p.CoreCellId)).ToArray(),
            state.AssemblyLinks.Select(l => new CorticalAssemblyLink(
                l.FirstFragmentId, l.SecondFragmentId, (CorticalAssemblyKind)l.Kind)).ToArray());
    }
}
