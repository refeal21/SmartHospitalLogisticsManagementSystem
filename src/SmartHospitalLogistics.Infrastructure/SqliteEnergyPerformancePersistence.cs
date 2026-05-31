using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Data.Sqlite;
using SmartHospitalLogistics.Application;
using SmartHospitalLogistics.Domain;

namespace SmartHospitalLogistics.Infrastructure;

public sealed class SqliteEnergyPerformancePersistence : IEnergyPerformancePersistence
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };

    private readonly string _connectionString;
    private readonly object _sync = new();

    public SqliteEnergyPerformancePersistence(string dbPath)
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

    public EnergyPerformancePersistenceSnapshot Load()
    {
        lock (_sync)
        {
            using var connection = OpenConnection();
            using var command = connection.CreateCommand();
            command.CommandText = """
                SELECT area_code, name, energy_type, campus, building, floor, room,
                       bim_element_id, responsible_team, primary_meter_point_code,
                       related_system_code, baseline_consumption, current_consumption,
                       cost_rate, status, source_evidence
                FROM energy_performance_areas
                ORDER BY area_code;
                """;

            using var reader = command.ExecuteReader();
            var areas = new List<EnergyPerformanceArea>();
            while (reader.Read())
            {
                areas.Add(new EnergyPerformanceArea(
                    ReadString(reader, "area_code"),
                    ReadString(reader, "name"),
                    ReadString(reader, "energy_type"),
                    ReadLocation(reader),
                    ReadString(reader, "responsible_team"),
                    ReadString(reader, "primary_meter_point_code"),
                    ReadString(reader, "related_system_code"),
                    ReadDecimal(reader, "baseline_consumption"),
                    ReadDecimal(reader, "current_consumption"),
                    ReadDecimal(reader, "cost_rate"),
                    ParseEnum<EnergyPerformanceAreaStatus>(reader, "status"),
                    Deserialize<FeatureEvidence[]>(ReadString(reader, "source_evidence"))));
            }

            return new EnergyPerformancePersistenceSnapshot(areas);
        }
    }

    public void SaveArea(EnergyPerformanceArea area)
    {
        lock (_sync)
        {
            using var connection = OpenConnection();
            using var command = connection.CreateCommand();
            command.CommandText = """
                INSERT INTO energy_performance_areas (
                    area_code, name, energy_type, campus, building, floor, room,
                    bim_element_id, responsible_team, primary_meter_point_code,
                    related_system_code, baseline_consumption, current_consumption,
                    cost_rate, status, source_evidence
                )
                VALUES (
                    $area_code, $name, $energy_type, $campus, $building, $floor, $room,
                    $bim_element_id, $responsible_team, $primary_meter_point_code,
                    $related_system_code, $baseline_consumption, $current_consumption,
                    $cost_rate, $status, $source_evidence
                )
                ON CONFLICT(area_code) DO UPDATE SET
                    name = excluded.name,
                    energy_type = excluded.energy_type,
                    campus = excluded.campus,
                    building = excluded.building,
                    floor = excluded.floor,
                    room = excluded.room,
                    bim_element_id = excluded.bim_element_id,
                    responsible_team = excluded.responsible_team,
                    primary_meter_point_code = excluded.primary_meter_point_code,
                    related_system_code = excluded.related_system_code,
                    baseline_consumption = excluded.baseline_consumption,
                    current_consumption = excluded.current_consumption,
                    cost_rate = excluded.cost_rate,
                    status = excluded.status,
                    source_evidence = excluded.source_evidence;
                """;
            command.Parameters.AddWithValue("$area_code", area.AreaCode);
            command.Parameters.AddWithValue("$name", area.Name);
            command.Parameters.AddWithValue("$energy_type", area.EnergyType);
            AddLocationParameters(command, area.Location);
            command.Parameters.AddWithValue("$responsible_team", area.ResponsibleTeam);
            command.Parameters.AddWithValue("$primary_meter_point_code", area.PrimaryMeterPointCode);
            command.Parameters.AddWithValue("$related_system_code", area.RelatedSystemCode);
            command.Parameters.AddWithValue("$baseline_consumption", area.BaselineConsumption);
            command.Parameters.AddWithValue("$current_consumption", area.CurrentConsumption);
            command.Parameters.AddWithValue("$cost_rate", area.CostRate);
            command.Parameters.AddWithValue("$status", area.Status.ToString());
            command.Parameters.AddWithValue("$source_evidence", Serialize(area.SourceEvidence));
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
            CREATE TABLE IF NOT EXISTS energy_performance_areas (
                area_code TEXT PRIMARY KEY,
                name TEXT NOT NULL,
                energy_type TEXT NOT NULL,
                campus TEXT NOT NULL,
                building TEXT NOT NULL,
                floor TEXT NOT NULL,
                room TEXT NOT NULL,
                bim_element_id TEXT NOT NULL,
                responsible_team TEXT NOT NULL,
                primary_meter_point_code TEXT NOT NULL,
                related_system_code TEXT NOT NULL,
                baseline_consumption REAL NOT NULL,
                current_consumption REAL NOT NULL,
                cost_rate REAL NOT NULL,
                status TEXT NOT NULL,
                source_evidence TEXT NOT NULL
            );

            CREATE INDEX IF NOT EXISTS ix_energy_performance_areas_meter_system
                ON energy_performance_areas(primary_meter_point_code, related_system_code);
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

    private static decimal ReadDecimal(SqliteDataReader reader, string name) =>
        reader.GetDecimal(reader.GetOrdinal(name));

    private static TEnum ParseEnum<TEnum>(SqliteDataReader reader, string name)
        where TEnum : struct, Enum =>
        Enum.Parse<TEnum>(ReadString(reader, name), ignoreCase: true);

    private static T Deserialize<T>(string value) =>
        JsonSerializer.Deserialize<T>(value, JsonOptions)
        ?? throw new InvalidOperationException($"Failed to deserialize {typeof(T).Name}.");

    private static string Serialize<T>(T value) =>
        JsonSerializer.Serialize(value, JsonOptions);
}
