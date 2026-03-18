using MaNGOSExtractor.Models;

namespace MaNGOSExtractor.Services;

public class GameDataService
{
    // Индексы для быстрого доступа
    public Dictionary<int, MySqlItem> ItemsById { get; private set; } = new();
    public Dictionary<int, MySqlCreature> CreaturesById { get; private set; } = new();
    public Dictionary<int, List<MySqlLoot>> LootByCreature { get; private set; } = new();
    public Dictionary<int, DbcSpell> SpellsById { get; private set; } = new();
    public Dictionary<int, DbcMap> MapsById { get; private set; } = new();
    public Dictionary<int, DbcAreaTable> AreasById { get; private set; } = new();

    // Связанные данные
    public Dictionary<int, List<DbcDungeonEncounter>> EncountersByMap { get; private set; } = new();
    public Dictionary<int, string> ItemIcons { get; private set; } = new();
    public Dictionary<int, string> CreatureIcons { get; private set; } = new();

    public async Task LoadFromFullDataAsync(FullGameData fullData, string mode)
    {
        Console.WriteLine("\n📦 Загрузка данных в память...");

        switch (mode)
        {
            case "1": // Только лут
                LoadLootData(fullData);
                break;
            case "2": // Лут + тактики
                LoadLootData(fullData);
                LoadSpellData(fullData);
                break;
            case "3": // Всё
                LoadLootData(fullData);
                LoadSpellData(fullData);
                LoadMapData(fullData);
                LoadIconData(fullData);
                break;
        }

        Console.WriteLine("\n✅ Данные загружены в память");
    }

    private void LoadLootData(FullGameData fullData)
    {
        Console.Write("   Загрузка предметов... ");
        ItemsById = fullData.Items.ToDictionary(x => x.Entry);
        Console.WriteLine($"{ItemsById.Count} шт.");

        Console.Write("   Загрузка существ... ");
        CreaturesById = fullData.Creatures.ToDictionary(x => x.Entry);
        Console.WriteLine($"{CreaturesById.Count} шт.");

        Console.Write("   Загрузка лута... ");
        LootByCreature = fullData.Loot
            .GroupBy(x => x.Entry)
            .ToDictionary(g => g.Key, g => g.ToList());
        Console.WriteLine($"{LootByCreature.Count} существ с лутом");
    }

    private void LoadSpellData(FullGameData fullData)
    {
        Console.Write("   Загрузка спеллов... ");
        SpellsById = fullData.Spells.ToDictionary(x => x.Id);
        Console.WriteLine($"{SpellsById.Count} шт.");
    }

    private void LoadMapData(FullGameData fullData)
    {
        Console.Write("   Загрузка карт... ");
        MapsById = fullData.Maps.ToDictionary(x => x.Id);
        Console.WriteLine($"{MapsById.Count} шт.");

        Console.Write("   Загрузка зон... ");
        AreasById = fullData.Areas.ToDictionary(x => x.Id);
        Console.WriteLine($"{AreasById.Count} шт.");

        Console.Write("   Загрузка энкаунтеров... ");
        EncountersByMap = fullData.DungeonEncounters
            .GroupBy(x => x.MapId)
            .ToDictionary(g => g.Key, g => g.ToList());
        Console.WriteLine($"{EncountersByMap.Count} карт с энкаунтерами");
    }

    private void LoadIconData(FullGameData fullData)
    {
        // Связываем displayId -> iconName
        var displayToIcon = fullData.ItemDisplayInfo
            .Where(x => !string.IsNullOrEmpty(x.IconName))
            .ToDictionary(x => x.Id, x => x.IconName);

        ItemIcons = fullData.Items
            .Where(x => displayToIcon.ContainsKey(x.DisplayId))
            .ToDictionary(x => x.Entry, x => displayToIcon[x.DisplayId]);

        Console.WriteLine($"   Загружено иконок предметов: {ItemIcons.Count}");
    }

    // Методы для быстрого доступа
    public MySqlItem GetItem(int id) => ItemsById.GetValueOrDefault(id);
    public MySqlCreature GetCreature(int id) => CreaturesById.GetValueOrDefault(id);
    public List<MySqlLoot> GetLoot(int creatureId) => LootByCreature.GetValueOrDefault(creatureId);
    public DbcSpell GetSpell(int id) => SpellsById.GetValueOrDefault(id);
    public string GetItemIcon(int itemId) => ItemIcons.GetValueOrDefault(itemId);
}