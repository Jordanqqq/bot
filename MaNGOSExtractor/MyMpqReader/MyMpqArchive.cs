using System;
using System.Collections.Generic;
using System.IO;
using War3Net.IO.Mpq;

namespace MaNGOSExtractor.MyMpqReader
{
    public class MyMpqArchive : IDisposable
    {
        private readonly MpqArchive _archive;
        private readonly string _archivePath;
        private readonly Dictionary<string, byte[]> _fileCache = new();
        private bool _isDisposed;

        public string ArchiveName => Path.GetFileName(_archivePath);

        public MyMpqArchive(string filePath)
        {
            if (!File.Exists(filePath))
                throw new FileNotFoundException($"MPQ файл не найден: {filePath}");

            _archivePath = filePath;

            try
            {
                var fs = File.OpenRead(filePath);
                _archive = MpqArchive.Open(fs);
                Console.WriteLine($"      ✅ Загружен архив: {Path.GetFileName(filePath)}");
            }
            catch (Exception ex)
            {
                throw new Exception($"Ошибка загрузки {filePath}: {ex.Message}");
            }
        }

        public bool FileExists(string fileName)
        {
            try
            {
                return _archive.FileExists(fileName);
            }
            catch
            {
                return false;
            }
        }

        public byte[] ExtractFile(string fileName)
        {
            // Проверяем кэш
            if (_fileCache.TryGetValue(fileName, out var cached))
                return cached;

            try
            {
                if (_archive.FileExists(fileName))
                {
                    Console.WriteLine($"            Чтение файла из {ArchiveName}...");

                    // Открываем файл
                    using (var stream = _archive.OpenFile(fileName))
                    {
                        // War3Net может не поддерживать Length, поэтому читаем через буфер
                        using (var ms = new MemoryStream())
                        {
                            byte[] buffer = new byte[8192];
                            int bytesRead;
                            long totalBytes = 0;

                            while ((bytesRead = stream.Read(buffer, 0, buffer.Length)) > 0)
                            {
                                ms.Write(buffer, 0, bytesRead);
                                totalBytes += bytesRead;
                            }

                            var data = ms.ToArray();
                            Console.WriteLine($"            ✅ Успешно прочитано {totalBytes} байт");

                            _fileCache[fileName] = data;
                            return data;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"            ❌ Ошибка: {ex.Message}");
            }

            return null;
        }
        public List<string> ListAllFiles()
        {
            var result = new List<string>();
            
            string[] knownFiles = {
                "DBFilesClient/Spell.dbc",
                "DBFilesClient/ItemDisplayInfo.dbc",
                "DBFilesClient/SpellIcon.dbc",
                "DBFilesClient/CreatureDisplayInfo.dbc",
                "DBFilesClient/Map.dbc",
                "DBFilesClient/AreaTable.dbc"
            };

            foreach (var file in knownFiles)
            {
                string testPath = file.Replace('/', '\\');
                if (FileExists(testPath))
                {
                    result.Add(testPath);
                    Console.WriteLine($"      - {testPath} (найден в {ArchiveName})");
                }
            }
            
            return result;
        }

        public void Dispose()
        {
            if (!_isDisposed)
            {
                _archive?.Dispose();
                _isDisposed = true;
            }
        }
    }
}