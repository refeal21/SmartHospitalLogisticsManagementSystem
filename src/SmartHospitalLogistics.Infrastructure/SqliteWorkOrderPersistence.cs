using System.Globalization;
using Microsoft.Data.Sqlite;
using SmartHospitalLogistics.Application;
using SmartHospitalLogistics.Domain;

namespace SmartHospitalLogistics.Infrastructure;

public sealed class SqliteWorkOrderPersistence : IWorkOrderPersistence
{
    private readonly string _connectionString;
    private readonly object _sync = new();

    public SqliteWorkOrderPersistence(string dbPath)
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

    public WorkOrderPersistenceSnapshot Load()
    {
        lock (_sync)
        {
            using var connection = OpenConnection();
            var workOrders = LoadWorkOrders(connection);
            var serviceRequests = LoadServiceRequests(connection);
            var timeline = LoadTimeline(connection);

            return new WorkOrderPersistenceSnapshot(workOrders, serviceRequests, timeline);
        }
    }

    public void SaveServiceRequest(ServiceRequest request)
    {
        lock (_sync)
        {
            using var connection = OpenConnection();
            UpsertServiceRequest(connection, null, request);
        }
    }

    public void SaveWorkOrder(WorkOrder workOrder)
    {
        lock (_sync)
        {
            using var connection = OpenConnection();
            UpsertWorkOrder(connection, null, workOrder);
        }
    }

    public void SaveTimelineEntry(string workOrderNo, WorkOrderTimelineEntry entry)
    {
        lock (_sync)
        {
            using var connection = OpenConnection();
            InsertTimelineEntry(connection, null, workOrderNo, entry);
        }
    }

    public void SaveWorkOrderTransition(WorkOrder workOrder, WorkOrderTimelineEntry entry)
    {
        lock (_sync)
        {
            using var connection = OpenConnection();
            using var transaction = connection.BeginTransaction();
            UpsertWorkOrder(connection, transaction, workOrder);
            InsertTimelineEntry(connection, transaction, workOrder.WorkOrderNo, entry);
            transaction.Commit();
        }
    }

    public void SaveServiceRequestConversion(ServiceRequest request, WorkOrder workOrder, WorkOrderTimelineEntry entry)
    {
        lock (_sync)
        {
            using var connection = OpenConnection();
            using var transaction = connection.BeginTransaction();
            UpsertWorkOrder(connection, transaction, workOrder);
            InsertTimelineEntry(connection, transaction, workOrder.WorkOrderNo, entry);
            UpsertServiceRequest(connection, transaction, request);
            transaction.Commit();
        }
    }

    private static void UpsertServiceRequest(SqliteConnection connection, SqliteTransaction? transaction, ServiceRequest request)
    {
        using var command = CreateCommand(connection, transaction, """
            INSERT INTO wo_service_requests (
                request_no, source_type, requester_name, requester_department,
                service_type, priority, description, campus, building, floor,
                room, bim_element_id, status, created_at, converted_work_order_no
            )
            VALUES (
                $request_no, $source_type, $requester_name, $requester_department,
                $service_type, $priority, $description, $campus, $building, $floor,
                $room, $bim_element_id, $status, $created_at, $converted_work_order_no
            )
            ON CONFLICT(request_no) DO UPDATE SET
                source_type = excluded.source_type,
                requester_name = excluded.requester_name,
                requester_department = excluded.requester_department,
                service_type = excluded.service_type,
                priority = excluded.priority,
                description = excluded.description,
                campus = excluded.campus,
                building = excluded.building,
                floor = excluded.floor,
                room = excluded.room,
                bim_element_id = excluded.bim_element_id,
                status = excluded.status,
                created_at = excluded.created_at,
                converted_work_order_no = excluded.converted_work_order_no;
            """);
        AddServiceRequestParameters(command, request);
        command.ExecuteNonQuery();
    }

    private static void UpsertWorkOrder(SqliteConnection connection, SqliteTransaction? transaction, WorkOrder workOrder)
    {
        using var command = CreateCommand(connection, transaction, """
            INSERT INTO wo_work_orders (
                work_order_no, title, service_type, priority, status, campus,
                building, floor, room, bim_element_id, responsible_team,
                created_at, sla_due_at
            )
            VALUES (
                $work_order_no, $title, $service_type, $priority, $status, $campus,
                $building, $floor, $room, $bim_element_id, $responsible_team,
                $created_at, $sla_due_at
            )
            ON CONFLICT(work_order_no) DO UPDATE SET
                title = excluded.title,
                service_type = excluded.service_type,
                priority = excluded.priority,
                status = excluded.status,
                campus = excluded.campus,
                building = excluded.building,
                floor = excluded.floor,
                room = excluded.room,
                bim_element_id = excluded.bim_element_id,
                responsible_team = excluded.responsible_team,
                created_at = excluded.created_at,
                sla_due_at = excluded.sla_due_at;
            """);
        AddWorkOrderParameters(command, workOrder);
        command.ExecuteNonQuery();
    }

