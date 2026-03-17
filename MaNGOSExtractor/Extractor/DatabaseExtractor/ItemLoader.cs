using MaNGOSExtractor.Extractor.Parsers;
using MySqlConnector;
using RaidLootCore.Models;
using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Threading.Tasks;

namespace MaNGOSExtractor.Extractor.DatabaseExtractor
{
    public class ItemLoader
    {
        private readonly MangosConnection _connection;
        private readonly ItemParser _itemParser;

        public ItemLoader(MangosConnection connection)
        {
            _connection = connection;
            _itemParser = new ItemParser();
        }

        public async Task<List<Item>> LoadItemsAsync()
        {
            var items = new List<Item>();

            string query = @"
                SELECT 
                    entry, name, Quality, ItemLevel, RequiredLevel, class, subclass,
                    InventoryType, displayid, description, armor, block,
                    stat_type1, stat_value1, stat_type2, stat_value2,
                    stat_type3, stat_value3, stat_type4, stat_value4,
                    stat_type5, stat_value5, stat_type6, stat_value6,
                    stat_type7, stat_value7, stat_type8, stat_value8,
                    stat_type9, stat_value9, stat_type10, stat_value10,
                    dmg_min1, dmg_max1, delay,
                    holy_res, fire_res, nature_res, frost_res, shadow_res, arcane_res,
                    socketColor_1, socketColor_2, socketColor_3, socketBonus,
                    bonding, MaxDurability, BuyPrice, SellPrice,
                    AllowableClass
                FROM item_template 
                WHERE Quality >= 3 
                ORDER BY entry";

            Console.WriteLine($"   Выполняется SQL запрос...");

            using var connection = _connection.GetConnection();
            await connection.OpenAsync();

            using var command = new MySqlCommand(query, connection);
            using var reader = await command.ExecuteReaderAsync();

            if (!reader.HasRows)
            {
                Console.WriteLine("   ⚠️ Нет данных в таблице item_template");
                return items;
            }

            int count = 0;
            while (await reader.ReadAsync())
            {
                count++;

                // Для отладки покажем первые 5 предметов
                if (count <= 5)
                {
                    Console.WriteLine($"\n   📦 Предмет #{count}:");
                    Console.WriteLine($"      ID: {reader["entry"]}");
                    Console.WriteLine($"      Name: {reader["name"]}");
                    Console.WriteLine($"      Quality: {reader["Quality"]}");
                }

                var item = new Item
                {
                    Id = GetInt32(reader, "entry"),
                    NameEn = GetString(reader, "name"),
                    NameRu = GetString(reader, "name"),
                    Quality = GetInt32(reader, "Quality").ToString(),
                    ItemLevel = GetInt32(reader, "ItemLevel"),
                    RequiredLevel = GetInt32(reader, "RequiredLevel"),
                    Type = GetInventoryType(GetInt32(reader, "InventoryType")),
                    Icon = GetInt32(reader, "displayid").ToString(),
                    Description = GetString(reader, "description"),
                    Armor = GetInt32(reader, "armor"),
                    Block = GetInt32(reader, "block"),
                    MinDamage = GetFloat(reader, "dmg_min1"),
                    MaxDamage = GetFloat(reader, "dmg_max1"),
                    Speed = GetInt32(reader, "delay") / 1000.0f,
                    FireResist = GetInt32(reader, "fire_res"),
                    FrostResist = GetInt32(reader, "frost_res"),
                    ShadowResist = GetInt32(reader, "shadow_res"),
                    NatureResist = GetInt32(reader, "nature_res"),
                    ArcaneResist = GetInt32(reader, "arcane_res"),
                    Durability = GetInt32(reader, "MaxDurability"),
                    BuyPrice = GetInt64(reader, "BuyPrice"),
                    SellPrice = GetInt64(reader, "SellPrice"),
                    AllowableClass = GetInt32(reader, "AllowableClass"),
                    Strength = 0,
                    Agility = 0,
                    Stamina = 0,
                    Intellect = 0,
                    Spirit = 0,
                    AttackPower = 0,
                    SpellPower = 0,
                    CritRating = 0,
                    HasteRating = 0,
                    HitRating = 0,
                    ExpertiseRating = 0,
                    ArmorPenetration = 0,
                    Sockets = new List<ItemSocket>(),
                    Bonding = GetBondingType(GetInt32(reader, "bonding"))
                };

                // Расчёт DPS
                if (item.MinDamage > 0 && item.MaxDamage > 0 && item.Speed > 0)
                {
                    item.DPS = ((item.MinDamage + item.MaxDamage) / 2) / item.Speed;
                }

                ParseStats(reader, item);
                ParseSockets(reader, item);

                // Создаем тултип
                string tooltip = _itemParser.BuildTooltip(item);
                item.TooltipRu = tooltip;
                item.TooltipEn = tooltip;

                items.Add(item);

                if (count % 1000 == 0)
                {
                    Console.Write($"\r   Загружено {count} предметов...");
                }
            }

            Console.WriteLine($"\n   ✅ Загружено {items.Count} предметов");
            return items;
        }

