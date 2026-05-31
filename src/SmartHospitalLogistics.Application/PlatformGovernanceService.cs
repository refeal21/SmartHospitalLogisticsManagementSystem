using SmartHospitalLogistics.Domain;

namespace SmartHospitalLogistics.Application;

public interface IPlatformGovernanceService
{
    PlatformGovernanceBoard GetBoard();

    PlatformGovernanceControlDetail? GetControlDetail(string controlCode);

    PlatformGovernanceActionResult RecordAction(string controlCode, CreateGovernanceActionCommand command);
}

public sealed class PlatformGovernanceService : IPlatformGovernanceService
{
    private static readonly DateTimeOffset SeedTime = new(2026, 5, 30, 9, 30, 0, TimeSpan.FromHours(8));

    private readonly IPlatformGovernancePersistence? _persistence;
    private readonly IWorkOrderDispatchService _workOrderDispatch;
    private readonly IMonitoringAlarmService _monitoringAlarms;
    private readonly IAssetMaintenanceService _assetMaintenance;
    private readonly IEnergyPerformanceService _energyPerformance;
    private readonly ISafetyEmergencyService _safetyEmergency;
    private readonly Dictionary<string, PlatformGovernanceControl> _controls;
    private readonly List<PlatformGovernanceAction> _actions;
    private readonly List<PlatformGovernanceAuditEntry> _auditTrail;

    public PlatformGovernanceService(
        IPlatformGovernancePersistence? persistence = null,
        IWorkOrderDispatchService? workOrderDispatch = null,
        IMonitoringAlarmService? monitoringAlarms = null,
        IAssetMaintenanceService? assetMaintenance = null,
        IEnergyPerformanceService? energyPerformance = null,
        ISafetyEmergencyService? safetyEmergency = null)
    {
        _persistence = persistence;
        _workOrderDispatch = workOrderDispatch ?? new WorkOrderDispatchService();
        _monitoringAlarms = monitoringAlarms ?? new MonitoringAlarmService(workOrderIntake: _workOrderDispatch as IWorkOrderIntakeService);
        _assetMaintenance = assetMaintenance ?? new AssetMaintenanceService(workOrderIntake: _workOrderDispatch as IWorkOrderIntakeService);
        _energyPerformance = energyPerformance ?? new EnergyPerformanceService(
            workOrderDispatch: _workOrderDispatch,
            monitoringAlarms: _monitoringAlarms);
        _safetyEmergency = safetyEmergency ?? new SafetyEmergencyService(
            assetMaintenance: _assetMaintenance,
            monitoringAlarms: _monitoringAlarms,
            workOrderDispatch: _workOrderDispatch);

        var persisted = _persistence?.Load();
        if (persisted is not null && persisted.Controls.Count > 0)
        {
            _controls = persisted.Controls.ToDictionary(control => control.ControlCode, StringComparer.OrdinalIgnoreCase);
            _actions = persisted.Actions.ToList();
            _auditTrail = persisted.AuditTrail.ToList();
        }
        else
        {
            _controls = BuildSeedControls().ToDictionary(control => control.ControlCode, StringComparer.OrdinalIgnoreCase);
            _actions = BuildSeedActions().ToList();
            _auditTrail = BuildSeedAuditTrail().ToList();
            PersistSeedData();
        }
    }