    private static void InsertTimelineEntry(
        SqliteConnection connection,
        SqliteTransaction? transaction,
        string workOrderNo,
        WorkOrderTimelineEntry entry)
    {
        using var command = CreateCommand(connection, transaction, """
            INSERT OR IGNORE INTO wo_work_order_transitions (
                work_order_no, occurred_at, operator_name, action,
                from_status, to_status, remark, rating
            )
            VALUES (
                $work_order_no, $occurred_at, $operator_name, $action,
                $from_status, $to_status, $remark, $rating
            );
            """);
        command.Parameters.AddWithValue("$work_order_no", workOrderNo);
        command.Parameters.AddWithValue("$occurred_at", ToStorage(entry.OccurredAt));
        command.Parameters.AddWithValue("$operator_name", entry.Operator);
        command.Parameters.AddWithValue("$action", entry.Action);
        command.Parameters.AddWithValue("$from_status", entry.FromStatus.ToString());
        command.Parameters.AddWithValue("$to_status", entry.ToStatus.ToString());
        command.Parameters.AddWithValue("$remark", entry.Remark);
        command.Parameters.AddWithValue("$rating", entry.Rating is null ? DBNull.Value : entry.Rating.Value);
        command.ExecuteNonQuery();
    }

    private static SqliteCommand CreateCommand(SqliteConnection connection, SqliteTransaction? transaction, string commandText)
    {
        var command = connection.CreateCommand();
        command.CommandText = commandText;
        command.Transaction = transaction;
        return command;
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
            CREATE TABLE IF NOT EXISTS wo_work_orders (
                work_order_no TEXT PRIMARY KEY,
                title TEXT NOT NULL,
                service_type TEXT NOT NULL,
                priority TEXT NOT NULL,
                status TEXT NOT NULL,
                campus TEXT NOT NULL,
                building TEXT NOT NULL,
                floor TEXT NOT NULL,
                room TEXT NOT NULL,
                bim_element_id TEXT NOT NULL,
                responsible_team TEXT NOT NULL,
                created_at TEXT NOT NULL,
                sla_due_at TEXT NOT NULL
            );

            CREATE TABLE IF NOT EXISTS wo_service_requests (
                request_no TEXT PRIMARY KEY,
                source_type TEXT NOT NULL,
                requester_name TEXT NOT NULL,
                requester_department TEXT NOT NULL,
                service_type TEXT NOT NULL,
                priority TEXT NOT NULL,
                description TEXT NOT NULL,
                campus TEXT NOT NULL,
                building TEXT NOT NULL,
                floor TEXT NOT NULL,
                room TEXT NOT NULL,
                bim_element_id TEXT NOT NULL,
                status TEXT NOT NULL,
                created_at TEXT NOT NULL,
                converted_work_order_no TEXT NULL,
                FOREIGN KEY (converted_work_order_no) REFERENCES wo_work_orders(work_order_no)
            );

            CREATE TABLE IF NOT EXISTS wo_work_order_transitions (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                work_order_no TEXT NOT NULL,
                occurred_at TEXT NOT NULL,
                operator_name TEXT NOT NULL,
                action TEXT NOT NULL,
                from_status TEXT NOT NULL,
                to_status TEXT NOT NULL,
                remark TEXT NOT NULL,
                rating INTEGER NULL,
                FOREIGN KEY (work_order_no) REFERENCES wo_work_orders(work_order_no)
            );

            CREATE INDEX IF NOT EXISTS ix_wo_work_orders_status_priority
                ON wo_work_orders(status, priority, sla_due_at);

            CREATE INDEX IF NOT EXISTS ix_wo_service_requests_status
                ON wo_service_requests(status, created_at);

            CREATE UNIQUE INDEX IF NOT EXISTS ux_wo_service_requests_converted_work_order
                ON wo_service_requests(converted_work_order_no)
                WHERE converted_work_order_no IS NOT NULL;

            CREATE INDEX IF NOT EXISTS ix_wo_transitions_work_order_time
                ON wo_work_order_transitions(work_order_no, occurred_at, id);

            CREATE UNIQUE INDEX IF NOT EXISTS ux_wo_transitions_deduplicate
                ON wo_work_order_transitions(work_order_no, occurred_at, operator_name, action, from_status, to_status);
            """;
        command.ExecuteNonQuery();
    }

    private static IReadOnlyList<WorkOrder> LoadWorkOrders(SqliteConnection connection)
    {
        using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT work_order_no, title, service_type, priority, status, campus,
                   building, floor, room, bim_element_id, responsible_team,
                   created_at, sla_due_at
            FROM wo_work_orders
            ORDER BY created_at, work_order_no;
            """;

        using var reader = command.ExecuteReader();
        var workOrders = new List<WorkOrder>();
        while (reader.Read())
        {
            workOrders.Add(new WorkOrder(
                ReadString(reader, "work_order_no"),
                ReadString(reader, "title"),
                ReadString(reader, "service_type"),
                ParseEnum<Priority>(reader, "priority"),
                ParseEnum<WorkOrderStatus>(reader, "status"),
                ReadLocation(reader),
                ReadString(reader, "responsible_team"),
                ReadDateTimeOffset(reader, "created_at"),
                ReadDateTimeOffset(reader, "sla_due_at")));
        }

        return workOrders;
    }

