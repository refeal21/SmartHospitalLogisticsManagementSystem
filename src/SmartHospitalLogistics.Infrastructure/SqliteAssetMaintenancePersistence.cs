using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Data.Sqlite;
using SmartHospitalLogistics.Application;
using SmartHospitalLogistics.Domain;

namespace SmartHospitalLogistics.Infrastructure;

public sealed class SqliteAssetMaintenancePersistence : IAssetMaintenancePersistence
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };

    private readonly string _connectionString;
    private readonly object _sync = new();

    public SqliteAssetMaintenancePersistence(string dbPath)
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

    public AssetMaintenancePersistenceSnapshot Load()
    {
        lock (_sync)
        {
            using var connection = OpenConnection();
            return new AssetMaintenancePersistenceSnapshot(
                LoadAssets(connection),
                LoadPlans(connection),
                LoadTasks(connection),
                LoadLifecycleEvents(connection));
        }
    }

    public void SaveAsset(AssetLedgerItem asset)
    {
        lock (_sync)
        {
            using var connection = OpenConnection();
            UpsertAsset(connection, null, asset);
        }
    }

    public void SavePlan(MaintenancePlan plan)
    {
        lock (_sync)
        {
            using var connection = OpenConnection();
            UpsertPlan(connection, null, plan);
        }
    }

    public void SaveTask(MaintenanceTask task)
    {
        lock (_sync)
        {
            using var connection = OpenConnection();
            UpsertTask(connection, null, task);
        }
    }

    public void SaveLifecycleEvent(AssetLifecycleEvent lifecycleEvent)
    {
        lock (_sync)
        {
            using var connection = OpenConnection();
            InsertLifecycleEvent(connection, null, lifecycleEvent);
        }
    }

    public void SaveTaskCompletion(MaintenanceTask task, AssetLifecycleEvent lifecycleEvent)
    {
        lock (_sync)
        {
            using var connection = OpenConnection();
            using var transaction = connection.BeginTransaction();
            UpsertTask(connection, transaction, task);
            InsertLifecycleEvent(connection, transaction, lifecycleEvent);
            transaction.Commit();
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
            CREATE TABLE IF NOT EXISTS asset_ledger_items (
                asset_code TEXT PRIMARY KEY,
                name TEXT NOT NULL,
                system TEXT NOT NULL,
                criticality TEXT NOT NULL,
                campus TEXT NOT NULL,
                building TEXT NOT NULL,
                floor TEXT NOT NULL,
                room TEXT NOT NULL,
                bim_element_id TEXT NOT NULL,
                status TEXT NOT NULL,
                owner_team TEXT NOT NULL,
                manufacturer TEXT NOT NULL,
                model TEXT NOT NULL,
                commissioned_on TEXT NOT NULL,
                maintenance_strategy TEXT NOT NULL,
                health_score INTEGER NOT NULL,
                current_risk TEXT NOT NULL,
                source_tags TEXT NOT NULL
            );

            CREATE TABLE IF NOT EXISTS asset_maintenance_plans (
                plan_code TEXT PRIMARY KEY,
                asset_code TEXT NOT NULL,
                name TEXT NOT NULL,
                task_type TEXT NOT NULL,
                cycle_days INTEGER NOT NULL,
                next_due_at TEXT NOT NULL,
                responsible_team TEXT NOT NULL,
                checklist_template TEXT NOT NULL,
                source_evidence TEXT NOT NULL,
                FOREIGN KEY (asset_code) REFERENCES asset_ledger_items(asset_code)
            );

            CREATE TABLE IF NOT EXISTS asset_maintenance_tasks (
                task_no TEXT PRIMARY KEY,
                plan_code TEXT NOT NULL,
                asset_code TEXT NOT NULL,
                title TEXT NOT NULL,
                task_type TEXT NOT NULL,
                status TEXT NOT NULL,
                priority TEXT NOT NULL,
                scheduled_at TEXT NOT NULL,
                due_at TEXT NOT NULL,
                responsible_team TEXT NOT NULL,
                checklist_results TEXT NOT NULL,
                outcome TEXT NULL,
                completed_at TEXT NULL,
                completed_by TEXT NULL,
                work_order_no TEXT NULL,
                FOREIGN KEY (asset_code) REFERENCES asset_ledger_items(asset_code),
                FOREIGN KEY (plan_code) REFERENCES asset_maintenance_plans(plan_code)
            );

            CREATE TABLE IF NOT EXISTS asset_lifecycle_events (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                occurred_at TEXT NOT NULL,
                asset_code TEXT NOT NULL,
                event_type TEXT NOT NULL,
                operator_name TEXT NOT NULL,
                summary TEXT NOT NULL,
                FOREIGN KEY (asset_code) REFERENCES asset_ledger_items(asset_code)
            );

            CREATE INDEX IF NOT EXISTS ix_asset_ledger_items_system_status
                ON asset_ledger_items(system, status);

            CREATE INDEX IF NOT EXISTS ix_asset_maintenance_plans_asset_due
                ON asset_maintenance_plans(asset_code, next_due_at);

            CREATE INDEX IF NOT EXISTS ix_asset_maintenance_tasks_status_due
                ON asset_maintenance_tasks(status, due_at);

            CREATE INDEX IF NOT EXISTS ix_asset_lifecycle_events_asset_time
                ON asset_lifecycle_events(asset_code, occurred_at, id);

            CREATE UNIQUE INDEX IF NOT EXISTS ux_asset_lifecycle_events_deduplicate
                ON asset_lifecycle_events(asset_code, occurred_at, event_type, operator_name, summary);
            """;
        command.ExecuteNonQuery();
    }

    private static IReadOnlyList<AssetLedgerItem> LoadAssets(SqliteConnection connection)
    {
        using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT asset_code, name, system, criticality, campus, building, floor,
                   room, bim_element_id, status, owner_team, manufacturer, model,
                   commissioned_on, maintenance_strategy, health_score, current_risk,
                   source_tags
            FROM asset_ledger_items
            ORDER BY system, asset_code;
            """;

        using var reader = command.ExecuteReader();
        var assets = new List<AssetLedgerItem>();
        while (reader.Read())
        {
            assets.Add(new AssetLedgerItem(
                ReadString(reader, "asset_code"),
                ReadString(reader, "name"),
                ReadString(reader, "system"),
                ParseEnum<AssetCriticality>(reader, "criticality"),
                ReadLocation(reader),
                ParseEnum<FacilityStatus>(reader, "status"),
                ReadString(reader, "owner_team"),
                ReadString(reader, "manufacturer"),
                ReadString(reader, "model"),
                ReadString(reader, "commissioned_on"),
                ReadString(reader, "maintenance_strategy"),
                reader.GetInt32(reader.GetOrdinal("health_score")),
                ReadString(reader, "current_risk"),
                Deserialize<string[]>(ReadString(reader, "source_tags"))));
        }

        return assets;
    }

    private static IReadOnlyList<MaintenancePlan> LoadPlans(SqliteConnection connection)
    {
        using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT plan_code, asset_code, name, task_type, cycle_days, next_due_at,
                   responsible_team, checklist_template, source_evidence
            FROM asset_maintenance_plans
            ORDER BY asset_code, next_due_at;
            """;

        using var reader = command.ExecuteReader();
        var plans = new List<MaintenancePlan>();
        while (reader.Read())
        {
            plans.Add(new MaintenancePlan(
                ReadString(reader, "plan_code"),
                ReadString(reader, "asset_code"),
                ReadString(reader, "name"),
                ParseEnum<MaintenanceTaskType>(reader, "task_type"),
                reader.GetInt32(reader.GetOrdinal("cycle_days")),
                ReadDateTimeOffset(reader, "next_due_at"),
                ReadString(reader, "responsible_team"),
                Deserialize<InspectionChecklistItem[]>(ReadString(reader, "checklist_template")),
                Deserialize<FeatureEvidence[]>(ReadString(reader, "source_evidence"))));
        }

        return plans;
    }

    private static IReadOnlyList<MaintenanceTask> LoadTasks(SqliteConnection connection)
    {
        using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT task_no, plan_code, asset_code, title, task_type, status, priority,
                   scheduled_at, due_at, responsible_team, checklist_results, outcome,
                   completed_at, completed_by, work_order_no
            FROM asset_maintenance_tasks
            ORDER BY scheduled_at, task_no;
            """;

        using var reader = command.ExecuteReader();
        var tasks = new List<MaintenanceTask>();
        while (reader.Read())
        {
            tasks.Add(new MaintenanceTask(
                ReadString(reader, "task_no"),
                ReadString(reader, "plan_code"),
                ReadString(reader, "asset_code"),
                ReadString(reader, "title"),
                ParseEnum<MaintenanceTaskType>(reader, "task_type"),
                ParseEnum<MaintenanceTaskStatus>(reader, "status"),
                ParseEnum<Priority>(reader, "priority"),
                ReadDateTimeOffset(reader, "scheduled_at"),
                ReadDateTimeOffset(reader, "due_at"),
                ReadString(reader, "responsible_team"),
                Deserialize<InspectionChecklistResult[]>(ReadString(reader, "checklist_results")),
                ParseNullableEnum<MaintenanceOutcome>(reader, "outcome"),
                ReadNullableDateTimeOffset(reader, "completed_at"),
                ReadNullableString(reader, "completed_by"),
                ReadNullableString(reader, "work_order_no")));
        }

        return tasks;
    }

    private static IReadOnlyList<AssetLifecycleEvent> LoadLifecycleEvents(SqliteConnection connection)
    {
        using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT occurred_at, asset_code, event_type, operator_name, summary
            FROM asset_lifecycle_events
            ORDER BY occurred_at, id;
            """;

        using var reader = command.ExecuteReader();
        var events = new List<AssetLifecycleEvent>();
        while (reader.Read())
        {
            events.Add(new AssetLifecycleEvent(
                ReadDateTimeOffset(reader, "occurred_at"),
                ReadString(reader, "asset_code"),
                ReadString(reader, "event_type"),
                ReadString(reader, "operator_name"),
                ReadString(reader, "summary")));
        }

        return events;
    }

    private static void UpsertAsset(SqliteConnection connection, SqliteTransaction? transaction, AssetLedgerItem asset)
    {
        using var command = CreateCommand(connection, transaction, """
            INSERT INTO asset_ledger_items (
                asset_code, name, system, criticality, campus, building, floor, room,
                bim_element_id, status, owner_team, manufacturer, model, commissioned_on,
                maintenance_strategy, health_score, current_risk, source_tags
            )
            VALUES (
                $asset_code, $name, $system, $criticality, $campus, $building, $floor, $room,
                $bim_element_id, $status, $owner_team, $manufacturer, $model, $commissioned_on,
                $maintenance_strategy, $health_score, $current_risk, $source_tags
            )
            ON CONFLICT(asset_code) DO UPDATE SET
                name = excluded.name,
                system = excluded.system,
                criticality = excluded.criticality,
                campus = excluded.campus,
                building = excluded.building,
                floor = excluded.floor,
                room = excluded.room,
                bim_element_id = excluded.bim_element_id,
                status = excluded.status,
                owner_team = excluded.owner_team,
                manufacturer = excluded.manufacturer,
                model = excluded.model,
                commissioned_on = excluded.commissioned_on,
                maintenance_strategy = excluded.maintenance_strategy,
                health_score = excluded.health_score,
                current_risk = excluded.current_risk,
                source_tags = excluded.source_tags;
            """);
        command.Parameters.AddWithValue("$asset_code", asset.AssetCode);
        command.Parameters.AddWithValue("$name", asset.Name);
        command.Parameters.AddWithValue("$system", asset.System);
        command.Parameters.AddWithValue("$criticality", asset.Criticality.ToString());
        AddLocationParameters(command, asset.Location);
        command.Parameters.AddWithValue("$status", asset.Status.ToString());
        command.Parameters.AddWithValue("$owner_team", asset.OwnerTeam);
        command.Parameters.AddWithValue("$manufacturer", asset.Manufacturer);
        command.Parameters.AddWithValue("$model", asset.Model);
        command.Parameters.AddWithValue("$commissioned_on", asset.CommissionedOn);
        command.Parameters.AddWithValue("$maintenance_strategy", asset.MaintenanceStrategy);
        command.Parameters.AddWithValue("$health_score", asset.HealthScore);
        command.Parameters.AddWithValue("$current_risk", asset.CurrentRisk);
        command.Parameters.AddWithValue("$source_tags", Serialize(asset.SourceTags));
        command.ExecuteNonQuery();
    }

    private static void UpsertPlan(SqliteConnection connection, SqliteTransaction? transaction, MaintenancePlan plan)
    {
        using var command = CreateCommand(connection, transaction, """
            INSERT INTO asset_maintenance_plans (
                plan_code, asset_code, name, task_type, cycle_days, next_due_at,
                responsible_team, checklist_template, source_evidence
            )
            VALUES (
                $plan_code, $asset_code, $name, $task_type, $cycle_days, $next_due_at,
                $responsible_team, $checklist_template, $source_evidence
            )
            ON CONFLICT(plan_code) DO UPDATE SET
                asset_code = excluded.asset_code,
                name = excluded.name,
                task_type = excluded.task_type,
                cycle_days = excluded.cycle_days,
                next_due_at = excluded.next_due_at,
                responsible_team = excluded.responsible_team,
                checklist_template = excluded.checklist_template,
                source_evidence = excluded.source_evidence;
            """);
        command.Parameters.AddWithValue("$plan_code", plan.PlanCode);
        command.Parameters.AddWithValue("$asset_code", plan.AssetCode);
        command.Parameters.AddWithValue("$name", plan.Name);
        command.Parameters.AddWithValue("$task_type", plan.TaskType.ToString());
        command.Parameters.AddWithValue("$cycle_days", plan.CycleDays);
        command.Parameters.AddWithValue("$next_due_at", ToStorage(plan.NextDueAt));
        command.Parameters.AddWithValue("$responsible_team", plan.ResponsibleTeam);
        command.Parameters.AddWithValue("$checklist_template", Serialize(plan.ChecklistTemplate));
        command.Parameters.AddWithValue("$source_evidence", Serialize(plan.SourceEvidence));
        command.ExecuteNonQuery();
    }

    private static void UpsertTask(SqliteConnection connection, SqliteTransaction? transaction, MaintenanceTask task)
    {
        using var command = CreateCommand(connection, transaction, """
            INSERT INTO asset_maintenance_tasks (
                task_no, plan_code, asset_code, title, task_type, status, priority,
                scheduled_at, due_at, responsible_team, checklist_results, outcome,
                completed_at, completed_by, work_order_no
            )
            VALUES (
                $task_no, $plan_code, $asset_code, $title, $task_type, $status, $priority,
                $scheduled_at, $due_at, $responsible_team, $checklist_results, $outcome,
                $completed_at, $completed_by, $work_order_no
            )
            ON CONFLICT(task_no) DO UPDATE SET
                plan_code = excluded.plan_code,
                asset_code = excluded.asset_code,
                title = excluded.title,
                task_type = excluded.task_type,
                status = excluded.status,
                priority = excluded.priority,
                scheduled_at = excluded.scheduled_at,
                due_at = excluded.due_at,
                responsible_team = excluded.responsible_team,
                checklist_results = excluded.checklist_results,
                outcome = excluded.outcome,
                completed_at = excluded.completed_at,
                completed_by = excluded.completed_by,
                work_order_no = excluded.work_order_no;
            """);
        command.Parameters.AddWithValue("$task_no", task.TaskNo);
        command.Parameters.AddWithValue("$plan_code", task.PlanCode);
        command.Parameters.AddWithValue("$asset_code", task.AssetCode);
        command.Parameters.AddWithValue("$title", task.Title);
        command.Parameters.AddWithValue("$task_type", task.TaskType.ToString());
        command.Parameters.AddWithValue("$status", task.Status.ToString());
        command.Parameters.AddWithValue("$priority", task.Priority.ToString());
        command.Parameters.AddWithValue("$scheduled_at", ToStorage(task.ScheduledAt));
        command.Parameters.AddWithValue("$due_at", ToStorage(task.DueAt));
        command.Parameters.AddWithValue("$responsible_team", task.ResponsibleTeam);
        command.Parameters.AddWithValue("$checklist_results", Serialize(task.ChecklistResults));
        command.Parameters.AddWithValue("$outcome", task.Outcome is null ? DBNull.Value : task.Outcome.Value.ToString());
        command.Parameters.AddWithValue("$completed_at", task.CompletedAt is null ? DBNull.Value : ToStorage(task.CompletedAt.Value));
        command.Parameters.AddWithValue("$completed_by", task.CompletedBy is null ? DBNull.Value : task.CompletedBy);
        command.Parameters.AddWithValue("$work_order_no", task.WorkOrderNo is null ? DBNull.Value : task.WorkOrderNo);
        command.ExecuteNonQuery();
    }

    private static void InsertLifecycleEvent(
        SqliteConnection connection,
        SqliteTransaction? transaction,
        AssetLifecycleEvent lifecycleEvent)
    {
        using var command = CreateCommand(connection, transaction, """
            INSERT OR IGNORE INTO asset_lifecycle_events (
                occurred_at, asset_code, event_type, operator_name, summary
            )
            VALUES (
                $occurred_at, $asset_code, $event_type, $operator_name, $summary
            );
            """);
        command.Parameters.AddWithValue("$occurred_at", ToStorage(lifecycleEvent.OccurredAt));
        command.Parameters.AddWithValue("$asset_code", lifecycleEvent.AssetCode);
        command.Parameters.AddWithValue("$event_type", lifecycleEvent.EventType);
        command.Parameters.AddWithValue("$operator_name", lifecycleEvent.Operator);
        command.Parameters.AddWithValue("$summary", lifecycleEvent.Summary);
        command.ExecuteNonQuery();
    }

    private static SqliteCommand CreateCommand(SqliteConnection connection, SqliteTransaction? transaction, string commandText)
    {
        var command = connection.CreateCommand();
        command.CommandText = commandText;
        command.Transaction = transaction;
        return command;
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

    private static DateTimeOffset? ReadNullableDateTimeOffset(SqliteDataReader reader, string name)
    {
        var value = ReadNullableString(reader, name);
        return value is null ? null : DateTimeOffset.Parse(value, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind);
    }

    private static DateTimeOffset ReadDateTimeOffset(SqliteDataReader reader, string name) =>
        DateTimeOffset.Parse(ReadString(reader, name), CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind);

    private static TEnum ParseEnum<TEnum>(SqliteDataReader reader, string name)
        where TEnum : struct =>
        Enum.Parse<TEnum>(ReadString(reader, name), ignoreCase: true);

    private static TEnum? ParseNullableEnum<TEnum>(SqliteDataReader reader, string name)
        where TEnum : struct
    {
        var value = ReadNullableString(reader, name);
        return string.IsNullOrWhiteSpace(value) ? null : Enum.Parse<TEnum>(value, ignoreCase: true);
    }

    private static string ToStorage(DateTimeOffset value) =>
        value.ToString("O", CultureInfo.InvariantCulture);

    private static string Serialize<T>(T value) =>
        JsonSerializer.Serialize(value, JsonOptions);

    private static T Deserialize<T>(string value) =>
        JsonSerializer.Deserialize<T>(value, JsonOptions)
            ?? throw new InvalidOperationException("Stored asset maintenance JSON could not be deserialized.");
}
