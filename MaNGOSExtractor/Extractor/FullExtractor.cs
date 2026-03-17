using System.Text.Json;
using MaNGOSExtractor.MPQ;
using MaNGOSExtractor.Models;
using MaNGOSExtractor.Services;

namespace MaNGOSExtractor.Extractors;

public class FullExtractor
{
    private readonly DatabaseService _db;
    private readonly MpqExtractor _mpq;
    private readonly string _outputPath;

    public FullExtractor(DatabaseService db, MpqExtractor mpq, string outputPath)
    {
        _db = db;
        _mpq = mpq;
        _outputPath = outputPath;
    }

    public async Task<FullGameData> ExtractAllAsync()
    {
        Console.WriteLine("\n" + "=".PadRight(50, '='));
        Console.WriteLine("🔍 ПОЛНОЕ ИЗВЛЕЧЕНИЕ ВСЕХ ДАННЫХ");
        Console.WriteLine("=".PadRight(50, '='));

        var data = new FullGameData();

        // 1. MySQL данные
        await ExtractMySqlData(data);

        // 2. DBC данные
        ExtractDbcData(data);

        // 3. Сохраняем статистику
        data.Stats = new Dictionary<string, int>
        {
            ["items"] = data.Items.Count,
            ["creatures"] = data.Creatures.Count,
            ["loot"] = data.Loot.Count,
            ["spells"] = data.Spells.Count,
            ["maps"] = data.Maps.Count,
            ["areas"] = data.Areas.Count,
            ["encounters"] = data.DungeonEncounters.Count
        };

        Console.WriteLine("\n" + "=".PadRight(50, '='));
        Console.WriteLine("📊 ИТОГОВАЯ СТАТИСТИКА");
        Console.WriteLine("=".PadRight(50, '='));
        foreach (var stat in data.Stats)
        {
            Console.WriteLine($"   {stat.Key.PadRight(15)}: {stat.Value,6}");
        }

        return data;
    }

    private async Task ExtractMySqlData(FullGameData data)
    {
        Console.WriteLine("\n📦 MySQL: Загрузка данных...");

        // Предметы
        data.Items = await _db.QueryAsync(
            "SELECT entry, name, Quality, class, subclass, displayid, ItemLevel, RequiredLevel, InventoryType FROM item_template",
            reader => new MySqlItem
            {
                Entry = reader.GetInt32("entry"),
                Name = reader.GetString("name"),
                Quality = reader.GetInt32("Quality"),
                Class = reader.GetInt32("class"),
                Subclass = reader.GetInt32("subclass"),
                DisplayId = reader.GetInt32("displayid"),
                ItemLevel = reader.GetInt32("ItemLevel"),
                RequiredLevel = reader.GetInt32("RequiredLevel"),
                InventoryType = reader.GetInt32("InventoryType")
            });
        Console.WriteLine($"   ✅ Предметов: {data.Items.Count}");

        // Существа (боссы)
        data.Creatures = await _db.QueryAsync(
            "SELECT entry, name, rank, zoneid, map, displayid FROM creature_template WHERE rank >= 3",
            reader => new MySqlCreature
            {
                Entry = reader.GetInt32("entry"),
                Name = reader.GetString("name"),
                Rank = reader.GetInt32("rank"),
                ZoneId = reader.GetInt32("zoneid"),
                MapId = reader.GetInt32("map"),
                DisplayId = reader.GetInt32("displayid")
            });
        Console.WriteLine($"   ✅ Существ: {data.Creatures.Count}");

        // Лут
        data.Loot = await _db.QueryAsync(
            "SELECT entry, item, ChanceOrQuestChance, groupid, mincountOrRef, maxcount FROM creature_loot_template WHERE item > 0",
            reader => new MySqlLoot
            {
                Entry = reader.GetInt32("entry"),
                Item = reader.GetInt32("item"),
                Chance = reader.GetFloat("ChanceOrQuestChance"),
                GroupId = reader.GetInt32("groupid"),
                MinCount = reader.GetInt32("mincountOrRef"),
                MaxCount = reader.GetInt32("maxcount")
            });
        Console.WriteLine($"   ✅ Лута: {data.Loot.Count}");
    }

    private void ExtractDbcData(FullGameData data)
    {
        Console.WriteLine("\n📚 DBC: Загрузка данных из MPQ...");

        // ItemDisplayInfo
        data.ItemDisplayInfo = _mpq.DbcParser.ParseItemDisplayInfo();
        Console.WriteLine($"   ✅ ItemDisplayInfo: {data.ItemDisplayInfo.Count}");

        // Spells
        data.Spells = _mpq.DbcParser.ParseSpells();
        Console.WriteLine($"   ✅ Spell.dbc: {data.Spells.Count}");

        data.SpellCastTimes = _mpq.DbcParser.ParseSpellCastTimes();
        Console.WriteLine($"   ✅ SpellCastTimes: {data.SpellCastTimes.Count}");

        data.SpellDurations = _mpq.DbcParser.ParseSpellDurations();
        Console.WriteLine($"   ✅ SpellDuration: {data.SpellDurations.Count}");

        data.SpellRanges = _mpq.DbcParser.ParseSpellRanges();
        Console.WriteLine($"   ✅ SpellRange: {data.SpellRanges.Count}");

        // CreatureDisplayInfo
        data.CreatureDisplayInfo = _mpq.DbcParser.ParseCreatureDisplayInfo();
        Console.WriteLine($"   ✅ CreatureDisplayInfo: {data.CreatureDisplayInfo.Count}");

        // Maps
        data.Maps = _mpq.DbcParser.ParseMaps();
        Console.WriteLine($"   ✅ Map.dbc: {data.Maps.Count}");

        // Areas
        data.Areas = _mpq.DbcParser.ParseAreas();
        Console.WriteLine($"   ✅ AreaTable: {data.Areas.Count}");

        // DungeonEncounters
        data.DungeonEncounters = _mpq.DbcParser.ParseDungeonEncounters();
        Console.WriteLine($"   ✅ DungeonEncounter: {data.DungeonEncounters.Count}");

        // Classes
        data.Classes = _mpq.DbcParser.ParseClasses();
        Console.WriteLine($"   ✅ ChrClasses: {data.Classes.Count}");

        // Races
        data.Races = _mpq.DbcParser.ParseRaces();
        Console.WriteLine($"   ✅ ChrRaces: {data.Races.Count}");

        // CombatRatings
        data.CombatRatings = _mpq.DbcParser.ParseCombatRatings();
        Console.WriteLine($"   ✅ GtCombatRatings: {data.CombatRatings.Count}");
    }

    public async Task SaveToJson(FullGameData data)
    {
        var options = new JsonSerializerOptions
        {
            WriteIndented = true,
            Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
        };

        var json = JsonSerializer.Serialize(data, options);
        var path = Path.Combine(_outputPath, "full_dump.json");
        await File.WriteAllTextAsync(path, json);
        Console.WriteLine($"\n💾 Полный дамп сохранён: {path}");
        Console.WriteLine($"   Размер файла: {new FileInfo(path).Length / 1024 / 1024:F2} MB");
    }
}