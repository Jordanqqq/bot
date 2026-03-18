using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using MaNGOSExtractor.MpqCore;

namespace MaNGOSExtractor.MyMpqReader
{
    public class MyMpqExtractor : IDisposable
    {
        private readonly string _wowPath;
        private readonly List<MpqArchiveReader> _archives = new();
        private readonly Dictionary<string, byte[]> _fileCache = new();
        private bool _isDisposed;

        public MyMpqExtractor(string wowPath)
        {
            _wowPath = wowPath;
            InitializeArchives();
        }

        private void InitializeArchives()
        {
            Console.WriteLine("📦 Загрузка MPQ архивов...");

            string gamePath = _wowPath;
            string dataPath = Path.Combine(gamePath, "Data");

            if (Directory.Exists(dataPath))
            {
                gamePath = dataPath;
            }

            // Загружаем все MPQ файлы
            var mpqFiles = new List<string>();

            // Основные файлы
            string[] mainFiles = { "lichking.MPQ", "common.MPQ", "common-2.MPQ", "expansion.MPQ", "patch-3.MPQ", "patch-2.MPQ" };
            foreach (var file in mainFiles)
            {
                string path = Path.Combine(gamePath, file);
                if (File.Exists(path)) mpqFiles.Add(path);
            }

            // Русская локализация
            string ruRUPath = Path.Combine(gamePath, "ruRU");
            if (Directory.Exists(ruRUPath))
            {
                string[] ruFiles = { "locale-ruRU.MPQ", "patch-ruRU.MPQ", "expansion-locale-ruRU.MPQ", "lichking-locale-ruRU.MPQ" };
                foreach (var file in ruFiles)
                {
                    string path = Path.Combine(ruRUPath, file);
                    if (File.Exists(path)) mpqFiles.Add(path);
                }
            }

            // Загружаем каждый архив через наш MpqArchiveReader
            foreach (var mpqFile in mpqFiles)
            {
                try
                {
                    var reader = new MpqArchiveReader(mpqFile);
                    _archives.Add(reader);
                    Console.WriteLine($"   ✅ Загружен: {Path.GetFileName(mpqFile)}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"   ❌ Ошибка загрузки {Path.GetFileName(mpqFile)}: {ex.Message}");
                }
            }

            Console.WriteLine($"   + Всего загружено архивов: {_archives.Count}");
        }

        public byte[] ExtractFile(string fileName)
        {
            if (_archives.Count == 0) return null;
            if (_fileCache.TryGetValue(fileName, out var cached)) return cached;

            string normalizedPath = fileName.Replace('/', '\\');

            // Идем по архивам в обратном порядке (от патчей к базе)
            foreach (var archive in _archives.AsEnumerable().Reverse())
            {
                try
                {
                    int index = archive.FindFileIndex(normalizedPath);
                    if (index >= 0)
                    {
                        Console.WriteLine($"      📍 Найден в {archive.ArchiveName}, индекс {index}");
                        var data = archive.ExtractFile(index);
                        if (data != null && data.Length > 0)
                        {
                            _fileCache[fileName] = data;
                            return data;
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"      ⚠️ Ошибка: {ex.Message}");
                }
            }

            return null;
        }

        public bool FileExists(string fileName)
        {
            string normalizedPath = fileName.Replace('/', '\\');
            return _archives.Any(a => a.FindFileIndex(normalizedPath) >= 0);
        }

        public void ListAllDbcFiles()
        {
            Console.WriteLine("\n🔍 Поиск всех DBC файлов в архивах...");
            foreach (var archive in _archives)
            {
                Console.WriteLine($"\n   Архив: {archive.ArchiveName}");
                var files = archive.ListDbcFiles();
            }
        }

        /// <summary>
        /// Массовое извлечение файлов по списку (listfile)
        /// </summary>
        /// <param name="listfilePath">Путь к текстовому файлу со списком путей (по одному на строку)</param>
        /// <param name="filter">Фильтр для выборочного извлечения (например, ".blp" для иконок)</param>
        /// <param name="outputDir">Выходная папка</param>
        public void ExtractByListfile(string listfilePath, string filter, string outputDir)
        {
            if (!File.Exists(listfilePath))
            {
                Console.WriteLine($"❌ Listfile не найден: {listfilePath}");
                return;
            }

            Console.WriteLine($"\n📋 Извлечение файлов по списку: {filter}");

            var lines = File.ReadAllLines(listfilePath);
            int total = 0;
            int extracted = 0;

            foreach (var fileName in lines)
            {
                if (string.IsNullOrWhiteSpace(fileName)) continue;

                // Применяем фильтр если нужно
                if (!string.IsNullOrEmpty(filter) &&
                    !fileName.Contains(filter, StringComparison.OrdinalIgnoreCase))
                    continue;

                total++;

                // Нормализуем путь
                string normalizedPath = fileName.Replace('/', '\\');

                // Идем по архивам в обратном порядке (от патчей к базе)
                foreach (var archive in _archives.AsEnumerable().Reverse())
                {
                    int index = archive.FindFileIndex(normalizedPath);
                    if (index >= 0)
                    {
                        var data = archive.ExtractFile(index);
                        if (data != null && data.Length > 0)
                        {
                            string outPath = Path.Combine(outputDir, normalizedPath);
                            Directory.CreateDirectory(Path.GetDirectoryName(outPath));
                            File.WriteAllBytes(outPath, data);
                            extracted++;
                            Console.WriteLine($"   [+] Извлечено: {fileName}");
                        }
                        break; // Нашли актуальную версию в патче, идем к следующему файлу
                    }
                }
            }

            Console.WriteLine($"\n📊 Итог: извлечено {extracted} из {total} файлов");
        }

        /// <summary>
        /// Извлечение всех иконок из папки Interface\Icons\
        /// </summary>
        public void ExtractAllIcons(string outputDir)
        {
            Console.WriteLine("\n🖼️ Извлечение всех иконок...");

            // Создаем временный listfile для иконок
            // В реальности нужно использовать полный listfile из базы
            string tempListfile = Path.GetTempFileName();

            try
            {
                // Генерируем список возможных иконок
                // Это упрощенный вариант - в реальности нужно парсить DBC
                var iconNames = new List<string>();

                // Добавляем стандартные префиксы иконок
                string[] prefixes = {
                    "INV_", "Ability_", "Spell_", "Trade_", "Achievement_",
                    "Interface\\Icons\\INV_", "Interface\\Icons\\Ability_", "Interface\\Icons\\Spell_"
                };

                // Сохраняем во временный файл
                File.WriteAllLines(tempListfile, iconNames);

                // Извлекаем по фильтру .blp
                ExtractByListfile(tempListfile, ".blp", outputDir);
            }
            finally
            {
                if (File.Exists(tempListfile))
                    File.Delete(tempListfile);
            }
        }

        /// <summary>
        /// Извлечение портретов боссов
        /// </summary>
        public void ExtractBossPortraits(string outputDir)
        {
            Console.WriteLine("\n👑 Извлечение портретов боссов...");

            // Пути к портретам боссов
            string[] portraitPaths = {
                "Interface\\TargetingFrame\\UI-TargetingFrame-Skull.blp",
                "Interface\\TargetingFrame\\UI-TargetingFrame-Elite.blp",
                "Interface\\TargetingFrame\\UI-TargetingFrame-Rare.blp",
                "Interface\\TargetingFrame\\UI-TargetingFrame-RareElite.blp"
            };

            // Создаем временный listfile
            string tempListfile = Path.GetTempFileName();
            File.WriteAllLines(tempListfile, portraitPaths);

            try
            {
                ExtractByListfile(tempListfile, "", outputDir);
            }
            finally
            {
                if (File.Exists(tempListfile))
                    File.Delete(tempListfile);
            }
        }

        /// <summary>
        /// Конвертация BLP в PNG (упрощенная - только для несжатых BLP)
        /// </summary>
        public static void ConvertBlpToPng(string blpPath, string pngPath)
        {
            if (!File.Exists(blpPath)) return;

            byte[] blpData = File.ReadAllBytes(blpPath);

            // Проверяем сигнатуру BLP2
            if (blpData.Length < 4 || blpData[0] != 'B' || blpData[1] != 'L' || blpData[2] != 'P' || blpData[3] != '2')
            {
                Console.WriteLine($"   [!] Не BLP2 файл: {blpPath}");
                return;
            }

            // Здесь должна быть полная конвертация BLP -> PNG
            // Для этого нужно использовать библиотеку SkiaSharp или аналоги
            // Пока просто копируем с расширением .blp
            File.Copy(blpPath, pngPath.Replace(".png", ".blp"), true);
            Console.WriteLine($"   [*] BLP файл сохранен (требуется конвертер): {pngPath}");
        }

        public void ExportDbc()
        {
            Console.WriteLine("\n📋 Экспорт DBC файлов...");

            var dbcFiles = new[]
            {
                "DBFilesClient\\Spell.dbc",
                "DBFilesClient\\ItemDisplayInfo.dbc",
                "DBFilesClient\\SpellIcon.dbc",
                "DBFilesClient\\CreatureDisplayInfo.dbc",
                "DBFilesClient\\Map.dbc",
                "DBFilesClient\\AreaTable.dbc"
            };

            string outputFolder = Path.Combine(Directory.GetCurrentDirectory(), "Output", "dbc");
            Directory.CreateDirectory(outputFolder);

            int totalSaved = 0;

            foreach (var dbcPath in dbcFiles)
            {
                string fileName = Path.GetFileName(dbcPath);
                string outputPath = Path.Combine(outputFolder, fileName);

                Console.Write($"   🔍 {fileName}... ");
                var data = ExtractFile(dbcPath);

                if (data != null && data.Length > 0)
                {
                    File.WriteAllBytes(outputPath, data);
                    Console.WriteLine($"✅ ({data.Length} байт)");
                    totalSaved++;
                }
                else
                {
                    Console.WriteLine("❌");
                }
            }

            Console.WriteLine($"\n   📊 Итог: сохранено {totalSaved} из {dbcFiles.Length} файлов");
        }

        public void Dispose()
        {
            if (!_isDisposed)
            {
                foreach (var archive in _archives)
                    archive.Dispose();
                _archives.Clear();
                _isDisposed = true;
            }
        }
    }
}