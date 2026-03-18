using System;

namespace MaNGOSExtractor.MpqCore
{
    /// <summary>
    /// Структура заголовка MPQ
    /// </summary>
    public struct MpqHeader
    {
        public uint Magic;              // 'MPQ\x1A'
        public uint HeaderSize;
        public uint ArchiveSize;
        public ushort Version;
        public ushort BlockSize;
        public uint HashTableOffset;
        public uint BlockTableOffset;
        public uint HashTableCount;
        public uint BlockTableCount;
    }

    /// <summary>
    /// Структура блока файла
    /// </summary>
    public struct MpqBlock
    {
        public uint Offset;              // Смещение в архиве
        public uint CompressedSize;      // Сжатый размер
        public uint FileSize;            // Реальный размер
        public uint Flags;                // Флаги
    }

    /// <summary>
    /// Структура записи в хеш-таблице
    /// </summary>
    public struct MpqHash
    {
        public uint NameHashA;            // Хеш A имени файла
        public uint NameHashB;            // Хеш B имени файла
        public ushort Locale;              // Локаль
        public ushort Platform;            // Платформа
        public uint BlockIndex;            // Индекс в таблице блоков
    }

    /// <summary>
    /// Флаги файлов в MPQ
    /// </summary>
    [Flags]
    public enum MpqFileFlags : uint
    {
        Exists = 0x80000000,
        Compressed = 0x00000200,
        Encrypted = 0x00010000,
        SingleUnit = 0x01000000,
        SectorCrc = 0x04000000,
        PatchFile = 0x00100000
    }

    /// <summary>
    /// Типы сжатия
    /// </summary>
    public enum CompressionType : byte
    {
        Huffman = 0x01,
        Zlib = 0x02,
        Pkware = 0x08,
        Bzip2 = 0x10,
        WaveMonopcm = 0x40,
        WaveStereoPcm = 0x80,
        ZlibHuffman = 0x12
    }

    /// <summary>
    /// Типы хешей для HashString
    /// </summary>
    public enum HashType : uint
    {
        TableOffset = 0,
        NameA = 1,
        NameB = 2,
        TableIndex = 3
    }
}