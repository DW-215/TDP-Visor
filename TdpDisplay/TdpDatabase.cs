using Microsoft.Data.Sqlite;

namespace TdpDisplay;

/// <summary>
/// Provides data access for TDP power readings stored in the local SQLite database.
/// </summary>
public sealed class TdpDatabase
{
    // Absolute path into the "database" folder next to the project — independent of the
    // working directory (elevated processes may start in System32).
    private static readonly string DatabaseDirectory = Path.GetFullPath(
        Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "database"));

    private static readonly string DatabasePath = Path.Combine(DatabaseDirectory, "tdp.db");

    public void Initialize()
    {
        Directory.CreateDirectory(DatabaseDirectory);
        using var connection = new SqliteConnection($"Data Source={DatabasePath}");
        connection.Open();
        using var command = connection.CreateCommand();
        command.CommandText = "CREATE TABLE IF NOT EXISTS TdpReading (Id INTEGER PRIMARY KEY AUTOINCREMENT, Date DATETIME, cpuW REAL, gpuW REAL)";
        command.ExecuteNonQuery();
    }
    
    public void SaveReading(double cpuW, double gpuW)
    {
        using var connection = new SqliteConnection($"Data Source={DatabasePath}");
        connection.Open();
        using var command = connection.CreateCommand();
        command.CommandText = "INSERT INTO TdpReading (Date, cpuW, gpuW) VALUES (datetime('now'), @cpuW, @gpuW)";
        command.Parameters.AddWithValue("@cpuW", cpuW);
        command.Parameters.AddWithValue("@gpuW", gpuW);
        command.ExecuteNonQuery();
    }
    
    //TODO Implement some GUI aspect for this
    public void DeleteReadings()
    {
        using var connection = new SqliteConnection($"Data Source={DatabasePath}");
        connection.Open();
        using var command = connection.CreateCommand();
        command.CommandText = "DELETE FROM TdpReading";
        command.ExecuteNonQuery();
    }
    
    // TODO Implement some GUI aspect for this
    public List<(DateTime Date, double CpuW, double GpuW)> GetReadings()
    {
        var readings = new List<(DateTime Date, double CpuW, double GpuW)>();
        using var connection = new SqliteConnection($"Data Source={DatabasePath}");
        connection.Open();
        using var command = connection.CreateCommand();
        command.CommandText = "SELECT Date, cpuW, gpuW FROM TdpReading ORDER BY Id";
        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            readings.Add((
                DateTime.Parse(reader.GetString(0)),
                reader.GetDouble(1),
                reader.GetDouble(2)));
        }
        return readings;
    }
}