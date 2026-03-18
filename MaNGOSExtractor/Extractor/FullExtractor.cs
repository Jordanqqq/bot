using MaNGOSExtractor.Models;
using MaNGOSExtractor.Services;
using MaNGOSExtractor.MyMpqReader;
using System.Text.Json;

namespace MaNGOSExtractor.Extractors;

public class FullExtractor
{
    private readonly DatabaseService _db;
    private readonly MyMpqExtractor _mpq;
    private readonly string _outputPath;

    public FullExtractor(DatabaseService db, MyMpqExtractor mpq, string outputPath)
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

        // 2. DBC данные (пока заглушка)
        Console.WriteLine("\n📚 DBC: Загрузка данных из MPQ...");
        _mpq.ExportDbc();
        Console.WriteLine($"   ✅ DBC файлы сохранены в Output/dbc");

        // 3. Сохраняем статистику
        data.Stats = new Dictionary<string, int>
        {
            ["items"] = data.Items.Count,
            ["creatures"] = data.Creatures.Count,
            ["loot"] = data.Loot.Count,
            ["spells"] = 0,
            ["maps"] = 0,
            ["areas"] = 0,
            ["encounters"] = 0
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

    public async Task ExtractMySqlData(FullGameData data)
    {
        Console.WriteLine("\n📦 MySQL: Загрузка данных...");

        // Предметы
        data.Items = await _db.QueryAsync(
            "SELECT entry, name, Quality, class, subclass, displayid, ItemLevel, RequiredLevel, InventoryType FROM item_template",
            reader => new MySqlItem
            {
                Entry = reader.GetInt32(reader.GetOrdinal("entry")),
                Name = reader.GetString(reader.GetOrdinal("name")),
                Quality = reader.GetInt32(reader.GetOrdinal("Quality")),
                Class = reader.GetInt32(reader.GetOrdinal("class")),
                Subclass = reader.GetInt32(reader.GetOrdinal("subclass")),
                DisplayId = reader.GetInt32(reader.GetOrdinal("displayid")),
                ItemLevel = reader.GetInt32(reader.GetOrdinal("ItemLevel")),
                RequiredLevel = reader.GetInt32(reader.GetOrdinal("RequiredLevel")),
                InventoryType = reader.GetInt32(reader.GetOrdinal("InventoryType"))
            });
        Console.WriteLine($"   ✅ Предметов: {data.Items.Count}");

        // Существа (боссы)
        data.Creatures = await _db.QueryAsync(
            "SELECT entry, name, rank, zoneid, map, displayid FROM creature_template WHERE rank >= 3",
            reader => new MySqlCreature
            {
                Entry = reader.GetInt32(reader.GetOrdinal("entry")),
                Name = reader.GetString(reader.GetOrdinal("name")),
                Rank = reader.GetInt32(reader.GetOrdinal("rank")),
                ZoneId = reader.GetInt32(reader.GetOrdinal("zoneid")),
                MapId = reader.GetInt32(reader.GetOrdinal("map")),
                DisplayId = reader.GetInt32(reader.GetOrdinal("displayid"))
            });
        Console.WriteLine($"   ✅ Существ: {data.Creatures.Count}");

        // Лут
        data.Loot = await _db.QueryAsync(
            "SELECT entry, item, ChanceOrQuestChance, groupid, mincountOrRef, maxcount FROM creature_loot_template WHERE item > 0",
            reader => new MySqlLoot
            {
                Entry = reader.GetInt32(reader.GetOrdinal("entry")),
                Item = reader.GetInt32(reader.GetOrdinal("item")),
                Chance = reader.GetFloat(reader.GetOrdinal("ChanceOrQuestChance")),
                GroupId = reader.GetInt32(reader.GetOrdinal("groupid")),
                MinCount = reader.GetInt32(reader.GetOrdinal("mincountOrRef")),
                MaxCount = reader.GetInt32(reader.GetOrdinal("maxcount"))
            });
        Console.WriteLine($"   ✅ Лута: {data.Loot.Count}");
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