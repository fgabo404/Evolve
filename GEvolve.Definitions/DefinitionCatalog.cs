using System.Text.Json;

namespace GEvolve.Definitions.Catalogs;

/// <summary>Read-only JSON catalog loaded from a versioned EvolveDoc snapshot.</summary>
public sealed class DefinitionCatalog : IDisposable
{
    private readonly Dictionary<string, JsonDocument> _documents = new(StringComparer.OrdinalIgnoreCase);
    public IReadOnlyCollection<string> Paths => _documents.Keys;

    public static DefinitionCatalog Load(string root)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(root);
        if (!Directory.Exists(root)) throw new DirectoryNotFoundException(root);
        var result = new DefinitionCatalog();
        try
        {
            foreach (string file in Directory.EnumerateFiles(root, "*.json", SearchOption.AllDirectories))
            {
                var relative = Path.GetRelativePath(root, file).Replace('\\', '/');
                result._documents.Add(relative, JsonDocument.Parse(File.ReadAllText(file)));
            }
            if (result._documents.Count == 0) throw new InvalidDataException("No JSON definitions found.");
            return result;
        }
        catch { result.Dispose(); throw; }
    }

    public JsonElement Get(string relativePath) =>
        _documents[relativePath.Replace('\\', '/')].RootElement;

    public void Dispose()
    {
        foreach (var document in _documents.Values) document.Dispose();
        _documents.Clear();
    }
}
