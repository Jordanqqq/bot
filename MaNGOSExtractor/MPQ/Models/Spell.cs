namespace MaNGOSExtractor.MPQ.Models;

public class Spell
{
    public int Id { get; set; }
    public int Category { get; set; }
    public int DispelType { get; set; }
    public int Mechanic { get; set; }
    public int Attributes { get; set; }
    public int AttributesEx { get; set; }
    public int AttributesEx2 { get; set; }
    public int AttributesEx3 { get; set; }
    public int AttributesEx4 { get; set; }
    public int AttributesEx5 { get; set; }
    public int AttributesEx6 { get; set; }
    public int AttributesEx7 { get; set; }
    public int School { get; set; }
    public int CostLevel1 { get; set; }
    public int CostLevel2 { get; set; }
    public int CostLevel3 { get; set; }
    public int CostLevel4 { get; set; }
    public int CostLevel5 { get; set; }
    public int CostLevel6 { get; set; }
    public int CostLevel7 { get; set; }
    public int CostLevel8 { get; set; }
    public int SpellLevel { get; set; }
    public int MaxLevel { get; set; }
    public int CastTimeId { get; set; }
    public int DurationId { get; set; }
    public int RangeId { get; set; }
    public int IconId { get; set; }
    public string NameEn { get; set; }
    public string NameRu { get; set; }
    public string RankEn { get; set; }
    public string RankRu { get; set; }
    public string DescriptionEn { get; set; }
    public string DescriptionRu { get; set; }
    public string ToolTipEn { get; set; }
    public string ToolTipRu { get; set; }
    public int ManaCost { get; set; }
    public int ManaCostPerLevel { get; set; }
    public int ManaPerSecond { get; set; }
    public int ManaPerSecondPerLevel { get; set; }
    public int PowerType { get; set; }
}

public class SpellCastTime
{
    public int Id { get; set; }
    public int CastTimeMs { get; set; }
    public int CastTimePerLevel { get; set; }
}

public class SpellDuration
{
    public int Id { get; set; }
    public int DurationMs { get; set; }
    public int DurationPerLevel { get; set; }
    public int MaxDuration { get; set; }
}

public class SpellRange
{
    public int Id { get; set; }
    public float MinRange { get; set; }
    public float MinRangeFriendly { get; set; }
    public float MaxRange { get; set; }
    public float MaxRangeFriendly { get; set; }
    public string NameEn { get; set; }
    public string NameRu { get; set; }
    public string ShortNameEn { get; set; }
    public string ShortNameRu { get; set; }
}

public class SpellIcon
{
    public int Id { get; set; }
    public string IconPath { get; set; }
}