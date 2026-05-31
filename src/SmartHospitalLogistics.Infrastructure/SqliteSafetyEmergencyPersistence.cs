using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Data.Sqlite;
using SmartHospitalLogistics.Application;
using SmartHospitalLogistics.Domain;

namespace SmartHospitalLogistics.Infrastructure;

public sealed class SqliteSafetyEmergencyPersistence : ISafetyEmergencyPersistence
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };

    private readonly string _connectionString;
    private readonly object _sync = new();

    public SqliteSafetyEmergencyPersistence(string dbPath)
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

    public SafetyEmergencyPersistenceSnapshot Load()
    {
        lock (_sync)
        {
            using var connection = OpenConnection();
            using var command = connection.CreateCommand();
            command.CommandText = """
                SELECT node_code, name, event_type, campus, building, floor, room,
                       bim_element_id, responsible_team, monitoring_point_code,
                       asset_code, status, response_level, linked_systems, source_evidence
                FROM safety_emergency_nodes
                ORDER BY node_code;
                """;

            using var reader = command.ExecuteReader();
            var nodes = new List<SafetyEmergencyNode>();
            while (reader.Read())
            {
                nodes.Add(new SafetyEmergencyNode(
                    ReadString(reader, "node_code"),
                    ReadString(reader, "name"),
                    ParseEnum<SafetyEmergencyEventType>(reader, "event_type"),
                    ReadLocation(reader),
                    ReadString(reader, "responsible_team"),
                    ReadString(reader, "monitoring_point_code"),
                    ReadString(reader, "asset_code"),
                    ParseEnum<SafetyEmergencyNodeStatus>(reader, "status"),
                    ParseEnum<EmergencyResponseLevel>(reader, "response_level"),
                    Deserialize<string[]>(ReadString(reader, "linked_systems")),
                    Deserialize<FeatureEvidence[]>(ReadString(reader, "source_evidence"))));
            }

            return new SafetyEmergencyPersistenceSnapshot(nodes);
        }
    }

    public void SaveNode(SafetyEmergencyNode node)
    {
        lock (_sync)
        {
            using var connection = OpenConnection();
            using var command = connection.CreateCommand();
            command.CommandText = """
                INSERT INTO safety_emergency_nodes (
                    node_code, name, event_type, campus, building, floor, room,
                    bim_element_id, responsible_team, monitoring_point_code,
                    asset_code, status, response_level, linked_systems, source_evidence
                )
                VALUES (
                    $node_code, $name, $event_type, $campus, $building, $floor, $room,
                    $bim_element_id, $responsible_team, $monitoring_point_code,
                    $asset_code, $status, $response_level, $linked_systems, $source_evidence
                )
                ON CONFLICT(node_code) DO UPDATE SET
                    name = excluded.name,
                    event_type = excluded.event_type,
                    campus = excluded.campus,
                    building = excluded.building,
                    floor = excluded.floor,
                    room = excluded.room,
                    bim_element_id = excluded.bim_element_id,
                    responsible_team = excluded.responsible_team,
                    monitoring_point_code = excluded.monitoring_point_code,
                    asset_code = excluded.asset_code,
                    status = excluded.status,
                    response_level = excluded.response_level,
                    linked_systems = excluded.linked_systems,
                    source_evidence = excluded.source_evidence;
                """;
            command.Parameters.AddWithValue("$node_code", node.NodeCode);
            command.Parameters.AddWithValue("$name", node.Name);
            command.Parameters.AddWithValue("$event_type", node.EventType.ToString());
            AddLocationParameters(command, node.Location);
            command.Parameters.AddWithValue("$responsible_team", node.ResponsibleTeam);
            command.Parameters.AddWithValue("$monitoring_point_code", node.MonitoringPointCode);
            command.Parameters.AddWithValue("$asset_code", node.AssetCode);
            command.Parameters.AddWithValue("$status", node.Status.ToString());
            command.Parameters.AddWithValue("$response_level", node.ResponseLevel.ToString());
            command.Parameters.AddWithValue("$linked_systems", Serialize(node.LinkedSystems));
            command.Parameters.AddWithValue("$source_evidence", Serialize(node.SourceEvidence));
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
            CREATE TABLE IF NOT EXISTS safety_emergency_nodes (
                node_code TEXT PRIMARY KEY,
                name TEXT NOT NULL,
                event_type TEXT NOT NULL,
                campus TEXT NOT NULL,
                building TEXT NOT NULL,
                floor TEXT NOT NULL,
                room TEXT NOT NULL,
                bim_element_id TEXT NOT NULL,
                responsible_team TEXT NOT NULL,
                monitoring_point_code TEXT NOT NULL,
                asset_code TEXT NOT NULL,
                status TEXT NOT NULL,
                response_level TEXT NOT NULL,
                linked_systems TEXT NOT NULL,
                source_evidence TEXT NOT NULL
            );

            CREATE INDEX IF NOT EXISTS ix_safety_emergency_nodes_point_asset
                ON safety_emergency_nodes(monitoring_point_code, asset_code);
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
