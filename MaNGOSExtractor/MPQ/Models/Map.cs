namespace MaNGOSExtractor.MPQ.Models;

public class Map
{
    public int Id { get; set; }
    public string Directory { get; set; }
    public int MapType { get; set; }
    public bool IsPVP { get; set; }
    public bool IsRaid { get; set; }
    public string NameEn { get; set; }
    public string NameRu { get; set; }
    public int MinLevel { get; set; }
    public int MaxLevel { get; set; }
    public int MaxPlayers { get; set; }
}

public class AreaTable
{
    public int Id { get; set; }
    public int MapId { get; set; }
    public int ZoneId { get; set; }
    public int ExploreFlag { get; set; }
    public int Flags { get; set; }
    public int SoundPreferences { get; set; }
    public int SoundPreferences2 { get; set; }
    public int SoundPreferences3 { get; set; }
    public int SoundPreferences4 { get; set; }
    public int AreaLevel { get; set; }
    public string NameEn { get; set; }
    public string NameRu { get; set; }
    public int Team { get; set; }
    public int LiquidOverride { get; set; }
    public float MinElevation { get; set; }
    public float AmbientMultiplier { get; set; }
    public int LightId { get; set; }
}

public class DungeonEncounter
{
    public int Id { get; set; }
    public int MapId { get; set; }
    public int Difficulty { get; set; }
    public int OrderIndex { get; set; }
    public int CreatureId { get; set; }
    public string NameEn { get; set; }
    public string NameRu { get; set; }
    public int SpellIconId { get; set; }
    public int Flags { get; set; }
}