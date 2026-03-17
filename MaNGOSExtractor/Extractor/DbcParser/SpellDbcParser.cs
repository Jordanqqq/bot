using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using RaidLootCore.Models;

namespace MaNGOSExtractor.Extractor.DbcParser
{
    public class SpellDbcParser
    {
        private readonly string _dbcPath;

        public SpellDbcParser(string dbcPath)
        {
            _dbcPath = dbcPath;
        }

        public List<Spell> ParseSpells()
        {
            var spells = new List<Spell>();
            string filePath = Path.Combine(_dbcPath, "Spell.dbc");

            if (!File.Exists(filePath))
            {
                Console.WriteLine($"   ⚠️ Файл не найден: {filePath}");
                return spells;
            }

            using var fs = new FileStream(filePath, FileMode.Open, FileAccess.Read);
            using var reader = new BinaryReader(fs);

            // Чтение заголовка DBC
            uint magic = reader.ReadUInt32(); // Должно быть 'WDBC'
            if (magic != 0x43424457) // 'WDBC' в little-endian
            {
                Console.WriteLine($"   ⚠️ Неверный формат файла (ожидался WDBC)");
                return spells;
            }

            int recordCount = reader.ReadInt32();
            int fieldCount = reader.ReadInt32();
            int recordSize = reader.ReadInt32();
            int stringBlockSize = reader.ReadInt32();

            Console.WriteLine($"   📊 Spell.dbc: {recordCount} записей, {fieldCount} полей");

            // Позиция строкового блока
            long stringBlockPos = fs.Position + (recordCount * recordSize);

            // Ограничим количество записей для скорости (первые 5000)
            int maxRecords = Math.Min(recordCount, 5000);

            for (int i = 0; i < maxRecords; i++)
            {
                long recordPos = fs.Position;

                int id = reader.ReadInt32();                          // 0: ID
                int category = reader.ReadInt32();                    // 1: Category
                int castTime = reader.ReadInt32();                    // 2: CastTime
                reader.ReadInt32();                                   // 3: Unknown
                int duration = reader.ReadInt32();                    // 4: Duration
                int cooldown = reader.ReadInt32();                    // 5: Cooldown

                // Пропускаем поля до названий
                fs.Seek(recordPos + 32, SeekOrigin.Begin);

                int nameIndex = reader.ReadInt32();                   // Индекс названия
                int rankIndex = reader.ReadInt32();                   // Индекс ранга
                int tooltipIndex = reader.ReadInt32();                // Индекс описания

                // Читаем строки
                string nameEn = ReadStringFromBlock(fs, stringBlockPos, stringBlockSize, nameIndex);
                string tooltipEn = ReadStringFromBlock(fs, stringBlockPos, stringBlockSize, tooltipIndex);

                // Переходим к следующей записи
                fs.Seek(recordPos + recordSize, SeekOrigin.Begin);

                var spell = new Spell
                {
                    Id = id,
                    NameEn = nameEn,
                    NameRu = nameEn,
                    DescriptionEn = tooltipEn,
                    DescriptionRu = tooltipEn,
                    CastTime = castTime,
                    Cooldown = cooldown / 1000f,
                    Duration = duration,
                    School = 0,
                    Radius = 0,
                    DangerLevel = 1,
                    IsDamage = nameEn.ToLower().Contains("damage") || nameEn.ToLower().Contains("strike"),
                    IsHeal = nameEn.ToLower().Contains("heal") || nameEn.ToLower().Contains("cure"),
                    IsDebuff = nameEn.ToLower().Contains("curse") || nameEn.ToLower().Contains("poison") || nameEn.ToLower().Contains("debuff")
                };

                spells.Add(spell);
            }

            Console.WriteLine($"   ✅ Загружено {spells.Count} заклинаний");
            return spells;
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