using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Data.Sqlite;
using SmartHospitalLogistics.Application;
using SmartHospitalLogistics.Domain;

namespace SmartHospitalLogistics.Infrastructure;

public sealed class SqliteIotIntegrationPersistence : IIotIntegrationPersistence
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };

    private readonly string _connectionString;
    private readonly object _sync = new();

    public SqliteIotIntegrationPersistence(string dbPath)
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

    public IotIntegrationPersistenceSnapshot Load()
    {
        lock (_sync)
        {
            using var connection = OpenConnection();
            return new IotIntegrationPersistenceSnapshot(
                LoadSystems(connection),
                LoadPoints(connection),
                LoadThresholdRules(connection),
                LoadReadings(connection));
        }
    }

    public void SaveSystem(IotSystemProfile system)
    {
        lock (_sync)
        {
            using var connection = OpenConnection();
            UpsertSystem(connection, system);
        }
    }

    public void SavePoint(IotMonitoringPoint point)
    {
        lock (_sync)
        {
            using var connection = OpenConnection();
            UpsertPoint(connection, point);
        }
    }

    public void SaveThresholdRule(TelemetryThresholdRule rule)
    {
        lock (_sync)
        {
            using var connection = OpenConnection();
            UpsertThresholdRule(connection, rule);
        }
    }

    public void SaveReading(TelemetryReading reading)
    {
        lock (_sync)
        {
            using var connection = OpenConnection();
            InsertReading(connection, reading);
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
            CREATE TABLE IF NOT EXISTS iot_system_profiles (
                category TEXT PRIMARY KEY,
                name TEXT NOT NULL,
                subsystems TEXT NOT NULL,
                roles TEXT NOT NULL,
                endpoints TEXT NOT NULL
            );

            CREATE TABLE IF NOT EXISTS iot_monitoring_points (
                point_code TEXT PRIMARY KEY,
                name TEXT NOT NULL,
                category TEXT NOT NULL,
                campus TEXT NOT NULL,
                building TEXT NOT NULL,
                floor TEXT NOT NULL,
                room TEXT NOT NULL,
                bim_element_id TEXT NOT NULL,
                device_code TEXT NOT NULL,
                protocol_adapter TEXT NOT NULL,
                metrics TEXT NOT NULL,
                source_evidence TEXT NOT NULL,
                FOREIGN KEY (category) REFERENCES iot_system_profiles(category)
            );

            CREATE TABLE IF NOT EXISTS iot_threshold_rules (
                point_code TEXT NOT NULL,
                metric_code TEXT NOT NULL,
                direction TEXT NOT NULL,
                warning_min TEXT NULL,
                warning_max TEXT NULL,
                critical_min TEXT NULL,
                critical_max TEXT NULL,
                rule_summary TEXT NOT NULL,
                PRIMARY KEY (point_code, metric_code),
                FOREIGN KEY (point_code) REFERENCES iot_monitoring_points(point_code)
            );

            CREATE TABLE IF NOT EXISTS iot_telemetry_readings (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                point_code TEXT NOT NULL,
                metric_code TEXT NOT NULL,
                value TEXT NOT NULL,
                unit TEXT NOT NULL,
                collected_at TEXT NOT NULL,
                risk_level TEXT NOT NULL,
                rule_summary TEXT NOT NULL,
                FOREIGN KEY (point_code) REFERENCES iot_monitoring_points(point_code)
            );

            CREATE INDEX IF NOT EXISTS ix_iot_points_category
                ON iot_monitoring_points(category, point_code);

            CREATE INDEX IF NOT EXISTS ix_iot_readings_point_metric_time
                ON iot_telemetry_readings(point_code, metric_code, collected_at);

            CREATE UNIQUE INDEX IF NOT EXISTS ux_iot_readings_deduplicate
                ON iot_telemetry_readings(point_code, metric_code, collected_at);
            """;
        command.ExecuteNonQuery();
    }

    private static IReadOnlyList<IotSystemProfile> LoadSystems(SqliteConnection connection)
    {
        using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT category, name, subsystems, roles, endpoints
            FROM iot_system_profiles
            ORDER BY category;
            """;

        using var reader = command.ExecuteReader();
        var systems = new List<IotSystemProfile>();
        while (reader.Read())
        {
            systems.Add(new IotSystemProfile(
                ParseEnum<IotSystemCategory>(reader, "category"),
                ReadString(reader, "name"),
                Deserialize<string[]>(ReadString(reader, "subsystems")),
                Deserialize<string[]>(ReadString(reader, "roles")),
                Deserialize<string[]>(ReadString(reader, "endpoints"))));
        }

        return systems;
    }

    private static IReadOnlyList<IotMonitoringPoint> LoadPoints(SqliteConnection connection)
    {
        using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT point_code, name, category, campus, building, floor, room,
                   bim_element_id, device_code, protocol_adapter, metrics, source_evidence
            FROM iot_monitoring_points
            ORDER BY category, point_code;
            """;

        using var reader = command.ExecuteReader();
        var points = new List<IotMonitoringPoint>();
        while (reader.Read())
        {
            points.Add(new IotMonitoringPoint(
                ReadString(reader, "point_code"),
                ReadString(reader, "name"),
                ParseEnum<IotSystemCategory>(reader, "category"),
                ReadLocation(reader),
                ReadString(reader, "device_code"),
                ReadString(reader, "protocol_adapter"),
                Deserialize<IotMetricDefinition[]>(ReadString(reader, "metrics")),
                Deserialize<FeatureEvidence[]>(ReadString(reader, "source_evidence"))));
        }

        return points;
    }

    private static IReadOnlyList<TelemetryThresholdRule> LoadThresholdRules(SqliteConnection connection)
    {
        using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT point_code, metric_code, direction, warning_min, warning_max,
                   critical_min, critical_max, rule_summary
            FROM iot_threshold_rules
            ORDER BY point_code, metric_code;
            """;

        using var reader = command.ExecuteReader();
        var rules = new List<TelemetryThresholdRule>();
        while (reader.Read())
        {
            rules.Add(new TelemetryThresholdRule(
                ReadString(reader, "point_code"),
                ReadString(reader, "metric_code"),
                ParseEnum<ThresholdDirection>(reader, "direction"),
                ReadNullableDecimal(reader, "warning_min"),
                ReadNullableDecimal(reader, "warning_max"),
                ReadNullableDecimal(reader, "critical_min"),
                ReadNullableDecimal(reader, "critical_max"),
                ReadString(reader, "rule_summary")));
        }

        return rules;
    }

    private static IReadOnlyList<TelemetryReading> LoadReadings(SqliteConnection connection)
    {
        using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT point_code, metric_code, value, unit, collected_at, risk_level, rule_summary
            FROM iot_telemetry_readings
            ORDER BY collected_at DESC, id DESC;
            """;

        using var reader = command.ExecuteReader();
        var readings = new List<TelemetryReading>();
        while (reader.Read())
        {
            readings.Add(new TelemetryReading(
                ReadString(reader, "point_code"),
                ReadString(reader, "metric_code"),
                ReadDecimal(reader, "value"),
                ReadString(reader, "unit"),
                ReadDateTimeOffset(reader, "collected_at"),
                ParseEnum<TelemetryRiskLevel>(reader, "risk_level"),
                ReadString(reader, "rule_summary")));
        }

        return readings;
    }

    private static void UpsertSystem(SqliteConnection connection, IotSystemProfile system)
    {
        using var command = connection.CreateCommand();
        command.CommandText = """
            INSERT INTO iot_system_profiles (
                category, name, subsystems, roles, endpoints
            )
            VALUES (
                $category, $name, $subsystems, $roles, $endpoints
            )
            ON CONFLICT(category) DO UPDATE SET
                name = excluded.name,
                subsystems = excluded.subsystems,
                roles = excluded.roles,
                endpoints = excluded.endpoints;
            """;
        command.Parameters.AddWithValue("$category", system.Category.ToString());
        command.Parameters.AddWithValue("$name", system.Name);
        command.Parameters.AddWithValue("$subsystems", Serialize(system.Subsystems));
        command.Parameters.AddWithValue("$roles", Serialize(system.Roles));
        command.Parameters.AddWithValue("$endpoints", Serialize(system.Endpoints));
        command.ExecuteNonQuery();
    }

    private static void UpsertPoint(SqliteConnection connection, IotMonitoringPoint point)
    {
        using var command = connection.CreateCommand();
        command.CommandText = """
            INSERT INTO iot_monitoring_points (
                point_code, name, category, campus, building, floor, room,
                bim_element_id, device_code, protocol_adapter, metrics, source_evidence
            )
            VALUES (
                $point_code, $name, $category, $campus, $building, $floor, $room,
                $bim_element_id, $device_code, $protocol_adapter, $metrics, $source_evidence
            )
            ON CONFLICT(point_code) DO UPDATE SET
                name = excluded.name,
                category = excluded.category,
                campus = excluded.campus,
                building = excluded.building,
                floor = excluded.floor,
                room = excluded.room,
                bim_element_id = excluded.bim_element_id,
                device_code = excluded.device_code,
                protocol_adapter = excluded.protocol_adapter,
                metrics = excluded.metrics,
                source_evidence = excluded.source_evidence;
            """;
        command.Parameters.AddWithValue("$point_code", point.PointCode);
        command.Parameters.AddWithValue("$name", point.Name);
        command.Parameters.AddWithValue("$category", point.Category.ToString());
        AddLocationParameters(command, point.Location);
        command.Parameters.AddWithValue("$device_code", point.DeviceCode);
        command.Parameters.AddWithValue("$protocol_adapter", point.ProtocolAdapter);
        command.Parameters.AddWithValue("$metrics", Serialize(point.Metrics));
        command.Parameters.AddWithValue("$source_evidence", Serialize(point.SourceEvidence));
        command.ExecuteNonQuery();
    }

    private static void UpsertThresholdRule(SqliteConnection connection, TelemetryThresholdRule rule)
    {
        using var command = connection.CreateCommand();
        command.CommandText = """
            INSERT INTO iot_threshold_rules (
                point_code, metric_code, direction, warning_min, warning_max,
                critical_min, critical_max, rule_summary
            )
            VALUES (
                $point_code, $metric_code, $direction, $warning_min, $warning_max,
                $critical_min, $critical_max, $rule_summary
            )
            ON CONFLICT(point_code, metric_code) DO UPDATE SET
                direction = excluded.direction,
                warning_min = excluded.warning_min,
                warning_max = excluded.warning_max,
                critical_min = excluded.critical_min,
                critical_max = excluded.critical_max,
                rule_summary = excluded.rule_summary;
            """;
        command.Parameters.AddWithValue("$point_code", rule.PointCode);
        command.Parameters.AddWithValue("$metric_code", rule.MetricCode);
        command.Parameters.AddWithValue("$direction", rule.Direction.ToString());
        command.Parameters.AddWithValue("$warning_min", ToDbDecimal(rule.WarningMin));
        command.Parameters.AddWithValue("$warning_max", ToDbDecimal(rule.WarningMax));
        command.Parameters.AddWithValue("$critical_min", ToDbDecimal(rule.CriticalMin));
        command.Parameters.AddWithValue("$critical_max", ToDbDecimal(rule.CriticalMax));
        command.Parameters.AddWithValue("$rule_summary", rule.RuleSummary);
        command.ExecuteNonQuery();
    }

    private static void InsertReading(SqliteConnection connection, TelemetryReading reading)
    {
        using var command = connection.CreateCommand();
        command.CommandText = """
            INSERT OR IGNORE INTO iot_telemetry_readings (
                point_code, metric_code, value, unit, collected_at, risk_level, rule_summary
            )
            VALUES (
                $point_code, $metric_code, $value, $unit, $collected_at, $risk_level, $rule_summary
            );
            """;
        command.Parameters.AddWithValue("$point_code", reading.PointCode);
        command.Parameters.AddWithValue("$metric_code", reading.MetricCode);
        command.Parameters.AddWithValue("$value", ToStorage(reading.Value));
        command.Parameters.AddWithValue("$unit", reading.Unit);
        command.Parameters.AddWithValue("$collected_at", ToStorage(reading.CollectedAt));
        command.Parameters.AddWithValue("$risk_level", reading.RiskLevel.ToString());
        command.Parameters.AddWithValue("$rule_summary", reading.RuleSummary);
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

    private static string? ReadNullableString(SqliteDataReader reader, string name)
    {
        var ordinal = reader.GetOrdinal(name);
        return reader.IsDBNull(ordinal) ? null : reader.GetString(ordinal);
    }

    private static DateTimeOffset ReadDateTimeOffset(SqliteDataReader reader, string name) =>
        DateTimeOffset.Parse(ReadString(reader, name), CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind);

    private static decimal ReadDecimal(SqliteDataReader reader, string name) =>
        decimal.Parse(ReadString(reader, name), CultureInfo.InvariantCulture);

    private static decimal? ReadNullableDecimal(SqliteDataReader reader, string name)
    {
        var value = ReadNullableString(reader, name);
        return string.IsNullOrWhiteSpace(value) ? null : decimal.Parse(value, CultureInfo.InvariantCulture);
    }

    private static TEnum ParseEnum<TEnum>(SqliteDataReader reader, string name)
        where TEnum : struct =>
        Enum.Parse<TEnum>(ReadString(reader, name), ignoreCase: true);

    private static object ToDbDecimal(decimal? value) =>
        value is null ? DBNull.Value : ToStorage(value.Value);

    private static string ToStorage(decimal value) =>
        value.ToString(CultureInfo.InvariantCulture);

    private static string ToStorage(DateTimeOffset value) =>
        value.ToString("O", CultureInfo.InvariantCulture);

    private static string Serialize<T>(T value) =>
        JsonSerializer.Serialize(value, JsonOptions);

    private static T Deserialize<T>(string value) =>
        JsonSerializer.Deserialize<T>(value, JsonOptions)
            ?? throw new InvalidOperationException("Stored IoT JSON could not be deserialized.");
}
