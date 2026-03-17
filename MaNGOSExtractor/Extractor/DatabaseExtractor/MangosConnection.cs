using System;
using System.Threading.Tasks;
using MySqlConnector;

namespace MaNGOSExtractor.Extractor.DatabaseExtractor
{
    public class MangosConnection
    {
        private readonly string _connectionString;

        public MangosConnection(string host, string database, string user, string password, int port = 3306)
        {
            _connectionString = $"Server={host};Port={port};Database={database};Uid={user};Pwd={password};Charset=utf8;AllowZeroDateTime=True;";
        }

        public async Task<bool> TestConnectionAsync()
        {
            try
            {
                using var connection = new MySqlConnection(_connectionString);
                await connection.OpenAsync();
                Console.WriteLine("✅ Подключение к MySQL успешно!");
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Ошибка подключения: {ex.Message}");
                return false;
            }
        }

        public MySqlConnection GetConnection()
        {
            return new MySqlConnection(_connectionString);
        }
    }
}