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
    private MpqArchive _archive;
    private DbcParser _dbcParser;

    public DbcParser DbcParser => _dbcParser ??= new DbcParser(this);

    public MpqExtractor(string wowPath)
    {
        _wowPath = wowPath;
        InitializeArchive();
    }

    private void InitializeArchive()
    {
        Console.WriteLine("📦 Загрузка MPQ архивов...");
        
        // Все возможные MPQ файлы WoW 3.3.5 в порядке приоритета
        var mpqFiles = new[]
        {
            Path.Combine(_wowPath, "Data", "lichking.MPQ"),
            Path.Combine(_wowPath, "Data", "patch-3.MPQ"),
            Path.Combine(_wowPath, "Data", "patch-2.MPQ"),
            Path.Combine(_wowPath, "Data", "expansion.MPQ"),
            Path.Combine(_wowPath, "Data", "common.MPQ"),
            Path.Combine(_wowPath, "Data", "common-2.MPQ"),
            Path.Combine(_wowPath, "Data", "locale-ruRU.MPQ"), // Для русских текстов
            Path.Combine(_wowPath, "Data", "patch-ruRU.MPQ")
        };

        foreach (var mpqPath in mpqFiles)
        {
            if (!File.Exists(mpqPath))
            {
                Console.WriteLine($"   ⚠️ Файл не найден: {Path.GetFileName(mpqPath)}");
                continue;
            }

            try
            {
                using var fs = File.OpenRead(mpqPath);
                _archive = MpqArchive.Open(fs);
                Console.WriteLine($"   ✅ Загружен: {Path.GetFileName(mpqPath)}");
                return; // Нашли рабочий архив - выходим
            }
            catch (Exception ex)
            {
                Console.WriteLine($"   ❌ Ошибка загрузки {Path.GetFileName(mpqPath)}: {ex.Message}");
            }
        }

        Console.WriteLine("   ⚠️ Не удалось загрузить MPQ архивы. Проверь путь к WoW.");
    }

    // Извлечение файла из MPQ
    public byte[] ExtractFile(string fileName)
    {
        if (_archive == null)
            return null;

        // Проверяем кэш
        if (_fileCache.TryGetValue(fileName, out var cached))
            return cached;

        try
        {
            // War3Net использует MpqArchive.OpenFile
            if (!_archive.FileExists(fileName))
                return null;

            using var mpqStream = _archive.OpenFile(fileName);
            using var ms = new MemoryStream();
            mpqStream.CopyTo(ms);
            var data = ms.ToArray();
            _fileCache[fileName] = data;
            return data;
        }
        catch
        {
            return null;
        }
    }

    // Конвертация BLP в PNG
    public async Task ConvertBlpToPng(string blpPath, string outputPath)
    {
        try
        {
            var data = ExtractFile(blpPath);
            if (data == null) return;

            using var ms = new MemoryStream(data);
            
            // BLPSharp конвертирует BLP в массив пикселей
            var blp = new BLPFile(ms);
            var pixels = blp.GetPixels(0, out var width, out var height);

            // Создаем Bitmap из пикселей
            using var bmp = new Bitmap(width, height, PixelFormat.Format32bppArgb);
            var rect = new Rectangle(0, 0, width, height);
            var bmpData = bmp.LockBits(rect, ImageLockMode.WriteOnly, PixelFormat.Format32bppArgb);
            
            System.Runtime.InteropServices.Marshal.Copy(pixels, 0, bmpData.Scan0, pixels.Length);
            bmp.UnlockBits(bmpData);

            // Создаем папку если нужно
            var dir = Path.GetDirectoryName(outputPath);
            if (!string.IsNullOrEmpty(dir))
                Directory.CreateDirectory(dir);

            // Сохраняем как PNG
            bmp.Save(outputPath, ImageFormat.Png);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"   ⚠️ Ошибка конвертации {blpPath}: {ex.Message}");
        }
    }

    // Извлечение иконок для списка предметов
    public async Task<int> ExtractItemIcons(List<int> itemIds, Dictionary<int, int> itemDisplayMap)
    {
        Console.WriteLine("\n🖼️ Извлечение иконок предметов...");
        
        // Получаем карту displayId -> iconName из DBC
        var iconMap = DbcParser.GetItemIconMap();
        Console.WriteLine($"   📋 Загружено {iconMap.Count} записей из ItemDisplayInfo.dbc");

        int extracted = 0;
        int total = itemIds.Count;

        for (int i = 0; i < total; i++)
        {
            var itemId = itemIds[i];
            
            if (!itemDisplayMap.TryGetValue(itemId, out var displayId))
                continue;

            if (!iconMap.TryGetValue(displayId, out var iconName))
                continue;

            // Путь к иконке в MPQ
            var blpPath = $"Interface/Icons/{iconName}.blp";
            var outputPath = Path.Combine("Output", "icons", "items", $"{itemId}.png");

            if (File.Exists(outputPath))
                continue;

            await ConvertBlpToPng(blpPath, outputPath);
            extracted++;

            if (extracted % 50 == 0)
                Console.Write($"\r   Прогресс: {extracted}/{total} иконок");
        }

        Console.WriteLine($"\n   ✅ Извлечено иконок: {extracted}");
        return extracted;
    }

    // Сохранение DBC файлов в CSV для просмотра
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

        Directory.CreateDirectory("Output/dbc");

        foreach (var dbc in dbcFiles)
        {
            var data = ExtractFile($"DBFilesClient\\{dbc}");
            if (data == null)
            {
                Console.WriteLine($"   ❌ {dbc} не найден");
                continue;
            }

            var outputPath = Path.Combine("Output", "dbc", Path.ChangeExtension(dbc, "csv"));
            
            using var ms = new MemoryStream(data);
            using var reader = new BinaryReader(ms);
            using var writer = new StreamWriter(outputPath);

            // Читаем заголовок
            var magic = reader.ReadUInt32();
            var recordCount = reader.ReadInt32();
            var fieldCount = reader.ReadInt32();
            var recordSize = reader.ReadInt32();
            var stringBlockSize = reader.ReadInt32();

            writer.WriteLine($"# {dbc} - {recordCount} записей, {fieldCount} полей");
            
            // Создаем заголовки колонок
            var headers = new List<string>();
            for (int i = 0; i < fieldCount; i++)
                headers.Add($"col{i}");
            writer.WriteLine(string.Join(",", headers));

            // Читаем строковый блок
            var stringBlock = reader.ReadBytes(stringBlockSize);

            // Читаем записи
            for (int i = 0; i < recordCount; i++)
            {
                var values = new List<string>();
                for (int f = 0; f < fieldCount; f++)
                {
                    values.Add(reader.ReadUInt32().ToString());
                }
                writer.WriteLine(string.Join(",", values));
            }

            Console.WriteLine($"   ✅ {dbc} -> CSV");
        }
    }

    public void Dispose()
    {
        _archive?.Dispose();
    }
}