        private void ParseStats(DbDataReader reader, Item item)
        {
            // Обнуляем все статы
            item.Strength = 0;
            item.Agility = 0;
            item.Stamina = 0;
            item.Intellect = 0;
            item.Spirit = 0;
            item.AttackPower = 0;
            item.SpellPower = 0;
            item.CritRating = 0;
            item.HasteRating = 0;
            item.HitRating = 0;
            item.ExpertiseRating = 0;
            item.ArmorPenetration = 0;

            for (int i = 1; i <= 10; i++)
            {
                int statType = GetInt32(reader, $"stat_type{i}");
                int statValue = GetInt32(reader, $"stat_value{i}");

                if (statValue == 0) continue;

                switch (statType)
                {
                    case 1: item.Strength += statValue; break;      // Сила
                    case 2: item.Agility += statValue; break;       // Ловкость
                    case 3: item.Stamina += statValue; break;       // Выносливость
                    case 4: item.Intellect += statValue; break;     // Интеллект
                    case 5: item.Spirit += statValue; break;        // Дух
                    case 6: item.AttackPower += statValue; break;   // Сила атаки (ближний бой)
                    case 7: item.AttackPower += statValue; break;   // Сила атаки (дальний бой)
                    case 8: item.SpellPower += statValue; break;    // Сила заклинаний (урон)
                    case 9: item.SpellPower += statValue; break;    // Сила заклинаний (лечение)
                    case 10: item.CritRating += statValue; break;   // Критический удар (ближний бой)
                    case 11: item.HasteRating += statValue; break;  // Скорость (ближний бой)
                    case 12: item.HitRating += statValue; break;    // Меткость (ближний бой)
                    case 13: item.ExpertiseRating += statValue; break; // Искусность
                    case 14: item.ArmorPenetration += statValue; break; // Пробивание брони
                    case 15: item.AttackPower += statValue; break;  // Сила атаки (доп)
                    case 16: item.SpellPower += statValue; break;   // Сила заклинаний (доп)
                    case 17: // Мана - игнорируем
                        break;
                    case 18: // Рейтинг защиты
                        break;
                    case 19: // Уклонение
                        break;
                    case 20: // Парирование
                        break;
                    case 21: // Блок
                        break;
                    case 22: // Критический удар заклинаний
                        item.CritRating += statValue; break;
                    case 23: // Меткость заклинаний
                        item.HitRating += statValue; break;
                    case 24: // Скорость заклинаний
                        item.HasteRating += statValue; break;
                    case 25: // Пробивание брони заклинаний
                        item.ArmorPenetration += statValue; break;
                    case 26: // Сила атаки (доп)
                        item.AttackPower += statValue; break;
                    case 27: // Сила заклинаний (доп)
                        item.SpellPower += statValue; break;
                    case 28: // Сила атаки в облике
                        item.AttackPower += statValue; break;
                    case 29: // Сила заклинаний в облике
                        item.SpellPower += statValue; break;
                    case 30: // Сила атаки (доп)
                        item.AttackPower += statValue; break;
                    case 31: // Крит заклинаний (доп)
                        item.CritRating += statValue; break;
                    case 32: // Рейтинг защиты (доп)
                        break;
                    case 33: // Уклонение (доп)
                        break;
                    case 34: // Парирование (доп)
                        break;
                    case 35: // Блок (доп)
                        break;
                    case 36: // Рейтинг меткости (доп)
                        item.HitRating += statValue; break;
                    case 37: // Рейтинг критического удара (доп)
                        item.CritRating += statValue; break;
                    case 38: // Рейтинг скорости (доп)
                        item.HasteRating += statValue; break;
                    case 39: // Рейтинг пробивания брони (доп)
                        item.ArmorPenetration += statValue; break;
                    case 40: // Сила атаки (доп)
                        item.AttackPower += statValue; break;
                    case 41: // Сила заклинаний (доп)
                        item.SpellPower += statValue; break;
                    case 42: // Рейтинг защиты от заклинаний
                        break;
                    case 43: // Рейтинг уклонения от заклинаний
                        break;
                    case 44: // Рейтинг парирования от заклинаний
                        break;
                    case 45: // Рейтинг блока от заклинаний
                        break;

                    case 46: // Рейтинг мастерства (доп) - суммируем с основным
                        item.ExpertiseRating += statValue; break;
                    case 47: // Крит заклинаний (доп) - суммируем с основным критическим ударом
                        item.CritRating += statValue; break;
                    case 48: // Скорость заклинаний (доп) - суммируем с основной скоростью
                        item.HasteRating += statValue; break;


                    default:
                        Console.WriteLine($"   ⚠️ Неизвестный тип стата: {statType} = {statValue}");
                        break;
                }
            }
        }

