using System.Text.Json;
using System.Text.Json.Serialization;
using Jobscan.Model;

namespace Jobscan.Storage;

public sealed class SeenRecord
{
    public DateTimeOffset FirstSeen { get; set; }
    public DateTimeOffset? LastSeen { get; set; }
    public string Company { get; set; } = "";
    public string Title { get; set; } = "";
    public string Url { get; set; } = "";
}

/// <summary>Seen-postings store. Keeps the backlog to what is actually new,
/// so the 6am alert means something.</summary>
public sealed class SeenStore
{
    private static readonly JsonSerializerOptions Options = new()
    {
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    private readonly string _path;
    private readonly Dictionary<string, SeenRecord> _map;

    public SeenStore(string path = "data/seen.json")
    {
        _path = path;
        _map = File.Exists(path)
            ? JsonSerializer.Deserialize<Dictionary<string, SeenRecord>>(File.ReadAllText(path), Options) ?? []
            : [];
    }

    /// <summary>Records everything; returns only what we had not seen before.</summary>
    public List<Posting> MarkAndReturnNew(IEnumerable<Posting> postings)
    {
        var now = DateTimeOffset.UtcNow;
        var fresh = new List<Posting>();
        foreach (var p in postings)
        {
            if (_map.TryGetValue(p.Id, out var rec))
            {
                rec.LastSeen = now;
            }
            else
            {
                _map[p.Id] = new SeenRecord
                {
                    FirstSeen = now, Company = p.Company, Title = p.Title, Url = p.Url,
                };
                fresh.Add(p);
            }
        }
        return fresh;
    }

    public void Save()
    {
        Directory.CreateDirectory(Path.GetDirectoryName(_path) is { Length: > 0 } d ? d : ".");
        var sorted = _map.OrderBy(kv => kv.Key).ToDictionary(kv => kv.Key, kv => kv.Value);
        File.WriteAllText(_path, JsonSerializer.Serialize(sorted, Options));
    }
}
