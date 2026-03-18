using System;
using System.IO;
using System.IO.Compression;

namespace MaNGOSExtractor.MpqCore
{
    /// <summary>
    /// Методы распаковки данных MPQ
    /// </summary>
    public static class MpqCompression
    {
        /// <summary>
        /// Распаковка zlib (пропуская первый байт с типом сжатия)
        /// </summary>
        public static byte[] DecompressZlib(byte[] compressedData)
        {
            if (compressedData == null || compressedData.Length < 2)
                return compressedData;

            // Пропускаем первый байт (тип сжатия)
            using var input = new MemoryStream(compressedData, 1, compressedData.Length - 1);
            using var zlib = new DeflateStream(input, CompressionMode.Decompress);
            using var output = new MemoryStream();

            zlib.CopyTo(output);
            return output.ToArray();
        }

        /// <summary>
        /// Распаковка bzip2
        /// </summary>
        public static byte[] DecompressBzip2(byte[] compressedData)
        {
            // TODO: Добавить поддержку bzip2
            // Для этого нужна библиотека SharpCompress или подобная
            throw new NotSupportedException("Bzip2 сжатие пока не поддерживается");
        }

        /// <summary>
        /// Распаковка PKWARE (Implode)
        /// </summary>
        public static byte[] DecompressPkware(byte[] compressedData)
        {
            // TODO: Добавить поддержку PKWARE
            throw new NotSupportedException("PKWARE сжатие пока не поддерживается");
        }

        /// <summary>
        /// Распаковка Huffman
        /// </summary>
        public static byte[] DecompressHuffman(byte[] compressedData)
        {
            // TODO: Добавить поддержку Huffman
            throw new NotSupportedException("Huffman сжатие пока не поддерживается");
        }

        /// <summary>
        /// Определение типа сжатия по первому байту
        /// </summary>
        public static CompressionType GetCompressionType(byte[] data)
        {
            if (data == null || data.Length == 0)
                throw new ArgumentException("Данные пусты");

            return (CompressionType)data[0];
        }

        /// <summary>
        /// Проверка, поддерживается ли данный тип сжатия
        /// </summary>
        public static bool IsSupported(CompressionType type)
        {
            return type == CompressionType.Zlib ||
                   type == CompressionType.ZlibHuffman;
        }
    }
}