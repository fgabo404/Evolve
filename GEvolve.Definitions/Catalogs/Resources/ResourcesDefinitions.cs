// Initial strongly-typed catalog contract. Further nested fields still need explicit modeling.
// JSON remains design reference only; this file does not enable runtime guide execution.
// C# property names map to camelCase JSON using JsonSerializerDefaults.Web.
namespace GEvolve.Definitions.Catalogs.Resources;
public sealed class ResourceCatalog {
 public string? SchemaVersion {get;set;}
 public string? ContentVersion {get;set;}
 public string? Kind {get;set;}
 public List<ResourceDefinition> Resources {get;set;} = [];
 public List<ResourceSourceBinding> SourceBindings {get;set;} = [];
 public List<ProcessingLink> ProcessingLinks {get;set;} = [];
}
public sealed class ResourceDefinition {
 public string Id {get;set;} = "";
 public string? Tag {get;set;}
 public Dictionary<string,string>? Name {get;set;}
 public Dictionary<string,string>? Description {get;set;}
 public string? Category {get;set;}
 public string? Stage {get;set;}
 public List<string>? Tags {get;set;}
 public string? DefinitionStatus {get;set;}
}
public sealed class ResourceSourceBinding {
 public string? SourceCollection {get;set;}
 public string? SourceId {get;set;}
 public string? ResourceId {get;set;}
 public string? AcquisitionTag {get;set;}
 public string? ReviewStatus {get;set;}
 public bool? RuntimeEnabled {get;set;}
}
public sealed class ProcessingLink {
 public string? ProcessTag {get;set;}
 public List<string>? InputResourceIds {get;set;}
 public List<string>? OutputResourceIds {get;set;}
 public List<string>? RequiredTechnologyIds {get;set;}
 public bool? RuntimeEnabled {get;set;}
}