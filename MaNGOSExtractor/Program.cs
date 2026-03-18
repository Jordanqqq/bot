using System.Text.Json;
using MaNGOSExtractor.Services;
using MaNGOSExtractor.Extractors;
using MaNGOSExtractor.Models;
using MaNGOSExtractor.MyMpqReader;
using MaNGOSExtractor.MpqCore;

namespace MaNGOSExtractor;

class Program
{
    static async Task Main(string[] args)
{
    Console.WriteLine("==========================================");
    Console.WriteLine("   MaNGOS FULL DATA EXTRACTOR v4.0      ");
    Console.WriteLine("==========================================\n");

    // 1. Загружаем конфиг
    var config = LoadConfig();

    // ТЕСТ MPQ CORE READER
    string testMpqPath = @"D:\ноут\CircleL\CircleL\Data\locale-ruRU.MPQ";
    MyMpqReader.MpqTest.RunTest(testMpqPath);

    // 2. Меню выбора режима
    Console.WriteLine("\nВыберите режим работы:");

        try
        {
            // Путь к тестовому архиву
            string mpqPath = @"D:\ноут\CircleL\CircleL\Data\locale-ruRU.MPQ";

            if (File.Exists(mpqPath))
            {
                using var reader = new MpqArchiveReader(mpqPath);

                // Поиск файла Spell.dbc
                Console.WriteLine("\n📦 Поиск Spell.dbc...");
                int index = reader.FindFileIndex("DBFilesClient\\Spell.dbc");

                if (index >= 0)
                {
                    Console.WriteLine($"   ✅ Найден, индекс: {index}");

                    // Извлекаем файл
                    var data = reader.ExtractFile(index);
                    if (data != null && data.Length > 0)
                    {
                        string outputPath = Path.Combine(Directory.GetCurrentDirectory(), "Spell_test.dbc");
                        File.WriteAllBytes(outputPath, data);
                        Console.WriteLine($"   ✅ Извлечено {data.Length} байт, сохранено в: {outputPath}");
                    }
                }
                else
                {
                    Console.WriteLine("   ❌ Spell.dbc не найден");
                }

                // Список всех DBC файлов
                Console.WriteLine("\n📋 Список DBC файлов:");
                var files = reader.ListDbcFiles();
                foreach (var file in files)
                {
                    Console.WriteLine($"   - {file}");
                }

                // Показываем статистику блоков
                Console.WriteLine("\n📊 Статистика блоков:");
                var blocks = reader.GetAllBlocks();
                int compressed = 0;
                int singleUnit = 0;
                int exists = 0;

                foreach (var block in blocks)
                {
                    if ((block.Flags & (uint)MpqFileFlags.Compressed) != 0) compressed++;
                    if ((block.Flags & (uint)MpqFileFlags.SingleUnit) != 0) singleUnit++;
                    if ((block.Flags & (uint)MpqFileFlags.Exists) != 0) exists++;
                }

                Console.WriteLine($"   Всего блоков: {blocks.Count}");
                Console.WriteLine($"   Существующих: {exists}");
                Console.WriteLine($"   Сжатых: {compressed}");
                Console.WriteLine($"   Single-unit: {singleUnit}");
            }
            else
            {
                Console.WriteLine($"❌ Файл не найден: {mpqPath}");

                // Показываем содержимое папки Data
                string dataFolder = @"D:\ноут\CircleL\CircleL\Data";
                if (Directory.Exists(dataFolder))
                {
                    Console.WriteLine($"\n📁 Содержимое папки Data:");
                    var files = Directory.GetFiles(dataFolder, "*.MPQ");
                    foreach (var file in files)
                    {
                        Console.WriteLine($"   - {Path.GetFileName(file)}");
                    }

                    string ruFolder = Path.Combine(dataFolder, "ruRU");
                    if (Directory.Exists(ruFolder))
                    {
                        Console.WriteLine($"\n📁 Содержимое папки ruRU:");
                        var ruFiles = Directory.GetFiles(ruFolder, "*.MPQ");
                        foreach (var file in ruFiles)
                        {
                            Console.WriteLine($"   - {Path.GetFileName(file)} (ruRU)");
                        }
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Ошибка теста: {ex.Message}");
            Console.WriteLine($"   Стек: {ex.StackTrace}");
        }
        Console.WriteLine("==========================================\n");
        // ============================================================

        // 2. Меню выбора режима
        Console.WriteLine("\nВыберите режим работы:");
        Console.WriteLine("1️⃣  Полное извлечение (MySQL + MPQ)");
        Console.WriteLine("2️⃣  Только MySQL");
        Console.WriteLine("3️⃣  Только MPQ (DBC + иконки)");
        Console.WriteLine("4️⃣  Загрузить выборочно из дампа");
        Console.Write("\nВаш выбор (1-4): ");

        var choice = Console.ReadLine();

        switch (choice)
        {
            case "1":
                await FullExtractAsync(config);
                break;
            case "2":
                await MySqlOnlyAsync(config);
                break;
            case "3":
                await MpqOnlyAsync(config);
                break;
            case "4":
                await LoadPartialFromDumpAsync();
                break;
            default:
                Console.WriteLine("Неверный выбор");
                WaitForExit();
                break;
        }
    }

    static string EnterWowPath()
    {
        Console.WriteLine("\n══════════════════════════════════════════════");
        Console.WriteLine("📁 Укажите путь к папке с World of Warcraft 3.3.5");
        Console.WriteLine("══════════════════════════════════════════════");
        Console.WriteLine("Пример: D:\\ноут\\CircleL\\CircleL");
        Console.WriteLine("(Это должна быть папка, в которой есть подпапка Data)");
        Console.WriteLine();
        Console.Write("Введите путь: ");

        string path = Console.ReadLine()?.Trim() ?? "";

        // Убираем кавычки, если пользователь их вставил
        path = path.Trim('"').Trim('\'');

        if (string.IsNullOrEmpty(path))
        {
            Console.WriteLine("   ⚠️ Путь не указан");
            return "";
        }

        // Проверяем, есть ли папка Data
        string dataPath = Path.Combine(path, "Data");
        if (!Directory.Exists(dataPath))
        {
            Console.WriteLine($"   ❌ Ошибка: в папке '{path}' нет подпапки 'Data'!");
            Console.WriteLine("   Проверьте путь и запустите программу снова.");
            WaitForExit();
            Environment.Exit(0);
        }

        Console.WriteLine($"   ✅ Путь принят: {path}");
        Console.WriteLine($"   ✅ Папка Data найдена: {dataPath}");
        return path;
    }

    static void WaitForExit()
    {
        Console.WriteLine("\n══════════════════════════════════════════════");
        Console.WriteLine("✅ Готово! Нажмите любую клавишу для выхода...");
        Console.WriteLine("══════════════════════════════════════════════");
        Console.ReadKey();
    }

    static Config LoadConfig()
    {
        var path = "Config.json";
        var fullPath = Path.GetFullPath(path);

        Console.WriteLine($"\n🔍 Поиск конфига: {fullPath}");

        if (!File.Exists(path))
        {
            Console.WriteLine("⚙️ Создание файла Config.json...");

            string wowPath = "";

            Console.Write("\nХотите указать путь к WoW для извлечения иконок? (y/n): ");
            var answer = Console.ReadLine()?.ToLower();

            if (answer == "y" || answer == "yes")
            {
                wowPath = EnterWowPath();
            }

            var defaultConfig = new Config
            {
                Host = "localhost",
                Port = 3306,
                Database = "mangos",
                User = "mangos",
                Password = "mangos",
                WowPath = wowPath
            };

            var json = JsonSerializer.Serialize(defaultConfig,
                new JsonSerializerOptions { WriteIndented = true });

            File.WriteAllText(path, json);
            Console.WriteLine($"✅ Конфиг создан: {fullPath}");
            Console.WriteLine("📝 Содержимое файла:");
            Console.WriteLine(json);
            Console.WriteLine("\n📝 Запустите программу снова.");
            WaitForExit();
        }

        // Читаем существующий файл
        Console.WriteLine($"📂 Читаем конфиг: {fullPath}");

        if (!File.Exists(path))
        {
            Console.WriteLine($"❌ Файл {path} не существует!");
            WaitForExit();
            Environment.Exit(0);
        }

        var configJson = File.ReadAllText(path);
        Console.WriteLine("📄 Содержимое файла (сырой текст):");
        Console.WriteLine("----------------------------------------");
        Console.WriteLine(configJson);
        Console.WriteLine("----------------------------------------");

        try
        {
            var config = JsonSerializer.Deserialize<Config>(configJson);

            Console.WriteLine("\n📊 Распарсенные значения:");
            Console.WriteLine($"   Host: '{config.Host}'");
            Console.WriteLine($"   Port: {config.Port}");
            Console.WriteLine($"   Database: '{config.Database}'");
            Console.WriteLine($"   User: '{config.User}'");
            Console.WriteLine($"   Password: {(string.IsNullOrEmpty(config.Password) ? "пустой" : "указан")}");
            Console.WriteLine($"   WowPath: '{config.WowPath}'");

            if (string.IsNullOrEmpty(config.WowPath))
            {
                Console.WriteLine("   ⚠️ WowPath пустой!");
            }
            else
            {
                Console.WriteLine($"   ✅ WowPath указан: {config.WowPath}");

                if (Directory.Exists(config.WowPath))
                {
                    Console.WriteLine($"   ✅ Папка существует: {config.WowPath}");

                    string dataPath = Path.Combine(config.WowPath, "Data");
                    if (Directory.Exists(dataPath))
                    {
                        Console.WriteLine($"   ✅ Папка Data существует: {dataPath}");

                        var mpqFiles = Directory.GetFiles(dataPath, "*.MPQ");
                        Console.WriteLine($"   📊 Найдено MPQ файлов: {mpqFiles.Length}");
                        foreach (var mpq in mpqFiles.Take(3))
                        {
                            Console.WriteLine($"      - {Path.GetFileName(mpq)}");
                        }
                    }
                    else
                    {
                        Console.WriteLine($"   ❌ Папка Data не найдена: {dataPath}");
                    }
                }
                else
                {
                    Console.WriteLine($"   ❌ Папка не существует: {config.WowPath}");
                }
            }

            return config;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Ошибка при парсинге JSON: {ex.Message}");
            WaitForExit();
            Environment.Exit(0);
            return null;
        }
    }

    static async Task FullExtractAsync(Config config)
    {
        using var db = new DatabaseService(config);
        if (!await db.TestConnectionAsync())
        {
            Console.WriteLine("❌ Не удалось подключиться к MySQL");
            WaitForExit();
            return;
        }

        // Проверяем путь к WoW (может быть пустым)
        if (!string.IsNullOrEmpty(config.WowPath))
        {
            if (!Directory.Exists(config.WowPath))
            {
                Console.WriteLine($"\n⚠️ Предупреждение: Папка WoW не существует: {config.WowPath}");
                Console.WriteLine("   Продолжаем без извлечения MPQ...");
            }
            else
            {
                string dataPath = Path.Combine(config.WowPath, "Data");
                if (!Directory.Exists(dataPath))
                {
                    Console.WriteLine($"\n⚠️ Предупреждение: Папка Data не найдена: {dataPath}");
                }
            }
        }

        using var mpq = new MyMpqExtractor(config.WowPath);
        var extractor = new FullExtractor(db, mpq, "Output");
        var data = await extractor.ExtractAllAsync();
        await extractor.SaveToJson(data);

        Console.WriteLine("\n✅ Полное извлечение завершено!");
        WaitForExit();
    }

    static async Task MySqlOnlyAsync(Config config)
    {
        using var db = new DatabaseService(config);
        if (!await db.TestConnectionAsync())
        {
            Console.WriteLine("❌ Не удалось подключиться к MySQL");
            WaitForExit();
            return;
        }

        var extractor = new FullExtractor(db, null, "Output");
        var data = new FullGameData();
        await extractor.ExtractMySqlData(data);

        var options = new JsonSerializerOptions { WriteIndented = true };
        var json = JsonSerializer.Serialize(data, options);
        await File.WriteAllTextAsync("Output/mysql_dump.json", json);

        Console.WriteLine("\n✅ MySQL дамп сохранён!");
        WaitForExit();
    }

    static async Task MpqOnlyAsync(Config config)
    {
        // Проверяем, указан ли путь к WoW
        if (string.IsNullOrEmpty(config.WowPath))
        {
            Console.WriteLine("\n❌ Путь к WoW не указан в Config.json!");
            Console.WriteLine("   Удалите Config.json и запустите программу заново,");
            Console.WriteLine("   чтобы указать путь к папке с игрой.");
            WaitForExit();
            return;
        }

        // Проверяем, существует ли папка
        if (!Directory.Exists(config.WowPath))
        {
            Console.WriteLine($"\n❌ Папка не существует: {config.WowPath}");
            Console.WriteLine("   Проверьте путь в Config.json");
            WaitForExit();
            return;
        }

        // Проверяем наличие папки Data
        string dataPath = Path.Combine(config.WowPath, "Data");
        if (!Directory.Exists(dataPath))
        {
            Console.WriteLine($"\n❌ Папка Data не найдена: {dataPath}");
            Console.WriteLine("   Убедитесь, что путь указывает на корневую папку игры");
            WaitForExit();
            return;
        }

        Console.WriteLine($"\n✅ Путь к WoW: {config.WowPath}");
        Console.WriteLine($"✅ Папка Data: {dataPath}");

        using var mpq = new MyMpqExtractor(config.WowPath);

        mpq.ListAllDbcFiles();
        mpq.ExportDbc();

        Console.WriteLine("\n✅ MPQ данные извлечены!");
        WaitForExit();
    }

    static async Task LoadPartialFromDumpAsync()
    {
        var dumpPath = Path.Combine("Output", "full_dump.json");
        if (!File.Exists(dumpPath))
        {
            Console.WriteLine("❌ Сначала сделай полное извлечение (режим 1)");
            WaitForExit();
            return;
        }

        Console.WriteLine("\n📂 Загружаем полный дамп...");
        var json = await File.ReadAllTextAsync(dumpPath);
        var fullData = JsonSerializer.Deserialize<FullGameData>(json);

        Console.WriteLine("\nЧто загрузить в память?");
        Console.WriteLine("1️⃣  Только лут (рейды)");
        Console.WriteLine("2️⃣  Лут + тактики (спеллы)");
        Console.WriteLine("3️⃣  Всё");

        var choice = Console.ReadLine();

        var gameData = new GameDataService();
        await gameData.LoadFromFullDataAsync(fullData, choice);

        Console.WriteLine("\n✅ Данные загружены в память!");
        WaitForExit();
    }
}

public class Config
{
    public string Host { get; set; }
    public int Port { get; set; }
    public string Database { get; set; }
    public string User { get; set; }
    public string Password { get; set; }
    public string WowPath { get; set; }
}