using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using RaidLootCore.Models;

namespace RaidLootServices
{
    public class RaidDataLoader
    {
        private readonly string _dataPath;
        private List<RaidConfig> _raidConfigs = new();
        private Dictionary<string, BossConfig> _bossCache = new();
        private Dictionary<string, List<int>> _bossItemCache = new();

        public RaidDataLoader(string dataPath)
        {
            _dataPath = dataPath;
        }

        public void LoadRaidConfigs()
        {
            var configPath = Path.Combine(_dataPath, "raids.json");

            // Пробуем разные пути
            if (!File.Exists(configPath))
            {
                configPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "raids.json");
            }

            if (!File.Exists(configPath))
            {
                configPath = "raids.json";
            }

            if (File.Exists(configPath))
            {
                try
                {
                    var json = File.ReadAllText(configPath);
                    _raidConfigs = JsonSerializer.Deserialize<List<RaidConfig>>(json) ?? new();

                    foreach (var raid in _raidConfigs)
                    {
                        foreach (var boss in raid.Bosses)
                        {
                            if (boss.ItemIds != null)
                            {
                                // Используем Name как основной ключ
                                _bossItemCache[boss.Name] = boss.ItemIds;

                                // Кэшируем босса по разным ключам
                                if (!string.IsNullOrEmpty(boss.NameRu))
                                    _bossCache[boss.NameRu] = boss;
                                if (!string.IsNullOrEmpty(boss.NameEn))
                                    _bossCache[boss.NameEn] = boss;
                                _bossCache[boss.Name] = boss;

                                // Также кэшируем по имени без пробелов
                                if (!string.IsNullOrEmpty(boss.NameEn))
                                    _bossCache[boss.NameEn.Replace(" ", "")] = boss;
                                if (!string.IsNullOrEmpty(boss.NameRu))
                                    _bossCache[boss.NameRu.Replace(" ", "")] = boss;
                            }
                        }
                    }

                    Console.WriteLine($"✅ Загружено {_raidConfigs.Count} рейдов из {configPath}");

                    // Выводим список для проверки
                    foreach (var raid in _raidConfigs)
                    {
                        Console.WriteLine($"   • {raid.RaidName}: {raid.Bosses.Count} боссов");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"❌ Ошибка загрузки {configPath}: {ex.Message}");
                    _raidConfigs = GetDefaultRaids();
                }
            }
            else
            {
                Console.WriteLine($"⚠️ Файл не найден: {configPath}");
                Console.WriteLine("📝 Создаю тестовые данные...");
                _raidConfigs = GetDefaultRaids();
            }
        }

        private List<RaidConfig> GetDefaultRaids()
        {
            return new List<RaidConfig>
            {
                new RaidConfig
                {
                    RaidName = "Icecrown Citadel",
                    RaidNameEn = "Icecrown Citadel",
                    Bosses = new List<BossConfig>
                    {
                        new BossConfig { Name = "Lord Marrowgar", NameRu = "Лорд Мэрроугар", NameEn = "Lord Marrowgar", Emoji = "🦴", ItemIds = new List<int> { 50707, 50704 } },
                        new BossConfig { Name = "Lady Deathwhisper", NameRu = "Леди Смертный Шепот", NameEn = "Lady Deathwhisper", Emoji = "🗣️👻", ItemIds = new List<int> { 50703, 50697 } },
                        new BossConfig { Name = "The Lich King", NameRu = "Король-лич", NameEn = "The Lich King", Emoji = "👑❄️💀", ItemIds = new List<int> { 50730, 50732 } }
                    }
                },
                new RaidConfig
                {
                    RaidName = "Naxxramas",
                    RaidNameEn = "Naxxramas",
                    Bosses = new List<BossConfig>
                    {
                        new BossConfig { Name = "Kel'Thuzad", NameRu = "Кел'Тузад", NameEn = "Kel'Thuzad", Emoji = "👑💀", ItemIds = new List<int> { 39402, 40395 } }
                    }
                }
            };
        }

        public List<string> GetAvailableRaids()
        {
            var raids = _raidConfigs.Select(r => r.RaidName).ToList();
            Console.WriteLine($"[RAID] Доступно {raids.Count} рейдов: {string.Join(", ", raids)}");
            return raids;
        }

