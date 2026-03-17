using System.Text.Json;
using MaNGOSExtractor.MPQ;
using MaNGOSExtractor.Services;
using MaNGOSExtractor.Extractors;
using MaNGOSExtractor.Models;

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
                break;
        }
    }

    static async Task FullExtractAsync(Config config)
    {
        using var db = new DatabaseService(config);
        if (!await db.TestConnectionAsync())
        {
            Console.WriteLine("❌ Не удалось подключиться к MySQL");
            return;
        }

        using var mpq = new MpqExtractor(config.WowPath);

        var extractor = new FullExtractor(db, mpq, "Output");
        var data = await extractor.ExtractAllAsync();
        await extractor.SaveToJson(data);

        Console.WriteLine("\n✅ Полное извлечение завершено!");
    }

    static async Task MySqlOnlyAsync(Config config)
    {
        using var db = new DatabaseService(config);
        if (!await db.TestConnectionAsync())
        {
            Console.WriteLine("❌ Не удалось подключиться к MySQL");
            return;
        }

        // Простая выгрузка только MySQL
        var extractor = new FullExtractor(db, null, "Output");
        var data = new FullGameData();
        await extractor.ExtractMySqlData(data);

        var options = new JsonSerializerOptions { WriteIndented = true };
        var json = JsonSerializer.Serialize(data, options);
        await File.WriteAllTextAsync("Output/mysql_dump.json", json);

        Console.WriteLine("\n✅ MySQL дамп сохранён!");
    }

    static async Task MpqOnlyAsync(Config config)
    {
        using var mpq = new MpqExtractor(config.WowPath);

        // Просто экспортируем DBC и иконки
        mpq.ExportDbcToCsv();

        Console.WriteLine("\n✅ MPQ данные извлечены!");
    }

    static async Task LoadPartialFromDumpAsync()
    {
        var dumpPath = Path.Combine("Output", "full_dump.json");
        if (!File.Exists(dumpPath))
        {
            Console.WriteLine("❌ Сначала сделай полное извлечение (режим 1)");
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
    }

    static Config LoadConfig()
    {
        var path = "Config.json";
        if (!File.Exists(path))
        {
            var defaultConfig = new Config
            {
                Host = "localhost",
                Port = 3306,
                Database = "mangos",
                User = "mangos",
                Password = "mangos",
                WowPath = "C:\\Program Files\\World of Warcraft 3.3.5"
            };
            var json = JsonSerializer.Serialize(defaultConfig,
                new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(path, json);
            Console.WriteLine("⚙️ Создан файл Config.json. Заполни данные и запусти снова.");
            Environment.Exit(0);
        }

        var configJson = File.ReadAllText(path);
        return JsonSerializer.Deserialize<Config>(configJson);
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