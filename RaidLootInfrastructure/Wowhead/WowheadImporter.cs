using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml.Linq;
using RaidLootCore.Models;
using System.Linq;

namespace RaidLootInfrastructure
{
    public class WowheadProvider
    {
        private readonly HttpClient _client;
        private readonly Dictionary<string, List<int>> _zoneCache = new();

        public WowheadProvider()
        {
            var handler = new HttpClientHandler
            {
                AllowAutoRedirect = true,
                UseCookies = true,
                AutomaticDecompression = System.Net.DecompressionMethods.GZip | System.Net.DecompressionMethods.Deflate
            };

            _client = new HttpClient(handler);

            // Добавляем заголовки как у реального браузера
            _client.DefaultRequestHeaders.Add("User-Agent",
                "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36");
            _client.DefaultRequestHeaders.Add("Accept",
                "text/html,application/xhtml+xml,application/xml;q=0.9,image/avif,image/webp,*/*;q=0.8");
            _client.DefaultRequestHeaders.Add("Accept-Language", "ru-RU,ru;q=0.9,en-US;q=0.8,en;q=0.7");
            _client.DefaultRequestHeaders.Add("Referer", "https://www.wowhead.com/");
            _client.Timeout = TimeSpan.FromSeconds(30);
        }

        /// <summary>
        /// Генерирует базу данных для указанного рейда
        /// </summary>
        public async Task<List<Item>> GenerateRaidDatabase(string zoneName, string zoneUrl, string size, string difficulty)
        {
            Console.WriteLine($"\n=== ЗАГРУЗКА: {zoneName} ({size} {difficulty}) ===");

            try
            {
                // Небольшая задержка перед запросом
                await Task.Delay(Random.Shared.Next(1000, 2000));

                string html = await _client.GetStringAsync(zoneUrl);

                // Находим все ID предметов
                var itemMatches = Regex.Matches(html, @"item=(\d+)");
                var bossMatches = Regex.Matches(html, @"name: '(.+?)',(.+?)minLevel");

                var itemIds = new HashSet<int>();
                foreach (Match m in itemMatches)
                {
                    if (int.TryParse(m.Groups[1].Value, out int id))
                        itemIds.Add(id);
                }

                // Парсим боссов
                var bossNames = new List<string>();
                foreach (Match m in bossMatches)
                {
                    string bossName = m.Groups[1].Value;
                    bossName = Regex.Replace(bossName, @"<[^>]+>", "").Trim();
                    if (!string.IsNullOrEmpty(bossName) && !bossName.Contains("Trash"))
                        bossNames.Add(bossName);
                }

                Console.WriteLine($"Найдено предметов: {itemIds.Count}, боссов: {bossNames.Count}");

                if (itemIds.Count == 0)
                {
                    Console.WriteLine("⚠️ Не найдено предметов. Возможно, Wowhead изменил структуру.");
                    return new List<Item>();
                }

                var items = new List<Item>();
                int itemsPerBoss = itemIds.Count / Math.Max(1, bossNames.Count);

                int count = 0;
                foreach (var id in itemIds)
                {
                    // Определяем босса для этого предмета
                    string currentBoss = "Unknown";
                    if (bossNames.Count > 0)
                    {
                        int bossIndex = count / itemsPerBoss;
                        if (bossIndex < bossNames.Count)
                            currentBoss = bossNames[bossIndex];
                    }

                    var item = await GetItemAsync(id, size, difficulty, currentBoss);

                    if (item != null)
                    {
                        items.Add(item);
                        Console.Write($"\r   Загружено: {items.Count}/{itemIds.Count} - {item.NameRu ?? id.ToString()}");
                    }

                    // Задержка между запросами
                    await Task.Delay(Random.Shared.Next(500, 1500));
                    count++;
                }

                Console.WriteLine($"\n✅ Завершено: {items.Count} предметов для {zoneName}");
                return items;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"\n❌ Ошибка загрузки {zoneName}: {ex.Message}");
                return new List<Item>();
            }
        }