    private static IReadOnlyList<ServiceRequest> LoadServiceRequests(SqliteConnection connection)
    {
        using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT request_no, source_type, requester_name, requester_department,
                   service_type, priority, description, campus, building, floor,
                   room, bim_element_id, status, created_at, converted_work_order_no
            FROM wo_service_requests
            ORDER BY created_at, request_no;
            """;

        using var reader = command.ExecuteReader();
        var requests = new List<ServiceRequest>();
        while (reader.Read())
        {
            requests.Add(new ServiceRequest(
                ReadString(reader, "request_no"),
                ReadString(reader, "source_type"),
                ReadString(reader, "requester_name"),
                ReadString(reader, "requester_department"),
                ReadString(reader, "service_type"),
                ParseEnum<Priority>(reader, "priority"),
                ReadString(reader, "description"),
                ReadLocation(reader),
                ParseEnum<ServiceRequestStatus>(reader, "status"),
                ReadDateTimeOffset(reader, "created_at"),
                ReadNullableString(reader, "converted_work_order_no")));
        }

        return requests;
    }

    private static IReadOnlyDictionary<string, IReadOnlyList<WorkOrderTimelineEntry>> LoadTimeline(SqliteConnection connection)
    {
        using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT work_order_no, occurred_at, operator_name, action, from_status,
                   to_status, remark, rating
            FROM wo_work_order_transitions
            ORDER BY work_order_no, occurred_at, id;
            """;

        using var reader = command.ExecuteReader();
        var timeline = new Dictionary<string, List<WorkOrderTimelineEntry>>(StringComparer.OrdinalIgnoreCase);
        while (reader.Read())
        {
            var workOrderNo = ReadString(reader, "work_order_no");
            if (!timeline.TryGetValue(workOrderNo, out var entries))
            {
                entries = [];
                timeline[workOrderNo] = entries;
            }

            entries.Add(new WorkOrderTimelineEntry(
                ReadDateTimeOffset(reader, "occurred_at"),
                ReadString(reader, "operator_name"),
                ReadString(reader, "action"),
                ParseEnum<WorkOrderStatus>(reader, "from_status"),
                ParseEnum<WorkOrderStatus>(reader, "to_status"),
                ReadString(reader, "remark"),
                ReadNullableInt32(reader, "rating")));
        }

        return timeline.ToDictionary(
            item => item.Key,
            item => (IReadOnlyList<WorkOrderTimelineEntry>)item.Value,
            StringComparer.OrdinalIgnoreCase);
    }

    private static void AddWorkOrderParameters(SqliteCommand command, WorkOrder workOrder)
    {
        command.Parameters.AddWithValue("$work_order_no", workOrder.WorkOrderNo);
        command.Parameters.AddWithValue("$title", workOrder.Title);
        command.Parameters.AddWithValue("$service_type", workOrder.ServiceType);
        command.Parameters.AddWithValue("$priority", workOrder.Priority.ToString());
        command.Parameters.AddWithValue("$status", workOrder.Status.ToString());
        AddLocationParameters(command, workOrder.Location);
        command.Parameters.AddWithValue("$responsible_team", workOrder.ResponsibleTeam);
        command.Parameters.AddWithValue("$created_at", ToStorage(workOrder.CreatedAt));
        command.Parameters.AddWithValue("$sla_due_at", ToStorage(workOrder.SlaDueAt));
    }

    private static void AddServiceRequestParameters(SqliteCommand command, ServiceRequest request)
    {
        command.Parameters.AddWithValue("$request_no", request.RequestNo);
        command.Parameters.AddWithValue("$source_type", request.SourceType);
        command.Parameters.AddWithValue("$requester_name", request.RequesterName);
        command.Parameters.AddWithValue("$requester_department", request.RequesterDepartment);
        command.Parameters.AddWithValue("$service_type", request.ServiceType);
        command.Parameters.AddWithValue("$priority", request.Priority.ToString());
        command.Parameters.AddWithValue("$description", request.Description);
        AddLocationParameters(command, request.Location);
        command.Parameters.AddWithValue("$status", request.Status.ToString());
        command.Parameters.AddWithValue("$created_at", ToStorage(request.CreatedAt));
        command.Parameters.AddWithValue("$converted_work_order_no", request.ConvertedWorkOrderNo is null ? DBNull.Value : request.ConvertedWorkOrderNo);
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

    private static int? ReadNullableInt32(SqliteDataReader reader, string name)
    {
        var ordinal = reader.GetOrdinal(name);
        return reader.IsDBNull(ordinal) ? null : reader.GetInt32(ordinal);
    }

    private static DateTimeOffset ReadDateTimeOffset(SqliteDataReader reader, string name) =>
        DateTimeOffset.Parse(ReadString(reader, name), CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind);

    private static TEnum ParseEnum<TEnum>(SqliteDataReader reader, string name)
        where TEnum : struct =>
        Enum.Parse<TEnum>(ReadString(reader, name), ignoreCase: true);

    private static string ToStorage(DateTimeOffset value) =>
        value.ToString("O", CultureInfo.InvariantCulture);
}
