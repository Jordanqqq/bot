using System;
using System.IO;
using System.Linq;
using MaNGOSExtractor.MpqCore;

namespace MaNGOSExtractor.MyMpqReader
{
    public static class MpqTest
    {
        public static void RunTest(string mpqFilePath)
        {
            Console.WriteLine("\n🔬 ТЕСТИРОВАНИЕ MPQ CORE");
            Console.WriteLine("==========================================");

            if (!File.Exists(mpqFilePath))
            {
                Console.WriteLine($"❌ Файл не найден: {mpqFilePath}");
                return;
            }

            try
            {
                // 1. Загружаем архив
                using var reader = new MpqArchiveReader(mpqFilePath);

                if (!reader.IsValid)
                {
                    Console.WriteLine($"❌ Архив невалиден: {reader.ArchiveName}");
                    return;
                }

                Console.WriteLine($"\n📦 Архив: {reader.ArchiveName}");

                // 2. Получаем список DBC файлов через новый метод
                var dbcFiles = reader.ListDbcFiles();

                // 3. Пробуем извлечь каждый файл
                int successCount = 0;
                foreach (var file in dbcFiles)
                {
                    Console.Write($"\n🔍 Извлекаю: {file}... ");

                    try
                    {
                        var data = reader.ExtractFile(file);

                        if (data != null && data.Length > 0)
                        {
                            Console.WriteLine($"✅ Успешно! ({data.Length} байт)");

                            // Сохраняем для проверки
                            string outputDir = Path.Combine(Directory.GetCurrentDirectory(), "Output", "dbc_test");
                            Directory.CreateDirectory(outputDir);

                            string outputPath = Path.Combine(outputDir, Path.GetFileName(file));
                            File.WriteAllBytes(outputPath, data);
                            Console.WriteLine($"   💾 Сохранено: {outputPath}");

                            successCount++;
                        }
                        else
                        {
                            Console.WriteLine("❌ Данные пусты");
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"❌ Ошибка: {ex.Message}");
                    }
                }

                // 4. Показываем статистику по блокам
                Console.WriteLine("\n📊 Статистика блоков:");
                var blocks = reader.GetAllBlocks();
                int compressed = 0;
                int singleUnit = 0;
                int exists = 0;

                foreach (var block in blocks.Take(10))
                {
                    bool isCompressed = (block.Flags & (uint)MpqFileFlags.Compressed) != 0;
                    bool isSingleUnit = (block.Flags & (uint)MpqFileFlags.SingleUnit) != 0;
                    bool isExists = (block.Flags & (uint)MpqFileFlags.Exists) != 0;

                    if (isCompressed) compressed++;
                    if (isSingleUnit) singleUnit++;
                    if (isExists) exists++;

                    Console.WriteLine($"   Блок: Offset=0x{block.Offset:X8}, " +
                                    $"Size={block.CompressedSize,8}, " +
                                    $"Flags=0x{block.Flags:X8}");
                }

                Console.WriteLine($"\n   Всего блоков: {blocks.Count}");
                Console.WriteLine($"   Успешно извлечено: {successCount} файлов");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"\n❌ Ошибка теста: {ex.Message}");
                Console.WriteLine($"   Стек вызовов: {ex.StackTrace}");
            }

            Console.WriteLine("\n==========================================\n");
        }

        public static void TestSpecificFile(string mpqFilePath, string fileName)
        {
            Console.WriteLine($"\n🔍 ТЕСТ КОНКРЕТНОГО ФАЙЛА: {fileName}");
            Console.WriteLine("==========================================");

            try
            {
                using var reader = new MpqArchiveReader(mpqFilePath);

                if (!reader.IsValid)
                {
                    Console.WriteLine($"❌ Архив невалиден: {reader.ArchiveName}");
                    return;
                }

                // Ищем файл по имени
                var data = reader.ExtractFile(fileName);
                if (data != null && data.Length > 0)
                {
                    Console.WriteLine($"   ✅ Извлечено: {data.Length} байт");

                    string outputPath = Path.Combine("Output", "dbc_test", Path.GetFileName(fileName));
                    Directory.CreateDirectory(Path.GetDirectoryName(outputPath));
                    File.WriteAllBytes(outputPath, data);
                    Console.WriteLine($"   💾 Сохранено: {outputPath}");
                }
                else
                {
                    Console.WriteLine($"   ❌ Файл не найден: {fileName}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"   ❌ Ошибка: {ex.Message}");
            }
        }
    }
}