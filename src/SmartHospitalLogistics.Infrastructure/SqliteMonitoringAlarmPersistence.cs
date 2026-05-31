using System.Globalization;
using Microsoft.Data.Sqlite;
using SmartHospitalLogistics.Application;
using SmartHospitalLogistics.Domain;

namespace SmartHospitalLogistics.Infrastructure;

public sealed class SqliteMonitoringAlarmPersistence : IMonitoringAlarmPersistence
{
    private readonly string _connectionString;
    private readonly object _sync = new();

    public SqliteMonitoringAlarmPersistence(string dbPath)
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

    public MonitoringAlarmPersistenceSnapshot Load()
    {
        lock (_sync)
        {
            using var connection = OpenConnection();
            using var command = connection.CreateCommand();
            command.CommandText = """
                SELECT alarm_no, point_code, metric_code, title, risk_level, status,
                       campus, building, floor, room, bim_element_id, value, unit,
                       triggered_at, rule_summary, responsible_team, work_order_no,
                       acknowledged_by, acknowledged_at, last_remark
                FROM iot_alarm_events
                ORDER BY triggered_at DESC, alarm_no;
                """;

            using var reader = command.ExecuteReader();
            var alarms = new List<MonitoringAlarmEvent>();
            while (reader.Read())
            {
                alarms.Add(new MonitoringAlarmEvent(
                    ReadString(reader, "alarm_no"),
                    ReadString(reader, "point_code"),
                    ReadString(reader, "metric_code"),
                    ReadString(reader, "title"),
                    ParseEnum<TelemetryRiskLevel>(reader, "risk_level"),
                    ParseEnum<MonitoringAlarmStatus>(reader, "status"),
                    ReadLocation(reader),
                    ReadDecimal(reader, "value"),
                    ReadString(reader, "unit"),
                    ReadDateTimeOffset(reader, "triggered_at"),
                    ReadString(reader, "rule_summary"),
                    ReadString(reader, "responsible_team"),
                    ReadNullableString(reader, "work_order_no"),
                    ReadNullableString(reader, "acknowledged_by"),
                    ReadNullableDateTimeOffset(reader, "acknowledged_at"),
                    ReadNullableString(reader, "last_remark")));
            }

            return new MonitoringAlarmPersistenceSnapshot(alarms);
        }
    }

