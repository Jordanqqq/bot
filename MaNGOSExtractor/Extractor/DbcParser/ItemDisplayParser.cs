using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace MaNGOSExtractor.Extractor.DbcParser
{
    public class ItemDisplayParser
    {
        private readonly string _dbcPath;

        public ItemDisplayParser(string dbcPath)
        {
            _dbcPath = dbcPath;
        }

        public Dictionary<int, string> ParseItemDisplayInfo()
        {
            var iconMap = new Dictionary<int, string>();
            string filePath = Path.Combine(_dbcPath, "ItemDisplayInfo.dbc");

            if (!File.Exists(filePath))
            {
                Console.WriteLine($"   ⚠️ Файл не найден: {filePath}");
                return iconMap;
            }

            using var fs = new FileStream(filePath, FileMode.Open, FileAccess.Read);
            using var reader = new BinaryReader(fs);

            // Чтение заголовка
            uint magic = reader.ReadUInt32();
            if (magic != 0x43424457)
            {
                Console.WriteLine($"   ⚠️ Неверный формат файла");
                return iconMap;
            }

            int recordCount = reader.ReadInt32();
            int fieldCount = reader.ReadInt32();
            int recordSize = reader.ReadInt32();
            int stringBlockSize = reader.ReadInt32();

            long stringBlockPos = fs.Position + (recordCount * recordSize);

            for (int i = 0; i < recordCount; i++)
            {
                long recordPos = fs.Position;

                int id = reader.ReadInt32();

                // Пропускаем поля до иконок (обычно 5-6 полей)
                fs.Seek(recordPos + 20, SeekOrigin.Begin);

                int iconIndex = reader.ReadInt32();

                string iconName = ReadStringFromBlock(fs, stringBlockPos, stringBlockSize, iconIndex);

                if (!string.IsNullOrEmpty(iconName))
                {
                    iconMap[id] = iconName;
                }

                fs.Seek(recordPos + recordSize, SeekOrigin.Begin);
            }

            Console.WriteLine($"   ✅ Загружено {iconMap.Count} иконок");
            return iconMap;
        }

        private string ReadStringFromBlock(FileStream fs, long stringBlockPos, int stringBlockSize, int index)
        {
            if (index == 0 || index >= stringBlockSize)
                return "";

            try
            {
                fs.Seek(stringBlockPos + index, SeekOrigin.Begin);
                var bytes = new List<byte>();
                int b;
                while ((b = fs.ReadByte()) != 0 && b != -1)
                {
                    bytes.Add((byte)b);
                }
                return Encoding.UTF8.GetString(bytes.ToArray());
            }
            catch
            {
                return "";
            }
        }
    }
}