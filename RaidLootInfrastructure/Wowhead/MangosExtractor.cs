using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using MySqlConnector;
using RaidLootCore.Models;

namespace RaidLootInfrastructure
{
    public class MangosExtractor
    {
        private readonly string _connectionString;

        public MangosExtractor(string host = "localhost", string database = "mangos",
                      string user = "mangos", string password = "mangos", int port = 3306)
        {
            _connectionString =
                $"Server={host};Port={port};Database={database};Uid={user};Pwd={password};Charset=utf8;AllowZeroDateTime=True;UseAffectedRows=True;";
        }

        /// <summary>
        /// Тест подключения к базе данных
        /// </summary>
        public async Task<bool> TestConnectionAsync()
        {
            try
            {
                using var connection = new MySqlConnection(_connectionString);
                await connection.OpenAsync();
                Console.WriteLine("✅ Соединение с базой данных MySQL установлено.");
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Ошибка подключения: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Выгружает все предметы из item_template
        /// </summary>
        public async Task<List<Item>> ExtractItemsAsync()
        {
            var items = new List<Item>();

            string query = @"
                SELECT 
                    entry, name, Quality, displayid, description, 
                    InventoryType, ItemLevel, RequiredLevel, class, subclass,
                    stat_type1, stat_value1, stat_type2, stat_value2,
                    stat_type3, stat_value3, stat_type4, stat_value4,
                    stat_type5, stat_value5, stat_type6, stat_value6,
                    stat_type7, stat_value7, stat_type8, stat_value8,
                    stat_type9, stat_value9, stat_type10, stat_value10,
                    spellid_1, spelltrigger_1, spellid_2, spelltrigger_2,
                    spellid_3, spelltrigger_3, spellid_4, spelltrigger_4,
                    spellid_5, spelltrigger_5,
                    armor, block, delay, dmg_min1, dmg_max1, dmg_type1,
                    holy_res, fire_res, nature_res, frost_res, shadow_res, arcane_res,
                    socketColor_1, socketContent_1, socketColor_2, socketContent_2,
                    socketColor_3, socketContent_3, socketBonus,
                    bonding, MaxDurability, Material, sheath,
                    Flags, ExtraFlags, BuyPrice, SellPrice
                FROM item_template 
                WHERE Quality >= 3 
                ORDER BY entry";

            try
            {
                using var connection = new MySqlConnection(_connectionString);
                await connection.OpenAsync();
                Console.WriteLine("\n📦 Загрузка предметов из item_template...");

                using var command = new MySqlCommand(query, connection);
                using var reader = await command.ExecuteReaderAsync();

                int count = 0;
                while (await reader.ReadAsync())
                {
                    var tooltipParts = new List<string>();

                    // Добавляем статы в тултип
                    for (int i = 1; i <= 10; i++)
                    {
                        int statType = GetValueSafely(reader, $"stat_type{i}", 0);
                        int statValue = GetValueSafely(reader, $"stat_value{i}", 0);

                        if (statType > 0 && statValue > 0)
                        {
                            string statName = GetStatName(statType);
                            tooltipParts.Add($"+{statValue} {statName}");
                        }
                    }

                    // Добавляем заклинания
                    for (int i = 1; i <= 5; i++)
                    {
                        int spellId = GetValueSafely(reader, $"spellid_{i}", 0);
                        int spellTrigger = GetValueSafely(reader, $"spelltrigger_{i}", 0);

                        if (spellId > 0)
                        {
                            string triggerText = spellTrigger switch
                            {
                                0 => "Использование:",
                                1 => "Экипировка:",
                                2 => "Удар:",
                                3 => "Способность:",
                                _ => "Эффект:"
                            };
                            tooltipParts.Add($"{triggerText} {spellId}");
                        }
                    }

                    // Добавляем броню
                    int armor = GetValueSafely(reader, "armor", 0);
                    if (armor > 0)
                        tooltipParts.Add($"{armor} брони");

                    // Добавляем блок
                    int block = GetValueSafely(reader, "block", 0);
                    if (block > 0)
                        tooltipParts.Add($"{block} блок");

                    // Добавляем скорость оружия
                    int delay = GetValueSafely(reader, "delay", 0);
                    if (delay > 0)
                        tooltipParts.Add($"Скорость {delay / 1000.0:F1}");

                    // Добавляем урон
                    float dmgMin = GetFloatSafely(reader, "dmg_min1", 0);
                    float dmgMax = GetFloatSafely(reader, "dmg_max1", 0);
                    if (dmgMin > 0 && dmgMax > 0)
                        tooltipParts.Add($"Урон {dmgMin}-{dmgMax}");

                    // Добавляем сопротивления
                    AddResistance(reader, tooltipParts, "holy_res", "Святой");
                    AddResistance(reader, tooltipParts, "fire_res", "Огонь");
                    AddResistance(reader, tooltipParts, "nature_res", "Природа");
                    AddResistance(reader, tooltipParts, "frost_res", "Лед");
                    AddResistance(reader, tooltipParts, "shadow_res", "Тьма");
                    AddResistance(reader, tooltipParts, "arcane_res", "Тайная магия");

                    // Добавляем сокеты
                    var socketParts = new List<string>();
                    for (int i = 1; i <= 3; i++)
                    {
                        int socketColor = GetValueSafely(reader, $"socketColor_{i}", 0);
                        if (socketColor > 0)
                        {
                            socketParts.Add(GetSocketColor(socketColor));
                        }
                    }

                    int socketBonus = GetValueSafely(reader, "socketBonus", 0);
                    if (socketParts.Count > 0)
                    {
                        string socketLine = string.Join(" ", socketParts);
                        if (socketBonus > 0)
                            socketLine += $" Бонус: +{socketBonus}";
                        tooltipParts.Insert(0, socketLine);
                    }

                    // Добавляем прочность
                    int maxDurability = GetValueSafely(reader, "MaxDurability", 0);
                    if (maxDurability > 0)
                        tooltipParts.Add($"Прочность: {maxDurability} / {maxDurability}");

                    string tooltip = string.Join("\n", tooltipParts);

                    int id = reader.GetInt32(reader.GetOrdinal("entry"));
                    string name = reader.IsDBNull(reader.GetOrdinal("name")) ? "Unknown Item" : reader.GetString(reader.GetOrdinal("name"));
                    int quality = reader.GetInt32(reader.GetOrdinal("Quality"));
                    int displayId = reader.GetInt32(reader.GetOrdinal("displayid"));
                    int invType = reader.GetInt32(reader.GetOrdinal("InventoryType"));
                    int itemLevel = reader.GetInt32(reader.GetOrdinal("ItemLevel"));
                    int bonding = GetValueSafely(reader, "bonding", 0);
                    int material = GetValueSafely(reader, "Material", 0);
                    int sheath = GetValueSafely(reader, "sheath", 0);

                    var item = new Item
                    {
                        Id = id,
                        NameRu = name,
                        NameEn = name,
                        Quality = quality.ToString(),
                        Icon = displayId.ToString(),
                        TooltipRu = tooltip,
                        TooltipEn = tooltip,
                        ItemLevel = itemLevel,
                        Type = GetInventoryType(invType),
                        BossName = "Неизвестно",
                        RequiredLevel = GetValueSafely(reader, "RequiredLevel", 0),
                        BuyPrice = GetValueSafely(reader, "BuyPrice", 0),
                        SellPrice = GetValueSafely(reader, "SellPrice", 0),
                        Flags = GetValueSafely(reader, "Flags", 0).ToString(),
                        Bonding = GetBondingType(bonding),
                        Material = GetMaterialName(material),
                        Sheath = GetSheathType(sheath)
                    };

                    items.Add(item);
                    count++;

                    if (count % 1000 == 0)
                    {
                        Console.Write($"\r   Загружено {count} предметов...");
                    }
                }

                Console.WriteLine($"\n   ✅ Загружено {items.Count} предметов");
                return items;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"\n❌ Ошибка при чтении предметов: {ex.Message}");
                return items;
            }
        }

        /// <summary>
        /// Выгружает информацию о луте с боссов
        /// </summary>
        public async Task<Dictionary<int, string>> ExtractBossLootAsync()
        {
            var bossLootMap = new Dictionary<int, string>();

            string query = @"
                SELECT clt.item AS ItemId, ct.name AS BossName, clt.ChanceOrQuestChance AS Chance
                FROM creature_loot_template clt
                JOIN creature_template ct ON ct.entry = clt.entry
                WHERE clt.item > 0 
                  AND ct.name NOT LIKE '%Trigger%' 
                  AND ct.name NOT LIKE '%Trash%'
                  AND ct.rank >= 3
                ORDER BY clt.ChanceOrQuestChance DESC";

            try
            {
                using var connection = new MySqlConnection(_connectionString);
                await connection.OpenAsync();
                Console.WriteLine("\n📦 Загрузка информации о луте с боссов...");

                using var command = new MySqlCommand(query, connection);
                using var reader = await command.ExecuteReaderAsync();

                int count = 0;
                while (await reader.ReadAsync())
                {
                    int itemId = reader.GetInt32(reader.GetOrdinal("ItemId"));
                    string bossName = reader.GetString(reader.GetOrdinal("BossName"));

                    if (!bossLootMap.ContainsKey(itemId))
                    {
                        bossLootMap[itemId] = bossName;
                        count++;
                    }

                    if (count % 1000 == 0)
                    {
                        Console.Write($"\r   Обработано {count} записей...");
                    }
                }

                Console.WriteLine($"\n   ✅ Найдено связей предмет-босс: {bossLootMap.Count}");
                return bossLootMap;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"\n❌ Ошибка при сопоставлении лута: {ex.Message}");
                return bossLootMap;
            }
        }

