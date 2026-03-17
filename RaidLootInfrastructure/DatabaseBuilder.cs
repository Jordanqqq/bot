using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using RaidLootCore.Models;

namespace RaidLootInfrastructure
{
    public class DatabaseBuilder
    {
        private readonly string _mangosJsonPath;
        private readonly string _raidsJsonPath;
        private readonly string _outputIndexPath;
        private readonly string _databaseFolder;

        private Dictionary<int, Item> _itemIndex = new();
        private List<RaidConfig> _raids = new();

        public DatabaseBuilder(string mangosJsonPath, string raidsJsonPath, string outputIndexPath, string databaseFolder)
        {
            _mangosJsonPath = mangosJsonPath;
            _raidsJsonPath = raidsJsonPath;
            _outputIndexPath = outputIndexPath;
            _databaseFolder = databaseFolder;
        }

        /// <summary>
        /// Строит базу данных
        /// </summary>
        public async Task BuildAsync()
        {
            Console.WriteLine("\n🔧 ПОСТРОЕНИЕ БАЗЫ ДАННЫХ");
            Console.WriteLine("===========================");

            // 1. Загружаем предметы из mangos_items.json
            await LoadItemsAsync();

            // 2. Загружаем структуру рейдов из raids.json
            await LoadRaidsAsync();

            // 3. Строим индекс предметов
            await BuildItemIndexAsync();

            // 4. Создаем папки Database
            await CreateDatabaseFoldersAsync();

            Console.WriteLine($"\n✅ БАЗА ДАННЫХ ПОСТРОЕНА!");
            Console.WriteLine($"📁 Индекс предметов: {_outputIndexPath}");
            Console.WriteLine($"📁 Папка Database: {_databaseFolder}");
        }

        private async Task LoadItemsAsync()
        {
            Console.Write("📦 Загрузка предметов из mangos_items.json... ");

            if (!File.Exists(_mangosJsonPath))
            {
                Console.WriteLine("❌ Файл не найден!");
                return;
            }

            string json = await File.ReadAllTextAsync(_mangosJsonPath);
            var items = JsonSerializer.Deserialize<List<Item>>(json);

            if (items == null)
            {
                Console.WriteLine("❌ Ошибка десериализации!");
                return;
            }

            // Строим индекс для быстрого поиска
            foreach (var item in items)
            {
                if (!_itemIndex.ContainsKey(item.Id))
                {
                    _itemIndex[item.Id] = item;
                }
            }

            Console.WriteLine($"✅ {_itemIndex.Count} предметов");
        }

        private async Task LoadRaidsAsync()
        {
            Console.Write("📦 Загрузка структуры рейдов из raids.json... ");

            if (!File.Exists(_raidsJsonPath))
            {
                Console.WriteLine("❌ Файл не найден!");
                return;
            }

            string json = await File.ReadAllTextAsync(_raidsJsonPath);
            _raids = JsonSerializer.Deserialize<List<RaidConfig>>(json) ?? new();

            Console.WriteLine($"✅ {_raids.Count} рейдов");
        }

        private async Task BuildItemIndexAsync()
        {
            Console.Write("💾 Сохранение индекса предметов... ");

            var options = new JsonSerializerOptions
            {
                WriteIndented = true,
                Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
            };

            string json = JsonSerializer.Serialize(_itemIndex, options);
            await File.WriteAllTextAsync(_outputIndexPath, json);

            Console.WriteLine("✅");
        }

