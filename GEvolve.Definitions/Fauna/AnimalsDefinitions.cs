// Initial strongly-typed catalog contract. Further nested fields still need explicit modeling.
// JSON remains design reference only; this file does not enable runtime guide execution.
// C# property names map to camelCase JSON using JsonSerializerDefaults.Web.
namespace GEvolve.Definitions.Catalogs.Animals;
public sealed class AnimalCatalog {
 public string? SchemaVersion {get;set;}
 public string? ContentVersion {get;set;}
 public string? Kind {get;set;}
 public string? Status {get;set;}
 public bool? RuntimeDisabled {get;set;}
 public List<AnimalDefinition> SpeciesCandidates {get;set;} = [];
 public List<EntityAlias> EntityAliases {get;set;} = [];
 public List<AnimalAcquisitionMethod> AcquisitionMethods {get;set;} = [];
}
public sealed class AnimalDefinition {
 public string Id {get;set;} = "";
 public string? Tag {get;set;}
 public Dictionary<string,string>? Name {get;set;}
 public string? Kind {get;set;}
 public string? DefinitionStatus {get;set;}
 public List<string>? NativeRegionIds {get;set;}
 public List<string>? BiomeVariantIds {get;set;}
}
public sealed class AnimalAcquisitionMethod {
 public string? Tag {get;set;}
 public Dictionary<string,string>? Name {get;set;}
 public bool? RuntimeEnabled {get;set;}
}
public sealed class EntityAlias {
 public string PreviousId {get;set;} = "";
 public string EntityId {get;set;} = "";
}
public sealed class AnimalBiologicalCatalog {
 public string? SchemaVersion {get;set;}
 public string? ContentVersion {get;set;}
 public string? Kind {get;set;}
 public List<BiologicalTaxon> Taxa {get;set;} = [];
 public List<BiologicalForm> BiologicalForms {get;set;} = [];
}
public sealed class BiologicalTaxon {
 public string Id {get;set;} = "";
 public string? Tag {get;set;}
 public Dictionary<string,string>? Name {get;set;}
 public string? ScientificName {get;set;}
 public bool? RuntimeEnabled {get;set;}
}
public sealed class BiologicalForm {
 public string Id {get;set;} = "";
 public string? TaxonId {get;set;}
 public string? Tag {get;set;}
 public Dictionary<string,string>? Name {get;set;}
 public bool? RuntimeEnabled {get;set;}
}