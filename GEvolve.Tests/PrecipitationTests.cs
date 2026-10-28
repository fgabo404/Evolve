using System.Numerics;
using GEvolve.Definitions.WorldGeneration.Configuration;
using GEvolve.Engine.Grid;
using GEvolve.Engine.World.Climate;
using GEvolve.Engine.World.LandmassGeneration;
using GEvolve.Engine.World.LandmassGeneration.Shared;
using GEvolve.Game.Graphics;
using GEvolve.State.Game;
using GEvolve.State.Persistence;

public static class PrecipitationTests
{
    public static void Run()
    {
        var generator = new PrecipitationGenerator();
        Check(PrecipitationGenerator.Wind(10).East < 0 &&
            PrecipitationGenerator.Wind(45).East > 0 &&
            PrecipitationGenerator.Wind(85).East < 0,
            "Trades, westerlies and polar circulation have expected orientations");
        Check(PrecipitationGenerator.CirculationRainFactor(0) >
            PrecipitationGenerator.CirculationRainFactor(29),
            "Subtropical subsidence is drier than the equatorial convergence belt");

        VerifyRainShadow(generator);
        int dryTotal=0, wetTotal=0, landTotal=0;
        foreach(var size in Enum.GetValues<GridSize>())
        foreach(int seed in new[] { 2026,42,-731 })
        {
            var world=new LandmassGenerationService().Generate(
                LandmassProfiles.Create(size,LandmassType.Pangaea,seed));
            var land=world.Landmass;
            var climate=land.Climate!;
            var rain=climate.BasePrecipitationMmByCellId!;
            var humidity=climate.BaseMoistureByCellId!;
            Check(climate.PrecipitationModelVersion==PrecipitationGenerator.Version &&
                rain.Length==land.SurfaceByCellId.Length &&
                humidity.Length==rain.Length && humidity.All(x=>x<=100),
                "Rainfall and humidity cover the whole world and are correctly versioned");
            var repeated=generator.Apply(new HexTopology(world.Grid),land);
            Check(rain.SequenceEqual(repeated.Climate!.BasePrecipitationMmByCellId!) &&
                humidity.SequenceEqual(repeated.Climate.BaseMoistureByCellId!) &&
                ReferenceEquals(repeated.Biomes,land.Biomes) &&
                ReferenceEquals(repeated.SurfaceByCellId,land.SurfaceByCellId) &&
                ReferenceEquals(repeated.ElevationByCellId,land.ElevationByCellId) &&
                ReferenceEquals(repeated.PlateByCellId,land.PlateByCellId),
                "Rain generation is deterministic and preserves terrain and assigned biomes");
            int dry=0,wet=0,landCount=0;
            for(int id=0;id<rain.Length;id++)
            {
                if (!land.IsLand(id)) continue;
                landCount++;
                if (rain[id]<250) dry++;
                if (rain[id]>=1000) wet++;
            }
            dryTotal+=dry;wetTotal+=wet;landTotal+=landCount;
            Check(rain.Max()>rain.Min() && rain.Distinct().Take(30).Count()>20,
                "Precipitation varies geographically instead of uniform latitude bands");
            Console.WriteLine(FormattableString.Invariant(
                $"RAIN {size}/{seed}: land {landCount}, min {rain.Min()} mm, max {rain.Max()} mm, mean {rain.Average(x=>(double)x):F1} mm, dry {100.0*dry/landCount:F1}%, wet {100.0*wet/landCount:F1}%"));
            if (seed==2026) VerifyPersistence(world);
            int testId=Array.FindIndex(land.SurfaceByCellId,x=>x==1);
            Check(MapViewColors.ForCell(land,testId,MapViewMode.Rainfall)==
                RainfallPalette.ForMillimetres(rain[testId]),
                "Rainfall map color uses saved precipitation");
        }
        Check(dryTotal>0 && wetTotal>0,
            "Nine reproducible worlds contain both dry and rainy terrestrial cells");
        Console.WriteLine(FormattableString.Invariant(
            $"RAIN AGGREGATE: {landTotal} land cells, dry {100.0*dryTotal/landTotal:F2}%, wet {100.0*wetTotal/landTotal:F2}%"));
    }

    private static void VerifyRainShadow(PrecipitationGenerator generator)
    {
        var definition=HexGridDefinitions.Get(GridSize.Small);
        var topology=new HexTopology(HexGrid.Generate(definition));
        int width=definition.Columns,rows=definition.Rows,count=topology.Count;
        var surface=Enumerable.Repeat((byte)1,count).ToArray();
        var heights=new byte[count];
        for(int row=0;row<rows;row++)
        for(int col=0;col<width;col++)
        {
            int id=row*width+col;
            if(col>=width*7/10 && col<width*9/10) surface[id]=0;
            if(col==width/2) heights[id]=70;
        }
        var climate=new ClimateState(TemperatureGenerator.Version,
            Enumerable.Repeat((short)250,count).ToArray());
        var terrain=new LandmassState(LandmassProfiles.Create(
            GridSize.Small,LandmassType.Pangaea,2026),surface,0)
            { ElevationByCellId=heights,Climate=climate };
        var output=generator.Apply(topology,terrain).Climate!;
        int rowIndex=rows/2;
        int ridge=rowIndex*width+width/2;
        int leeward=rowIndex*width+width/2-4;
        Check(output.BasePrecipitationMmByCellId![ridge]>
            output.BasePrecipitationMmByCellId[leeward],
            "Windward ridge is wetter than its leeward shadow");
        Check(generator.Apply(topology,terrain with {ElevationByCellId=null}).Climate==climate,
            "No elevation means precipitation is not invented");
    }

    private static void VerifyPersistence(GeneratedWorld world)
    {
        string path=Path.Combine(Path.GetTempPath(),$"evolve-rain-{Guid.NewGuid():N}.json");
        try
        {
            var saved=new GameState { HexGrid=new(world.Grid.Definition),
                Landmass=world.Landmass };
            GameSaveStore.Save(path,saved);
            var loaded=GameSaveStore.Load(path).Landmass!.Climate!;
            Check(loaded.BasePrecipitationMmByCellId!.SequenceEqual(
                world.Landmass.Climate!.BasePrecipitationMmByCellId!) &&
                loaded.BaseMoistureByCellId!.SequenceEqual(
                world.Landmass.Climate.BaseMoistureByCellId!),
                "Rain and moisture survive save/load");
            saved.Landmass=world.Landmass with { Climate=world.Landmass.Climate with {
                BasePrecipitationMmByCellId=null, BaseMoistureByCellId=null,
                PrecipitationModelVersion=null } };
            GameSaveStore.Save(path,saved);
            Check(GameSaveStore.Load(path).Landmass!.Climate!.BasePrecipitationMmByCellId is null,
                "Legacy saves without precipitation remain valid");
        }
        finally {if(File.Exists(path))File.Delete(path);}
    }

    private static void Check(bool condition,string message)
    {
        if(!condition)throw new InvalidOperationException(message);
    }
}