        /// <summary>
        /// Полная выгрузка всех данных
        /// </summary>
        public async Task<List<Item>> ExtractAllDataAsync()
        {
            // Сначала проверяем подключение
            if (!await TestConnectionAsync())
            {
                throw new Exception("Не удалось подключиться к базе данных");
            }

            // Выгружаем предметы
            var items = await ExtractItemsAsync();

            // Выгружаем связи с боссами
            var bossLoot = await ExtractBossLootAsync();

            // Обновляем информацию о боссах для предметов
            int updated = 0;
            foreach (var item in items)
            {
                if (bossLoot.TryGetValue(item.Id, out string bossName))
                {
                    item.BossName = bossName;
                    updated++;
                }
            }

            Console.WriteLine($"\n✅ Обновлено названий боссов для {updated} предметов");
            return items;
        }

        /// <summary>
        /// Сохраняет все предметы в JSON файл
        /// </summary>
        public async Task SaveToJsonAsync(string outputPath)
        {
            Console.WriteLine("\n🔍 НАЧАЛО ВЫГРУЗКИ ДАННЫХ ИЗ MANGOS");
            Console.WriteLine("======================================");

            // Проверяем подключение
            if (!await TestConnectionAsync())
            {
                Console.WriteLine("❌ Не удалось подключиться к базе данных. Завершение.");
                return;
            }

            // Выгружаем данные
            var items = await ExtractAllDataAsync();

            Console.WriteLine("\n💾 СОХРАНЕНИЕ В ФАЙЛ...");

            var options = new JsonSerializerOptions
            {
                WriteIndented = true,
                Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
            };

            string json = JsonSerializer.Serialize(items, options);
            await File.WriteAllTextAsync(outputPath, json);

            Console.WriteLine($"\n✅ УСПЕШНО! Сохранено {items.Count} предметов");
            Console.WriteLine($"📁 Файл: {outputPath}");

            // Статистика по качеству
            var qualityStats = items.GroupBy(x => x.Quality)
                                    .Select(g => new { Quality = g.Key, Count = g.Count() })
                                    .OrderBy(g => g.Quality);

            Console.WriteLine("\n📊 СТАТИСТИКА ПО КАЧЕСТВУ:");
            foreach (var stat in qualityStats)
            {
                string qualityName = stat.Quality switch
                {
                    "3" => "Редкие",
                    "4" => "Эпические",
                    "5" => "Легендарные",
                    _ => $"Качество {stat.Quality}"
                };
                Console.WriteLine($"   • {qualityName}: {stat.Count} предметов");
            }
        }