        /// <summary>
        /// Загружает все рейды из конфигурации
        /// </summary>
        public async Task DownloadAllRaids(Dictionary<string, bool> raidSettings)
        {
            var allItems = new List<Item>();
            var zoneUrls = new Dictionary<string, string>
            {
                // Classic (Level 60)
                ["Molten Core"] = "https://www.wowhead.com/classic/zone=2677/molten-core",
                ["Blackwing Lair"] = "https://www.wowhead.com/classic/zone=2677/blackwing-lair",
                ["Temple of Ahn'Qiraj"] = "https://www.wowhead.com/classic/zone=3428/temple-of-ahnqiraj",
                ["Ruins of Ahn'Qiraj"] = "https://www.wowhead.com/classic/zone=3429/ruins-of-ahnqiraj",
                ["Zul'Gurub"] = "https://www.wowhead.com/classic/zone=1977/zulgurub",

                // The Burning Crusade (Level 70)
                ["Karazhan"] = "https://www.wowhead.com/tbc/zone=3457/karazhan",
                ["Gruul's Lair"] = "https://www.wowhead.com/tbc/zone=3923/gruuls-lair",
                ["Magtheridon's Lair"] = "https://www.wowhead.com/tbc/zone=3836/magtheridons-lair",
                ["Serpentshrine Cavern"] = "https://www.wowhead.com/tbc/zone=3607/serpentshrine-cavern",
                ["The Eye (Tempest Keep)"] = "https://www.wowhead.com/tbc/zone=3842/the-eye",
                ["Battle for Mount Hyjal"] = "https://www.wowhead.com/tbc/zone=3959/battle-for-mount-hyjal",
                ["Black Temple"] = "https://www.wowhead.com/tbc/zone=3959/black-temple",
                ["Sunwell Plateau"] = "https://www.wowhead.com/tbc/zone=4075/sunwell-plateau",
                ["Zul'Aman"] = "https://www.wowhead.com/tbc/zone=3790/zulaman",

                // Wrath of the Lich King (Level 80)
                ["Naxxramas"] = "https://www.wowhead.com/wotlk/zone=3456/naxxramas",
                ["The Eye of Eternity"] = "https://www.wowhead.com/wotlk/zone=4500/the-eye-of-eternity",
                ["The Obsidian Sanctum"] = "https://www.wowhead.com/wotlk/zone=4493/the-obsidian-sanctum",
                ["Ulduar"] = "https://www.wowhead.com/wotlk/zone=4273/ulduar",
                ["Trial of the Crusader"] = "https://www.wowhead.com/wotlk/zone=4722/trial-of-the-crusader",
                ["Onyxia's Lair"] = "https://www.wowhead.com/wotlk/zone=4984/onyxias-lair",
                ["Icecrown Citadel"] = "https://www.wowhead.com/wotlk/zone=4812/icecrown-citadel",
                ["Ruby Sanctum"] = "https://www.wowhead.com/wotlk/zone=4987/ruby-sanctum",
                ["Vault of Archavon"] = "https://www.wowhead.com/wotlk/zone=4603/vault-of-archavon"
            };

            foreach (var raid in raidSettings.Where(x => x.Value))
            {
                if (zoneUrls.ContainsKey(raid.Key))
                {
                    Console.WriteLine($"\n>>> ЗАГРУЗКА: {raid.Key}");

                    // Загружаем разные сложности
                    if (raid.Key == "Icecrown Citadel")
                    {
                        var items10n = await GenerateRaidDatabase(raid.Key, zoneUrls[raid.Key], "10", "Normal");
                        allItems.AddRange(items10n);

                        await Task.Delay(3000);

                        var items10h = await GenerateRaidDatabase(raid.Key, zoneUrls[raid.Key], "10", "Heroic");
                        allItems.AddRange(items10h);

                        await Task.Delay(3000);

                        var items25n = await GenerateRaidDatabase(raid.Key, zoneUrls[raid.Key], "25", "Normal");
                        allItems.AddRange(items25n);

                        await Task.Delay(3000);

                        var items25h = await GenerateRaidDatabase(raid.Key, zoneUrls[raid.Key], "25", "Heroic");
                        allItems.AddRange(items25h);
                    }
                    else
                    {
                        var items = await GenerateRaidDatabase(raid.Key, zoneUrls[raid.Key], "25", "Heroic");
                        allItems.AddRange(items);
                    }

                    await Task.Delay(5000); // Пауза между рейдами
                }
            }

            // Сохраняем все в один файл
            string json = JsonSerializer.Serialize(allItems, new JsonSerializerOptions
            {
                WriteIndented = true,
                Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
            });

            string fileName = $"wow_loot_{DateTime.Now:yyyyMMdd_HHmmss}.json";
            File.WriteAllText(fileName, json);

            Console.WriteLine($"\n✅ ВСЕГО ЗАГРУЖЕНО: {allItems.Count} предметов");
            Console.WriteLine($"📁 Файл сохранен: {fileName}");

            // Показываем статистику по качеству
            var qualityGroups = allItems.GroupBy(x => x.Quality).OrderBy(g => g.Key);
            Console.WriteLine("\n📊 Статистика по качеству:");
            foreach (var group in qualityGroups)
            {
                string qualityName = group.Key switch
                {
                    "3" => "Редкие",
                    "4" => "Эпические",
                    "5" => "Легендарные",
                    _ => $"Качество {group.Key}"
                };
                Console.WriteLine($"   • {qualityName}: {group.Count()} предметов");
            }
        }

