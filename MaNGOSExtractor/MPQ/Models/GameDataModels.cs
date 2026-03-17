namespace MaNGOSExtractor.Models;

// Главный контейнер — ВСЕ ДАННЫЕ В ОДНОМ ФАЙЛЕ
public class FullGameData
{
    public DateTime ExtractedAt { get; set; } = DateTime.UtcNow;
    public string Version { get; set; } = "3.3.5";

    // MySQL данные
    public List<MySqlItem> Items { get; set; } = new();
    public List<MySqlCreature> Creatures { get; set; } = new();
    public List<MySqlLoot> Loot { get; set; } = new();

    // DBC данные
    public List<DbcItemDisplayInfo> ItemDisplayInfo { get; set; } = new();
    public List<DbcSpell> Spells { get; set; } = new();
    public List<DbcSpellCastTime> SpellCastTimes { get; set; } = new();
    public List<DbcSpellDuration> SpellDurations { get; set; } = new();
    public List<DbcSpellRange> SpellRanges { get; set; } = new();
    public List<DbcCreatureDisplayInfo> CreatureDisplayInfo { get; set; } = new();
    public List<DbcMap> Maps { get; set; } = new();
    public List<DbcAreaTable> Areas { get; set; } = new();
    public List<DbcDungeonEncounter> DungeonEncounters { get; set; } = new();
    public List<DbcChrClasses> Classes { get; set; } = new();
    public List<DbcChrRaces> Races { get; set; } = new();
    public List<DbcCombatRating> CombatRatings { get; set; } = new();

    // Метаданные для быстрого поиска
    public Dictionary<string, int> Stats { get; set; } = new();
}

// MySQL модели
public class MySqlItem
{
    public int Entry { get; set; }
    public string Name { get; set; }
    public int Quality { get; set; }
    public int Class { get; set; }
    public int Subclass { get; set; }
    public int DisplayId { get; set; }
    public int ItemLevel { get; set; }
    public int RequiredLevel { get; set; }
    public int InventoryType { get; set; }
}

public class MySqlCreature
{
    public int Entry { get; set; }
    public string Name { get; set; }
    public int Rank { get; set; }
    public int ZoneId { get; set; }
    public int MapId { get; set; }
    public int DisplayId { get; set; }
}

public class MySqlLoot
{
    public int Entry { get; set; }
    public int Item { get; set; }
    public float Chance { get; set; }
    public int GroupId { get; set; }
    public int MinCount { get; set; }
    public int MaxCount { get; set; }
}

// DBC модели (короткие, только нужное)
public class DbcItemDisplayInfo
{
    public int Id { get; set; }
    public string IconName { get; set; }
}

public class DbcSpell
{
    public int Id { get; set; }
    public string NameEn { get; set; }
    public string NameRu { get; set; }
    public int CastTimeId { get; set; }
    public int DurationId { get; set; }
    public int RangeId { get; set; }
    public int IconId { get; set; }
}

public class DbcSpellCastTime
{
    public int Id { get; set; }
    public int CastTimeMs { get; set; }
}

public class DbcSpellDuration
{
    public int Id { get; set; }
    public int DurationMs { get; set; }
}

public class DbcSpellRange
{
    public int Id { get; set; }
    public float MinRange { get; set; }
    public float MaxRange { get; set; }
    public string NameEn { get; set; }
}

public class DbcCreatureDisplayInfo
{
    public int Id { get; set; }
    public string IconName { get; set; }
}

public class DbcMap
{
    public int Id { get; set; }
    public string NameEn { get; set; }
    public string NameRu { get; set; }
    public int MapType { get; set; }
}

public class DbcAreaTable
{
    public int Id { get; set; }
    public int MapId { get; set; }
    public string NameEn { get; set; }
    public string NameRu { get; set; }
    public int AreaLevel { get; set; }
}

public class DbcDungeonEncounter
{
    public int Id { get; set; }
    public int MapId { get; set; }
    public int Difficulty { get; set; }
    public int CreatureId { get; set; }
    public string NameEn { get; set; }
    public string NameRu { get; set; }
}

public class DbcChrClasses
{
    public int Id { get; set; }
    public string NameEn { get; set; }
    public string NameRu { get; set; }
}

public class DbcChrRaces
{
    public int Id { get; set; }
    public string NameEn { get; set; }
    public string NameRu { get; set; }
}

public class DbcCombatRating
{
    public int Level { get; set; }
    public float MeleeCrit { get; set; }
    public float RangedCrit { get; set; }
    public float SpellCrit { get; set; }
    public float Dodge { get; set; }
    public float Parry { get; set; }
    public float Block { get; set; }
    public float Hit { get; set; }
    public float SpellHit { get; set; }
    public float Resilience { get; set; }
    public float Haste { get; set; }
    public float SpellHaste { get; set; }
}