using SmartHospitalLogistics.Domain;

namespace SmartHospitalLogistics.Application;

public interface IOperationsDashboardService
{
    OperationsDashboard GetDashboard();

    LogisticsBlueprint GetLogisticsBlueprint();

    IReadOnlyList<WorkOrder> GetActiveWorkOrders();

    IReadOnlyList<FacilityAsset> GetRiskAssets();
}

public sealed class OperationsDashboardService : IOperationsDashboardService
{
    private static readonly DateTimeOffset SeedTime = new(2026, 5, 30, 9, 30, 0, TimeSpan.FromHours(8));

    private readonly OperationsDashboard _dashboard = BuildDashboard();

    private readonly LogisticsBlueprint _blueprint = BuildBlueprint();

    public OperationsDashboard GetDashboard() => _dashboard;

    public LogisticsBlueprint GetLogisticsBlueprint() => _blueprint;

    public IReadOnlyList<WorkOrder> GetActiveWorkOrders() =>
        _dashboard.WorkOrders
            .Where(order => order.Status is not WorkOrderStatus.Closed)
            .OrderByDescending(order => order.Priority)
            .ThenBy(order => order.SlaDueAt)
            .ToArray();

    public IReadOnlyList<FacilityAsset> GetRiskAssets() =>
        _dashboard.Assets
            .Where(asset => asset.Status is FacilityStatus.Warning or FacilityStatus.Fault)
            .OrderByDescending(asset => asset.Status)
            .ThenBy(asset => asset.LastSignalAt)
            .ToArray();

