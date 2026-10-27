// Initial strongly-typed catalog contract. Further nested fields still need explicit modeling.
// JSON remains design reference only; this file does not enable runtime guide execution.
// C# property names map to camelCase JSON using JsonSerializerDefaults.Web.
namespace GEvolve.Definitions.Catalogs.Minerals;
public sealed class MineralCatalog {
 public string? SchemaVersion {get;set;}
 public string? ContentVersion {get;set;}
 public string? Kind {get;set;}
 public List<MineralDefinition> GeologicalResourceCandidates {get;set;} = [];
}
public sealed class MineralDefinition {
 public string Id {get;set;} = "";
 public Dictionary<string,string>? Name {get;set;}
 public string? DefinitionStatus {get;set;}
 public string? RefinedOutputResourceId {get;set;}
 public string? ExtractedResourceId {get;set;}
 public string? SourceLabel {get;set;}
}