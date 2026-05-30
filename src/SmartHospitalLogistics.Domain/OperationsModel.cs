namespace SmartHospitalLogistics.Domain;

public enum OperationDomain
{
    Foundation,
    LogisticsService,
    Environment,
    IntegratedManagement
}

public enum WorkOrderStatus
{
    New,
    Dispatched,
    InProgress,
    PendingAcceptance,
    Closed,
    Escalated
}

public enum Priority
{
    Low,
    Normal,
    High,
    Critical
}

public enum FacilityStatus
{
    Normal,
    Warning,
    Fault,
    Maintenance
}

public enum SignalStatus
{
    Normal,
    Warning,
    Critical
}

public sealed record SpatialLocation(
    string Campus,
    string Building,
    string Floor,
    string Room,
    string BimElementId);

public sealed record OperationalModule(
    string Code,
    string Name,
    OperationDomain Domain,
    string Objective,
    int PrimaryFunctions,
    int SecondaryFunctions,
    string[] IntegrationPoints);

public sealed record FacilityAsset(
    string AssetCode,
    string Name,
    string System,
    SpatialLocation Location,
    FacilityStatus Status,
    DateTimeOffset LastSignalAt,
    string CurrentRisk);

public sealed record WorkOrder(
    string WorkOrderNo,
    string Title,
    string ServiceType,
    Priority Priority,
    WorkOrderStatus Status,
    SpatialLocation Location,
    string ResponsibleTeam,
    DateTimeOffset CreatedAt,
    DateTimeOffset SlaDueAt);

public sealed record EnvironmentSignal(
    string SignalCode,
    string Name,
    string Metric,
    decimal Value,
    string Unit,
    SignalStatus Status,
    SpatialLocation Location,
    DateTimeOffset CollectedAt);

public sealed record LogisticsMetric(
    string Code,
    string Name,
    string Value,
    string Trend,
    OperationDomain Domain);

public sealed record OperationsDashboard(
    DateTimeOffset GeneratedAt,
    IReadOnlyList<OperationalModule> Modules,
    IReadOnlyList<FacilityAsset> Assets,
    IReadOnlyList<WorkOrder> WorkOrders,
    IReadOnlyList<EnvironmentSignal> EnvironmentSignals,
    IReadOnlyList<LogisticsMetric> Metrics)
{
    public int OpenWorkOrders => WorkOrders.Count(order => order.Status is not WorkOrderStatus.Closed);

    public int EscalatedWorkOrders => WorkOrders.Count(order => order.Status == WorkOrderStatus.Escalated);

    public int RiskAssets => Assets.Count(asset => asset.Status is FacilityStatus.Warning or FacilityStatus.Fault);

    public int WarningSignals => EnvironmentSignals.Count(signal => signal.Status is SignalStatus.Warning or SignalStatus.Critical);
}

public sealed record NavigationGroup(
    string Code,
    string Name,
    IReadOnlyList<string> Items);

public sealed record LogisticsFeatureGroup(
    string Code,
    string Name,
    OperationDomain Domain,
    int PrimaryModuleCount,
    int SecondaryItemCount,
    IReadOnlyList<string> Modules);

public sealed record LogisticsWorkflow(
    string Code,
    string Name,
    IReadOnlyList<string> Steps);

public sealed record WorkbenchMetric(
    string Code,
    string Name,
    string Value,
    string Trend,
    string Tone);

public sealed record TeamLoad(
    string TeamName,
    string Domain,
    int ActiveTasks,
    int Capacity,
    string Recommendation);

public sealed record LogisticsBlueprint(
    int PptReportedPrimaryModuleTotal,
    int PptReportedSecondaryItemTotal,
    IReadOnlyList<NavigationGroup> NavigationGroups,
    IReadOnlyList<LogisticsFeatureGroup> FeatureGroups,
    IReadOnlyList<LogisticsWorkflow> Workflows,
    IReadOnlyList<WorkbenchMetric> WorkbenchMetrics,
    IReadOnlyList<TeamLoad> TeamLoads);
