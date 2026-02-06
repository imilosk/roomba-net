using Microsoft.Data.Sqlite;
using RoombaNet.Api.Models;
using RoombaNet.Api.Services.Secrets;
using RoombaNet.Api.Settings;

namespace RoombaNet.Api.Services.RobotRegistry;

public class SqliteRobotRegistry : IRobotRegistry
{
    private const int DefaultPort = 8883;
    private readonly ILogger<SqliteRobotRegistry> _logger;
    private readonly ISecretProtector _secretProtector;
    private readonly string _connectionString;

    public SqliteRobotRegistry(
        RobotRegistrySettings settings,
        ISecretProtector secretProtector,
        ILogger<SqliteRobotRegistry> logger)
    {
        _logger = logger;
        _secretProtector = secretProtector;
        _connectionString = BuildConnectionString(settings);

        EnsureDatabase();
    }

    public Task<IReadOnlyList<RobotRecord>> GetAll(CancellationToken cancellationToken = default)
    {
        var records = new List<RobotRecord>();

        using var connection = OpenConnection();
        using var command = connection.CreateCommand();
        command.CommandText = """
                              SELECT blid, name, ip, port, password
                              FROM robots
                              ORDER BY blid;
                              """;

        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            records.Add(ToRecord(reader));
        }

        return Task.FromResult<IReadOnlyList<RobotRecord>>(records);
    }

    public Task<RobotRecord?> Get(string blid, CancellationToken cancellationToken = default)
    {
        using var connection = OpenConnection();
        using var command = connection.CreateCommand();
        command.CommandText = """
                              SELECT blid, name, ip, port, password
                              FROM robots
                              WHERE blid = $blid;
                              """;
        command.Parameters.AddWithValue("$blid", blid);

        using var reader = command.ExecuteReader();
        if (!reader.Read())
        {
            return Task.FromResult<RobotRecord?>(null);
        }

        return Task.FromResult<RobotRecord?>(ToRecord(reader));
    }

    public Task<RobotCredentials?> GetCredentials(string blid, CancellationToken cancellationToken = default)
    {
        var row = GetRobotRow(blid);
        if (row is null)
        {
            return Task.FromResult<RobotCredentials?>(null);
        }

        var password = _secretProtector.Unprotect(row.PasswordEncrypted);
        if (string.IsNullOrEmpty(password))
        {
            return Task.FromResult<RobotCredentials?>(null);
        }

        var credentials = new RobotCredentials(
            row.Blid,
            row.Ip,
            row.Port,
            password
        );

        return Task.FromResult<RobotCredentials?>(credentials);
    }

    public Task<RobotRecord> Create(RobotCreateRequest request, CancellationToken cancellationToken = default)
    {
        ValidateCreateRequest(request);

        var now = DateTime.UtcNow;
        var password = _secretProtector.Protect(request.Password);
        var port = request.Port ?? DefaultPort;

        using var connection = OpenConnection();
        using var command = connection.CreateCommand();
        command.CommandText = """
                              INSERT INTO robots (blid, name, ip, port, password, created_utc, updated_utc)
                              VALUES ($blid, $name, $ip, $port, $password, $created_utc, $updated_utc)
                              ON CONFLICT(blid) DO UPDATE SET
                                  name = excluded.name,
                                  ip = excluded.ip,
                                  port = excluded.port,
                                  password = COALESCE(excluded.password, robots.password),
                                  updated_utc = excluded.updated_utc;
                              """;

        command.Parameters.AddWithValue("$blid", request.Blid);
        command.Parameters.AddWithValue("$name", request.Name);
        command.Parameters.AddWithValue("$ip", request.Ip);
        command.Parameters.AddWithValue("$port", port);
        command.Parameters.AddWithValue("$password", (object?)password ?? DBNull.Value);
        command.Parameters.AddWithValue("$created_utc", now);
        command.Parameters.AddWithValue("$updated_utc", now);

        command.ExecuteNonQuery();

        var record = new RobotRecord(
            request.Blid,
            request.Name,
            request.Ip,
            port,
            !string.IsNullOrEmpty(request.Password)
        );

        return Task.FromResult(record);
    }

    public Task<RobotRecord> Update(string blid, RobotUpdateRequest request, CancellationToken cancellationToken = default)
    {
        var row = GetRobotRow(blid);
        if (row is null)
        {
            throw new KeyNotFoundException($"Robot '{blid}' not found.");
        }

        var now = DateTime.UtcNow;
        var password = request.Password is null
            ? row.PasswordEncrypted
            : _secretProtector.Protect(request.Password);

        var updated = row with
        {
            Name = request.Name ?? row.Name,
            Ip = request.Ip ?? row.Ip,
            Port = request.Port ?? row.Port,
            PasswordEncrypted = password,
            UpdatedUtc = now,
        };

        using var connection = OpenConnection();
        using var command = connection.CreateCommand();
        command.CommandText = """
                              UPDATE robots SET
                                  name = $name,
                                  ip = $ip,
                                  port = $port,
                                  password = $password,
                                  updated_utc = $updated_utc
                              WHERE blid = $blid;
                              """;

        command.Parameters.AddWithValue("$blid", updated.Blid);
        command.Parameters.AddWithValue("$name", updated.Name);
        command.Parameters.AddWithValue("$ip", updated.Ip);
        command.Parameters.AddWithValue("$port", updated.Port);
        command.Parameters.AddWithValue("$password", (object?)updated.PasswordEncrypted ?? DBNull.Value);
        command.Parameters.AddWithValue("$updated_utc", updated.UpdatedUtc ?? now);

        command.ExecuteNonQuery();

        var record = new RobotRecord(
            updated.Blid,
            updated.Name,
            updated.Ip,
            updated.Port,
            !string.IsNullOrEmpty(updated.PasswordEncrypted)
        );

        return Task.FromResult(record);
    }

    public Task<bool> Delete(string blid, CancellationToken cancellationToken = default)
    {
        using var connection = OpenConnection();
        using var command = connection.CreateCommand();
        command.CommandText = "DELETE FROM robots WHERE blid = $blid;";
        command.Parameters.AddWithValue("$blid", blid);

        var rows = command.ExecuteNonQuery();
        return Task.FromResult(rows > 0);
    }

    public Task<bool> UpdatePassword(string blid, string password, CancellationToken cancellationToken = default)
    {
        var row = GetRobotRow(blid);
        if (row is null)
        {
            return Task.FromResult(false);
        }

        var encrypted = _secretProtector.Protect(password);
        using var connection = OpenConnection();
        using var command = connection.CreateCommand();
        command.CommandText = """
                              UPDATE robots
                              SET password = $password, updated_utc = $updated_utc
                              WHERE blid = $blid;
                              """;

        command.Parameters.AddWithValue("$blid", blid);
        command.Parameters.AddWithValue("$password", (object?)encrypted ?? DBNull.Value);
        command.Parameters.AddWithValue("$updated_utc", DateTime.UtcNow);

        var rows = command.ExecuteNonQuery();
        return Task.FromResult(rows > 0);
    }

    private static void ValidateCreateRequest(RobotCreateRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Blid))
        {
            throw new ArgumentException("BLID is required.", nameof(request));
        }

        if (string.IsNullOrWhiteSpace(request.Name))
        {
            throw new ArgumentException("Name is required.", nameof(request));
        }

        if (string.IsNullOrWhiteSpace(request.Ip))
        {
            throw new ArgumentException("IP address is required.", nameof(request));
        }
    }

    private static string BuildConnectionString(RobotRegistrySettings settings)
    {
        var path = settings.DatabasePath;
        if (string.IsNullOrWhiteSpace(path))
        {
            path = Path.Combine(AppContext.BaseDirectory, "roombanet.db");
        }

        var directory = Path.GetDirectoryName(path);
        if (!string.IsNullOrWhiteSpace(directory) && !Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }

        return new SqliteConnectionStringBuilder
        {
            DataSource = path,
        }.ToString();
    }

    private void EnsureDatabase()
    {
        using var connection = OpenConnection();
        EnsureTableSchema(connection);

        _logger.LogInformation("Robot registry database ready at {ConnectionString}", _connectionString);
    }

    private SqliteConnection OpenConnection()
    {
        var connection = new SqliteConnection(_connectionString);
        connection.Open();
        return connection;
    }

    private RobotRow? GetRobotRow(string blid)
    {
        using var connection = OpenConnection();
        using var command = connection.CreateCommand();
        command.CommandText = """
                              SELECT blid, name, ip, port, password, created_utc, updated_utc
                              FROM robots
                              WHERE blid = $blid;
                              """;
        command.Parameters.AddWithValue("$blid", blid);

        using var reader = command.ExecuteReader();
        if (!reader.Read())
        {
            return null;
        }

        return ToRow(reader);
    }

    private static RobotRecord ToRecord(SqliteDataReader reader)
    {
        var blid = reader.GetString(0);
        var name = reader.GetString(1);
        var ip = reader.GetString(2);
        var port = reader.GetInt32(3);
        var password = reader.IsDBNull(4) ? null : reader.GetString(4);

        return new RobotRecord(
            blid,
            name,
            ip,
            port,
            !string.IsNullOrEmpty(password)
        );
    }

    private static RobotRow ToRow(SqliteDataReader reader)
    {
        return new RobotRow(
            reader.GetString(0),
            reader.GetString(1),
            reader.GetString(2),
            reader.GetInt32(3),
            reader.IsDBNull(4) ? null : reader.GetString(4),
            reader.IsDBNull(5) ? (DateTime?)null : ParseUtc(reader.GetString(5)),
            reader.IsDBNull(6) ? (DateTime?)null : ParseUtc(reader.GetString(6))
        );
    }

    private static DateTime? ParseUtc(string value)
    {
        if (DateTimeOffset.TryParse(value, out var parsed))
        {
            return parsed.UtcDateTime;
        }

        return null;
    }

    private sealed record RobotRow(
        string Blid,
        string Name,
        string Ip,
        int Port,
        string? PasswordEncrypted,
        DateTime? CreatedUtc,
        DateTime? UpdatedUtc
    );

    private static void EnsureTableSchema(SqliteConnection connection)
    {
        using var pragma = connection.CreateCommand();
        pragma.CommandText = "PRAGMA table_info(robots);";
        using var reader = pragma.ExecuteReader();

        var columns = new List<string>();
        while (reader.Read())
        {
            columns.Add(reader.GetString(1));
        }

        var expected = new[]
        {
            "blid", "name", "ip", "port", "password", "created_utc", "updated_utc"
        };

        if (columns.Count == 0)
        {
            using var create = connection.CreateCommand();
            create.CommandText = """
                                 CREATE TABLE robots (
                                     blid TEXT PRIMARY KEY,
                                     name TEXT NOT NULL,
                                     ip TEXT NOT NULL,
                                     port INTEGER NOT NULL,
                                     password TEXT,
                                     created_utc TEXT,
                                     updated_utc TEXT
                                 );
                                 """;
            create.ExecuteNonQuery();
            return;
        }

        if (columns.SequenceEqual(expected, StringComparer.OrdinalIgnoreCase))
        {
            return;
        }

        using var transaction = connection.BeginTransaction();
        using var migrate = connection.CreateCommand();
        migrate.Transaction = transaction;
        migrate.CommandText = """
                              CREATE TABLE robots_new (
                                  blid TEXT PRIMARY KEY,
                                  name TEXT NOT NULL,
                                  ip TEXT NOT NULL,
                                  port INTEGER NOT NULL,
                                  password TEXT,
                                  created_utc TEXT,
                                  updated_utc TEXT
                              );
                              INSERT INTO robots_new (blid, name, ip, port, password, created_utc, updated_utc)
                              SELECT blid, blid, ip, port, password, created_utc, updated_utc FROM robots;
                              DROP TABLE robots;
                              ALTER TABLE robots_new RENAME TO robots;
                              """;
        migrate.ExecuteNonQuery();
        transaction.Commit();
    }
}
