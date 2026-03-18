using War3Net.IO.Mpq;
using BLPSharp;
using System.Drawing;
using System.Drawing.Imaging;
using MaNGOSExtractor.MPQ.Models;

namespace MaNGOSExtractor.MPQ;

public class MpqExtractor : IDisposable
{
    private readonly string _wowPath;
    private readonly Dictionary<string, byte[]> _fileCache = new();
    private readonly List<MpqArchive> _archives = new();
    private DbcParser _dbcParser;
    private readonly bool _isRussianClient;

    public DbcParser DbcParser => _dbcParser ??= new DbcParser(this);

    public MpqExtractor(string wowPath)
    {
        _wowPath = wowPath;

        // Проверяем, есть ли папка ruRU (русский клиент)
        string dataPath = Path.Combine(wowPath, "Data");
        _isRussianClient = Directory.Exists(Path.Combine(dataPath, "ruRU"));

        if (_isRussianClient)
        {
            Console.WriteLine("   🌍 Обнаружен русский клиент WoW");
        }

        InitializeArchive();
    }

    private void InitializeArchive()
    {
        Console.WriteLine("📦 Загрузка MPQ архивов...");

        // Путь к папке с игрой
        string gamePath = _wowPath;
        Console.WriteLine($"   + Базовый путь из конфига: {gamePath}");

        // Проверяем, есть ли подпапка Data
        string dataPath = Path.Combine(gamePath, "Data");
        if (Directory.Exists(dataPath))
        {
            gamePath = dataPath;
            Console.WriteLine($"   + Найдена папка Data, используем: {gamePath}");
        }
        else
        {
            Console.WriteLine($"   - Папка Data не найдена по пути: {dataPath}");
        }

        // Проверяем существование папки
        if (!Directory.Exists(gamePath))
        {
            Console.WriteLine($"   ! Папка не существует: {gamePath}");
            Console.WriteLine("   ! Не удалось загрузить MPQ архивы. Проверь путь к WoW.");
            return;
        }

        // Добавляем локальные пути для русского клиента
        if (_isRussianClient)
        {
            string ruRUPath = Path.Combine(gamePath, "ruRU");
            if (Directory.Exists(ruRUPath))
            {
                Console.WriteLine($"   + Найдена папка локализации: {ruRUPath}");
            }
        }

        // Все возможные MPQ файлы WoW 3.3.5
        var mpqFiles = GetMpqFilePaths(gamePath);

        // Загружаем ВСЕ доступные MPQ архивы
        foreach (var mpqPath in mpqFiles)
        {
            Console.WriteLine($"   > Проверяю: {mpqPath}");

            if (!File.Exists(mpqPath))
            {
                Console.WriteLine($"      - Файл не найден: {Path.GetFileName(mpqPath)}");
                continue;
            }

            try
            {
                using var fs = File.OpenRead(mpqPath);
                var archive = MpqArchive.Open(fs);
                _archives.Add(archive);
                Console.WriteLine($"      + Загружен: {Path.GetFileName(mpqPath)}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"      ! Ошибка загрузки {Path.GetFileName(mpqPath)}: {ex.Message}");
            }
        }

        Console.WriteLine($"   + Всего загружено архивов: {_archives.Count}");
        if (_archives.Count == 0)
        {
            Console.WriteLine("   ! Не удалось загрузить MPQ архивы. Проверь путь к WoW.");
        }
    }

    private List<string> GetMpqFilePaths(string gamePath)
    {
        var mpqFiles = new List<string>
        {
            Path.Combine(gamePath, "lichking.MPQ"),
            Path.Combine(gamePath, "common.MPQ"),
            Path.Combine(gamePath, "common-2.MPQ"),
            Path.Combine(gamePath, "expansion.MPQ"),
            Path.Combine(gamePath, "patch-3.MPQ"),
            Path.Combine(gamePath, "patch-2.MPQ")
        };

        // Добавляем локальные файлы для русского клиента
        if (_isRussianClient)
        {
            string ruRUPath = Path.Combine(gamePath, "ruRU");
            if (Directory.Exists(ruRUPath))
            {
                mpqFiles.Add(Path.Combine(ruRUPath, "locale-ruRU.MPQ"));
                mpqFiles.Add(Path.Combine(ruRUPath, "patch-ruRU.MPQ"));
                mpqFiles.Add(Path.Combine(ruRUPath, "expansion-locale-ruRU.MPQ"));
                mpqFiles.Add(Path.Combine(ruRUPath, "lichking-locale-ruRU.MPQ"));
            }
        }

        return mpqFiles;
    }

    public byte[] ExtractFile(string fileName)
    {
        if (_archives.Count == 0)
            return null;

        if (_fileCache.TryGetValue(fileName, out var cached))
            return cached;

        var searchPaths = new List<string> { fileName };

        if (_isRussianClient)
        {
            searchPaths.Add($"ruRU\\{fileName}");
            searchPaths.Add(fileName.Replace("DBFilesClient\\", "DBFilesClient\\ruRU\\"));
        }

        foreach (var archive in _archives)
        {
            foreach (var path in searchPaths)
            {
                try
                {
                    if (archive.FileExists(path))
                    {
                        using var mpqStream = archive.OpenFile(path);
                        using var ms = new MemoryStream();
                        mpqStream.CopyTo(ms);
                        var data = ms.ToArray();
                        _fileCache[fileName] = data;
                        return data;
                    }
                }
                catch
                {
                    // Пробуем следующий путь или архив
                }
            }
        }

        return null;
    }

    public void FindDbcFiles()
    {
        Console.WriteLine("\n🔍 Поиск DBC файлов в архивах...");

        var dbcFiles = new[]
        {
            "ItemDisplayInfo.dbc",
            "Spell.dbc",
            "SpellIcon.dbc",
            "CreatureDisplayInfo.dbc",
            "Map.dbc",
            "AreaTable.dbc"
        };

        for (int i = 0; i < _archives.Count; i++)
        {
            var archive = _archives[i];
            Console.WriteLine($"\n   Архив {i + 1}:");

            foreach (var dbc in dbcFiles)
            {
                var paths = new[]
                {
                    $"DBFilesClient\\{dbc}",
                    $"DBFilesClient\\ruRU\\{dbc}",
                    $"ruRU\\DBFilesClient\\{dbc}",
                    $"{dbc}"
                };

                foreach (var path in paths)
                {
                    try
                    {
                        if (archive.FileExists(path))
                        {
                            Console.WriteLine($"      + Найден: {path}");
                            break;
                        }
                    }
                    catch
                    {
                        // Игнорируем ошибки
                    }
                }
            }
        }
    }

    public void ExportDbcToCsv()
    {
        Console.WriteLine("\n📋 Экспорт DBC файлов в CSV...");

        var dbcFiles = new[]
        {
        "ItemDisplayInfo.dbc",
        "Spell.dbc",
        "SpellIcon.dbc",
        "CreatureDisplayInfo.dbc",
        "Map.dbc",
        "AreaTable.dbc"
    };

        string outputFolder = Path.Combine(Directory.GetCurrentDirectory(), "Output", "dbc");
        Directory.CreateDirectory(outputFolder);
        Console.WriteLine($"   + Папка для сохранения: {outputFolder}");

        int totalSaved = 0;

        for (int archiveIndex = 0; archiveIndex < _archives.Count; archiveIndex++)
        {
            var archive = _archives[archiveIndex];

            foreach (var dbc in dbcFiles)
            {
                string path = $"DBFilesClient\\{dbc}";
                string outputPath = Path.Combine(outputFolder, dbc);

                try
                {
                    if (archive.FileExists(path))
                    {
                        Console.WriteLine($"   + Найден в архиве {archiveIndex + 1}: {path}");

                        // Используем MemoryStream для копирования
                        using (var mpqStream = archive.OpenFile(path))
                        using (var ms = new MemoryStream())
                        {
                            // Копируем в MemoryStream
                            byte[] buffer = new byte[4096];
                            int count;
                            while ((count = mpqStream.Read(buffer, 0, buffer.Length)) > 0)
                            {
                                ms.Write(buffer, 0, count);
                            }

                            byte[] data = ms.ToArray();
                            Console.WriteLine($"      + Прочитано {data.Length} байт");

                            if (data.Length > 0)
                            {
                                File.WriteAllBytes(outputPath, data);
                                Console.WriteLine($"      ✅ Сохранен {dbc} ({data.Length} байт)");
                                totalSaved++;
                            }
                        }
                    }
                }
                catch (NotSupportedException)
                {
                    Console.WriteLine($"      ! Метод не поддерживается для {dbc}, пробуем альтернативный способ...");

                    // Альтернативный способ - читаем через StreamReader
                    try
                    {
                        using (var mpqStream = archive.OpenFile(path))
                        using (var fileStream = File.Create(outputPath))
                        {
                            var task = mpqStream.CopyToAsync(fileStream);
                            task.Wait();
                        }

                        long fileSize = new FileInfo(outputPath).Length;
                        if (fileSize > 0)
                        {
                            Console.WriteLine($"      ✅ Сохранен {dbc} через CopyToAsync ({fileSize} байт)");
                            totalSaved++;
                        }
                    }
                    catch (Exception ex2)
                    {
                        Console.WriteLine($"      ! Альтернативный способ тоже не сработал: {ex2.Message}");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"      ! Ошибка с {dbc}: {ex.Message}");
                }
            }
        }

        Console.WriteLine($"\n   + Всего сохранено файлов: {totalSaved}");
    }

    private void CreateSimpleCsv(byte[] data, string csvPath, string dbcName)
    {
        try
        {
            using (var ms = new MemoryStream(data))
            using (var reader = new BinaryReader(ms))
            using (var writer = new StreamWriter(csvPath))
            {
                // Читаем заголовок DBC
                uint magic = reader.ReadUInt32();
                int recordCount = reader.ReadInt32();
                int fieldCount = reader.ReadInt32();
                int recordSize = reader.ReadInt32();
                int stringBlockSize = reader.ReadInt32();

                writer.WriteLine($"# {dbcName} - {recordCount} записей, {fieldCount} полей");

                // Заголовки колонок
                for (int i = 0; i < fieldCount; i++)
                {
                    writer.Write($"col{i}");
                    if (i < fieldCount - 1) writer.Write(",");
                }
                writer.WriteLine();

                // Пропускаем строковый блок
                reader.ReadBytes(stringBlockSize);

                // Читаем записи
                for (int i = 0; i < recordCount; i++)
                {
                    for (int f = 0; f < fieldCount; f++)
                    {
                        writer.Write(reader.ReadUInt32().ToString());
                        if (f < fieldCount - 1) writer.Write(",");
                    }
                    writer.WriteLine();
                }
            }
            Console.WriteLine($"      + Создан CSV: {Path.GetFileName(csvPath)}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"      ! Ошибка создания CSV: {ex.Message}");
        }
    }

    public async Task ConvertBlpToPng(string blpPath, string outputPath)
    {
        try
        {
            var data = ExtractFile(blpPath);
            if (data == null) return;

            using var ms = new MemoryStream(data);

            var blp = new BLPFile(ms);
            var pixels = blp.GetPixels(0, out var width, out var height);

            using var bmp = new Bitmap(width, height, PixelFormat.Format32bppArgb);
            var rect = new Rectangle(0, 0, width, height);
            var bmpData = bmp.LockBits(rect, ImageLockMode.WriteOnly, PixelFormat.Format32bppArgb);

            System.Runtime.InteropServices.Marshal.Copy(pixels, 0, bmpData.Scan0, pixels.Length);
            bmp.UnlockBits(bmpData);

            var dir = Path.GetDirectoryName(outputPath);
            if (!string.IsNullOrEmpty(dir))
                Directory.CreateDirectory(dir);

            bmp.Save(outputPath, ImageFormat.Png);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"   ! Ошибка конвертации {blpPath}: {ex.Message}");
        }
    }

    public async Task<int> ExtractItemIcons(List<int> itemIds, Dictionary<int, int> itemDisplayMap)
    {
        Console.WriteLine("\n🖼️ Извлечение иконок предметов...");

        var iconMap = DbcParser.GetItemIconMap();
        Console.WriteLine($"   + Загружено {iconMap.Count} записей из ItemDisplayInfo.dbc");

        int extracted = 0;
        int total = itemIds.Count;

        for (int i = 0; i < total; i++)
        {
            var itemId = itemIds[i];

            if (!itemDisplayMap.TryGetValue(itemId, out var displayId))
                continue;

            if (!iconMap.TryGetValue(displayId, out var iconName))
                continue;

            var blpPath = $"Interface\\Icons\\{iconName}.blp";
            var outputPath = Path.Combine("Output", "icons", "items", $"{itemId}.png");

            if (File.Exists(outputPath))
                continue;

            await ConvertBlpToPng(blpPath, outputPath);
            extracted++;

            if (extracted % 50 == 0)
                Console.Write($"\r   Прогресс: {extracted}/{total} иконок");
        }

        Console.WriteLine($"\n   + Извлечено иконок: {extracted}");
        return extracted;
    }

    public void Dispose()
    {
        foreach (var archive in _archives)
            archive.Dispose();
        _archives.Clear();
    }
}