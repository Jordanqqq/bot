using System;
using System.IO;
using System.IO.Compression;
using System.Text;

namespace MaNGOSExtractor.MpqCore
{
    public static class MpqCrypto
    {
        // Крипто-таблица для MPQ (0x500 элементов)
        private static readonly uint[] CryptTable;

        // Типы хешей (константы)
        public const uint HASH_TYPE_TABLE_OFFSET = 0;
        public const uint HASH_TYPE_NAME_A = 1;
        public const uint HASH_TYPE_NAME_B = 2;
        public const uint HASH_TYPE_FILE_KEY = 3;

        // Флаги MPQ
        public const uint MPQ_FILE_COMPRESSED = 0x00000200;
        public const uint MPQ_FILE_ENCRYPTED = 0x00010000;
        public const uint MPQ_FILE_FIX_KEY = 0x00020000;
        public const uint MPQ_FILE_SINGLE_UNIT = 0x01000000;
        public const uint MPQ_FILE_EXISTS = 0x80000000;

        static MpqCrypto()
        {
            CryptTable = new uint[0x500];
            uint seed = 0x00100001;

            for (uint i = 0; i < 0x100; i++)
            {
                for (uint j = 0, k = i; j < 5; j++, k += 0x100)
                {
                    seed = (seed * 125 + 3) % 0x2AAAAB;
                    uint high = (seed & 0xFFFF) << 16;

                    seed = (seed * 125 + 3) % 0x2AAAAB;
                    uint low = seed & 0xFFFF;

                    CryptTable[k] = high | low;
                }
            }
        }

        /// <summary>
        /// Хеширование строки для MPQ (алгоритм Storm)
        /// </summary>
        /// <param name="input">Входная строка (будет преобразована в верхний регистр)</param>
        /// <param name="hashType">Тип хеша (0-3)</param>
        public static uint HashString(string input, uint hashType)
        {
            if (string.IsNullOrEmpty(input))
                return 0;

            uint seed1 = 0x7FED7FED;
            uint seed2 = 0xEEEEEEEE;

            foreach (char ch in input.ToUpperInvariant())
            {
                uint val = CryptTable[(hashType << 8) + (byte)ch];
                seed1 = (val ^ (seed1 + seed2)) & 0xFFFFFFFF;
                seed2 = (uint)ch + seed1 + seed2 + (seed2 << 5) + 3;
                seed2 &= 0xFFFFFFFF;
            }
            return seed1;
        }

        /// <summary>
        /// Дешифровка блока данных (работает с uint[])
        /// </summary>
        public static void DecryptBlock(uint[] data, uint key)
        {
            uint seed = 0xEEEEEEEE;

            for (int i = 0; i < data.Length; i++)
            {
                seed += CryptTable[0x400 + (key & 0xFF)];
                uint result = data[i] ^ (key + seed);

                key = ((~key << 0x15) + 0x11111111) | (key >> 0x0B);
                seed = result + seed + (seed << 5) + 3;

                data[i] = result;
            }
        }

        /// <summary>
        /// Удобный метод для дешифровки byte[]
        /// </summary>
        public static void DecryptBlock(byte[] data, uint key)
        {
            if (data == null) return;
            if (data.Length % 4 != 0)
                throw new ArgumentException("Длина данных должна быть кратна 4");

            int uintCount = data.Length / 4;
            uint[] uintData = new uint[uintCount];

            Buffer.BlockCopy(data, 0, uintData, 0, data.Length);
            DecryptBlock(uintData, key);
            Buffer.BlockCopy(uintData, 0, data, 0, data.Length);
        }

        /// <summary>
        /// Корректировка ключа для файла с флагом FIX_KEY
        /// </summary>
        public static uint AdjustFileKey(uint baseKey, long filePos, uint fileSize, uint flags)
        {
            if ((flags & MPQ_FILE_FIX_KEY) != 0)
            {
                return (baseKey + (uint)filePos) ^ fileSize;
            }
            return baseKey;
        }

        /// <summary>
        /// Распаковка данных MPQ (Deflate/ZLib)
        /// </summary>
        public static byte[] Decompress(byte[] data)
        {
            if (data == null || data.Length == 0)
                return Array.Empty<byte>();

            // Пробуем ZLibStream (стандартный формат)
            try
            {
                using var msInput = new MemoryStream(data);
                using var decompressionStream = new ZLibStream(msInput, CompressionMode.Decompress);
                using var msOutput = new MemoryStream();
                decompressionStream.CopyTo(msOutput);

                if (msOutput.Length > 0)
                    return msOutput.ToArray();
            }
            catch
            {
                // Игнорируем, пробуем DeflateStream
            }

            // Пробуем DeflateStream (без ZLib заголовка)
            try
            {
                using var msInput = new MemoryStream(data);
                using var decompressionStream = new DeflateStream(msInput, CompressionMode.Decompress);
                using var msOutput = new MemoryStream();
                decompressionStream.CopyTo(msOutput);

                if (msOutput.Length > 0)
                    return msOutput.ToArray();
            }
            catch
            {
                // Игнорируем
            }

            // Если ничего не сработало, возвращаем исходные данные
            return data;
        }

        /// <summary>
        /// Получение ключа для хеш-таблицы
        /// </summary>
        public static uint GetHashTableKey()
        {
            return HashString("(hash table)", HASH_TYPE_FILE_KEY);
        }

        /// <summary>
        /// Получение ключа для таблицы блоков
        /// </summary>
        public static uint GetBlockTableKey()
        {
            return HashString("(block table)", HASH_TYPE_FILE_KEY);
        }

        /// <summary>
        /// Получение ключа для дешифровки файла
        /// </summary>
        public static uint GetFileKey(string fileName)
        {
            string justName = Path.GetFileName(fileName).ToUpperInvariant();
            return HashString(justName, HASH_TYPE_FILE_KEY);
        }
    }
}