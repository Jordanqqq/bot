using System;
using System.IO;
using System.Threading.Tasks;
using MaNGOSExtractor.Extractor.DatabaseExtractor;
using MaNGOSExtractor.Extractor.DbcParser;
using MaNGOSExtractor.Extractor.Parsers;
using MaNGOSExtractor.Extractor.DataSaver;
using RaidLootCore.Models;

namespace MaNGOSExtractor
{
    internal class Program
    {
        static async Task Main(string[] args)
        {
            Console.WriteLine("================================================");
            Console.WriteLine("         MANGOS DATABASE EXTRACTOR              ");
            Console.WriteLine("================================================\n");

            Console.WriteLine("Этот инструмент извлечёт данные из базы MaNGOS");
            Console.WriteLine("и создаст JSON файлы для Discord бота.\n");

            // Путь к папке бота
            string botOutputPath = @"C:\Users\jalil\source\repos\RaidLootSystem\RaidLootBot";

            try
            {
                // Запрашиваем параметры подключения
                Console.Write("Хост MySQL [localhost]: ");
                string host = Console.ReadLine();
                if (string.IsNullOrWhiteSpace(host)) host = "localhost";

                Console.Write("Порт [3306]: ");
                string portStr = Console.ReadLine();
                int port = string.IsNullOrWhiteSpace(portStr) ? 3306 : int.Parse(portStr);

                Console.Write("База данных [mangos]: ");
                string database = Console.ReadLine();
                if (string.IsNullOrWhiteSpace(database)) database = "mangos";

                Console.Write("Пользователь [mangos]: ");
                string user = Console.ReadLine();
                if (string.IsNullOrWhiteSpace(user)) user = "mangos";

                Console.Write("Пароль [mangos]: ");
                string password = Console.ReadLine();
                if (string.IsNullOrWhiteSpace(password)) password = "mangos";

                Console.Write("\nПуть к папке DBC (например C:\\Wow\\dbc): ");
                string dbcPath = Console.ReadLine();
                if (string.IsNullOrWhiteSpace(dbcPath)) dbcPath = @".\dbc";

                Console.WriteLine("\n🚀 НАЧАЛО ИЗВЛЕЧЕНИЯ ДАННЫХ...\n");

                // Создаём выходные папки в папке бота
                Directory.CreateDirectory(Path.Combine(botOutputPath, "Output"));
                Directory.CreateDirectory(Path.Combine(botOutputPath, "Output", "icons", "items"));
                Directory.CreateDirectory(Path.Combine(botOutputPath, "Output", "icons", "spells"));
                Directory.CreateDirectory(Path.Combine(botOutputPath, "Output", "icons", "bosses"));

                // 1. Подключение к MySQL
                Console.WriteLine("\n🔌 Подключение к MySQL...");
                var connection = new MangosConnection(host, database, user, password, port);
                if (!await connection.TestConnectionAsync())
                {
                    Console.WriteLine("❌ Не удалось подключиться к MySQL");
                    Console.WriteLine("\nНажмите любую клавишу для выхода...");
                    Console.ReadKey();
                    return;
                }

                // 2. Загрузка предметов из MySQL
                Console.WriteLine("\n📦 Загрузка предметов из MySQL...");
                var itemLoader = new ItemLoader(connection);
                var items = await itemLoader.LoadItemsAsync();
                Console.WriteLine($"   ✅ Загружено {items.Count} предметов");

                // 3. Загрузка боссов из MySQL
                Console.WriteLine("\n👑 Загрузка боссов из MySQL...");
                var bossLoader = new BossLoader(connection);
                var bosses = await bossLoader.LoadBossesAsync();
                Console.WriteLine($"   ✅ Загружено {bosses.Count} боссов");

                // 4. Загрузка лута из MySQL
                Console.WriteLine("\n📋 Загрузка лута...");
                var lootLoader = new LootLoader(connection);
                var lootRelations = await lootLoader.LoadLootRelationsAsync();
                Console.WriteLine($"   ✅ Загружено {lootRelations.Count} связей босс-лут");

                // 5. Парсинг DBC файлов
                Console.WriteLine("\n🔮 Парсинг DBC файлов...");

                var spellDbcParser = new SpellDbcParser(dbcPath);
                var spells = spellDbcParser.ParseSpells();
                Console.WriteLine($"   ✅ Загружено {spells.Count} заклинаний из Spell.dbc");

                var itemDisplayParser = new ItemDisplayParser(dbcPath);
                var displayIcons = itemDisplayParser.ParseItemDisplayInfo();
                Console.WriteLine($"   ✅ Загружено {displayIcons.Count} иконок из ItemDisplayInfo.dbc");

                // 6. Привязка иконок к предметам
                Console.WriteLine("\n🔗 Привязка иконок к предметам...");
                int iconCount = 0;
                foreach (var item in items)
                {
                    if (displayIcons.TryGetValue(item.Id, out string iconName))
                    {
                        item.Icon = iconName;
                        iconCount++;
                    }
                }
                Console.WriteLine($"   ✅ Привязано {iconCount} иконок");

                // 7. Парсинг предметов (создание тултипов)
                Console.WriteLine("\n📝 Парсинг предметов...");
                var itemParser = new ItemParser();
                foreach (var item in items)
                {
                    string tooltip = itemParser.BuildTooltip(item);
                    item.TooltipRu = tooltip;
                    item.TooltipEn = tooltip;
                }
                Console.WriteLine($"   ✅ Созданы тултипы для {items.Count} предметов");

                // 8. Анализ заклинаний
                Console.WriteLine("\n🔬 Анализ заклинаний...");
                var spellAnalyzer = new SpellAnalyzer();
                spellAnalyzer.ClassifySpells(spells);
                Console.WriteLine($"   ✅ Проанализировано {spells.Count} заклинаний");

                // 9. Сохранение JSON файлов в папку бота
                Console.WriteLine("\n💾 Сохранение JSON файлов...");
                var jsonSaver = new JsonSaver();
                await jsonSaver.SaveItemsAsync(items, Path.Combine(botOutputPath, "Output", "items.json"));
                await jsonSaver.SaveBossesAsync(bosses, Path.Combine(botOutputPath, "Output", "bosses.json"));
                await jsonSaver.SaveSpellsAsync(spells, Path.Combine(botOutputPath, "Output", "spells.json"));
                await jsonSaver.SaveLootAsync(lootRelations, items, bosses, Path.Combine(botOutputPath, "Output", "loot.json"));

                Console.WriteLine("\n✅ ИЗВЛЕЧЕНИЕ ЗАВЕРШЕНО!");
                Console.WriteLine($"📁 Файлы сохранены в: {Path.Combine(botOutputPath, "Output")}");
                Console.WriteLine($"   - items.json: {items.Count} предметов");
                Console.WriteLine($"   - bosses.json: {bosses.Count} боссов");
                Console.WriteLine($"   - spells.json: {spells.Count} заклинаний");
                Console.WriteLine($"   - loot.json: {lootRelations.Count} связей босс-лут");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"\n❌ ОШИБКА: {ex.Message}");
                Console.WriteLine(ex.StackTrace);
            }

            Console.WriteLine("\nНажмите любую клавишу для выхода...");
            Console.ReadKey();
        }
    }
}