        public List<BossConfig> GetBossesForRaid(string raidName)
        {
            var raid = _raidConfigs.FirstOrDefault(r =>
                r.RaidName.Equals(raidName, StringComparison.OrdinalIgnoreCase) ||
                (r.RaidNameEn != null && r.RaidNameEn.Equals(raidName, StringComparison.OrdinalIgnoreCase)));

            var bosses = raid?.Bosses ?? new List<BossConfig>();
            Console.WriteLine($"[BOSS] Для рейда {raidName} найдено {bosses.Count} боссов");
            return bosses;
        }

        /// <summary>
        /// Получить босса по имени (русскому, английскому или оригинальному)
        /// </summary>
        public BossConfig? GetBossByName(string bossName)
        {
            if (string.IsNullOrEmpty(bossName)) return null;

            // Пробуем получить из кэша
            if (_bossCache.TryGetValue(bossName, out var boss))
                return boss;

            // Пробуем без пробелов
            string noSpaces = bossName.Replace(" ", "");
            if (_bossCache.TryGetValue(noSpaces, out boss))
                return boss;

            // Ищем во всех рейдах
            foreach (var raid in _raidConfigs)
            {
                foreach (var b in raid.Bosses)
                {
                    if (b.Name.Equals(bossName, StringComparison.OrdinalIgnoreCase) ||
                        (b.NameRu != null && b.NameRu.Equals(bossName, StringComparison.OrdinalIgnoreCase)) ||
                        (b.NameEn != null && b.NameEn.Equals(bossName, StringComparison.OrdinalIgnoreCase)))
                    {
                        _bossCache[bossName] = b;
                        return b;
                    }

                    // Проверяем совпадение без пробелов
                    if (b.NameEn != null && b.NameEn.Replace(" ", "").Equals(noSpaces, StringComparison.OrdinalIgnoreCase))
                    {
                        _bossCache[bossName] = b;
                        return b;
                    }
                    if (b.NameRu != null && b.NameRu.Replace(" ", "").Equals(noSpaces, StringComparison.OrdinalIgnoreCase))
                    {
                        _bossCache[bossName] = b;
                        return b;
                    }
                }
            }

            return null;
        }

        /// <summary>
        /// Получить босса по английскому имени (специально для поиска в базе предметов)
        /// </summary>
        public BossConfig? GetBossByEnName(string enName)
        {
            if (string.IsNullOrEmpty(enName)) return null;

            // Пробуем получить из кэша
            if (_bossCache.TryGetValue(enName, out var boss))
                return boss;

            // Пробуем без пробелов
            string noSpaces = enName.Replace(" ", "");
            if (_bossCache.TryGetValue(noSpaces, out boss))
                return boss;

            // Ищем во всех рейдах по английскому имени
            foreach (var raid in _raidConfigs)
            {
                foreach (var b in raid.Bosses)
                {
                    if (b.NameEn != null && b.NameEn.Equals(enName, StringComparison.OrdinalIgnoreCase))
                    {
                        _bossCache[enName] = b;
                        return b;
                    }

                    // Проверяем совпадение без пробелов
                    if (b.NameEn != null && b.NameEn.Replace(" ", "").Equals(noSpaces, StringComparison.OrdinalIgnoreCase))
                    {
                        _bossCache[enName] = b;
                        return b;
                    }

                    // Проверяем содержит ли английское имя искомое (для случаев "Lady Deathwhisper" vs "Deathwhisper")
                    if (b.NameEn != null && b.NameEn.Contains(enName, StringComparison.OrdinalIgnoreCase))
                    {
                        _bossCache[enName] = b;
                        return b;
                    }
                }
            }

            return null;
        }

        public List<int> GetItemIdsForBoss(string bossName)
        {
            if (_bossItemCache.TryGetValue(bossName, out var ids))
                return ids;
            return new List<int>();
        }

        public List<Item> GetItemsForBoss(string bossName, List<Item> allItems)
        {
            var itemIds = GetItemIdsForBoss(bossName);
            return allItems.Where(i => itemIds.Contains(i.Id)).ToList();
        }
    }
}