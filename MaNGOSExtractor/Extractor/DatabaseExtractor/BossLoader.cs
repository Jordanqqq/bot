using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Threading.Tasks;
using MySqlConnector;
using RaidLootCore.Models;

namespace MaNGOSExtractor.Extractor.DatabaseExtractor
{
    public class BossLoader
    {
        private readonly MangosConnection _connection;

        public BossLoader(MangosConnection connection)
        {
            _connection = connection;
        }

        public async Task<List<Boss>> LoadBossesAsync()
        {
            var bosses = new List<Boss>();

            // ИСПРАВЛЕННЫЙ ЗАПРОС под твою структуру
            string query = @"
                SELECT 
                    entry, 
                    name, 
                    minlevel, 
                    maxlevel, 
                    rank, 
                    modelid_1,           -- вместо DisplayId1
                    minhealth,            -- вместо HealthMin
                    maxhealth,            -- вместо HealthMax
                    mingold,              -- вместо MinGold
                    maxgold,              -- вместо MaxGold
                    ScriptName, 
                    lootid                -- ID для связи с лутом
                FROM creature_template 
                WHERE rank >= 3            -- боссы
                ORDER BY entry";

            Console.WriteLine($"   Выполняется SQL запрос для боссов...");

            using var connection = _connection.GetConnection();
            await connection.OpenAsync();

            using var command = new MySqlCommand(query, connection);
            using var reader = await command.ExecuteReaderAsync();

            if (!reader.HasRows)
            {
                Console.WriteLine("   ⚠️ Боссы не найдены");
                return bosses;
            }

            int count = 0;
            while (await reader.ReadAsync())
            {
                count++;

                int healthMax = GetInt32(reader, "maxhealth");
                int rank = GetInt32(reader, "rank");

                // Пропускаем слабых мобов
                if (healthMax < 100000) continue;

                var boss = new Boss
                {
                    Id = GetInt32(reader, "entry"),
                    NameEn = GetString(reader, "name"),
                    NameRu = GetString(reader, "name"),
                    Level = GetInt32(reader, "maxlevel"),
                    DisplayId = GetInt32(reader, "modelid_1"),
                    Health = GetFloat(reader, "maxhealth"),
                    MinGold = GetInt32(reader, "mingold"),
                    MaxGold = GetInt32(reader, "maxgold"),
                    MapId = 0, // карты нет в этом запросе
                    PortraitFile = $"boss_{GetInt32(reader, "entry")}.png"
                };

                bosses.Add(boss);

                if (count % 100 == 0)
                {
                    Console.Write($"\r   Загружено {count} боссов...");
                }
            }

            Console.WriteLine($"\n   ✅ Загружено {bosses.Count} боссов");
            return bosses;
        }

        // ========== ВСПОМОГАТЕЛЬНЫЕ МЕТОДЫ ==========

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

        private long GetInt64(DbDataReader reader, string column)
        {
            try
            {
                int ord = reader.GetOrdinal(column);
                return reader.IsDBNull(ord) ? 0 : reader.GetInt64(ord);
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

        private string GetString(DbDataReader reader, string column)
        {
            try
            {
                int ord = reader.GetOrdinal(column);
                return reader.IsDBNull(ord) ? "" : reader.GetString(ord);
            }
            catch
            {
                return "";
            }
        }
    }
}