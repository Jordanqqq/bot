namespace MaNGOSExtractor.MPQ.Models;

public class Spell
{
    public int Id { get; set; }
    public string NameRu { get; set; }    // Из локали
    public string NameEn { get; set; }
    public string DescriptionRu { get; set; }
    public string DescriptionEn { get; set; }
    public int CastTimeId { get; set; }    // Ссылка на SpellCastTimes.dbc
    public int DurationId { get; set; }     // Ссылка на SpellDuration.dbc
    public int RangeId { get; set; }        // Ссылка на SpellRange.dbc
    public int IconId { get; set; }
    public int School { get; set; }         // Школа магии
    public int ManaCost { get; set; }
    public int Cooldown { get; set; }
}

public class SpellCastTime
{
    public int Id { get; set; }
    public int CastTimeMs { get; set; }     // Время каста в миллисекундах
}

public class SpellDuration
{
    public int Id { get; set; }
    public int DurationMs { get; set; }     // Длительность эффекта
}

public class SpellRange
{
    public int Id { get; set; }
    public float MinRange { get; set; }
    public float MaxRange { get; set; }
    public string NameRu { get; set; }
    public string NameEn { get; set; }
}