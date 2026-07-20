using System.Text.Json;
using System.Text.Json.Serialization;

namespace Jobscan.Config;

/// <summary>
/// profile.jsonc: the machine-readable slice of brief.md.
/// brief.md is the prose and the source of truth. If they disagree, this has the bug.
/// </summary>
public sealed class Profile
{
    public CompConfig Comp { get; set; } = new();
    public LocationConfig Location { get; set; } = new();
    public LevelConfig Level { get; set; } = new();
    public StackConfig Stack { get; set; } = new();
    public List<string> GapsHard { get; set; } = [];
    public List<string> ScamSignals { get; set; } = [];
    public DayShapeConfig DayShape { get; set; } = new();
    public Rubric Rubric { get; set; } = new();
    public Dictionary<string, double> DomainAffinity { get; set; } = [];
    public AiSignals AiFriendlySignals { get; set; } = new();
    public Thresholds Thresholds { get; set; } = new();
}

public sealed class CompConfig
{
    public int SalaryFloor { get; set; } = 135_000;
    public int HourlyFloor { get; set; } = 65;
}

public sealed class LocationConfig
{
    public bool RemoteOk { get; set; } = true;
    public string Home { get; set; } = "";
    public List<string> OnsiteAllow { get; set; } = [];
    public List<string> OnsiteDeny { get; set; } = [];
}

public sealed class LevelConfig
{
    public List<string> Accept { get; set; } = [];
    public List<string> Reject { get; set; } = [];
}

public sealed class StackConfig
{
    public Dictionary<string, int> Primary { get; set; } = [];
    public Dictionary<string, int> Adjacent { get; set; } = [];
    public Dictionary<string, int> Dated { get; set; } = [];
}

public sealed class DayShapeConfig
{
    public List<string> RunSignals { get; set; } = [];
    public List<string> BuildSignals { get; set; } = [];
}

/// <summary>brief.md section 13. Weights sum to 100.</summary>
public sealed class Rubric
{
    public int StackDepth { get; set; } = 35;
    public int Domain { get; set; } = 15;
    public int Level { get; set; } = 15;
    public int Comp { get; set; } = 10;
    public int Freshness { get; set; } = 10;
    public int RemoteCulture { get; set; } = 10;
    public int AiFriendly { get; set; } = 5;

    public int Total => StackDepth + Domain + Level + Comp + Freshness + RemoteCulture + AiFriendly;
}

public sealed class AiSignals
{
    public List<string> Positive { get; set; } = [];
    public List<string> Negative { get; set; } = [];
}

public sealed class Thresholds
{
    public int BacklogMinScore { get; set; } = 45;
    public int PriorityMinScore { get; set; } = 70;
}

// ---------------------------------------------------------------------------

/// <summary>companies.jsonc: the watchlist.</summary>
public sealed class Watchlist
{
    public List<CompanyEntry> Companies { get; set; } = [];
    public List<CompanyEntry> Feeds { get; set; } = [];
    public List<AggregatorEntry> Aggregators { get; set; } = [];
}

public sealed class CompanyEntry
{
    public string Name { get; set; } = "";
    public string Board { get; set; } = "";        // greenhouse | lever | ashby | rss | workday
    public string? Token { get; set; }
    public bool Active { get; set; } = true;
    public string Domain { get; set; } = "other";
    public string? Notes { get; set; }
}

public sealed class AggregatorEntry
{
    public string Name { get; set; } = "";
    public string Kind { get; set; } = "";
    public string Url { get; set; } = "";
    public bool Active { get; set; } = true;
    public string Domain { get; set; } = "other";
}

// ---------------------------------------------------------------------------

public static class ConfigLoader
{
    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        ReadCommentHandling = JsonCommentHandling.Skip,   // JSONC: comments allowed
        AllowTrailingCommas = true,
        NumberHandling = JsonNumberHandling.AllowReadingFromString,
    };

    public static T Load<T>(string path) where T : new()
    {
        if (!File.Exists(path))
            throw new FileNotFoundException($"config not found: {path}");
        var json = File.ReadAllText(path);
        return JsonSerializer.Deserialize<T>(json, Options)
               ?? throw new InvalidDataException($"config parsed to null: {path}");
    }
}
