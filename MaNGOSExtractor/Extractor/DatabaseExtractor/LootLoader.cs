using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Threading.Tasks;
using MySqlConnector;
using RaidLootCore.Models;

namespace MaNGOSExtractor.Extractor.DatabaseExtractor
{
    public class LootLoader
    {
        private readonly MangosConnection _connection;

        public LootLoader(MangosConnection connection)
        {
            _connection = connection;
        }

        public async Task<Dictionary<int, List<LootItem>>> LoadLootRelationsAsync()
        {
            var lootRelations = new Dictionary<int, List<LootItem>>();

            string query = @"
                SELECT 
                    entry,
                    item,
                    ChanceOrQuestChance,
                    mincountOrRef,
                    maxcount
                FROM creature_loot_template 
                WHERE item > 0 
                ORDER BY entry, ChanceOrQuestChance DESC";

            Console.WriteLine($"   Выполняется SQL запрос для лута...");

            using var connection = _connection.GetConnection();
            await connection.OpenAsync();

            using var command = new MySqlCommand(query, connection);
            using var reader = await command.ExecuteReaderAsync();

            if (!reader.HasRows)
            {
                Console.WriteLine("   ⚠️ Лут не найден");
                return lootRelations;
            }

            int count = 0;
            while (await reader.ReadAsync())
            {
                count++;

                int creatureId = GetInt32(reader, "entry");
                float chance = GetFloat(reader, "ChanceOrQuestChance");
                int mincountOrRef = GetInt32(reader, "mincountOrRef");

                // Пропускаем ссылки на reference_loot_template
                if (mincountOrRef < 0) continue;

                var lootItem = new LootItem
                {
                    ItemId = GetInt32(reader, "item"),
                    DropChance = Math.Abs(chance),
                    MinCount = mincountOrRef,
                    MaxCount = GetInt32(reader, "maxcount"),
                    IsQuestItem = chance < 0
                };

                if (!lootRelations.ContainsKey(creatureId))
                    lootRelations[creatureId] = new List<LootItem>();

                lootRelations[creatureId].Add(lootItem);

                if (count % 1000 == 0)
                {
                    Console.Write($"\r   Обработано {count} записей...");
                }
            }

            Console.WriteLine($"\n   ✅ Загружено {lootRelations.Count} связей босс-лут");
            return lootRelations;
        }

        private int GetInt32(DbDataReader reader, string column)
        {
            try
            {
                int ord = reader.GetOrdinal(column);
                return reader.IsDBNull(ord) ? 0 : reader.GetInt32(ord);
            }
            catch
            {
                return 0;
            }
        }

        private float GetFloat(DbDataReader reader, string column)
        {
            try
            {
                int ord = reader.GetOrdinal(column);
                return reader.IsDBNull(ord) ? 0 : reader.GetFloat(ord);
            }
            catch
            {
                return 0;
            }
        }
    }
}