using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace MaNGOSExtractor.MpqCore
{
    public class MpqArchiveReader : IDisposable
    {
        private readonly FileStream _stream;
        private readonly BinaryReader _reader;
        private MpqHeader _header;
        private List<MpqBlock> _blocks;
        private List<MpqHash> _hashes;
        private long _headerOffset;
        private bool _isDisposed;

        public string ArchiveName { get; }
        public bool IsValid { get; private set; }

        public MpqArchiveReader(string filePath)
        {
            if (!File.Exists(filePath)) throw new FileNotFoundException(filePath);

            ArchiveName = Path.GetFileName(filePath);
            _stream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read);
            _reader = new BinaryReader(_stream);

            try
            {
                if (Initialize())
                {
                    IsValid = true;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"  [!] Ошибка в {ArchiveName}: {ex.Message}");
                IsValid = false;
            }
        }

        private bool Initialize()
        {
            // 1. Поиск сигнатуры MPQ\x1A (0x1A51504D)
            _headerOffset = -1;
            for (long i = 0; i < _stream.Length; i += 512)
            {
                _stream.Position = i;
                if (_reader.ReadUInt32() == 0x1A51504D)
                {
                    _headerOffset = i;
                    Console.WriteLine($"     [+] Найдена сигнатура MPQ по смещению 0x{_headerOffset:X8}");
                    break;
                }
            }

            if (_headerOffset == -1)
            {
                Console.WriteLine($"     [!] Сигнатура MPQ не найдена в файле");
                return false;
            }

            // 2. Чтение заголовка
            _stream.Position = _headerOffset;
            _header = new MpqHeader
            {
                Magic = _reader.ReadUInt32(),
                HeaderSize = _reader.ReadUInt32(),
                ArchiveSize = _reader.ReadUInt32(),
                Version = _reader.ReadUInt16(),
                BlockSize = _reader.ReadUInt16(),
                HashTableOffset = _reader.ReadUInt32(),
                BlockTableOffset = _reader.ReadUInt32(),
                HashTableCount = _reader.ReadUInt32(),
                BlockTableCount = _reader.ReadUInt32()
            };

            // 3. Проверка валидности
            long hashTableRealPos = _headerOffset + _header.HashTableOffset;
            long blockTableRealPos = _headerOffset + _header.BlockTableOffset;

            if (hashTableRealPos + (long)_header.HashTableCount * 16 > _stream.Length ||
                blockTableRealPos + (long)_header.BlockTableCount * 16 > _stream.Length)
            {
                Console.WriteLine($"     [!] Таблицы выходят за пределы файла");
                return false;
            }

            // 4. Проверочный ключ для хеш-таблицы
            uint testKey = MpqCrypto.HashString("(hash table)", MpqCrypto.HASH_TYPE_FILE_KEY);
            Console.WriteLine($"     [DEBUG] Ключ для хеш-таблицы: 0x{testKey:X8}");

            // 5. Чтение и дешифровка таблиц
            _hashes = ReadTable<MpqHash>(_header.HashTableOffset, _header.HashTableCount, "(hash table)");
            _blocks = ReadTable<MpqBlock>(_header.BlockTableOffset, _header.BlockTableCount, "(block table)");

            int nonEmptyHashes = 0;
            foreach (var h in _hashes)
            {
                if (h.BlockIndex < _blocks.Count && h.BlockIndex != 0xFFFFFFFF)
                    nonEmptyHashes++;
            }

            Console.WriteLine($"     [+] Хешей всего: {_hashes.Count}, непустых: {nonEmptyHashes}");
            Console.WriteLine($"     [+] Блоков всего: {_blocks.Count}");

            // Покажем первые несколько непустых хешей для диагностики
            int shown = 0;
            for (int i = 0; i < _hashes.Count && shown < 5; i++)
            {
                var h = _hashes[i];
                if (h.BlockIndex < _blocks.Count && h.BlockIndex != 0xFFFFFFFF)
                {
                    shown++;
                    Console.WriteLine($"        Хеш[{i}]: A=0x{h.NameHashA:X8}, B=0x{h.NameHashB:X8}, Index={h.BlockIndex}");
                }
            }

            return _hashes.Count > 0 && _blocks.Count > 0;
        }

        private List<T> ReadTable<T>(uint offset, uint count, string keyLabel) where T : struct
        {
            var result = new List<T>();
            if (count == 0) return result;

            long absolutePos = _headerOffset + offset;
            _stream.Position = absolutePos;

            int sizeOfStruct = 16;
            byte[] rawData = _reader.ReadBytes((int)count * sizeOfStruct);

            uint[] data = new uint[rawData.Length / 4];
            Buffer.BlockCopy(rawData, 0, data, 0, rawData.Length);

            uint key = MpqCrypto.HashString(keyLabel, MpqCrypto.HASH_TYPE_FILE_KEY);
            MpqCrypto.DecryptBlock(data, key);

            for (int i = 0; i < count; i++)
            {
                int baseIdx = i * 4;
                uint[] entry = new uint[4];
                entry[0] = data[baseIdx];
                entry[1] = data[baseIdx + 1];
                entry[2] = data[baseIdx + 2];
                entry[3] = data[baseIdx + 3];

                if (typeof(T) == typeof(MpqHash))
                {
                    result.Add((T)(object)new MpqHash
                    {
                        NameHashA = entry[0],
                        NameHashB = entry[1],
                        Locale = (ushort)(entry[2] & 0xFFFF),
                        Platform = (ushort)((entry[2] >> 16) & 0xFFFF),
                        BlockIndex = entry[3]
                    });
                }
                else
                {
                    result.Add((T)(object)new MpqBlock
                    {
                        Offset = entry[0],
                        CompressedSize = entry[1],
                        FileSize = entry[2],
                        Flags = entry[3]
                    });
                }
            }
            return result;
        }

        public int FindFileIndex(string fileName)
        {
            if (!IsValid || _hashes == null || _blocks == null) return -1;

            string name = fileName.Replace('/', '\\').ToUpperInvariant();
            uint hashIndex = MpqCrypto.HashString(name, MpqCrypto.HASH_TYPE_TABLE_OFFSET);
            uint hashA = MpqCrypto.HashString(name, MpqCrypto.HASH_TYPE_NAME_A);
            uint hashB = MpqCrypto.HashString(name, MpqCrypto.HASH_TYPE_NAME_B);

            uint i = hashIndex % (uint)_hashes.Count;
            uint start = i;

            while (_hashes[(int)i].BlockIndex != 0xFFFFFFFF)
            {
                if (_hashes[(int)i].NameHashA == hashA && _hashes[(int)i].NameHashB == hashB)
                    return (int)_hashes[(int)i].BlockIndex;

                i = (i + 1) % (uint)_hashes.Count;
                if (i == start) break;
            }

            return -1;
        }

        public byte[] ExtractFile(string fileName)
        {
            int index = FindFileIndex(fileName);
            if (index == -1)
            {
                Console.WriteLine($"      [!] Файл не найден: {fileName}");
                return null;
            }

            Console.WriteLine($"      [*] Найден по индексу {index}");
            return ExtractFile(index, fileName);
        }

        public byte[] ExtractFile(int index, string fileName = null)
        {
            if (index < 0 || index >= _blocks.Count)
            {
                Console.WriteLine($"      [!] Неверный индекс: {index}");
                return null;
            }

            var block = _blocks[index];
            if ((block.Flags & (uint)MpqFileFlags.Exists) == 0)
            {
                Console.WriteLine($"      [!] Блок {index} не существует");
                return null;
            }

            long filePos = _headerOffset + block.Offset;
            _stream.Position = filePos;

            byte[] fileData = _reader.ReadBytes((int)block.CompressedSize);
            Console.WriteLine($"      [*] Прочитано {fileData.Length} байт");
            Console.WriteLine($"      [*] Флаги блока: 0x{block.Flags:X8}");

            byte[] result;

            if ((block.Flags & (uint)MpqFileFlags.Compressed) != 0)
            {
                Console.WriteLine($"      [*] Файл сжат");
                if ((block.Flags & (uint)MpqFileFlags.SingleUnit) != 0)
                {
                    Console.WriteLine($"      [*] SingleUnit файл");
                    result = DecompressSingleUnit(fileData, (int)block.FileSize, fileName, block);
                }
                else
                {
                    Console.WriteLine($"      [*] Multi-sector файл");
                    result = DecompressSectors(fileData, block, fileName);
                }
            }
            else
            {
                Console.WriteLine($"      [*] Несжатый файл");
                result = fileData;
            }

            // Проверяем заголовок DBC
            if (result != null && result.Length >= 4)
            {
                uint magic = BitConverter.ToUInt32(result, 0);
                if (magic == 0x43424457) // 'WDBC' в little-endian
                {
                    Console.WriteLine($"\n      [✓] ЗАГОЛОВОК DBC КОРРЕКТЕН! (0x{magic:X8})");

                    if (result.Length >= 20)
                    {
                        int recordCount = BitConverter.ToInt32(result, 4);
                        int fieldCount = BitConverter.ToInt32(result, 8);
                        int recordSize = BitConverter.ToInt32(result, 12);
                        int stringBlockSize = BitConverter.ToInt32(result, 16);

                        Console.WriteLine($"      [*] Записей: {recordCount:N0}");
                        Console.WriteLine($"      [*] Полей: {fieldCount}");
                        Console.WriteLine($"      [*] Размер записи: {recordSize} байт");
                        Console.WriteLine($"      [*] Строковый блок: {stringBlockSize:N0} байт");
                    }
                }
                else
                {
                    Console.WriteLine($"      [!] Неверный заголовок: 0x{magic:X8}");

                    // Покажем первые 32 байта для диагностики
                    Console.Write($"      [!] Первые 32 байта: ");
                    for (int j = 0; j < 32 && j < result.Length; j++)
                    {
                        Console.Write($"{result[j]:X2} ");
                        if ((j + 1) % 8 == 0) Console.Write(" ");
                    }
                    Console.WriteLine();
                }
            }

            return result;
        }

        private byte[] DecompressSectors(byte[] data, MpqBlock block, string fileName)
        {
            int sectorSize = 512 << (int)_header.BlockSize;
            int sectorCount = (int)((block.FileSize + sectorSize - 1) / sectorSize);

            Console.WriteLine($"      [*] Секторов: {sectorCount}, размер сектора: {sectorSize} байт");
            Console.WriteLine($"      [*] Размер файла в архиве: {block.CompressedSize} байт");
            Console.WriteLine($"      [*] Ожидаемый размер после распаковки: {block.FileSize} байт");

            // 1. Вычисляем ключ для дешифровки
            uint baseKey = 0;
            uint adjustedKey = 0;

            if (!string.IsNullOrEmpty(fileName) && (block.Flags & (uint)MpqFileFlags.Encrypted) != 0)
            {
                baseKey = MpqCrypto.GetFileKey(fileName);
                adjustedKey = MpqCrypto.AdjustFileKey(baseKey, block.Offset, block.FileSize, block.Flags);
                Console.WriteLine($"      [*] Базовый ключ: 0x{baseKey:X8}, скорректированный: 0x{adjustedKey:X8}");
            }

            // 2. Читаем таблицу смещений
            uint physicalTableSize = BitConverter.ToUInt32(data, 0);
            int expectedTableSize = (sectorCount + 1) * 4;

            Console.WriteLine($"      [*] Физический размер таблицы: {physicalTableSize} байт (0x{physicalTableSize:X8})");
            Console.WriteLine($"      [*] Ожидаемый размер таблицы: {expectedTableSize} байт (0x{expectedTableSize:X8})");

            uint[] sectorOffsets = new uint[sectorCount + 1];

            if (physicalTableSize < expectedTableSize) // Таблица сжата
            {
                Console.WriteLine($"      [*] Таблица смещений сжата! Первый байт: 0x{data[0]:X2}");

                byte[] compressedTable = new byte[physicalTableSize];
                Buffer.BlockCopy(data, 0, compressedTable, 0, (int)physicalTableSize);

                byte compressionType = compressedTable[0];
                byte[] decompressedTable;

                if (compressionType == 0x02) // ZLib
                {
                    byte[] toDecompress = new byte[compressedTable.Length - 1];
                    Buffer.BlockCopy(compressedTable, 1, toDecompress, 0, toDecompress.Length);
                    decompressedTable = MpqCrypto.Decompress(toDecompress); // ← ИСПРАВЛЕНО
                    Console.WriteLine($"      [*] Размер распакованной таблицы: {decompressedTable.Length} байт");

                    if (decompressedTable.Length < expectedTableSize)
                    {
                        Array.Resize(ref decompressedTable, expectedTableSize);
                    }
                }
                else
                {
                    decompressedTable = MpqCrypto.Decompress(compressedTable); // ← ИСПРАВЛЕНО
                }

                for (int i = 0; i <= sectorCount; i++)
                {
                    if (i * 4 + 4 <= decompressedTable.Length)
                        sectorOffsets[i] = BitConverter.ToUInt32(decompressedTable, i * 4);
                    else
                        sectorOffsets[i] = 0;
                }
            }
            else // Таблица не сжата
            {
                Console.WriteLine($"      [*] Таблица смещений не сжата");
                for (int i = 0; i <= sectorCount; i++)
                {
                    sectorOffsets[i] = BitConverter.ToUInt32(data, i * 4);
                }
            }

            Console.WriteLine($"      [*] Первое смещение: 0x{sectorOffsets[0]:X8}");

            // 3. Сборка итогового файла
            byte[] result = new byte[block.FileSize];
            int errorCount = 0;

            for (int i = 0; i < sectorCount; i++)
            {
                uint start = sectorOffsets[i];
                uint end = sectorOffsets[i + 1];
                int compressedSize = (int)(end - start);
                int outOffset = i * sectorSize;
                int outSize = Math.Min(sectorSize, (int)block.FileSize - outOffset);

                if (compressedSize <= 0)
                {
                    if (errorCount < 5)
                        Console.WriteLine($"      [!] Сектор {i} имеет нулевой размер");
                    errorCount++;
                    continue;
                }

                if (start + compressedSize > data.Length)
                {
                    if (errorCount < 5)
                        Console.WriteLine($"      [!] Сектор {i} выходит за пределы данных");
                    errorCount++;
                    continue;
                }

                byte[] sectorData = new byte[compressedSize];
                Buffer.BlockCopy(data, (int)start, sectorData, 0, compressedSize);

                // ВАЖНО: Сначала дешифровка, потом распаковка!
                if ((block.Flags & (uint)MpqFileFlags.Encrypted) != 0)
                {
                    // Ключ для каждого сектора = базовый ключ + индекс сектора
                    uint sectorKey = adjustedKey + (uint)i;
                    MpqCrypto.DecryptBlock(sectorData, sectorKey);
                }

                if (compressedSize < outSize) // Сектор сжат
                {
                    try
                    {
                        // После дешифровки первый байт - тип сжатия
                        byte compressionType = sectorData[0];

                        if (compressionType == 0x02) // ZLib
                        {
                            // Пропускаем первый байт
                            byte[] toDecompress = new byte[sectorData.Length - 1];
                            Buffer.BlockCopy(sectorData, 1, toDecompress, 0, sectorData.Length - 1);

                            byte[] decompressed = MpqCrypto.Decompress(toDecompress); // ← ИСПРАВЛЕНО

                            int bytesToCopy = Math.Min(decompressed.Length, outSize);
                            Buffer.BlockCopy(decompressed, 0, result, outOffset, bytesToCopy);

                            if (i % 500 == 0) Console.Write("+");
                        }
                        else
                        {
                            Buffer.BlockCopy(sectorData, 0, result, outOffset, Math.Min(sectorData.Length, outSize));
                            if (i % 500 == 0) Console.Write("?");

                            if (errorCount < 5)
                            {
                                Console.WriteLine($"\n      [!] Неподдерживаемый тип сжатия: 0x{compressionType:X2} в секторе {i}");
                                errorCount++;
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        if (errorCount < 5)
                        {
                            Console.WriteLine($"\n      [!] Ошибка в секторе {i}: {ex.Message}");
                            errorCount++;
                        }
                        Console.Write("?");
                    }
                }
                else // Сектор не сжат
                {
                    Buffer.BlockCopy(sectorData, 0, result, outOffset, outSize);
                    if (i % 500 == 0) Console.Write(".");
                }

                if ((i + 1) % 500 == 0)
                {
                    double percent = (i + 1) * 100.0 / sectorCount;
                    Console.WriteLine($"\n      [*] Прогресс: {i + 1}/{sectorCount} секторов ({percent:F1}%)");
                }
            }

            Console.WriteLine($"\n      [✓] Распаковка завершена");
            return result;
        }

        private byte[] DecompressSingleUnit(byte[] data, int expectedSize, string fileName, MpqBlock block)
        {
            if (data.Length == 0) return data;

            // Если файл зашифрован, сначала дешифруем
            if ((block.Flags & (uint)MpqFileFlags.Encrypted) != 0 && !string.IsNullOrEmpty(fileName))
            {
                uint baseKey = MpqCrypto.GetFileKey(fileName);
                uint adjustedKey = MpqCrypto.AdjustFileKey(baseKey, block.Offset, block.FileSize, block.Flags);
                MpqCrypto.DecryptBlock(data, adjustedKey);
            }

            CompressionType compType = (CompressionType)data[0];
            Console.WriteLine($"      [*] Тип сжатия SingleUnit: {compType} (0x{(byte)compType:X2})");

            switch (compType)
            {
                case CompressionType.Zlib:
                case CompressionType.ZlibHuffman:
                    // Пропускаем первый байт
                    byte[] toDecompress = new byte[data.Length - 1];
                    Buffer.BlockCopy(data, 1, toDecompress, 0, data.Length - 1);
                    byte[] decompressed = MpqCrypto.Decompress(toDecompress); // ← ИСПРАВЛЕНО

                    if (decompressed.Length != expectedSize)
                    {
                        Console.WriteLine($"      [!] Несоответствие размера: ожидалось {expectedSize}, получено {decompressed.Length}");
                        if (decompressed.Length < expectedSize)
                            Array.Resize(ref decompressed, expectedSize);
                    }
                    return decompressed;

                default:
                    Console.WriteLine($"      [!] Неподдерживаемый тип сжатия: {compType}");
                    return data;
            }
        }

        public List<string> ListDbcFiles()
        {
            var result = new List<string>();

            string[] knownDbcFiles = {
                "DBFilesClient\\Spell.dbc",
                "DBFilesClient\\ItemDisplayInfo.dbc",
                "DBFilesClient\\SpellIcon.dbc",
                "DBFilesClient\\CreatureDisplayInfo.dbc",
                "DBFilesClient\\Map.dbc",
                "DBFilesClient\\AreaTable.dbc"
            };

            Console.WriteLine($"\n   Архив: {ArchiveName}");
            foreach (var file in knownDbcFiles)
            {
                int idx = FindFileIndex(file);
                if (idx >= 0)
                {
                    result.Add(file);
                    Console.WriteLine($"      [+] {file} (индекс {idx})");
                }
                else
                {
                    Console.WriteLine($"      [-] {file} (не найден)");
                }
            }

            return result;
        }

        public List<MpqBlock> GetAllBlocks()
        {
            return new List<MpqBlock>(_blocks);
        }

        public void Dispose()
        {
            if (!_isDisposed)
            {
                _reader?.Dispose();
                _stream?.Dispose();
                _isDisposed = true;
            }
        }
    }
}