using MySql.Data.MySqlClient;
using System.Data;

namespace MaNGOSExtractor.Services;

public class DatabaseService : IDisposable
{
    private readonly MySqlConnection _connection;
    private readonly Config _config;

    public DatabaseService(Config config)
    {
        _config = config;
        var connString = $"Server={config.Host};Port={config.Port};Database={config.Database};" +
                         $"Uid={config.User};Pwd={config.Password};Charset=utf8;AllowZeroDateTime=True;";
        _connection = new MySqlConnection(connString);
    }

    public async Task<bool> TestConnectionAsync()
    {
        try
        {
            await _connection.OpenAsync();
            Console.WriteLine("✅ Подключение к MySQL успешно");
            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Ошибка подключения: {ex.Message}");
            return false;
        }
    }
    
    public async Task<List<T>> QueryAsync<T>(string sql, Func<IDataReader, T> mapper)
    {
        var results = new List<T>();

        if (_connection.State != ConnectionState.Open)
            await _connection.OpenAsync();

        using var cmd = new MySqlCommand(sql, _connection);
        using var reader = await cmd.ExecuteReaderAsync();

        while (await reader.ReadAsync())
        {
            results.Add(mapper(reader));
        }

        return results;
    }

    public void Dispose()
    {
        if (_connection.State == ConnectionState.Open)
            _connection.Close();
        _connection.Dispose();
    }
}