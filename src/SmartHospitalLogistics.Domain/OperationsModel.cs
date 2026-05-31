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
    Accepted,
    InProgress,
    Suspended,
    Transferred,
    PendingAcceptance,
    PendingEvaluation,
    Closed,
    Escalated
}

public enum ServiceRequestStatus
{
    Accepted,
    Converted,
    Cancelled
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

public enum AssetCriticality
{
    Low,
    Medium,
    High,
    LifeSafety
}

public enum MaintenanceTaskType
{
    Inspection,
    PreventiveMaintenance,
    Calibration,
    SafetyCheck
}

public enum MaintenanceTaskStatus
{
    Planned,
    Due,
    Overdue,
    Completed,
    RequiresRepair,
    ConvertedToWorkOrder
}

public enum MaintenanceOutcome
{
    Normal,
    Abnormal,
    Skipped
}

public enum SignalStatus
{
    Normal,
    Warning,
    Critical
}

public enum IotSystemCategory
{
    StrongElectric,
    Hvac,
    WaterSupplyDrainage,
    MedicalGas,
    EnvironmentQuality,
    Sewage
}

public enum TelemetryRiskLevel
{
    Normal,
    Warning,
    Critical
}

public enum MonitoringAlarmStatus
{
    New,
    Acknowledged,
    ConvertedToWorkOrder,
    Closed
}

public enum ThresholdDirection
{
    Above,
    Below,
    OutsideRange
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

public sealed record CreateServiceRequestCommand(
    string SourceType,
    string RequesterName,
    string RequesterDepartment,
    string ServiceType,
    Priority Priority,
    string Description,
    SpatialLocation Location);

public sealed record ConvertServiceRequestCommand(
    string AcceptedBy,
    string Remark);

public sealed record ServiceRequest(
    string RequestNo,
    string SourceType,
    string RequesterName,
    string RequesterDepartment,
    string ServiceType,
    Priority Priority,
    string Description,
    SpatialLocation Location,
    ServiceRequestStatus Status,
    DateTimeOffset CreatedAt,
    string? ConvertedWorkOrderNo);

public enum WorkOrderTransitionAction
{
    Accept,
    Suspend,
    Transfer,
    Complete,
    AcceptCompletion,
    RejectCompletion,
    Evaluate,
    Escalate
}

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

public sealed record DispatchWorkOrderCommand(
    string TeamName,
    string Dispatcher,
    string Remark);

public sealed record TransitionWorkOrderCommand(
    WorkOrderTransitionAction Action,
    string Operator,
    string Remark,
    string? TargetTeam = null,
    int? Rating = null);

public sealed record WorkOrderTimelineEntry(
    DateTimeOffset OccurredAt,
    string Operator,
    string Action,
    WorkOrderStatus FromStatus,
    WorkOrderStatus ToStatus,
    string Remark,
    int? Rating = null);

public sealed record DispatchRecommendation(
    string WorkOrderNo,
    string RecommendedTeam,
    string Reason,
    Priority Priority,
    int SlaMinutesRemaining);

public sealed record SlaRiskSummary(
    int OpenWorkOrders,
    int OverdueWorkOrders,
    int DueSoonWorkOrders,
    int EscalatedWorkOrders,
    string HighestRiskLevel,
    string? HighestRiskWorkOrderNo);

public sealed record WorkOrderDetail(
    WorkOrder WorkOrder,
    SpatialLocation Location,
    IReadOnlyList<FeatureEvidence> SourceEvidence,
    IReadOnlyList<WorkOrderTimelineEntry> Timeline,
    string SlaRiskLevel,
    int SlaMinutesRemaining,
    IReadOnlyList<string> AllowedActions);

public sealed record DispatchBoard(
    IReadOnlyList<WorkOrder> WorkOrders,
    IReadOnlyList<TeamLoad> TeamLoads,
    IReadOnlyList<DispatchRecommendation> Recommendations,
    SlaRiskSummary SlaRisk);

public sealed record DispatchOperationResult(
    bool Succeeded,
    string? ErrorMessage,
    WorkOrderDetail? Detail,
    bool NotFound = false);

public sealed record ServiceRequestConversionResult(
    bool Succeeded,
    string? ErrorMessage,
    ServiceRequest? Request,
    WorkOrderDetail? Detail,
    bool NotFound = false);

public sealed record AssetLedgerItem(
    string AssetCode,
    string Name,
    string System,
    AssetCriticality Criticality,
    SpatialLocation Location,
    FacilityStatus Status,
    string OwnerTeam,
    string Manufacturer,
    string Model,
    string CommissionedOn,
    string MaintenanceStrategy,
    int HealthScore,
    string CurrentRisk,
    IReadOnlyList<string> SourceTags);

public sealed record InspectionChecklistItem(
    string Code,
    string Name,
    string Standard,
    bool Required);

public sealed record MaintenancePlan(
    string PlanCode,
    string AssetCode,
    string Name,
    MaintenanceTaskType TaskType,
    int CycleDays,
    DateTimeOffset NextDueAt,
    string ResponsibleTeam,
    IReadOnlyList<InspectionChecklistItem> ChecklistTemplate,
    IReadOnlyList<FeatureEvidence> SourceEvidence);

public sealed record InspectionChecklistResult(
    string Code,
    string Result,
    string Remark);

public sealed record MaintenanceTask(
    string TaskNo,
    string PlanCode,
    string AssetCode,
    string Title,
    MaintenanceTaskType TaskType,
    MaintenanceTaskStatus Status,
    Priority Priority,
    DateTimeOffset ScheduledAt,
    DateTimeOffset DueAt,
    string ResponsibleTeam,
    IReadOnlyList<InspectionChecklistResult> ChecklistResults,
    MaintenanceOutcome? Outcome = null,
    DateTimeOffset? CompletedAt = null,
    string? CompletedBy = null,
    string? WorkOrderNo = null);

public sealed record AssetLifecycleEvent(
    DateTimeOffset OccurredAt,
    string AssetCode,
    string EventType,
    string Operator,
    string Summary);

public sealed record AssetMaintenanceKpi(
    int TotalAssets,
    int RiskAssets,
    int DueTasks,
    int OverdueTasks,
    decimal PreventiveCompletionRate,
    int AverageHealthScore);

public sealed record MaintenanceGeneratedWorkOrder(
    string WorkOrderNo,
    string Title,
    string ServiceType,
    Priority Priority,
    WorkOrderStatus Status,
    SpatialLocation Location,
    string ResponsibleTeam,
    DateTimeOffset CreatedAt);

public sealed record AssetMaintenanceDetail(
    AssetLedgerItem Asset,
    IReadOnlyList<MaintenancePlan> Plans,
    IReadOnlyList<MaintenanceTask> Tasks,
    IReadOnlyList<AssetLifecycleEvent> Lifecycle,
    IReadOnlyList<FeatureEvidence> SourceEvidence);

public sealed record AssetMaintenanceBoard(
    DateTimeOffset GeneratedAt,
    IReadOnlyList<AssetLedgerItem> Assets,
    IReadOnlyList<MaintenancePlan> Plans,
    IReadOnlyList<MaintenanceTask> DueTasks,
    IReadOnlyList<AssetLifecycleEvent> LifecycleEvents,
    IReadOnlyList<FeatureEvidence> SourceEvidence,
    AssetMaintenanceKpi Kpis);

public sealed record CompleteMaintenanceTaskCommand(
    string Operator,
    MaintenanceOutcome Outcome,
    string Remark,
    IReadOnlyList<InspectionChecklistResult> ChecklistResults,
    bool ConvertToWorkOrder);

public sealed record MaintenanceTaskOperationResult(
    bool Succeeded,
    string? ErrorMessage,
    MaintenanceTask? Task,
    MaintenanceGeneratedWorkOrder? GeneratedWorkOrder = null,
    bool NotFound = false);

public sealed record IotSystemProfile(
    IotSystemCategory Category,
    string Name,
    IReadOnlyList<string> Subsystems,
    IReadOnlyList<string> Roles,
    IReadOnlyList<string> Endpoints);

public sealed record IotMetricDefinition(
    string Code,
    string Name,
    string Unit,
    string DataType,
    string SourceField);

public sealed record IotMonitoringPoint(
    string PointCode,
    string Name,
    IotSystemCategory Category,
    SpatialLocation Location,
    string DeviceCode,
    string ProtocolAdapter,
    IReadOnlyList<IotMetricDefinition> Metrics,
    IReadOnlyList<FeatureEvidence> SourceEvidence);

public sealed record TelemetryThresholdRule(
    string PointCode,
    string MetricCode,
    ThresholdDirection Direction,
    decimal? WarningMin,
    decimal? WarningMax,
    decimal? CriticalMin,
    decimal? CriticalMax,
    string RuleSummary);

public sealed record TelemetryReading(
    string PointCode,
    string MetricCode,
    decimal Value,
    string Unit,
    DateTimeOffset CollectedAt,
    TelemetryRiskLevel RiskLevel,
    string RuleSummary);

public sealed record MonitoringAlarmEvent(
    string AlarmNo,
    string PointCode,
    string MetricCode,
    string Title,
    TelemetryRiskLevel RiskLevel,
    MonitoringAlarmStatus Status,
    SpatialLocation Location,
    decimal Value,
    string Unit,
    DateTimeOffset TriggeredAt,
    string RuleSummary,
    string ResponsibleTeam,
    string? WorkOrderNo = null,
    string? AcknowledgedBy = null,
    DateTimeOffset? AcknowledgedAt = null,
    string? LastRemark = null);

public sealed record MonitoringAlarmBoard(
    DateTimeOffset GeneratedAt,
    IReadOnlyList<MonitoringAlarmEvent> Alarms,
    IReadOnlyList<FeatureEvidence> SourceEvidence);

public sealed record AcknowledgeAlarmCommand(
    string Operator,
    string Remark);

public sealed record ConvertAlarmToWorkOrderCommand(
    string Operator,
    string TargetTeam,
    string Remark);

public sealed record MonitoringAlarmOperationResult(
    bool Succeeded,
    string? ErrorMessage,
    MonitoringAlarmEvent? Alarm,
    bool NotFound = false);

public sealed record TelemetryIngestionCommand(
    string PointCode,
    string MetricCode,
    decimal Value,
    string Unit,
    DateTimeOffset CollectedAt);

public sealed record TelemetryIngestionResult(
    bool Succeeded,
    string? ErrorMessage,
    string PointCode,
    string MetricCode,
    TelemetryRiskLevel RiskLevel,
    string RuleSummary,
    TelemetryReading? Reading = null,
    bool NotFound = false);

public sealed record IotPointDetail(
    IotMonitoringPoint Point,
    IReadOnlyList<TelemetryThresholdRule> ThresholdRules,
    IReadOnlyList<TelemetryReading> RecentReadings,
    IReadOnlyList<FeatureEvidence> SourceEvidence);

public sealed record IotIntegrationCatalog(
    DateTimeOffset GeneratedAt,
    IReadOnlyList<IotSystemProfile> Systems,
    IReadOnlyList<IotMonitoringPoint> Points,
    IReadOnlyList<TelemetryThresholdRule> ThresholdRules,
    IReadOnlyList<FeatureEvidence> SourceEvidence);

public enum MedicalGasSupplyType
{
    Oxygen,
    CompressedAir,
    Vacuum
}

public enum MedicalGasZoneStatus
{
    Normal,
    Warning,
    Critical,
    Maintenance
}

public sealed record MedicalGasZone(
    string ZoneCode,
    string Name,
    string Department,
    SpatialLocation Location,
    IReadOnlyList<MedicalGasSupplyType> SupplyTypes,
    string ResponsibleTeam,
    string PressurePointCode,
    string ValveAssetCode,
    MedicalGasZoneStatus Status,
    string RiskSummary,
    IReadOnlyList<FeatureEvidence> SourceEvidence);

public sealed record MedicalGasBoardKpi(
    int ZoneCount,
    int AbnormalZones,
    int ActiveAlarms,
    int OpenWorkOrders,
    int DueMaintenanceTasks);

public sealed record MedicalGasBoard(
    DateTimeOffset GeneratedAt,
    IReadOnlyList<MedicalGasZone> Zones,
    IReadOnlyList<IotMonitoringPoint> MonitoringPoints,
    IReadOnlyList<AssetLedgerItem> ValveAssets,
    IReadOnlyList<MonitoringAlarmEvent> ActiveAlarms,
    IReadOnlyList<WorkOrder> OpenWorkOrders,
    IReadOnlyList<MaintenanceTask> DueMaintenanceTasks,
    IReadOnlyList<FeatureEvidence> SourceEvidence,
    MedicalGasBoardKpi Kpis);

public sealed record MedicalGasZoneDetail(
    MedicalGasZone Zone,
    IotPointDetail? MonitoringPoint,
    AssetMaintenanceDetail? ValveAsset,
    IReadOnlyList<MonitoringAlarmEvent> ActiveAlarms,
    IReadOnlyList<WorkOrder> OpenWorkOrders,
    IReadOnlyList<MaintenanceTask> MaintenanceTasks,
    IReadOnlyList<FeatureEvidence> SourceEvidence);

public sealed record FeatureEvidence(
    string FeatureName,
    IReadOnlyList<string> Sources,
    string EvidenceSummary);

public sealed record CustomerDataSystem(
    string Major,
    IReadOnlyList<string> Subsystems,
    IReadOnlyList<string> Sensors,
    IReadOnlyList<string> Locations,
    IReadOnlyList<string> DataFields,
    IReadOnlyList<string> DesiredData,
    IReadOnlyList<string> Roles,
    IReadOnlyList<string> Endpoints);

public sealed record CompetitorModule(
    string Name,
    IReadOnlyList<string> FeatureAreas);

public sealed record ImplementationPhase(
    int Order,
    string Name,
    IReadOnlyList<string> Capabilities);

public sealed record LogisticsBlueprint(
    int PptReportedPrimaryModuleTotal,
    int PptReportedSecondaryItemTotal,
    IReadOnlyList<NavigationGroup> NavigationGroups,
    IReadOnlyList<LogisticsFeatureGroup> FeatureGroups,
    IReadOnlyList<LogisticsWorkflow> Workflows,
    IReadOnlyList<WorkbenchMetric> WorkbenchMetrics,
    IReadOnlyList<TeamLoad> TeamLoads,
    IReadOnlyList<FeatureEvidence> FeatureEvidence,
    IReadOnlyList<CustomerDataSystem> CustomerDataSystems,
    IReadOnlyList<CompetitorModule> CompetitorModules,
    IReadOnlyList<ImplementationPhase> ImplementationPhases);