        private void ParseSockets(DbDataReader reader, Item item)
        {
            for (int i = 1; i <= 3; i++)
            {
                int socketColor = GetInt32(reader, $"socketColor_{i}");
                if (socketColor == 0) continue;

                var socket = new ItemSocket
                {
                    Color = socketColor switch
                    {
                        1 => "Meta",
                        2 => "Red",
                        4 => "Yellow",
                        8 => "Blue",
                        _ => "Unknown"
                    }
                };
                item.Sockets.Add(socket);
            }

            int socketBonusId = GetInt32(reader, "socketBonus");
            if (socketBonusId > 0)
            {
                item.SocketBonus = $"Бонус: +{socketBonusId}";
            }
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
                10 => "Кисти рук",
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
                21 => "Двуручное оружие",
                22 => "Оружие",
                23 => "Оружие",
                24 => "Ружьё",
                25 => "Лук",
                26 => "Посох",
                27 => "Разное",
                _ => "Разное"
            };
        }

        private string GetBondingType(int bonding)
        {
            return bonding switch
            {
                1 => "При получении",
                2 => "При надевании",
                3 => "При использовании",
                _ => ""
            };
        }

        private int GetInt32(DbDataReader reader, string column)
        {
            try
            {
                int ord = reader.GetOrdinal(column);
                return reader.IsDBNull(ord) ? 0 : reader.GetInt32(ord);
            }
            catch
            {
                return 0;
            }
        }

        private long GetInt64(DbDataReader reader, string column)
        {
            try
            {
                int ord = reader.GetOrdinal(column);
                return reader.IsDBNull(ord) ? 0 : reader.GetInt64(ord);
            }
            catch
            {
                return 0;
            }
        }

        private float GetFloat(DbDataReader reader, string column)
        {
            try
            {
                int ord = reader.GetOrdinal(column);
                return reader.IsDBNull(ord) ? 0 : reader.GetFloat(ord);
            }
            catch
            {
                return 0;
            }
        }

        private string GetString(DbDataReader reader, string column)
        {
            try
            {
                int ord = reader.GetOrdinal(column);
                return reader.IsDBNull(ord) ? "" : reader.GetString(ord);
            }
            catch
            {
                return "";
            }
        }
    }
}