        // ========== ВСПОМОГАТЕЛЬНЫЕ МЕТОДЫ ==========

        private int GetValueSafely(System.Data.Common.DbDataReader reader, string columnName, int defaultValue)
        {
            try
            {
                int ordinal = reader.GetOrdinal(columnName);
                return reader.IsDBNull(ordinal) ? defaultValue : reader.GetInt32(ordinal);
            }
            catch
            {
                return defaultValue;
            }
        }

        private float GetFloatSafely(System.Data.Common.DbDataReader reader, string columnName, float defaultValue)
        {
            try
            {
                int ordinal = reader.GetOrdinal(columnName);
                return reader.IsDBNull(ordinal) ? defaultValue : reader.GetFloat(ordinal);
            }
            catch
            {
                return defaultValue;
            }
        }

        private void AddResistance(System.Data.Common.DbDataReader reader, List<string> tooltipParts, string columnName, string resName)
        {
            int value = GetValueSafely(reader, columnName, 0);
            if (value > 0)
                tooltipParts.Add($"+{value} к сопротивлению {resName}");
        }
        private string GetStatName(int statType)
        {
            return statType switch
            {
                1 => "Сила",
                2 => "Ловкость",
                3 => "Выносливость",
                4 => "Интеллект",
                5 => "Дух",
                6 => "Защита",
                7 => "Уклонение",
                8 => "Парирование",
                9 => "Блок",
                10 => "Попадание",
                11 => "Критический удар",
                12 => "Искусность",
                13 => "Скорость",
                14 => "Пробивание брони",
                15 => "Сила заклинаний",
                16 => "Атака",
                17 => "Мана",
                _ => $"Стат {statType}"
            };
        }