    private static OperationsDashboard BuildDashboard()
    {
        var outpatientLobby = new SpatialLocation("同仁亦庄院区", "门诊医技楼", "F1", "共享大厅", "BIM-OPD-F1-LOBBY");
        var inpatientWard = new SpatialLocation("同仁亦庄院区", "住院楼", "F8", "眼科病区", "BIM-IPD-F8-WARD");
        var energyRoom = new SpatialLocation("同仁亦庄院区", "能源中心", "B1", "冷站机房", "BIM-ENE-B1-CHILLER");
        var wasteRoom = new SpatialLocation("同仁亦庄院区", "后勤楼", "F1", "医废暂存间", "BIM-LOG-F1-WASTE");

        var modules = new[]
        {
            new OperationalModule(
                "FOUNDATION",
                "基础运行保障",
                OperationDomain.Foundation,
                "全域感知、告警预警、预测防控，保障水电气暖和关键设施稳定。",
                10,
                56,
                ["BIM空间底座", "IoT遥测", "一站式服务中心"]),
            new OperationalModule(
                "LOGISTICS_SERVICE",
                "后勤服务",
                OperationDomain.LogisticsService,
                "数据流转、线上闭环、人性化服务，覆盖保洁、配送、餐饮、被服和病区支持。",
                8,
                43,
                ["工单中心", "仓储库存", "外包绩效"]),
            new OperationalModule(
                "ENVIRONMENT",
                "环境监管",
                OperationDomain.Environment,
                "智能调控、智慧体验、合规管控，沉淀监测、预警和处置策略。",
                3,
                23,
                ["环境传感器", "BIM点位", "第三方运维"]),
            new OperationalModule(
                "INTEGRATED",
                "综合管理",
                OperationDomain.IntegratedManagement,
                "降本增效、精细管理、统筹优化，支撑质量、合同、考核和成本闭环。",
                8,
                30,
                ["质量体系", "合同台账", "考核指标"])
        };

        var assets = new[]
        {
            new FacilityAsset("ELV-OPD-01", "门诊楼 1 号医梯", "电梯管理", outpatientLobby, FacilityStatus.Warning, SeedTime.AddMinutes(-8), "运行频次突增，建议提前巡检曳引系统"),
            new FacilityAsset("CHW-B1-02", "冷站 2 号冷冻泵", "暖通空调", energyRoom, FacilityStatus.Normal, SeedTime.AddMinutes(-2), "运行稳定"),
            new FacilityAsset("MEDGAS-IPD-8F", "住院 8F 医用气体分区阀箱", "医用气体", inpatientWard, FacilityStatus.Maintenance, SeedTime.AddMinutes(-25), "计划保养中"),
            new FacilityAsset("WASTE-F1-01", "医废暂存间负压设备", "医疗废物", wasteRoom, FacilityStatus.Fault, SeedTime.AddMinutes(-5), "负压值低于阈值，已触发应急工单")
        };

        var workOrders = new[]
        {
            new WorkOrder("WO-20260530-0001", "医废暂存间负压异常处置", "环境应急", Priority.Critical, WorkOrderStatus.Escalated, wasteRoom, "环境监管班组", SeedTime.AddMinutes(-42), SeedTime.AddMinutes(18)),
            new WorkOrder("WO-20260530-0002", "门诊大厅医梯运行异响巡检", "设备维修", Priority.High, WorkOrderStatus.Dispatched, outpatientLobby, "电梯维保组", SeedTime.AddMinutes(-26), SeedTime.AddHours(2)),
            new WorkOrder("WO-20260530-0003", "眼科病区被服补给", "后勤配送", Priority.Normal, WorkOrderStatus.InProgress, inpatientWard, "被服配送组", SeedTime.AddMinutes(-18), SeedTime.AddHours(3)),
            new WorkOrder("WO-20260530-0004", "冷站机房夜间节能策略复核", "能耗优化", Priority.Normal, WorkOrderStatus.PendingAcceptance, energyRoom, "能源管理组", SeedTime.AddHours(-4), SeedTime.AddHours(4))
        };

        var signals = new[]
        {
            new EnvironmentSignal("ENV-WASTE-PRESSURE", "医废暂存间负压", "pressure", -3.2m, "Pa", SignalStatus.Critical, wasteRoom, SeedTime.AddMinutes(-3)),
            new EnvironmentSignal("ENV-IPD-8F-TEMP", "住院 8F 温度", "temperature", 23.6m, "C", SignalStatus.Normal, inpatientWard, SeedTime.AddMinutes(-1)),
            new EnvironmentSignal("ENV-OPD-CO2", "门诊大厅 CO2", "co2", 860m, "ppm", SignalStatus.Warning, outpatientLobby, SeedTime.AddMinutes(-4)),
            new EnvironmentSignal("ENE-CHILLER-KWH", "冷站实时功率", "energy", 436.8m, "kW", SignalStatus.Normal, energyRoom, SeedTime.AddMinutes(-2))
        };

        var metrics = new[]
        {
            new LogisticsMetric("SLA", "今日 SLA 达成率", "93.6%", "+2.1%", OperationDomain.LogisticsService),
            new LogisticsMetric("OPEN", "未闭环工单", "4", "-3", OperationDomain.LogisticsService),
            new LogisticsMetric("ALARM", "高风险告警", "2", "+1", OperationDomain.Environment),
            new LogisticsMetric("ENERGY", "单位面积能耗", "0.82 kWh/m2", "-6.4%", OperationDomain.IntegratedManagement)
        };

        return new OperationsDashboard(SeedTime, modules, assets, workOrders, signals, metrics);
    }

