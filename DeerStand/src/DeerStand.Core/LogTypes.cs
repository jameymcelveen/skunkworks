namespace DeerStand.Core;

/// <summary>Activity log entry type. Stored as a lowercase string in the database.</summary>
public static class LogTypes
{
    public const string Sighting = "sighting";
    public const string Harvest = "harvest";
    public const string BaitSite = "bait_site";
    public const string Photo = "photo";

    public static bool IsValid(string logType) =>
        logType is Sighting or Harvest or BaitSite or Photo;
}