        private string GetInventoryType(int type)
        {
            return type switch
            {
                1 => "Голова",
                2 => "Шея",
                3 => "Плечи",
                4 => "Рубашка",
                5 => "Грудь",
                6 => "Пояс",
                7 => "Ноги",
                8 => "Ноги",
                9 => "Ноги",
                10 => "Кисти",
                11 => "Запястья",
                12 => "Перчатки",
                13 => "Кольцо",
                14 => "Аксессуар",
                15 => "Плащ",
                16 => "Оружие",
                17 => "Оружие",
                18 => "Оружие",
                19 => "Оружие",
                20 => "Оружие",
                21 => "Двуручное",
                22 => "Оружие",
                23 => "Оружие",
                24 => "Ружьё",
                25 => "Лук",
                26 => "Посох",
                _ => "Разное"
            };
        }

        private string GetSocketColor(int color)
        {
            return color switch
            {
                1 => "💎 Мета",
                2 => "🔴 Красный",
                4 => "🟡 Желтый",
                8 => "🔵 Синий",
                _ => "⚪ Бесцветный"
            };
        }

        private string GetBondingType(int bonding)
        {
            return bonding switch
            {
                1 => "При получении",
                2 => "При надевании",
                3 => "При использовании",
                _ => "Нет"
            };
        }

        private string GetMaterialName(int material)
        {
            return material switch
            {
                0 => "Металл",
                1 => "Древесина",
                2 => "Ткань",
                3 => "Кожа",
                4 => "Драгоценный камень",
                5 => "Стекло",
                6 => "Жидкость",
                7 => "Бумага",
                8 => "Плоть",
                9 => "Кость",
                10 => "Элементаль",
                _ => "Разное"
            };
        }

        private string GetSheathType(int sheath)
        {
            return sheath switch
            {
                1 => "Двуручное",
                2 => "Посох",
                3 => "Одноручное",
                4 => "Щит",
                5 => "Лук",
                6 => "Кинжал",
                7 => "Молот",
                _ => "Разное"
            };
        }
    }
}