        public async Task<Item?> GetItemAsync(int itemId, string size, string diff, string boss)
        {
            try
            {
                // Русская версия
                string urlRu = $"https://www.wowhead.com/wotlk/ru/item={itemId}&xml";
                var responseRu = await _client.GetAsync(urlRu);
                if (!responseRu.IsSuccessStatusCode) return null;

                var xmlRu = XDocument.Parse(await responseRu.Content.ReadAsStringAsync());
                var itemRu = xmlRu.Element("wowhead")?.Element("item");
                if (itemRu == null) return null;

                // Английская версия
                string urlEn = $"https://www.wowhead.com/wotlk/item={itemId}&xml";
                var responseEn = await _client.GetAsync(urlEn);
                if (!responseEn.IsSuccessStatusCode) return null;

                var xmlEn = XDocument.Parse(await responseEn.Content.ReadAsStringAsync());
                var itemEn = xmlEn.Element("wowhead")?.Element("item");

                // Определяем тип предмета из названия
                string itemName = CleanText(itemRu.Element("name")?.Value ?? "");
                string itemType = GetItemTypeFromName(itemName);

                var item = new Item
                {
                    Id = itemId,
                    NameRu = CleanText(itemRu.Element("name")?.Value ?? "Неизвестно"),
                    NameEn = CleanText(itemEn?.Element("name")?.Value ?? "Unknown"),
                    Quality = itemRu.Element("quality")?.Attribute("id")?.Value ?? "3",
                    Icon = itemRu.Element("icon")?.Value ?? "inv_misc_questionmark",
                    TooltipRu = CleanTooltip(itemRu.Element("htmlTooltip")?.Value ?? ""),
                    TooltipEn = CleanTooltip(itemEn?.Element("htmlTooltip")?.Value ?? ""),
                    RaidSize = size,
                    Difficulty = diff,
                    BossName = boss,
                    Type = itemType,
                    ItemLevel = 0 // Можно будет добавить позже
                };

                // Скачиваем иконку в фоне
                _ = Task.Run(() => DownloadIcon(item.Icon));

                return item;
            }
            catch (Exception ex)
            {
                Console.Write($"❌");
                return null;
            }
        }

        private string GetItemTypeFromName(string name)
        {
            name = name.ToLower();

            if (name.Contains("меч") || name.Contains("sword")) return "Меч";
            if (name.Contains("кинжал") || name.Contains("dagger")) return "Кинжал";
            if (name.Contains("топор") || name.Contains("axe")) return "Топор";
            if (name.Contains("молот") || name.Contains("mace") || name.Contains("hammer")) return "Молот";
            if (name.Contains("посох") || name.Contains("staff")) return "Посох";
            if (name.Contains("лук") || name.Contains("bow")) return "Лук";
            if (name.Contains("арбалет") || name.Contains("crossbow")) return "Арбалет";
            if (name.Contains("ружьё") || name.Contains("gun")) return "Ружьё";
            if (name.Contains("щит") || name.Contains("shield")) return "Щит";
            if (name.Contains("шлем") || name.Contains("helm") || name.Contains("head")) return "Шлем";
            if (name.Contains("наплеч") || name.Contains("shoulder")) return "Наплечники";
            if (name.Contains("нагруд") || name.Contains("chest") || name.Contains("robe")) return "Нагрудник";
            if (name.Contains("перчат") || name.Contains("glove") || name.Contains("hand")) return "Перчатки";
            if (name.Contains("пояс") || name.Contains("belt") || name.Contains("waist")) return "Пояс";
            if (name.Contains("штаны") || name.Contains("legg") || name.Contains("pants")) return "Штаны";
            if (name.Contains("сапог") || name.Contains("boot") || name.Contains("feet")) return "Сапоги";
            if (name.Contains("кольцо") || name.Contains("ring")) return "Кольцо";
            if (name.Contains("амулет") || name.Contains("neck") || name.Contains("amulet")) return "Амулет";
            if (name.Contains("плащ") || name.Contains("cloak") || name.Contains("cape")) return "Плащ";

            return "Разное";
        }

        private string CleanText(string text)
        {
            if (string.IsNullOrEmpty(text)) return "";
            return Regex.Replace(text, @"<[^>]+>", "").Trim();
        }

        private string CleanTooltip(string tooltip)
        {
            if (string.IsNullOrEmpty(tooltip)) return "";

            // Убираем HTML теги
            string clean = Regex.Replace(tooltip, @"<[^>]+>", " ");

            // Убираем лишние пробелы
            clean = Regex.Replace(clean, @"\s+", " ");

            // Убираем техническую информацию
            clean = Regex.Replace(clean, @"Прочность: \d+/\d+", "");
            clean = Regex.Replace(clean, @"Цена продажи: \d+", "");
            clean = Regex.Replace(clean, @"Sell Price: \d+", "");
            clean = Regex.Replace(clean, @"Durability: \d+/\d+", "");

            return clean.Trim();
        }

        private async Task DownloadIcon(string iconName)
        {
            if (string.IsNullOrEmpty(iconName)) return;

            string folder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Icons");
            Directory.CreateDirectory(folder);

            string filePath = Path.Combine(folder, $"{iconName}.jpg");
            if (File.Exists(filePath)) return;

            try
            {
                var iconUrl = $"https://wow.zamimg.com/images/wow/icons/large/{iconName}.jpg";
                var bytes = await _client.GetByteArrayAsync(iconUrl);
                await File.WriteAllBytesAsync(filePath, bytes);
            }
            catch
            {
                // Игнорируем ошибки скачивания иконок
            }
        }
    }
}