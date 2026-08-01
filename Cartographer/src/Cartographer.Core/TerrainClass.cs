namespace Cartographer.Core;

/// <summary>
/// Closed set of terrain classes produced by sentinel-style classification.
/// </summary>
public enum TerrainClass
{
    Water,
    Swamp,
    Grass,
    Trees,
    Dirt,
    Field,
    Sand,
    DirtRoad,
    PavedRoad,
    Structure,
    Unknown,
}

/// <summary>
/// Wire and storage helpers for <see cref="TerrainClass"/>.
/// </summary>
public static class TerrainClassCodec
{
    public static string ToWire(TerrainClass value) => value switch
    {
        TerrainClass.Water => "water",
        TerrainClass.Swamp => "swamp",
        TerrainClass.Grass => "grass",
        TerrainClass.Trees => "trees",
        TerrainClass.Dirt => "dirt",
        TerrainClass.Field => "field",
        TerrainClass.Sand => "sand",
        TerrainClass.DirtRoad => "dirt_road",
        TerrainClass.PavedRoad => "paved_road",
        TerrainClass.Structure => "structure",
        TerrainClass.Unknown => "unknown",
        _ => throw new ArgumentOutOfRangeException(nameof(value), value, null),
    };

    public static bool TryParse(string? wire, out TerrainClass value)
    {
        value = TerrainClass.Unknown;
        if (string.IsNullOrWhiteSpace(wire))
        {
            return false;
        }

        switch (wire.Trim().ToLowerInvariant())
        {
            case "water":
                value = TerrainClass.Water;
                return true;
            case "swamp":
                value = TerrainClass.Swamp;
                return true;
            case "grass":
                value = TerrainClass.Grass;
                return true;
            case "trees":
                value = TerrainClass.Trees;
                return true;
            case "dirt":
                value = TerrainClass.Dirt;
                return true;
            case "field":
                value = TerrainClass.Field;
                return true;
            case "sand":
                value = TerrainClass.Sand;
                return true;
            case "dirt_road":
                value = TerrainClass.DirtRoad;
                return true;
            case "paved_road":
                value = TerrainClass.PavedRoad;
                return true;
            case "structure":
                value = TerrainClass.Structure;
                return true;
            case "unknown":
                value = TerrainClass.Unknown;
                return true;
            default:
                return false;
        }
    }

    public static TerrainClass Parse(string wire)
    {
        if (!TryParse(wire, out var value))
        {
            throw new FormatException($"Unknown terrain class '{wire}'.");
        }

        return value;
    }
}
