using System;
using System.Data;
using System.Threading.Tasks;
using MySqlConnector; // Import the driver namespace


namespace Avalonia_DB_Connection;

public class database
{
   

    public static async Task<bool> TestConnectionAsync()
    {
        String _connectionString = "Server=localhost;Port=3306;Database=test_db;User ID=root;Password=;";
        // "await using" handles clean resource disposal automatically
        await using var connection = new MySqlConnection(_connectionString);
        try
        {
            await connection.OpenAsync();
            return true; // Successfully connected!
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Database Error: {ex.Message}");
            return false; 
        }
    }

    public async Task FetchDataExampleAsync()
    {
        String _connectionString = "Server=localhost;Port=3306;Database=sample_tbl;User ID=root;Password=;";
        await using var connection = new MySqlConnection(_connectionString);
        await connection.OpenAsync();

        var query = "SELECT * FROM sample_tbl";
        await using var command = new MySqlCommand(query, connection);
        await using var reader = await command.ExecuteReaderAsync();

        while (await reader.ReadAsync())
        {
            var id = reader.GetInt32("id");
            var username = reader.GetString("username");
            // You can map this data to an ObservableCollection to bind to your Avalonia UI
            Console.WriteLine($"User: {id} - {username}");
        }
    }
}