    public PlatformGovernanceBoard GetBoard()
    {
        var dispatchBoard = _workOrderDispatch.GetDispatchBoard();
        var alarmBoard = _monitoringAlarms.GetAlarmBoard();
        var energyBoard = _energyPerformance.GetBoard();
        var safetyBoard = _safetyEmergency.GetBoard();
        var controls = EnrichControls(energyBoard, safetyBoard).OrderBy(control => control.ControlCode).ToArray();
        var activeActions = _actions
            .Where(action => action.Status != GovernanceActionStatus.Closed)
            .OrderBy(action => action.DueAt)
            .ToArray();
        var relatedWorkOrders = dispatchBoard.WorkOrders
            .Where(order => order.Status != WorkOrderStatus.Closed)
            .OrderByDescending(order => order.Priority)
            .ThenBy(order => order.SlaDueAt)
            .Take(8)
            .ToArray();
        var relatedAlarms = alarmBoard.Alarms
            .Where(alarm => alarm.Status != MonitoringAlarmStatus.Closed)
            .OrderByDescending(alarm => alarm.RiskLevel)
            .ThenByDescending(alarm => alarm.TriggeredAt)
            .Take(8)
            .ToArray();

        return new PlatformGovernanceBoard(
            SeedTime,
            controls,
            activeActions,
            relatedWorkOrders,
            relatedAlarms,
            BuildPerformanceMetrics(controls, activeActions, relatedWorkOrders, relatedAlarms),
            BuildSourceEvidence(),
            new PlatformGovernanceKpi(
                controls.Length,
                controls.Count(control => control.Status is PlatformGovernanceControlStatus.Attention or PlatformGovernanceControlStatus.InProgress or PlatformGovernanceControlStatus.Overdue),
                controls.Count(control => control.Status == PlatformGovernanceControlStatus.Overdue),
                activeActions.Length,
                _auditTrail.Count));
    }

    public PlatformGovernanceControlDetail? GetControlDetail(string controlCode)
    {
        var board = GetBoard();
        var control = board.Controls.FirstOrDefault(item => string.Equals(item.ControlCode, controlCode, StringComparison.OrdinalIgnoreCase));
        if (control is null)
        {
            return null;
        }

        return new PlatformGovernanceControlDetail(
            control,
            _actions.Where(action => string.Equals(action.ControlCode, control.ControlCode, StringComparison.OrdinalIgnoreCase))
                .OrderByDescending(action => action.CreatedAt)
                .ToArray(),
            board.RelatedWorkOrders.Where(order => IsControlWorkOrder(order, control)).ToArray(),
            board.RelatedAlarms.Where(alarm => IsControlAlarm(alarm, control)).ToArray(),
            _auditTrail.Where(entry => string.Equals(entry.ControlCode, control.ControlCode, StringComparison.OrdinalIgnoreCase))
                .OrderByDescending(entry => entry.OccurredAt)
                .ToArray(),
            BuildSourceEvidence());
    }

    public PlatformGovernanceActionResult RecordAction(string controlCode, CreateGovernanceActionCommand command)
    {
        if (!_controls.TryGetValue(controlCode, out var control))
        {
            return new PlatformGovernanceActionResult(false, $"Governance control {controlCode} was not found.", null);
        }

        var actionIndex = _actions.Count(action => string.Equals(action.ControlCode, control.ControlCode, StringComparison.OrdinalIgnoreCase)) + 1;
        var action = new PlatformGovernanceAction(
            $"ACT-{control.ControlCode}-{actionIndex:000}",
            control.ControlCode,
            command.Title,
            command.ResponsibleRole,
            command.RecordedBy,
            GovernanceActionStatus.InProgress,
            SeedTime.AddMinutes(actionIndex),
            SeedTime.AddDays(3),
            null);
        var updatedControl = control with { Status = PlatformGovernanceControlStatus.InProgress };
        var auditEntry = new PlatformGovernanceAuditEntry(
            $"AUD-{control.ControlCode}-{_auditTrail.Count + 1:000}",
            control.ControlCode,
            "RecordAction",
            command.RecordedBy,
            SeedTime.AddMinutes(actionIndex),
            command.Title);

        _controls[control.ControlCode] = updatedControl;
        _actions.Add(action);
        _auditTrail.Add(auditEntry);
        _persistence?.SaveControl(updatedControl);
        _persistence?.SaveAction(action);
        _persistence?.SaveAuditEntry(auditEntry);

        return new PlatformGovernanceActionResult(true, "Governance action recorded.", GetControlDetail(control.ControlCode));
    }