    private static LogisticsBlueprint BuildBlueprint()
    {
        var navigationGroups = new[]
        {
            new NavigationGroup("WORKBENCH", "运营工作台", ["后勤首页", "待办中心", "风险告警"]),
            new NavigationGroup("SERVICE", "一站式服务", ["服务受理", "工单调度", "任务执行", "验收回访", "服务评价"]),
            new NavigationGroup("FACILITY", "设备设施", ["设备台账", "巡检保养", "维修记录", "备件库存", "电梯专项", "暖通/给排水/医气专项"]),
            new NavigationGroup("SPATIAL", "BIM 空间", ["空间台账", "楼层视图", "设备点位", "告警点位", "工单点位"]),
            new NavigationGroup("ENVIRONMENT", "环境监管", ["环境点位", "预警池", "报警策略", "医废处置", "智慧卫生间"]),
            new NavigationGroup("MANAGEMENT", "综合管理", ["质量标准", "合同管理", "考核管理", "人员班组", "运营分析", "能耗成本", "碳排双控"]),
            new NavigationGroup("GOVERNANCE", "系统治理", ["角色权限", "流程配置", "SLA 配置", "字典配置", "审计日志"])
        };

        var featureGroups = new[]
        {
            new LogisticsFeatureGroup(
                "FOUNDATION",
                "基础运行",
                OperationDomain.Foundation,
                10,
                56,
                ["设备设施管理", "智慧电力", "智能机房", "智慧照明", "给排水系统管理", "能源管理", "暖通空调管理", "医用气体管理", "电梯管理", "食堂基础设施"]),
            new LogisticsFeatureGroup(
                "LOGISTICS_SERVICE",
                "后勤服务",
                OperationDomain.LogisticsService,
                8,
                43,
                ["一站式服务中心", "话务系统", "膳食服务管理", "工装被服管理", "房屋空间管理", "后勤资产实物管理", "物资配送管理", "太平间管理"]),
            new LogisticsFeatureGroup(
                "ENVIRONMENT",
                "环境监管",
                OperationDomain.Environment,
                3,
                23,
                ["室内外环境管理", "智慧卫生间管理", "医疗废物收集处置管理"]),
            new LogisticsFeatureGroup(
                "INTEGRATED",
                "综合管理",
                OperationDomain.IntegratedManagement,
                8,
                30,
                ["质量体系", "合同管理", "考核管理", "运营分析", "人员管理", "运行管理", "能耗成本管理", "碳排双控"])
        };

        var workflows = new[]
        {
            new LogisticsWorkflow("SERVICE_LOOP", "服务闭环", ["服务请求", "受理登记", "工单池", "派工", "执行反馈", "验收回访", "评价归档"]),
            new LogisticsWorkflow("FACILITY_LOOP", "设施运行闭环", ["设备台账", "监测巡检", "异常发现", "维修保养", "复盘分析", "生命周期记录"]),
            new LogisticsWorkflow("ENVIRONMENT_LOOP", "环境监管闭环", ["点位采集", "阈值策略", "预警池", "告警确认", "处置工单", "策略优化"]),
            new LogisticsWorkflow("QUALITY_COST_LOOP", "质量成本闭环", ["质量标准", "过程采集", "结果评价", "问题整改", "考核结算", "运营分析"])
        };

        var metrics = new[]
        {
            new WorkbenchMetric("TODAY_ORDERS", "今日工单", "126", "+18", "blue"),
            new WorkbenchMetric("SLA_RISK", "超时风险", "7", "+2", "orange"),
            new WorkbenchMetric("ACTIVE_ALERTS", "运行告警", "12", "-4", "red"),
            new WorkbenchMetric("ASSET_HEALTH", "设备完好率", "98.2%", "+0.6%", "green"),
            new WorkbenchMetric("SLA_RATE", "SLA 达成率", "93.6%", "+2.1%", "cyan")
        };

        var teamLoads = new[]
        {
            new TeamLoad("综合维修班", "一站式服务", 18, 24, "可承接一般维修与巡检工单"),
            new TeamLoad("电梯维保组", "设备设施", 6, 8, "保留困人事件应急余量"),
            new TeamLoad("环境监管班组", "环境监管", 11, 12, "医废负压告警优先派工"),
            new TeamLoad("能源管理组", "基础运行", 9, 16, "适合承接夜间节能策略复核")
        };

        return new LogisticsBlueprint(29, 156, navigationGroups, featureGroups, workflows, metrics, teamLoads);
    }
}
