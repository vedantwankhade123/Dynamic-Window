using System.Text.Json;
using Newton.Core.Interfaces;
using Newton.Core.Models;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Logging;

namespace Newton.Infrastructure.Database;

public sealed class SqliteSettingsService : ISettingsService
{
    private readonly string _dbPath;
    private readonly ILogger<SqliteSettingsService> _logger;
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

    public NewtonSettings Current { get; private set; } = new();
    public event EventHandler<NewtonSettings>? SettingsChanged;

    public SqliteSettingsService(ILogger<SqliteSettingsService> logger, string? customDbPath = null)
    {
        _logger = logger;
        var appData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        var folder = Path.Combine(appData, "Newton");
        Directory.CreateDirectory(folder);
        _dbPath = customDbPath ?? Path.Combine(folder, "newton.db");
    }

    public async Task LoadAsync()
    {
        try
        {
            await using var conn = new SqliteConnection($"Data Source={_dbPath}");
            await conn.OpenAsync();

            await using var cmd = conn.CreateCommand();
            cmd.CommandText = """
                CREATE TABLE IF NOT EXISTS Settings (
                    Key TEXT PRIMARY KEY,
                    Value TEXT NOT NULL,
                    UpdatedAt TEXT NOT NULL
                );
                SELECT Value FROM Settings WHERE Key = 'NewtonSettings';
                """;

            var result = await cmd.ExecuteScalarAsync() as string;
            if (!string.IsNullOrWhiteSpace(result))
            {
                var loaded = JsonSerializer.Deserialize<NewtonSettings>(result, JsonOptions);
                if (loaded is not null)
                {
                    Current = loaded;
                }
            }
            else
            {
                await SaveAsync(Current);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load settings from SQLite at {DbPath}. Using defaults.", _dbPath);
        }
    }

    public async Task SaveAsync(NewtonSettings settings)
    {
        Current = settings;
        try
        {
            await using var conn = new SqliteConnection($"Data Source={_dbPath}");
            await conn.OpenAsync();

            await using var ensureCmd = conn.CreateCommand();
            ensureCmd.CommandText = """
                CREATE TABLE IF NOT EXISTS Settings (
                    Key TEXT PRIMARY KEY,
                    Value TEXT NOT NULL,
                    UpdatedAt TEXT NOT NULL
                );
                """;
            await ensureCmd.ExecuteNonQueryAsync();

            await using var upsertCmd = conn.CreateCommand();
            upsertCmd.CommandText = """
                INSERT INTO Settings (Key, Value, UpdatedAt)
                VALUES ('NewtonSettings', $value, $updatedAt)
                ON CONFLICT(Key) DO UPDATE SET
                    Value = excluded.Value,
                    UpdatedAt = excluded.UpdatedAt;
                """;
            upsertCmd.Parameters.AddWithValue("$value", JsonSerializer.Serialize(settings, JsonOptions));
            upsertCmd.Parameters.AddWithValue("$updatedAt", DateTimeOffset.UtcNow.ToString("O"));
            await upsertCmd.ExecuteNonQueryAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save settings to SQLite at {DbPath}.", _dbPath);
        }

        SettingsChanged?.Invoke(this, Current);
    }

    public async Task ResetToDefaultsAsync()
    {
        await SaveAsync(new NewtonSettings());
    }
}