    private PlatformGovernanceControl[] EnrichControls(EnergyPerformanceBoard energyBoard, SafetyEmergencyBoard safetyBoard)
    {
        return _controls.Values.Select(control =>
        {
            var openActions = _actions.Count(action =>
                string.Equals(action.ControlCode, control.ControlCode, StringComparison.OrdinalIgnoreCase) &&
                action.Status != GovernanceActionStatus.Closed);
            var status = control.Status;
            var currentValue = control.CurrentValue;

            if (openActions > 0 && status != PlatformGovernanceControlStatus.Overdue)
            {
                status = PlatformGovernanceControlStatus.InProgress;
            }

            if (control.ControlCode == "GOV-ENERGY-COST")
            {
                currentValue = energyBoard.Kpis.TotalEnergyCost;
                if (energyBoard.Kpis.AbnormalAreaCount > 0 && status == PlatformGovernanceControlStatus.OnTrack)
                {
                    status = PlatformGovernanceControlStatus.Attention;
                }
            }

            if (control.ControlCode == "GOV-SAFETY-EMERGENCY")
            {
                currentValue = safetyBoard.Kpis.ActiveAlarms + safetyBoard.Kpis.OpenWorkOrders;
                if (safetyBoard.Kpis.ActiveEmergencyEvents > 0)
                {
                    status = PlatformGovernanceControlStatus.Overdue;
                }
            }

            return control with { Status = status, CurrentValue = currentValue };
        }).ToArray();
    }

    private static OperationPerformanceMetric[] BuildPerformanceMetrics(
        IReadOnlyList<PlatformGovernanceControl> controls,
        IReadOnlyList<PlatformGovernanceAction> activeActions,
        IReadOnlyList<WorkOrder> relatedWorkOrders,
        IReadOnlyList<MonitoringAlarmEvent> relatedAlarms)
    {
        var closureRate = controls.Count == 0
            ? 100
            : Math.Round((controls.Count(control => control.Status is PlatformGovernanceControlStatus.OnTrack or PlatformGovernanceControlStatus.Closed) / (decimal)controls.Count) * 100m, 1);
        var riskScore = Math.Min(100, controls.Count(control => control.Status == PlatformGovernanceControlStatus.Overdue) * 25 + activeActions.Count * 5 + relatedAlarms.Count * 6);

        return
        [
            new OperationPerformanceMetric(
                "governance-closure-rate",
                "治理闭环率",
                closureRate,
                "%",
                "目标 >= 90%",
                closureRate >= 90 ? EnergyPerformanceAreaStatus.Normal : EnergyPerformanceAreaStatus.Warning),
            new OperationPerformanceMetric(
                "governance-risk-score",
                "运营风险指数",
                riskScore,
                "分",
                "控制在 30 分以内",
                riskScore > 60 || relatedWorkOrders.Any(order => order.Priority == Priority.Critical)
                    ? EnergyPerformanceAreaStatus.Critical
                    : EnergyPerformanceAreaStatus.Warning)
        ];
    }

    private static bool IsControlWorkOrder(WorkOrder order, PlatformGovernanceControl control) =>
        control.ControlCode switch
        {
            "GOV-SLA-ONE-STOP" => order.SlaDueAt <= SeedTime.AddHours(2) || order.Priority >= Priority.High,
            "GOV-ENERGY-COST" => order.ServiceType.Contains("能耗", StringComparison.OrdinalIgnoreCase) ||
                                 order.Location.BimElementId.Contains("BIM-ENE", StringComparison.OrdinalIgnoreCase),
            "GOV-SAFETY-EMERGENCY" => order.Location.BimElementId.Contains("BIM-SEC", StringComparison.OrdinalIgnoreCase) ||
                                      order.WorkOrderNo.Contains("FIRE", StringComparison.OrdinalIgnoreCase) ||
                                      order.WorkOrderNo.Contains("SEC", StringComparison.OrdinalIgnoreCase),
            _ => order.Priority >= Priority.High
        };

