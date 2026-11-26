// Authors and verifies complete candidate Mantle references with raw logical hashes and explicit profile metadata.
// Keeps fifteen unique full buffers separate from sixty logical cases and immutable historical R/M/Influence fixtures.
using System.Buffers.Binary;
using System.IO.Compression;
using System.Text.Json;
using Evolve.Definitions.WorldGeneration;
using Evolve.Engine.WorldGeneration;
using Evolve.Engine.WorldGeneration.Layers.Tectonics;
using static Evolve.WorldGeneration.Tectonics.Checks.TectonicCheckFixtures;

namespace Evolve.WorldGeneration.Tectonics.Checks;

internal static class MantleCandidateVectors
{
    internal const string Encoding="CellId ascending; Potential,FlowX,FlowY,SignedVertical; IEEE754 binary32 little-endian; 16 bytes/cell; no padding/header";
    internal sealed record SeedVector(long Seed,string PrivateSeed,string Broad,string Regional);
    internal sealed record Case(string Size,long Seed,string WorldType,string File,int CellCount,int RawBytes,string RawSha256,string CompressedSha256,
        string OwnershipSha256,string MotionSha256,string InfluenceSha256,bool Cal01Cal02Overlap);
    internal sealed record Manifest(int FormatVersion,MantleProfileIdentity Identity,MantleCalibrationParameters Parameters,string Encoding,string Status,SeedVector[] Seeds,Case[] Cases);
    internal static string Hash(byte[] bytes)=>TectonicInfluenceVectors.Hash(bytes);
    internal static byte[] Serialize(MantleConvectionField field)
    {
        byte[] bytes=new byte[checked(field.Count*16)];
        for(int cell=0;cell<field.Count;cell++)
        {
            var value=field[cell];Write(0,value.Potential);Write(4,value.FlowX);Write(8,value.FlowY);Write(12,value.SignedVertical);
            void Write(int offset,float f)=>BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(cell*16+offset),BitConverter.SingleToUInt32Bits(f));
        }
        return bytes;
    }

    internal static void Export(string directory)
    {
        if(Directory.Exists(directory)&&Directory.EnumerateFileSystemEntries(directory).Any())throw new ArgumentException("Candidate export requires an empty directory.");
        MantleFinalChecks.Identity();Directory.CreateDirectory(directory);var cases=new List<Case>();
        foreach(var size in Enum.GetValues<MapSize>())foreach(long seed in TectonicInfluenceFinalChecks.Seeds)
        {
            var(grid,packed)=GenerateComplete(size,seed);var sources=TectonicInfluenceSources.Extract(grid,TectonicData.FromLayerData(packed,grid));
            var influence=TectonicInfluenceFields.Build(sources,TectonicInfluenceProfile.Resolve(grid));var distances=TectonicBoundaryDistances.Build(sources);
            var request=Request(size,seed);var noise=MantleNoiseFields.Build(grid,request,MantleHistoricalProfile.Parameters);
            var field=MantleHistoricalProfile.Build(grid,request,influence);MantleFinalChecks.Verify(grid,request,influence,noise,field);
            byte[] raw=Serialize(field);string rawHash=Hash(raw);Historical(size,seed,rawHash);
            string file=$"{size}-{seed}.mantle-candidate.br";
            using(var output=File.Create(Path.Combine(directory,file)))using(var encoder=new BrotliStream(output,CompressionLevel.SmallestSize))encoder.Write(raw);
            string compressedHash=Hash(File.ReadAllBytes(Path.Combine(directory,file)));
            string b=Legacy(packed,size,seed,true),m=Legacy(packed,size,seed,false);
            foreach(var type in Enum.GetValues<WorldType>())
            {
                var(_,again)=GenerateComplete(size,seed,type);Require(again.SequenceEqual(packed),"World type changed full R/M candidate input.");
                var againSources=TectonicInfluenceSources.Extract(grid,TectonicData.FromLayerData(again,grid));
                var againI=TectonicInfluenceFields.Build(againSources,TectonicInfluenceProfile.Resolve(grid));
                string ih=InfluenceBinding(againI,TectonicBoundaryDistances.Build(againSources),size,seed,type);
                var replay=MantleHistoricalProfile.Build(grid,Request(size,seed,type),againI);Require(raw.SequenceEqual(Serialize(replay)),"Full Mantle world-type replay differs.");
                cases.Add(new(size.ToString(),seed,type.ToString(),file,grid.CellCount,raw.Length,rawHash,compressedHash,b,m,ih,seed!=1));
            }
            Console.WriteLine($"MANTLE CANDIDATE {size}/{seed}: independent full oracle and history passed; SHA256={rawHash}");
        }
        var seedVectors=TectonicInfluenceFinalChecks.Seeds.Select(seed=>
        {
            var pair=MantleFinalChecks.Seeds(seed);ulong privateSeed=DeterministicStageRandom.DeriveSeed(seed,GenerationStage.Tectonics)^MantleHistoricalProfile.Identity.SeedDomain;
            return new SeedVector(seed,privateSeed.ToString("X16"),pair.Broad.ToString("X16"),pair.Regional.ToString("X16"));
        }).ToArray();
        var manifest=new Manifest(1,MantleHistoricalProfile.Identity,MantleHistoricalProfile.Parameters,Encoding,
            "Candidate for architectural review/audit/owner acceptance; NOT formally frozen",seedVectors,cases.ToArray());
        File.WriteAllText(Path.Combine(directory,"manifest.json"),JsonSerializer.Serialize(manifest,new JsonSerializerOptions{WriteIndented=true}));
        Console.WriteLine("MANTLE CANDIDATE EXPORT: 15 unique full buffers, 60 logical cases, 12 historical-map overlaps; no old reference regenerated.");
    }

    internal static void Verify()
    {
        string directory=Path.Combine(AppContext.BaseDirectory,"mantle-candidate-vectors");
        var manifest=JsonSerializer.Deserialize<Manifest>(File.ReadAllText(Path.Combine(directory,"manifest.json")))!;
        ValidateManifest(manifest);
        MalformedManifest(manifest);
        var hashes = manifest.Cases.GroupBy(row => row.Size).Select(group => group.Select(row => row.RawSha256).Distinct().Count());
        Require(hashes.All(count => count == 5), "Distinct seeds unexpectedly produced identical complete fields.");
        foreach(var row in manifest.Cases)
        {
            var size=Enum.Parse<MapSize>(row.Size);var type=Enum.Parse<WorldType>(row.WorldType);
            Require(row.File==$"{size}-{row.Seed}.mantle-candidate.br","Unexpected candidate reference path.");
            byte[] compressed=File.ReadAllBytes(Path.Combine(directory,row.File));
            var(grid,packed)=GenerateComplete(size,row.Seed,type);
            Require(row.CellCount==grid.CellCount&&row.RawBytes==grid.CellCount*16,"Candidate dimensions/length.");
            byte[] expected=Decode(row,compressed);
            if(row==manifest.Cases[0]) MalformedBuffer(row,compressed);
            var sources=TectonicInfluenceSources.Extract(grid,TectonicData.FromLayerData(packed,grid));
            var influence=TectonicInfluenceFields.Build(sources,TectonicInfluenceProfile.Resolve(grid));var distances=TectonicBoundaryDistances.Build(sources);
            var field=MantleHistoricalProfile.Build(grid,Request(size,row.Seed,type),influence);
            Require(expected.SequenceEqual(Serialize(field)),"Complete Mantle candidate differs word for word.");
            Require(Legacy(packed,size,row.Seed,true)==row.OwnershipSha256&&Legacy(packed,size,row.Seed,false)==row.MotionSha256&&
                InfluenceBinding(influence,distances,size,row.Seed,type)==row.InfluenceSha256,"Frozen input binding differs.");
            Historical(size,row.Seed,row.RawSha256);Require(packed[1]==1&&packed[2]==7,"Normal publication changed.");
        }
        Console.WriteLine("MANTLE CANDIDATE: 60/60 full word/hash/length/type cases; 15 unique buffers; all frozen input bindings passed.");
    }

    private static byte[] Decode(Case row, byte[] compressed)
    {
        Require(Hash(compressed)==row.CompressedSha256,"Compressed Mantle checksum.");
        byte[] expected=new byte[row.RawBytes];
        using(var stream=new MemoryStream(compressed))
        using(var decoder=new BrotliStream(stream,CompressionMode.Decompress))
        { decoder.ReadExactly(expected); Require(decoder.ReadByte()==-1,"Trailing candidate decoded bytes."); }
        Require(Hash(expected)==row.RawSha256,"Raw Mantle checksum."); return expected;
    }

    private static void MalformedBuffer(Case row, byte[] compressed)
    {
        void Reject(Case bad, byte[] data)
        {
            bool rejected=false;
            try { Decode(bad,data); } catch(InvalidOperationException) { rejected=true; } catch(IOException) { rejected=true; }
            Require(rejected,"Corrupted candidate vector was accepted.");
        }
        byte[] corrupted=compressed.ToArray(); corrupted[0]^=1; Reject(row,corrupted);
        Reject(row with {RawSha256=new string('0',64)},compressed);
        Reject(row with {RawBytes=row.RawBytes-4},compressed);
        Reject(row with {RawBytes=row.RawBytes+4},compressed);
        byte[] truncated=compressed[..(compressed.Length/2)];
        Reject(row with {CompressedSha256=Hash(truncated)},truncated);
    }

    internal static void ValidateManifest(Manifest manifest)
    {
        MantleHistoricalProfile.Validate(manifest.Identity,manifest.Parameters);
        Require(manifest.FormatVersion==1&&manifest.Encoding==Encoding&&manifest.Status.Contains("NOT formally frozen",StringComparison.Ordinal),"Candidate manifest identity/status.");
        var keys=(from size in Enum.GetValues<MapSize>() from seed in TectonicInfluenceFinalChecks.Seeds from type in Enum.GetValues<WorldType>() select $"{size}/{seed}/{type}").ToHashSet();
        Require(manifest.Cases.Length==keys.Count&&manifest.Cases.Select(row=>$"{row.Size}/{row.Seed}/{row.WorldType}").ToHashSet().SetEquals(keys),"Missing/duplicate candidate cases.");
        Require(manifest.Cases.Select(row=>row.File).Distinct().Count()==15,"Duplicate physical candidate buffer layout.");
        Require(manifest.Seeds.Length==5&&manifest.Seeds.Select(row=>row.Seed).ToHashSet().SetEquals(TectonicInfluenceFinalChecks.Seeds),"Seed matrix differs.");
        foreach(var seed in manifest.Seeds)
        {
            var expected=MantleFinalChecks.Seeds(seed.Seed);ulong domain=DeterministicStageRandom.DeriveSeed(seed.Seed,GenerationStage.Tectonics)^MantleHistoricalProfile.Identity.SeedDomain;
            Require(seed.PrivateSeed==domain.ToString("X16")&&seed.Broad==expected.Broad.ToString("X16")&&seed.Regional==expected.Regional.ToString("X16"),"Seed manifest vector differs.");
        }
        foreach(var row in manifest.Cases)
        {
            var size = Enum.Parse<MapSize>(row.Size);
            int cells = new Evolve.Engine.WorldGeneration.Layers.Grid.HexGridData(size).CellCount;
            Require(row.Cal01Cal02Overlap==(row.Seed!=1),"Historical overlap differs.");
            Require(row.File==$"{size}-{row.Seed}.mantle-candidate.br" && row.CellCount==cells && row.RawBytes==cells*16,
                "Candidate path/dimensions/length differ.");
            foreach(string hash in new[]{row.RawSha256,row.CompressedSha256,row.OwnershipSha256,row.MotionSha256,row.InfluenceSha256})
                Require(hash.Length==64 && hash.All(Uri.IsHexDigit), "Malformed SHA-256.");
        }
        foreach(var group in manifest.Cases.GroupBy(row=>row.File))
            Require(group.Select(row=>(row.RawSha256,row.CompressedSha256,row.OwnershipSha256,row.MotionSha256,row.InfluenceSha256)).Distinct().Count()==1,
                "Equivalent world types disagree on candidate/input hashes.");
    }

    // Corrupt metadata in memory only; normal verification never updates a fixture to make it pass.
    private static void MalformedManifest(Manifest manifest)
    {
        void Reject(Manifest bad)
        {
            bool rejected=false;
            try { ValidateManifest(bad); } catch(ArgumentException) { rejected=true; } catch(InvalidOperationException) { rejected=true; }
            Require(rejected,"Malformed candidate manifest was accepted.");
        }
        Reject(manifest with {FormatVersion=2}); Reject(manifest with {Encoding="native padded"});
        Reject(manifest with {Status="frozen"}); Reject(manifest with {Identity=manifest.Identity with {Revision=2}});
        Reject(manifest with {Parameters=manifest.Parameters with {BroadWeight=1}});
        Reject(manifest with {Cases=manifest.Cases.Skip(1).ToArray()});
        Case[] changed=manifest.Cases.ToArray(); changed[0]=changed[1]; Reject(manifest with {Cases=changed});
        foreach(var bad in new[]{manifest.Cases[0] with {RawBytes=1},manifest.Cases[0] with {CellCount=1},
            manifest.Cases[0] with {File="../outside.br"},manifest.Cases[0] with {RawSha256="broken"},
            manifest.Cases[0] with {Cal01Cal02Overlap=!manifest.Cases[0].Cal01Cal02Overlap}})
        { changed=manifest.Cases.ToArray(); changed[0]=bad; Reject(manifest with {Cases=changed}); }
        SeedVector[] seeds=manifest.Seeds.ToArray(); seeds[0]=seeds[0] with {Broad="0000000000000000"};
        Reject(manifest with {Seeds=seeds});
    }

    internal static void Historical(MapSize size,long seed,string hash)
    {
        if(seed==1)return;
        MantleSpatialDesign.VerifyHistorical(size.ToString(),seed,"CONTROL",hash);
        string path=Path.Combine(AppContext.BaseDirectory,"experiments","mantle-cal02","raw","metrics.csv");
        var rows=File.ReadLines(path).Skip(1).Select(line=>line.Split(',').Select(value=>value.Trim('"')).ToArray())
            .Where(row=>row[0]==size.ToString()&&row[1]==seed.ToString(System.Globalization.CultureInfo.InvariantCulture)&&row[2]=="CONTROL"&&row[3]=="1").ToArray();
        Require(rows.Length==1&&rows[0][7]==hash,"Complete CAL02 BALANCED hash differs.");
    }
    private static string Legacy(LayerData<uint> packed,MapSize size,long seed,bool ownership)
    {
        var data=ownership?OwnershipOnly(packed):packed;byte[] raw=new byte[data.Count*4];
        for(int index=0;index<data.Count;index++)BinaryPrimitives.WriteUInt32LittleEndian(raw.AsSpan(index*4),data[index]);
        byte[] expected=File.ReadAllBytes(Path.Combine(AppContext.BaseDirectory,ownership?"vectors":"motion-vectors",$"{size}-{seed}.words"));
        Require(raw.SequenceEqual(expected),"Historical B/M buffer differs.");return Hash(raw);
    }
    private static string InfluenceBinding(TectonicInfluenceFields fields,TectonicBoundaryDistances distances,MapSize size,long seed,WorldType type)
    {
        var manifest=JsonSerializer.Deserialize<TectonicInfluenceVectors.Manifest>(File.ReadAllText(Path.Combine(AppContext.BaseDirectory,"influence-vectors","manifest.json")))!;
        var row=manifest.Cases.Single(value=>value.Size==size.ToString()&&value.Seed==seed&&value.WorldType==type.ToString());
        string hash=Hash(TectonicInfluenceVectors.Serialize(fields,distances));Require(hash==row.Sha256,"Frozen Influence reference differs.");return hash;
    }
}
