using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Data.Sqlite;
using SmartHospitalLogistics.Application;
using SmartHospitalLogistics.Domain;

namespace SmartHospitalLogistics.Infrastructure;

public sealed class SqlitePowerDistributionPersistence : IPowerDistributionPersistence
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };

    private readonly string _connectionString;
    private readonly object _sync = new();

    public SqlitePowerDistributionPersistence(string dbPath)
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

    public PowerDistributionPersistenceSnapshot Load()
    {
        lock (_sync)
        {
            using var connection = OpenConnection();
            using var command = connection.CreateCommand();
            command.CommandText = """
                SELECT circuit_code, name, system, campus, building, floor, room,
                       bim_element_id, responsible_team, meter_point_code, asset_code,
                       status, monitored_metrics, risk_summary, source_evidence
                FROM power_distribution_circuits
                ORDER BY circuit_code;
                """;

            using var reader = command.ExecuteReader();
            var circuits = new List<PowerDistributionCircuit>();
            while (reader.Read())
            {
                circuits.Add(new PowerDistributionCircuit(
                    ReadString(reader, "circuit_code"),
                    ReadString(reader, "name"),
                    ReadString(reader, "system"),
                    ReadLocation(reader),
                    ReadString(reader, "responsible_team"),
                    ReadString(reader, "meter_point_code"),
                    ReadString(reader, "asset_code"),
                    ParseEnum<PowerDistributionCircuitStatus>(reader, "status"),
                    Deserialize<string[]>(ReadString(reader, "monitored_metrics")),
                    ReadString(reader, "risk_summary"),
                    Deserialize<FeatureEvidence[]>(ReadString(reader, "source_evidence"))));
            }

            return new PowerDistributionPersistenceSnapshot(circuits);
        }
    }

    public void SaveCircuit(PowerDistributionCircuit circuit)
    {
        lock (_sync)
        {
            using var connection = OpenConnection();
            using var command = connection.CreateCommand();
            command.CommandText = """
                INSERT INTO power_distribution_circuits (
                    circuit_code, name, system, campus, building, floor, room,
                    bim_element_id, responsible_team, meter_point_code, asset_code,
                    status, monitored_metrics, risk_summary, source_evidence
                )
                VALUES (
                    $circuit_code, $name, $system, $campus, $building, $floor, $room,
                    $bim_element_id, $responsible_team, $meter_point_code, $asset_code,
                    $status, $monitored_metrics, $risk_summary, $source_evidence
                )
                ON CONFLICT(circuit_code) DO UPDATE SET
                    name = excluded.name,
                    system = excluded.system,
                    campus = excluded.campus,
                    building = excluded.building,
                    floor = excluded.floor,
                    room = excluded.room,
                    bim_element_id = excluded.bim_element_id,
                    responsible_team = excluded.responsible_team,
                    meter_point_code = excluded.meter_point_code,
                    asset_code = excluded.asset_code,
                    status = excluded.status,
                    monitored_metrics = excluded.monitored_metrics,
                    risk_summary = excluded.risk_summary,
                    source_evidence = excluded.source_evidence;
                """;
            command.Parameters.AddWithValue("$circuit_code", circuit.CircuitCode);
            command.Parameters.AddWithValue("$name", circuit.Name);
            command.Parameters.AddWithValue("$system", circuit.System);
            AddLocationParameters(command, circuit.Location);
            command.Parameters.AddWithValue("$responsible_team", circuit.ResponsibleTeam);
            command.Parameters.AddWithValue("$meter_point_code", circuit.MeterPointCode);
            command.Parameters.AddWithValue("$asset_code", circuit.AssetCode);
            command.Parameters.AddWithValue("$status", circuit.Status.ToString());
            command.Parameters.AddWithValue("$monitored_metrics", Serialize(circuit.MonitoredMetrics));
            command.Parameters.AddWithValue("$risk_summary", circuit.RiskSummary);
            command.Parameters.AddWithValue("$source_evidence", Serialize(circuit.SourceEvidence));
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
            CREATE TABLE IF NOT EXISTS power_distribution_circuits (
                circuit_code TEXT PRIMARY KEY,
                name TEXT NOT NULL,
                system TEXT NOT NULL,
                campus TEXT NOT NULL,
                building TEXT NOT NULL,
                floor TEXT NOT NULL,
                room TEXT NOT NULL,
                bim_element_id TEXT NOT NULL,
                responsible_team TEXT NOT NULL,
                meter_point_code TEXT NOT NULL,
                asset_code TEXT NOT NULL,
                status TEXT NOT NULL,
                monitored_metrics TEXT NOT NULL,
                risk_summary TEXT NOT NULL,
                source_evidence TEXT NOT NULL
            );

            CREATE INDEX IF NOT EXISTS ix_power_distribution_circuits_point_asset
                ON power_distribution_circuits(meter_point_code, asset_code);
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