        private async Task CreateDatabaseFoldersAsync()
        {
            Console.WriteLine("\n📁 СОЗДАНИЕ ПАПОК DATABASE");

            if (!Directory.Exists(_databaseFolder))
            {
                Directory.CreateDirectory(_databaseFolder);
            }

            int totalBossFiles = 0;

            // Проходим по всем рейдам
            foreach (var raid in _raids)
            {
                string raidFolder = Path.Combine(_databaseFolder, raid.RaidNameEn ?? raid.RaidName);

                // Создаем папки для разных сложностей (Normal/Heroic)
                string normalFolder = Path.Combine(raidFolder, "Normal");
                string heroicFolder = Path.Combine(raidFolder, "Heroic");

                Directory.CreateDirectory(normalFolder);
                Directory.CreateDirectory(heroicFolder);

                // Создаем папки для 10 и 25
                Directory.CreateDirectory(Path.Combine(normalFolder, "10"));
                Directory.CreateDirectory(Path.Combine(normalFolder, "25"));
                Directory.CreateDirectory(Path.Combine(heroicFolder, "10"));
                Directory.CreateDirectory(Path.Combine(heroicFolder, "25"));

                // Проходим по всем боссам рейда
                foreach (var boss in raid.Bosses)
                {
                    if (boss.ItemIds == null || boss.ItemIds.Count == 0) continue;

                    // Создаем файл для Normal/10
                    await CreateBossLootFileAsync(
                        Path.Combine(normalFolder, "10", $"{boss.NameEn ?? boss.Name}.json"),
                        raid.RaidName,
                        boss,
                        "Normal",
                        "10"
                    );
                    totalBossFiles++;

                    // Создаем файл для Normal/25
                    await CreateBossLootFileAsync(
                        Path.Combine(normalFolder, "25", $"{boss.NameEn ?? boss.Name}.json"),
                        raid.RaidName,
                        boss,
                        "Normal",
                        "25"
                    );
                    totalBossFiles++;

                    // Создаем файл для Heroic/10
                    await CreateBossLootFileAsync(
                        Path.Combine(heroicFolder, "10", $"{boss.NameEn ?? boss.Name}.json"),
                        raid.RaidName,
                        boss,
                        "Heroic",
                        "10"
                    );
                    totalBossFiles++;

                    // Создаем файл для Heroic/25
                    await CreateBossLootFileAsync(
                        Path.Combine(heroicFolder, "25", $"{boss.NameEn ?? boss.Name}.json"),
                        raid.RaidName,
                        boss,
                        "Heroic",
                        "25"
                    );
                    totalBossFiles++;

                    Console.Write($"\r   Создано файлов: {totalBossFiles}");
                }
            }

            Console.WriteLine($"\n   ✅ Создано {totalBossFiles} файлов боссов");
        }

        private async Task CreateBossLootFileAsync(string filePath, string raidName, BossConfig boss, string difficulty, string size)
        {
            if (boss.ItemIds == null || boss.ItemIds.Count == 0) return;

            // Собираем предметы для этого босса
            var bossItems = new List<Item>();
            foreach (var itemId in boss.ItemIds)
            {
                if (_itemIndex.TryGetValue(itemId, out var item))
                {
                    bossItems.Add(item);
                }
            }

            if (bossItems.Count == 0) return;

            var bossLootData = new BossLootData
            {
                Raid = raidName,
                Difficulty = difficulty,
                PlayerSize = size,
                Boss = new BossInfo
                {
                    NameRu = boss.NameRu,
                    NameEn = boss.NameEn,
                    Emoji = boss.Emoji,
                    Items = bossItems
                }
            };

            var options = new JsonSerializerOptions
            {
                WriteIndented = true,
                Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
            };

            string json = JsonSerializer.Serialize(bossLootData, options);
            await File.WriteAllTextAsync(filePath, json);
        }
    }

    // Вспомогательные классы
    public class BossLootData
    {
        public string Raid { get; set; } = "";
        public string Difficulty { get; set; } = "";
        public string PlayerSize { get; set; } = "";
        public BossInfo Boss { get; set; } = new();
    }

    public class BossInfo
    {
        public string? NameRu { get; set; }
        public string? NameEn { get; set; }
        public string? Emoji { get; set; }
        public List<Item> Items { get; set; } = new();
    }
}