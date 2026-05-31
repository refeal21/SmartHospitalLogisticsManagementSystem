using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Data.Sqlite;
using SmartHospitalLogistics.Application;
using SmartHospitalLogistics.Domain;

namespace SmartHospitalLogistics.Infrastructure;

public sealed class SqliteMedicalGasPersistence : IMedicalGasPersistence
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };

    private readonly string _connectionString;
    private readonly object _sync = new();

    public SqliteMedicalGasPersistence(string dbPath)
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

    public MedicalGasPersistenceSnapshot Load()
    {
        lock (_sync)
        {
            using var connection = OpenConnection();
            using var command = connection.CreateCommand();
            command.CommandText = """
                SELECT zone_code, name, department, campus, building, floor, room,
                       bim_element_id, supply_types, responsible_team, pressure_point_code,
                       valve_asset_code, status, risk_summary, source_evidence
                FROM medical_gas_zones
                ORDER BY zone_code;
                """;

            using var reader = command.ExecuteReader();
            var zones = new List<MedicalGasZone>();
            while (reader.Read())
            {
                zones.Add(new MedicalGasZone(
                    ReadString(reader, "zone_code"),
                    ReadString(reader, "name"),
                    ReadString(reader, "department"),
                    ReadLocation(reader),
                    Deserialize<MedicalGasSupplyType[]>(ReadString(reader, "supply_types")),
                    ReadString(reader, "responsible_team"),
                    ReadString(reader, "pressure_point_code"),
                    ReadString(reader, "valve_asset_code"),
                    ParseEnum<MedicalGasZoneStatus>(reader, "status"),
                    ReadString(reader, "risk_summary"),
                    Deserialize<FeatureEvidence[]>(ReadString(reader, "source_evidence"))));
            }

            return new MedicalGasPersistenceSnapshot(zones);
        }
    }

    public void SaveZone(MedicalGasZone zone)
    {
        lock (_sync)
        {
            using var connection = OpenConnection();
            using var command = connection.CreateCommand();
            command.CommandText = """
                INSERT INTO medical_gas_zones (
                    zone_code, name, department, campus, building, floor, room,
                    bim_element_id, supply_types, responsible_team, pressure_point_code,
                    valve_asset_code, status, risk_summary, source_evidence
                )
                VALUES (
                    $zone_code, $name, $department, $campus, $building, $floor, $room,
                    $bim_element_id, $supply_types, $responsible_team, $pressure_point_code,
                    $valve_asset_code, $status, $risk_summary, $source_evidence
                )
                ON CONFLICT(zone_code) DO UPDATE SET
                    name = excluded.name,
                    department = excluded.department,
                    campus = excluded.campus,
                    building = excluded.building,
                    floor = excluded.floor,
                    room = excluded.room,
                    bim_element_id = excluded.bim_element_id,
                    supply_types = excluded.supply_types,
                    responsible_team = excluded.responsible_team,
                    pressure_point_code = excluded.pressure_point_code,
                    valve_asset_code = excluded.valve_asset_code,
                    status = excluded.status,
                    risk_summary = excluded.risk_summary,
                    source_evidence = excluded.source_evidence;
                """;
            command.Parameters.AddWithValue("$zone_code", zone.ZoneCode);
            command.Parameters.AddWithValue("$name", zone.Name);
            command.Parameters.AddWithValue("$department", zone.Department);
            AddLocationParameters(command, zone.Location);
            command.Parameters.AddWithValue("$supply_types", Serialize(zone.SupplyTypes));
            command.Parameters.AddWithValue("$responsible_team", zone.ResponsibleTeam);
            command.Parameters.AddWithValue("$pressure_point_code", zone.PressurePointCode);
            command.Parameters.AddWithValue("$valve_asset_code", zone.ValveAssetCode);
            command.Parameters.AddWithValue("$status", zone.Status.ToString());
            command.Parameters.AddWithValue("$risk_summary", zone.RiskSummary);
            command.Parameters.AddWithValue("$source_evidence", Serialize(zone.SourceEvidence));
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
            CREATE TABLE IF NOT EXISTS medical_gas_zones (
                zone_code TEXT PRIMARY KEY,
                name TEXT NOT NULL,
                department TEXT NOT NULL,
                campus TEXT NOT NULL,
                building TEXT NOT NULL,
                floor TEXT NOT NULL,
                room TEXT NOT NULL,
                bim_element_id TEXT NOT NULL,
                supply_types TEXT NOT NULL,
                responsible_team TEXT NOT NULL,
                pressure_point_code TEXT NOT NULL,
                valve_asset_code TEXT NOT NULL,
                status TEXT NOT NULL,
                risk_summary TEXT NOT NULL,
                source_evidence TEXT NOT NULL
            );

            CREATE INDEX IF NOT EXISTS ix_medical_gas_zones_point_asset
                ON medical_gas_zones(pressure_point_code, valve_asset_code);
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
