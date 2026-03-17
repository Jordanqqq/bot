using System.Text.Json;
using RaidLootCore.Models;

namespace RaidLootServices;

// Этот сервис будет жить в памяти всё время, пока работает бот.
public class GameDataService
{
    // --- Индексы для мгновенного поиска ---
    // Предметы по их ID (самый частый запрос)
    public Dictionary<int, Item> ItemsById { get; private set; } = new();

    // Лут по имени босса (или по ID босса, если добавишь позже)
    public Dictionary<string, List<Item>> LootByBossName { get; private set; } = new();

    // Список всех рейдов
    public List<RaidConfig> Raids { get; private set; } = new();

    // --- Метод для загрузки всех данных (вызвать при старте бота) ---
    public async Task LoadDataAsync(string dataDirectory)
    {
        Console.WriteLine("📦 GameDataService: Загрузка данных...");

        // 1. Загружаем предметы
        var itemsPath = Path.Combine(dataDirectory, "mangos_items.json");
        if (File.Exists(itemsPath))
        {
            var itemsJson = await File.ReadAllTextAsync(itemsPath);
            var items = JsonSerializer.Deserialize<List<Item>>(itemsJson);
            if (items != null)
            {
                ItemsById = items.ToDictionary(i => i.Id);
                Console.WriteLine($"   ✅ Загружено предметов: {ItemsById.Count}");
            }
        }

        // 2. Загружаем рейды и строим индекс лута
        var raidsPath = Path.Combine(dataDirectory, "raids.json");
        if (File.Exists(raidsPath))
        {
            var raidsJson = await File.ReadAllTextAsync(raidsPath);
            Raids = JsonSerializer.Deserialize<List<RaidConfig>>(raidsJson) ?? new();

            // Строим индекс: Имя босса -> Список предметов
            LootByBossName.Clear();
            foreach (var raid in Raids)
            {
                foreach (var boss in raid.Bosses)
                {
                    var bossName = boss.NameRu; // Или NameEn, смотря что придет из аддона
                    if (string.IsNullOrEmpty(bossName)) continue;

                    var bossItems = new List<Item>();
                    foreach (var itemId in boss.ItemIds)
                    {
                        if (ItemsById.TryGetValue(itemId, out var item))
                        {
                            bossItems.Add(item);
                        }
                    }
                    LootByBossName[bossName] = bossItems;
                }
            }
            Console.WriteLine($"   ✅ Загружено рейдов: {Raids.Count}, индекс для {LootByBossName.Count} боссов построен.");
        }
        Console.WriteLine("📦 GameDataService: Загрузка завершена.");
    }
}