    private static bool IsControlAlarm(MonitoringAlarmEvent alarm, PlatformGovernanceControl control) =>
        control.ControlCode switch
        {
            "GOV-ENERGY-COST" => alarm.PointCode.StartsWith("PWR-", StringComparison.OrdinalIgnoreCase) ||
                                 alarm.PointCode.StartsWith("HVAC-", StringComparison.OrdinalIgnoreCase) ||
                                 alarm.PointCode.StartsWith("WATER-", StringComparison.OrdinalIgnoreCase),
            "GOV-SAFETY-EMERGENCY" => alarm.PointCode.StartsWith("FIRE-", StringComparison.OrdinalIgnoreCase) ||
                                      alarm.PointCode.StartsWith("SEC-", StringComparison.OrdinalIgnoreCase),
            _ => alarm.RiskLevel >= TelemetryRiskLevel.Warning
        };

    private static PlatformGovernanceControl[] BuildSeedControls() =>
    [
        new PlatformGovernanceControl("GOV-SLA-ONE-STOP", "一站式服务 SLA 与闭环质量", PlatformGovernanceControlType.QualitySla, "后勤管理部", "SLA 达成率", 95m, 93.6m, "%", PlatformGovernanceControlStatus.Attention, SeedTime.AddDays(2), "一站式服务 / 工单调度", BuildSourceEvidence()),
        new PlatformGovernanceControl("GOV-CONTRACT-TEAM", "外包合同与班组绩效复核", PlatformGovernanceControlType.ContractPerformance, "综合管理岗", "合同履约得分", 90m, 87m, "分", PlatformGovernanceControlStatus.Attention, SeedTime.AddDays(5), "合同管理 / 考核管理", BuildSourceEvidence()),
        new PlatformGovernanceControl("GOV-ENERGY-COST", "能耗成本异常治理", PlatformGovernanceControlType.EnergyCost, "能源管理岗", "今日能耗成本", 1800m, 0m, "元", PlatformGovernanceControlStatus.OnTrack, SeedTime.AddDays(3), "能耗与运行绩效", BuildSourceEvidence()),
        new PlatformGovernanceControl("GOV-SAFETY-EMERGENCY", "消防安防应急联动复盘", PlatformGovernanceControlType.SafetyEmergency, "安全应急管理岗", "未闭环事件", 0m, 0m, "项", PlatformGovernanceControlStatus.Attention, SeedTime.AddDays(1), "消防/安防/应急联动", BuildSourceEvidence())
    ];

    private static PlatformGovernanceAction[] BuildSeedActions() =>
    [
        new PlatformGovernanceAction("ACT-GOV-SLA-ONE-STOP-001", "GOV-SLA-ONE-STOP", "复核夜间高优先级工单派工超时原因", "后勤调度员", "系统种子", GovernanceActionStatus.InProgress, SeedTime, SeedTime.AddDays(1), null)
    ];

    private static PlatformGovernanceAuditEntry[] BuildSeedAuditTrail() =>
    [
        new PlatformGovernanceAuditEntry("AUD-GOV-SLA-ONE-STOP-001", "GOV-SLA-ONE-STOP", "SeedControl", "系统种子", SeedTime, "建立 SLA 治理项和初始整改动作")
    ];

    private static FeatureEvidence[] BuildSourceEvidence() =>
    [
        new FeatureEvidence("综合管理与运营闭环", ["中科医信", "PPT"], "竞品功能树包含质量体系、合同、考核、服务品质和运营分析；PPT 将质量、成本、能耗和安全纳入综合管理闭环。"),
        new FeatureEvidence("客户运行数据驱动治理项", ["北建院", "中科医信", "PPT"], "客户调研数据中的强电、暖通、给排水、消防安防等系统需要从专项告警、工单和能耗结果上卷到管理层治理动作。")
    ];

    private void PersistSeedData()
    {
        if (_persistence is null)
        {
            return;
        }

        foreach (var control in _controls.Values)
        {
            _persistence.SaveControl(control);
        }

        foreach (var action in _actions)
        {
            _persistence.SaveAction(action);
        }

        foreach (var entry in _auditTrail)
        {
            _persistence.SaveAuditEntry(entry);
        }
    }
}
