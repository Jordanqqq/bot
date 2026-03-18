using MaNGOSExtractor.Models;

namespace MaNGOSExtractor.Services;

public class BossLootExtractor
{
    private readonly DatabaseService _db;
    private readonly ItemFilter _itemFilter;

    public BossLootExtractor(DatabaseService db)
    {
        _db = db;
        _itemFilter = new ItemFilter();
    }

    public async Task<Dictionary<int, BossLootInfo>> ExtractAllBossLootAsync()
    {
        var bossLoot = new Dictionary<int, BossLootInfo>();

        // 1. Получаем всех боссов (creature_template с rank >= 3)
        var bosses = await GetBossesAsync();
        Console.WriteLine($"   Найдено боссов: {bosses.Count}");

        // 2. Получаем весь лут из creature_loot_template
        var allLoot = await GetLootAsync();
        Console.WriteLine($"   Найдено записей лута: {allLoot.Count}");

        // 3. Получаем все предметы с их качеством
        var items = await GetItemsAsync();
        var itemsDict = items.ToDictionary(i => i.Entry);
        Console.WriteLine($"   Предметов в базе: {itemsDict.Count}");

        // 4. Собираем лут для каждого босса
        var bossLootGroups = allLoot
            .GroupBy(l => l.Entry)
            .ToDictionary(g => g.Key, g => g.ToList());

        int skippedLowQuality = 0;
        int processed = 0;

        foreach (var boss in bosses)
        {
            if (!bossLootGroups.TryGetValue(boss.Entry, out var lootList))
                continue;

            var bossInfo = new BossLootInfo
            {
                Entry = boss.Entry,
                NameRu = boss.Name,
                NameEn = boss.Name, // В MaNGOS обычно только EN
                ZoneId = boss.ZoneId,
                ItemIds = new List<int>()
            };

            foreach (var loot in lootList)
            {
                if (!itemsDict.TryGetValue(loot.Item, out var item))
                    continue;

                // Фильтруем только эпики (4) и легендарки (5)
                if (!_itemFilter.IsEpicOrLegendary(item.Quality))
                {
                    skippedLowQuality++;
                    continue;
                }

                bossInfo.ItemIds.Add(loot.Item);
            }

            if (bossInfo.ItemIds.Count > 0)
            {
                bossLoot[boss.Entry] = bossInfo;
                processed++;
            }
        }

        Console.WriteLine($"   ✅ Обработано боссов с лутом: {processed}");
        Console.WriteLine($"   ⏭️ Пропущено предметов (ниже эпика): {skippedLowQuality}");

        return bossLoot;
    }

    private async Task<List<RawCreature>> GetBossesAsync()
    {
        string sql = @"
        SELECT entry, name, minlevel, maxlevel, rank, HealthModifier, 
               ManaModifier, ArmorModifier, damage_modifier, 
               lootid, SkinLootId, zoneid
        FROM creature_template
        WHERE rank >= 3 
          AND name NOT LIKE '%Trigger%'
          AND name NOT LIKE '%Trash%'
          AND name NOT LIKE '%Invisible%'
        ORDER BY entry";

        return await _db.QueryAsync(sql, reader => new RawCreature
        {
            Entry = reader.GetInt32(reader.GetOrdinal("entry")),
            Name = reader.GetString(reader.GetOrdinal("name")),
            Rank = reader.GetInt32(reader.GetOrdinal("rank")),
            ZoneId = reader.GetInt32(reader.GetOrdinal("zoneid"))
        });
    }

    private async Task<List<RawLoot>> GetLootAsync()
    {
        string sql = @"
        SELECT entry, item, ChanceOrQuestChance, groupid, mincountOrRef, maxcount
        FROM creature_loot_template
        WHERE item > 0";

        return await _db.QueryAsync(sql, reader => new RawLoot
        {
            Entry = reader.GetInt32(reader.GetOrdinal("entry")),
            Item = reader.GetInt32(reader.GetOrdinal("item")),
            Chance = reader.GetFloat(reader.GetOrdinal("ChanceOrQuestChance"))
        });
    }

    private async Task<List<RawItem>> GetItemsAsync()
    {
        string sql = @"
        SELECT entry, name, Quality, class, subclass, InventoryType,
               ItemLevel, RequiredLevel, bonding
        FROM item_template
        WHERE Quality >= 3  -- Редкие и выше
        ORDER BY entry";

        return await _db.QueryAsync(sql, reader => new RawItem
        {
            Entry = reader.GetInt32(reader.GetOrdinal("entry")),
            Name = reader.GetString(reader.GetOrdinal("name")),
            Quality = reader.GetInt32(reader.GetOrdinal("Quality")),
            Class = reader.GetInt32(reader.GetOrdinal("class"))
        });
    }
}

    public class BossLootInfo
{
    public int Entry { get; set; }
    public string NameRu { get; set; }
    public string NameEn { get; set; }
    public int ZoneId { get; set; }
    public List<int> ItemIds { get; set; }
}