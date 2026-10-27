// Initial strongly-typed catalog contract. Further nested fields still need explicit modeling.
// JSON remains design reference only; this file does not enable runtime guide execution.
// C# property names map to camelCase JSON using JsonSerializerDefaults.Web.
namespace GEvolve.Definitions.Catalogs.Plants;
public sealed class PlantCatalog {
 public string? SchemaVersion {get;set;}
 public string? ContentVersion {get;set;}
 public string? Kind {get;set;}
 public List<PlantDefinition> SpeciesCandidates {get;set;} = [];
 public List<CropDefinition> AgriculturalCropCandidates {get;set;} = [];
 public List<PlantAlias> EntityAliases {get;set;} = [];
}
public sealed class PlantDefinition {
 public string Id {get;set;} = "";
 public string? Tag {get;set;}
 public Dictionary<string,string>? Name {get;set;}
 public string? Kind {get;set;}
 public string? DefinitionStatus {get;set;}
 public List<string>? NativeRegionIds {get;set;}
 public List<string>? BiomeVariantIds {get;set;}
}
public sealed class CropDefinition {
 public string Id {get;set;} = "";
 public Dictionary<string,string>? Name {get;set;}
 public string? CropUseTag {get;set;}
 public string? ScientificName {get;set;}
}
public sealed class PlantAlias {
 public string PreviousId {get;set;} = "";
 public string EntityId {get;set;} = "";
}
public sealed class PlantBiologicalCatalog {
 public string? SchemaVersion {get;set;}
 public string? ContentVersion {get;set;}
 public List<PlantTaxon> Taxa {get;set;} = [];
 public List<PlantBiologicalForm> BiologicalForms {get;set;} = [];
}
public sealed class PlantTaxon {
 public string Id {get;set;} = "";
 public string? Tag {get;set;}
 public Dictionary<string,string>? Name {get;set;}
 public string? ScientificName {get;set;}
 public bool? RuntimeEnabled {get;set;}
}
public sealed class PlantBiologicalForm {
 public string Id {get;set;} = "";
 public string? Tag {get;set;}
 public string? TaxonId {get;set;}
 public Dictionary<string,string>? Name {get;set;}
 public bool? RuntimeEnabled {get;set;}
}