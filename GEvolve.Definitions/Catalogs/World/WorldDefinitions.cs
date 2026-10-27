// Initial strongly-typed catalog contract. Further nested fields still need explicit modeling.
// JSON remains design reference only; this file does not enable runtime guide execution.
// C# property names map to camelCase JSON using JsonSerializerDefaults.Web.
namespace GEvolve.Definitions.Catalogs.World;
public sealed class WorldCatalog {
 public string? SchemaVersion {get;set;}
 public string? ContentVersion {get;set;}
 public string? Kind {get;set;}
 public List<BiomeDefinition> Biomes {get;set;} = [];
 public List<RegionalVariantDefinition> RegionalVariants {get;set;} = [];
 public List<WorldResourceOccurrence> ResourceOccurrences {get;set;} = [];
}
public sealed class BiomeDefinition {
 public string Id {get;set;} = "";
 public Dictionary<string,string>? Name {get;set;}
 public bool? InterfaceCategoryOnly {get;set;}
}
public sealed class RegionalVariantDefinition {
 public string Id {get;set;} = "";
 public string? BiomeId {get;set;}
 public Dictionary<string,string>? Name {get;set;}
 public List<string>? NativeRegionIds {get;set;}
}
public sealed class WorldResourceOccurrence {
 public string? Id {get;set;}
 public string? Kind {get;set;}
 public string? DefinitionId {get;set;}
 public string? SourceDefinitionId {get;set;}
 public string? BiomeId {get;set;}
 public string? VariantId {get;set;}
 public string? SourceText {get;set;}
 public bool? RuntimeEnabled {get;set;}
}
