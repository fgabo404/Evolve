using GEvolve.Definitions.WorldGeneration.Configuration;
using GEvolve.Simulator.Grid;

var builder = WebApplication.CreateBuilder(args);
builder.WebHost.UseUrls("http://127.0.0.1:8766");
builder.Services.AddSingleton<GridSession>();

var app = builder.Build();
app.UseDefaultFiles();
app.UseStaticFiles();
app.MapGet("/api/grid/sizes", () => HexGridDefinitions.All.Select(definition => new
{
    id = definition.Size.ToString().ToLowerInvariant(),
    columns = definition.Columns,
    rows = definition.Rows
}));
app.MapGet("/api/grid", (GridSession session) => session.Get());
app.MapGet("/api/grid/state", (GridSession session) => session.GetState());
app.MapGet("/api/grid/cells/{column:int}/{row:int}", (int column, int row, GridSession session) =>
{
    var cell = session.GetCell(column, row);
    return cell is null ? Results.NotFound() : Results.Ok(cell);
});
app.MapPost("/api/grid", (GridSelection request, GridSession session) =>
    Enum.TryParse<GridSize>(request.Size, true, out var size) && Enum.IsDefined(size)
        ? Results.Ok(session.Select(size))
        : Results.BadRequest(new { error = "Unknown grid size." }));
app.Run();

public sealed record GridSelection(string Size);
