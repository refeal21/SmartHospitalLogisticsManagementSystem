using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Data.Sqlite;
using SmartHospitalLogistics.Application;
using SmartHospitalLogistics.Domain;

namespace SmartHospitalLogistics.Infrastructure;

public sealed class SqliteHvacPersistence : IHvacPersistence
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };

    private readonly string _connectionString;
    private readonly object _sync = new();

    public SqliteHvacPersistence(string dbPath)
    {
        if (string.IsNullOrWhiteSpace(dbPath))
        {
            throw new ArgumentException("SQLite database path is required.", nameof(dbPath));
        }

        var directory = Path.GetDirectoryName(Path.GetFullPath(dbPath));
        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }

        var builder = new SqliteConnectionStringBuilder
        {
            DataSource = dbPath,
            ForeignKeys = true,
            Pooling = false
        };
        _connectionString = builder.ToString();

        lock (_sync)
        {
            using var connection = OpenConnection();
            EnsureSchema(connection);
        }
    }

    public HvacPersistenceSnapshot Load()
    {
        lock (_sync)
        {
            using var connection = OpenConnection();
            using var command = connection.CreateCommand();
            command.CommandText = """
                SELECT loop_code, name, system, campus, building, floor, room,
                       bim_element_id, responsible_team, monitoring_point_code, asset_code,
                       status, monitored_metrics, risk_summary, source_evidence
                FROM hvac_cooling_loops
                ORDER BY loop_code;
                """;

            using var reader = command.ExecuteReader();
            var loops = new List<HvacLoop>();
            while (reader.Read())
            {
                loops.Add(new HvacLoop(
                    ReadString(reader, "loop_code"),
                    ReadString(reader, "name"),
                    ReadString(reader, "system"),
                    ReadLocation(reader),
                    ReadString(reader, "responsible_team"),
                    ReadString(reader, "monitoring_point_code"),
                    ReadString(reader, "asset_code"),
                    ParseEnum<HvacLoopStatus>(reader, "status"),
                    Deserialize<string[]>(ReadString(reader, "monitored_metrics")),
                    ReadString(reader, "risk_summary"),
                    Deserialize<FeatureEvidence[]>(ReadString(reader, "source_evidence"))));
            }

            return new HvacPersistenceSnapshot(loops);
        }
    }

    public void SaveLoop(HvacLoop loop)
    {
        lock (_sync)
        {
            using var connection = OpenConnection();
            using var command = connection.CreateCommand();
            command.CommandText = """
                INSERT INTO hvac_cooling_loops (
                    loop_code, name, system, campus, building, floor, room,
                    bim_element_id, responsible_team, monitoring_point_code, asset_code,
                    status, monitored_metrics, risk_summary, source_evidence
                )
                VALUES (
                    $loop_code, $name, $system, $campus, $building, $floor, $room,
                    $bim_element_id, $responsible_team, $monitoring_point_code, $asset_code,
                    $status, $monitored_metrics, $risk_summary, $source_evidence
                )
                ON CONFLICT(loop_code) DO UPDATE SET
                    name = excluded.name,
                    system = excluded.system,
                    campus = excluded.campus,
                    building = excluded.building,
                    floor = excluded.floor,
                    room = excluded.room,
                    bim_element_id = excluded.bim_element_id,
                    responsible_team = excluded.responsible_team,
                    monitoring_point_code = excluded.monitoring_point_code,
                    asset_code = excluded.asset_code,
                    status = excluded.status,
                    monitored_metrics = excluded.monitored_metrics,
                    risk_summary = excluded.risk_summary,
                    source_evidence = excluded.source_evidence;
                """;
            command.Parameters.AddWithValue("$loop_code", loop.LoopCode);
            command.Parameters.AddWithValue("$name", loop.Name);
            command.Parameters.AddWithValue("$system", loop.System);
            AddLocationParameters(command, loop.Location);
            command.Parameters.AddWithValue("$responsible_team", loop.ResponsibleTeam);
            command.Parameters.AddWithValue("$monitoring_point_code", loop.MonitoringPointCode);
            command.Parameters.AddWithValue("$asset_code", loop.AssetCode);
            command.Parameters.AddWithValue("$status", loop.Status.ToString());
            command.Parameters.AddWithValue("$monitored_metrics", Serialize(loop.MonitoredMetrics));
            command.Parameters.AddWithValue("$risk_summary", loop.RiskSummary);
            command.Parameters.AddWithValue("$source_evidence", Serialize(loop.SourceEvidence));
            command.ExecuteNonQuery();
        }
    }

    private SqliteConnection OpenConnection()
    {
        var connection = new SqliteConnection(_connectionString);
        connection.Open();
        return connection;
    }

    private static void EnsureSchema(SqliteConnection connection)
    {
        using var command = connection.CreateCommand();
        command.CommandText = """
            CREATE TABLE IF NOT EXISTS hvac_cooling_loops (
                loop_code TEXT PRIMARY KEY,
                name TEXT NOT NULL,
                system TEXT NOT NULL,
                campus TEXT NOT NULL,
                building TEXT NOT NULL,
                floor TEXT NOT NULL,
                room TEXT NOT NULL,
                bim_element_id TEXT NOT NULL,
                responsible_team TEXT NOT NULL,
                monitoring_point_code TEXT NOT NULL,
                asset_code TEXT NOT NULL,
                status TEXT NOT NULL,
                monitored_metrics TEXT NOT NULL,
                risk_summary TEXT NOT NULL,
                source_evidence TEXT NOT NULL
            );

            CREATE INDEX IF NOT EXISTS ix_hvac_cooling_loops_point_asset
                ON hvac_cooling_loops(monitoring_point_code, asset_code);
            """;
        command.ExecuteNonQuery();
    }

    private static void AddLocationParameters(SqliteCommand command, SpatialLocation location)
    {
        command.Parameters.AddWithValue("$campus", location.Campus);
        command.Parameters.AddWithValue("$building", location.Building);
        command.Parameters.AddWithValue("$floor", location.Floor);
        command.Parameters.AddWithValue("$room", location.Room);
        command.Parameters.AddWithValue("$bim_element_id", location.BimElementId);
    }

    private static SpatialLocation ReadLocation(SqliteDataReader reader) =>
        new(
            ReadString(reader, "campus"),
            ReadString(reader, "building"),
            ReadString(reader, "floor"),
            ReadString(reader, "room"),
            ReadString(reader, "bim_element_id"));

    private static string ReadString(SqliteDataReader reader, string name) =>
        reader.GetString(reader.GetOrdinal(name));

    private static TEnum ParseEnum<TEnum>(SqliteDataReader reader, string name)
        where TEnum : struct, Enum =>
        Enum.Parse<TEnum>(ReadString(reader, name), ignoreCase: true);

    private static T Deserialize<T>(string value) =>
        JsonSerializer.Deserialize<T>(value, JsonOptions)
        ?? throw new InvalidOperationException($"Failed to deserialize {typeof(T).Name}.");

    private static string Serialize<T>(T value) =>
        JsonSerializer.Serialize(value, JsonOptions);
}
