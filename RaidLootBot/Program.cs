using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using RaidLootCore.Models;
using RaidLootInfrastructure;
using RaidLootServices;

namespace RaidLootBot
{
    internal class Program
    {
        static async Task Main(string[] args)
        {
            Console.WriteLine("===============================================");
            Console.WriteLine("    WOW MULTI-EXPANSION LOOT SYSTEM (RU/EN)    ");
            Console.WriteLine("===============================================\n");

            // --- 1. НАСТРОЙКА КОНФИГУРАЦИИ ---
            var configFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "bot_settings.json");
            string botToken = "";
            ulong channelId = 0;

            if (File.Exists(configFilePath))
            {
                try
                {
                    var savedConfig = JsonSerializer.Deserialize<Dictionary<string, string>>(File.ReadAllText(configFilePath));
                    if (savedConfig != null)
                    {
                        savedConfig.TryGetValue("Token", out botToken);
                        if (savedConfig.TryGetValue("ChannelId", out string? cId))
                            ulong.TryParse(cId, out channelId);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"⚠️ Ошибка чтения конфига: {ex.Message}");
                }
            }

            if (string.IsNullOrEmpty(botToken) || channelId == 0)
            {
                Console.WriteLine("[НАСТРОЙКА] Требуется ввод данных для Discord...");

                if (string.IsNullOrEmpty(botToken))
                {
                    Console.Write("Введите Token: ");
                    botToken = Console.ReadLine() ?? "";
                }

                if (channelId == 0)
                {
                    Console.Write("Введите ID канала: ");
                    ulong.TryParse(Console.ReadLine(), out channelId);
                }

                var configToSave = new Dictionary<string, string>
                {
                    { "Token", botToken },
                    { "ChannelId", channelId.ToString() }
                };

                File.WriteAllText(
                    configFilePath,
                    JsonSerializer.Serialize(configToSave, new JsonSerializerOptions { WriteIndented = true })
                );
            }

            // --- 2. ПРОВЕРКА ФАЙЛОВ ---
            if (!File.Exists("raids.json"))
            {
                Console.WriteLine("[ОШИБКА] Файл raids.json не найдены!");
                Console.WriteLine("Текущая папка: " + AppDomain.CurrentDomain.BaseDirectory);
                return;
            }

            // --- 3. ЗАГРУЗКА БАЗЫ ПРЕДМЕТОВ ИЗ OUTPUT В КОРНЕ ПРОЕКТА ---
            Console.WriteLine("\n📦 Загрузка базы предметов...");
            List<Item> loadingItems = new();

            // Путь к папке Output в корне проекта (не в bin)
            string projectRoot = Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", ".."));
            string itemsPath = Path.Combine(projectRoot, "Output", "items.json");

            Console.WriteLine($"🔍 Поиск файлов в: {itemsPath}");

            if (!File.Exists(itemsPath))
            {
                Console.WriteLine("❌ Файл Output/items.json не найден!");
                Console.WriteLine("Сначала запустите MaNGOSExtractor для создания базы данных.");
                return;
            }

            Console.WriteLine("📥 Чтение Output/items.json...");

            try
            {
                var json = await File.ReadAllTextAsync(itemsPath);
                var items = JsonSerializer.Deserialize<List<Item>>(json);

                if (items != null)
                    loadingItems = items;

                Console.WriteLine($"✅ Загружено {loadingItems.Count} предметов");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Ошибка чтения базы: {ex.Message}");

                return;
            }

            // --- 4. ЗАГРУЗКА РЕЙДОВ ---
            var raidLoader = new RaidDataLoader(AppDomain.CurrentDomain.BaseDirectory);
            raidLoader.LoadRaidConfigs();

            // --- 5. ЗАПУСК БОТА ---
            Console.WriteLine("\n🤖 ЗАПУСК БОТА...");
            Console.WriteLine("===============================================");

            var sessionManager = new RaidSessionManager();
            var botService = new DiscordBotService();

            try
            {
                await botService.StartWithDataAsync(
                    botToken,
                    loadingItems,
                    raidLoader,
                    sessionManager,
                    channelId
                );
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[КРИТИЧЕСКАЯ ОШИБКА] Бот упал: {ex.Message}");
                Console.WriteLine(ex.StackTrace);
            }
        }
    }
}