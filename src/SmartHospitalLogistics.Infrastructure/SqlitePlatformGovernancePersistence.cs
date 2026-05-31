using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Data.Sqlite;
using SmartHospitalLogistics.Application;
using SmartHospitalLogistics.Domain;

namespace SmartHospitalLogistics.Infrastructure;

public sealed class SqlitePlatformGovernancePersistence : IPlatformGovernancePersistence
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };

    private readonly string _connectionString;
    private readonly object _sync = new();

    public SqlitePlatformGovernancePersistence(string dbPath)
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

    public PlatformGovernancePersistenceSnapshot Load()
    {
        lock (_sync)
        {
            using var connection = OpenConnection();
            return new PlatformGovernancePersistenceSnapshot(
                LoadControls(connection),
                LoadActions(connection),
                LoadAuditTrail(connection));
        }
    }

    public void SaveControl(PlatformGovernanceControl control)
    {
        lock (_sync)
        {
            using var connection = OpenConnection();
            using var command = connection.CreateCommand();
            command.CommandText = """
                INSERT INTO platform_governance_controls (
                    control_code, name, control_type, owner_role, metric_name,
                    target_value, current_value, unit, status, due_at,
                    related_module, source_evidence
                )
                VALUES (
                    $control_code, $name, $control_type, $owner_role, $metric_name,
                    $target_value, $current_value, $unit, $status, $due_at,
                    $related_module, $source_evidence
                )
                ON CONFLICT(control_code) DO UPDATE SET
                    name = excluded.name,
                    control_type = excluded.control_type,
                    owner_role = excluded.owner_role,
                    metric_name = excluded.metric_name,
                    target_value = excluded.target_value,
                    current_value = excluded.current_value,
                    unit = excluded.unit,
                    status = excluded.status,
                    due_at = excluded.due_at,
                    related_module = excluded.related_module,
                    source_evidence = excluded.source_evidence;
                """;
            command.Parameters.AddWithValue("$control_code", control.ControlCode);
            command.Parameters.AddWithValue("$name", control.Name);
            command.Parameters.AddWithValue("$control_type", control.ControlType.ToString());
            command.Parameters.AddWithValue("$owner_role", control.OwnerRole);
            command.Parameters.AddWithValue("$metric_name", control.MetricName);
            command.Parameters.AddWithValue("$target_value", control.TargetValue);
            command.Parameters.AddWithValue("$current_value", control.CurrentValue);
            command.Parameters.AddWithValue("$unit", control.Unit);
            command.Parameters.AddWithValue("$status", control.Status.ToString());
            command.Parameters.AddWithValue("$due_at", control.DueAt.ToString("O"));
            command.Parameters.AddWithValue("$related_module", control.RelatedModule);
            command.Parameters.AddWithValue("$source_evidence", Serialize(control.SourceEvidence));
            command.ExecuteNonQuery();
        }
    }

    public void SaveAction(PlatformGovernanceAction action)
    {
        lock (_sync)
        {
            using var connection = OpenConnection();
            using var command = connection.CreateCommand();
            command.CommandText = """
                INSERT INTO platform_governance_actions (
                    action_code, control_code, title, responsible_role, recorded_by,
                    status, created_at, due_at, related_work_order_no
                )
                VALUES (
                    $action_code, $control_code, $title, $responsible_role, $recorded_by,
                    $status, $created_at, $due_at, $related_work_order_no
                )
                ON CONFLICT(action_code) DO UPDATE SET
                    control_code = excluded.control_code,
                    title = excluded.title,
                    responsible_role = excluded.responsible_role,
                    recorded_by = excluded.recorded_by,
                    status = excluded.status,
                    created_at = excluded.created_at,
                    due_at = excluded.due_at,
                    related_work_order_no = excluded.related_work_order_no;
                """;
            command.Parameters.AddWithValue("$action_code", action.ActionCode);
            command.Parameters.AddWithValue("$control_code", action.ControlCode);
            command.Parameters.AddWithValue("$title", action.Title);
            command.Parameters.AddWithValue("$responsible_role", action.ResponsibleRole);
            command.Parameters.AddWithValue("$recorded_by", action.RecordedBy);
            command.Parameters.AddWithValue("$status", action.Status.ToString());
            command.Parameters.AddWithValue("$created_at", action.CreatedAt.ToString("O"));
            command.Parameters.AddWithValue("$due_at", action.DueAt.ToString("O"));
            command.Parameters.AddWithValue("$related_work_order_no", (object?)action.RelatedWorkOrderNo ?? DBNull.Value);
            command.ExecuteNonQuery();
        }
    }

    public void SaveAuditEntry(PlatformGovernanceAuditEntry entry)
    {
        lock (_sync)
        {
            using var connection = OpenConnection();
            using var command = connection.CreateCommand();
            command.CommandText = """
                INSERT INTO platform_governance_audit_trail (
                    audit_code, control_code, operation, actor, occurred_at, summary
                )
                VALUES (
                    $audit_code, $control_code, $operation, $actor, $occurred_at, $summary
                )
                ON CONFLICT(audit_code) DO UPDATE SET
                    control_code = excluded.control_code,
                    operation = excluded.operation,
                    actor = excluded.actor,
                    occurred_at = excluded.occurred_at,
                    summary = excluded.summary;
                """;
            command.Parameters.AddWithValue("$audit_code", entry.AuditCode);
            command.Parameters.AddWithValue("$control_code", entry.ControlCode);
            command.Parameters.AddWithValue("$operation", entry.Operation);
            command.Parameters.AddWithValue("$actor", entry.Actor);
            command.Parameters.AddWithValue("$occurred_at", entry.OccurredAt.ToString("O"));
            command.Parameters.AddWithValue("$summary", entry.Summary);
            command.ExecuteNonQuery();
        }
    }

    private IReadOnlyList<PlatformGovernanceControl> LoadControls(SqliteConnection connection)
    {
        using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT control_code, name, control_type, owner_role, metric_name,
                   target_value, current_value, unit, status, due_at,
                   related_module, source_evidence
            FROM platform_governance_controls
            ORDER BY control_code;
            """;

        using var reader = command.ExecuteReader();
        var controls = new List<PlatformGovernanceControl>();
        while (reader.Read())
        {
            controls.Add(new PlatformGovernanceControl(
                ReadString(reader, "control_code"),
                ReadString(reader, "name"),
                ParseEnum<PlatformGovernanceControlType>(reader, "control_type"),
                ReadString(reader, "owner_role"),
                ReadString(reader, "metric_name"),
                ReadDecimal(reader, "target_value"),
                ReadDecimal(reader, "current_value"),
                ReadString(reader, "unit"),
                ParseEnum<PlatformGovernanceControlStatus>(reader, "status"),
                ReadDateTimeOffset(reader, "due_at"),
                ReadString(reader, "related_module"),
                Deserialize<FeatureEvidence[]>(ReadString(reader, "source_evidence"))));
        }

        return controls;
    }

    private IReadOnlyList<PlatformGovernanceAction> LoadActions(SqliteConnection connection)
    {
        using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT action_code, control_code, title, responsible_role, recorded_by,
                   status, created_at, due_at, related_work_order_no
            FROM platform_governance_actions
            ORDER BY due_at;
            """;

        using var reader = command.ExecuteReader();
        var actions = new List<PlatformGovernanceAction>();
        while (reader.Read())
        {
            actions.Add(new PlatformGovernanceAction(
                ReadString(reader, "action_code"),
                ReadString(reader, "control_code"),
                ReadString(reader, "title"),
                ReadString(reader, "responsible_role"),
                ReadString(reader, "recorded_by"),
                ParseEnum<GovernanceActionStatus>(reader, "status"),
                ReadDateTimeOffset(reader, "created_at"),
                ReadDateTimeOffset(reader, "due_at"),
                ReadNullableString(reader, "related_work_order_no")));
        }

        return actions;
    }

    private IReadOnlyList<PlatformGovernanceAuditEntry> LoadAuditTrail(SqliteConnection connection)
    {
        using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT audit_code, control_code, operation, actor, occurred_at, summary
            FROM platform_governance_audit_trail
            ORDER BY occurred_at;
            """;

        using var reader = command.ExecuteReader();
        var auditTrail = new List<PlatformGovernanceAuditEntry>();
        while (reader.Read())
        {
            auditTrail.Add(new PlatformGovernanceAuditEntry(
                ReadString(reader, "audit_code"),
                ReadString(reader, "control_code"),
                ReadString(reader, "operation"),
                ReadString(reader, "actor"),
                ReadDateTimeOffset(reader, "occurred_at"),
                ReadString(reader, "summary")));
        }

        return auditTrail;
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
            CREATE TABLE IF NOT EXISTS platform_governance_controls (
                control_code TEXT PRIMARY KEY,
                name TEXT NOT NULL,
                control_type TEXT NOT NULL,
                owner_role TEXT NOT NULL,
                metric_name TEXT NOT NULL,
                target_value REAL NOT NULL,
                current_value REAL NOT NULL,
                unit TEXT NOT NULL,
                status TEXT NOT NULL,
                due_at TEXT NOT NULL,
                related_module TEXT NOT NULL,
                source_evidence TEXT NOT NULL
            );

            CREATE TABLE IF NOT EXISTS platform_governance_actions (
                action_code TEXT PRIMARY KEY,
                control_code TEXT NOT NULL,
                title TEXT NOT NULL,
                responsible_role TEXT NOT NULL,
                recorded_by TEXT NOT NULL,
                status TEXT NOT NULL,
                created_at TEXT NOT NULL,
                due_at TEXT NOT NULL,
                related_work_order_no TEXT NULL
            );

            CREATE TABLE IF NOT EXISTS platform_governance_audit_trail (
                audit_code TEXT PRIMARY KEY,
                control_code TEXT NOT NULL,
                operation TEXT NOT NULL,
                actor TEXT NOT NULL,
                occurred_at TEXT NOT NULL,
                summary TEXT NOT NULL
            );

            CREATE INDEX IF NOT EXISTS ix_platform_governance_actions_control
                ON platform_governance_actions(control_code, status, due_at);

            CREATE INDEX IF NOT EXISTS ix_platform_governance_audit_control
                ON platform_governance_audit_trail(control_code, occurred_at);
            """;
        command.ExecuteNonQuery();
    }

    private static string ReadString(SqliteDataReader reader, string name) =>
        reader.GetString(reader.GetOrdinal(name));

    private static string? ReadNullableString(SqliteDataReader reader, string name)
    {
        var ordinal = reader.GetOrdinal(name);
        return reader.IsDBNull(ordinal) ? null : reader.GetString(ordinal);
    }

    private static decimal ReadDecimal(SqliteDataReader reader, string name) =>
        reader.GetDecimal(reader.GetOrdinal(name));

    private static DateTimeOffset ReadDateTimeOffset(SqliteDataReader reader, string name) =>
        DateTimeOffset.Parse(ReadString(reader, name));

    private static TEnum ParseEnum<TEnum>(SqliteDataReader reader, string name)
        where TEnum : struct, Enum =>
        Enum.Parse<TEnum>(ReadString(reader, name), ignoreCase: true);

    private static T Deserialize<T>(string value) =>
        JsonSerializer.Deserialize<T>(value, JsonOptions)
        ?? throw new InvalidOperationException($"Failed to deserialize {typeof(T).Name}.");

    private static string Serialize<T>(T value) =>
        JsonSerializer.Serialize(value, JsonOptions);
}
