// Initial strongly-typed catalog contract. Further nested fields still need explicit modeling.
// JSON remains design reference only; this file does not enable runtime guide execution.
// C# property names map to camelCase JSON using JsonSerializerDefaults.Web.
namespace GEvolve.Definitions.Catalogs.Technologies;
public sealed class AgricultureTechnologyCatalog {
 public string? SchemaVersion {get;set;}
 public string? ContentVersion {get;set;}
 public string? Kind {get;set;}
 public string? Status {get;set;}
 public string? Id {get;set;}
 public Dictionary<string,string>? Name {get;set;}
 public List<AgricultureTechnologyDefinition> Technologies {get;set;} = [];
}
public sealed class AgricultureTechnologyDefinition {
 public string Id {get;set;} = "";
 // Technology effects/condition grammar are intentionally not executable yet.
}