    public void SaveAlarm(MonitoringAlarmEvent alarm)
    {
        lock (_sync)
        {
            using var connection = OpenConnection();
            using var command = connection.CreateCommand();
            command.CommandText = """
                INSERT INTO iot_alarm_events (
                    alarm_no, point_code, metric_code, title, risk_level, status,
                    campus, building, floor, room, bim_element_id, value, unit,
                    triggered_at, rule_summary, responsible_team, work_order_no,
                    acknowledged_by, acknowledged_at, last_remark
                )
                VALUES (
                    $alarm_no, $point_code, $metric_code, $title, $risk_level, $status,
                    $campus, $building, $floor, $room, $bim_element_id, $value, $unit,
                    $triggered_at, $rule_summary, $responsible_team, $work_order_no,
                    $acknowledged_by, $acknowledged_at, $last_remark
                )
                ON CONFLICT(alarm_no) DO UPDATE SET
                    point_code = excluded.point_code,
                    metric_code = excluded.metric_code,
                    title = excluded.title,
                    risk_level = excluded.risk_level,
                    status = excluded.status,
                    campus = excluded.campus,
                    building = excluded.building,
                    floor = excluded.floor,
                    room = excluded.room,
                    bim_element_id = excluded.bim_element_id,
                    value = excluded.value,
                    unit = excluded.unit,
                    triggered_at = excluded.triggered_at,
                    rule_summary = excluded.rule_summary,
                    responsible_team = excluded.responsible_team,
                    work_order_no = excluded.work_order_no,
                    acknowledged_by = excluded.acknowledged_by,
                    acknowledged_at = excluded.acknowledged_at,
                    last_remark = excluded.last_remark;
                """;
            command.Parameters.AddWithValue("$alarm_no", alarm.AlarmNo);
            command.Parameters.AddWithValue("$point_code", alarm.PointCode);
            command.Parameters.AddWithValue("$metric_code", alarm.MetricCode);
            command.Parameters.AddWithValue("$title", alarm.Title);
            command.Parameters.AddWithValue("$risk_level", alarm.RiskLevel.ToString());
            command.Parameters.AddWithValue("$status", alarm.Status.ToString());
            AddLocationParameters(command, alarm.Location);
            command.Parameters.AddWithValue("$value", ToStorage(alarm.Value));
            command.Parameters.AddWithValue("$unit", alarm.Unit);
            command.Parameters.AddWithValue("$triggered_at", ToStorage(alarm.TriggeredAt));
            command.Parameters.AddWithValue("$rule_summary", alarm.RuleSummary);
            command.Parameters.AddWithValue("$responsible_team", alarm.ResponsibleTeam);
            command.Parameters.AddWithValue("$work_order_no", alarm.WorkOrderNo is null ? DBNull.Value : alarm.WorkOrderNo);
            command.Parameters.AddWithValue("$acknowledged_by", alarm.AcknowledgedBy is null ? DBNull.Value : alarm.AcknowledgedBy);
            command.Parameters.AddWithValue("$acknowledged_at", alarm.AcknowledgedAt is null ? DBNull.Value : ToStorage(alarm.AcknowledgedAt.Value));
            command.Parameters.AddWithValue("$last_remark", alarm.LastRemark is null ? DBNull.Value : alarm.LastRemark);
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
            CREATE TABLE IF NOT EXISTS iot_alarm_events (
                alarm_no TEXT PRIMARY KEY,
                point_code TEXT NOT NULL,
                metric_code TEXT NOT NULL,
                title TEXT NOT NULL,
                risk_level TEXT NOT NULL,
                status TEXT NOT NULL,
                campus TEXT NOT NULL,
                building TEXT NOT NULL,
                floor TEXT NOT NULL,
                room TEXT NOT NULL,
                bim_element_id TEXT NOT NULL,
                value TEXT NOT NULL,
                unit TEXT NOT NULL,
                triggered_at TEXT NOT NULL,
                rule_summary TEXT NOT NULL,
                responsible_team TEXT NOT NULL,
                work_order_no TEXT NULL,
                acknowledged_by TEXT NULL,
                acknowledged_at TEXT NULL,
                last_remark TEXT NULL
            );

            CREATE INDEX IF NOT EXISTS ix_iot_alarm_events_status_risk
                ON iot_alarm_events(status, risk_level, triggered_at);

            CREATE INDEX IF NOT EXISTS ix_iot_alarm_events_point_metric
                ON iot_alarm_events(point_code, metric_code, triggered_at);
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

    private static string? ReadNullableString(SqliteDataReader reader, string name)
    {
        var ordinal = reader.GetOrdinal(name);
        return reader.IsDBNull(ordinal) ? null : reader.GetString(ordinal);
    }

    private static DateTimeOffset ReadDateTimeOffset(SqliteDataReader reader, string name) =>
        DateTimeOffset.Parse(ReadString(reader, name), CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind);

    private static DateTimeOffset? ReadNullableDateTimeOffset(SqliteDataReader reader, string name)
    {
        var value = ReadNullableString(reader, name);
        return value is null ? null : DateTimeOffset.Parse(value, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind);
    }

    private static decimal ReadDecimal(SqliteDataReader reader, string name) =>
        decimal.Parse(ReadString(reader, name), CultureInfo.InvariantCulture);

    private static TEnum ParseEnum<TEnum>(SqliteDataReader reader, string name)
        where TEnum : struct =>
        Enum.Parse<TEnum>(ReadString(reader, name), ignoreCase: true);

    private static string ToStorage(decimal value) =>
        value.ToString(CultureInfo.InvariantCulture);

    private static string ToStorage(DateTimeOffset value) =>
        value.ToString("O", CultureInfo.InvariantCulture);
}
