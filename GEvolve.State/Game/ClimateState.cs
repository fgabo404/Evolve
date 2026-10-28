namespace GEvolve.State.Game;

/// <summary>Persisted annual baseline and optional sparse offsets, all in tenths of °C.
/// No current-temperature copy or climatic events are maintained.</summary>
public sealed record ClimateState(string ModelVersion, short[] BaseTemperatureTenthsByCellId)
{
    public short GlobalOffsetTenths { get; init; }
    public Dictionary<int, short>? RegionalOffsetsTenthsByCellId { get; init; }

    public double BaseCelsius(int cellId) => BaseTemperatureTenthsByCellId[cellId] / 10.0;
    public double CurrentCelsius(int cellId) =>
        (BaseTemperatureTenthsByCellId[cellId] + GlobalOffsetTenths +
         (RegionalOffsetsTenthsByCellId?.GetValueOrDefault(cellId) ?? 0)) / 10.0;
}
