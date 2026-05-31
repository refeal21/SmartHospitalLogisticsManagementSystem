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

        return new LogisticsBlueprint(
            29,
            156,
            navigationGroups,
            featureGroups,
            workflows,
            metrics,
            teamLoads,
            BuildFeatureEvidence(),
            BuildCustomerDataSystems(),
            BuildCompetitorModules(),
            BuildImplementationPhases());
    }

    private static FeatureEvidence[] BuildFeatureEvidence() =>
    [
        new FeatureEvidence("个人工作台", ["中科医信"], "支持待办、报警、快捷导航、巡检任务统计、工作日历和应用中心。"),
        new FeatureEvidence("统一登录与权限", ["中科医信"], "覆盖用户、角色、平台/子系统权限、单点登录、登录记录和数据权限。"),
        new FeatureEvidence("工单全流程管理", ["中科医信", "PPT"], "竞品要求填报、派单、接单、挂单、转单、完工、验收、评价；PPT 要求一站式服务闭环。"),
        new FeatureEvidence("消息推送与待办", ["中科医信"], "统一接入报警、任务、审批、通知，支持 WEB/APP/短信/微信等通道。"),
        new FeatureEvidence("设备设施资产台账", ["中科医信", "PPT"], "竞品要求资产分类、台账、维修；PPT 要求基础运行设备设施全生命周期管理。"),
        new FeatureEvidence("巡检保养计划", ["中科医信", "PPT"], "竞品要求工作日历、计划、巡检/保养任务；PPT 要求监测预警和运维处置联动。"),
        new FeatureEvidence("仓库耗材与备件", ["中科医信", "PPT"], "竞品要求仓库、采购决策、耗材精细化；PPT 一站式服务中心联动备件库存。"),
        new FeatureEvidence("供配电监测", ["北建院", "中科医信", "PPT"], "客户表包含变电室智能配电、多功能远传电表、电压电流功率电能等字段；竞品有供配电监测系统。"),
        new FeatureEvidence("暖通冷热站监测", ["北建院", "中科医信", "PPT"], "客户表包含冷源、空调水、空气处理、新风、净化空调等数据；竞品有冷热站监测。"),
        new FeatureEvidence("给排水监测", ["北建院", "中科医信", "PPT"], "客户表包含给水、热水、中水、排水、消防水等系统；竞品有给排水监测。"),
        new FeatureEvidence("医用气体监测", ["北建院", "中科医信", "PPT"], "客户表和 PPT 均涉及医用气体，竞品细化到氧气、压缩空气、负压真空和汇流排。"),
        new FeatureEvidence("环境质量监测", ["北建院", "中科医信", "PPT"], "客户表包含室内空气质量、CO2、温湿度等点位；竞品有环境质量监测。"),
        new FeatureEvidence("污水站监测", ["北建院", "中科医信", "PPT"], "客户表给排水与污水数据结合 PPT 医疗废水监测，竞品有污水站报警处理。"),
        new FeatureEvidence("电梯运行监测", ["中科医信", "PPT"], "竞品包含运行实时监测、报警、可视报警求助和统计分析；PPT 有电梯管理专项。"),
        new FeatureEvidence("医疗废弃物管理", ["中科医信", "PPT"], "竞品包含收集总览、全生命周期、暂存站、扎带和统计；PPT 环境监管涉及医废处置。"),
        new FeatureEvidence("可视化空间运维", ["中科医信", "PPT"], "竞品包含建筑空间、平面图、空间使用、统计和租赁；PPT 要求 BIM/空间底座。"),
        new FeatureEvidence("综合能耗监管", ["中科医信", "PPT"], "竞品包含用能总览、实时监控、告警、分析、报表、成本和配置；PPT 综合管理包含能耗成本。"),
        new FeatureEvidence("服务品质管理", ["中科医信", "PPT"], "竞品包含品质管理和报表；PPT 质量管理按 PDCA 联动合同考核。")
    ];

    private static CustomerDataSystem[] BuildCustomerDataSystems() =>
    [
        new CustomerDataSystem(
            "结构健康系统",
            ["沉降传感器", "位移传感器", "应变计传感器", "温度监测传感器", "强震仪"],
            ["沉降传感器", "位移传感器", "应变计", "温度监测传感器", "强震仪"],
            ["结构监测点位"],
            ["院区", "楼号", "楼层", "空间编号", "设备编号", "时间", "数值"],
            ["结构位移", "沉降", "应变", "温度", "震动"],
            ["总务处管理者", "医院管理者", "第三方服务方"],
            ["控制室电脑端", "移动端"]),
        new CustomerDataSystem(
            "强电系统",
            ["变电室智能配电系统", "多功能远传电表系统", "电力系统", "照明", "防雷接地"],
            ["多功能测量仪表", "电能质量测量仪表", "单相/三相多功能电表"],
            ["变电室低压进线及馈出回路", "楼层配电柜", "配电分盘"],
            ["电压", "电流", "有功功率", "无功功率", "视在功率", "功率因数", "频率", "电度", "谐波", "温湿度"],
            ["高低压出线柜监测", "温湿度", "浪涌保护器运行状态", "照明设备数量", "光照度"],
            ["电工班工作人员", "总务处管理者", "医院管理者"],
            ["控制室电脑端", "移动端"]),
        new CustomerDataSystem(
            "供暖空调系统",
            ["热源及供暖水系统", "通风系统", "冷源及空调水系统", "空气处理机组", "新风机组", "净化空调系统", "医用气体系统", "燃气系统"],
            ["温度传感器", "湿度传感器", "CO2浓度传感器", "压差传感器", "空气质量传感器"],
            ["送风口", "水盘管处", "室内点位", "过滤器处", "送风机处"],
            ["供回水温", "压力", "流量", "能耗", "电流", "电压", "启停状态", "故障状态", "风阀开度", "水阀开度", "CO2浓度"],
            ["冷源状态", "空调水系统状态", "净化空调状态", "医气压力与流量", "室内空气质量"],
            ["暖通班工作人员", "总务处管理者", "医院管理者", "第三方服务"],
            ["控制室电脑端", "移动端"]),
        new CustomerDataSystem(
            "给排水系统",
            ["给水系统", "热水系统", "中水系统", "排水系统", "消防水", "饮用水系统", "供油系统"],
            ["压力传感器", "液位传感器", "流量计", "水质传感器"],
            ["泵房", "水箱", "管网", "污水处理站"],
            ["压力", "流量", "液位", "水温", "水质", "泵运行状态", "故障状态"],
            ["医疗废水监测", "污水系统监测", "供水安全保障", "设备轮换运行"],
            ["给排水班工作人员", "总务处管理者", "医院管理者", "第三方服务"],
            ["控制室电脑端", "移动端"]),
        new CustomerDataSystem(
            "火灾自动报警及联动控制系统",
            ["火灾自动报警", "电气火灾监控系统", "防火门监控", "可燃气体探测报警系统"],
            ["烟感", "温感", "可燃气体探测器", "电气火灾监测设备", "防火门监控器"],
            ["消防控制室", "楼层公共区", "设备间"],
            ["报警状态", "设备状态", "联动状态", "故障状态", "时间"],
            ["消防报警联动", "设备故障", "防火门状态", "可燃气体浓度"],
            ["消防值班人员", "总务处管理者", "医院管理者"],
            ["控制室电脑端", "移动端"]),
        new CustomerDataSystem(
            "智能化系统",
            ["智能化集成系统", "建筑设备管理系统", "公共安全系统", "信息设施系统", "信息化应用系统", "机房工程"],
            ["摄像机", "门禁", "楼控网关", "网络设备", "机房环境传感器"],
            ["安防机房", "弱电机房", "楼宇设备间", "公共区域"],
            ["设备在线状态", "报警状态", "运行参数", "事件记录", "空间定位"],
            ["系统集成数据", "安防事件", "楼控状态", "机房环境"],
            ["信息化管理者", "保卫人员", "总务处管理者", "医院管理者"],
            ["控制室电脑端", "移动端"]),
        new CustomerDataSystem(
            "机器人",
            ["变电室巡检机器人", "安防巡检机器人", "配送机器人"],
            ["机器人本体传感器", "摄像头", "定位模块", "任务状态采集"],
            ["变电室", "公共安防区域", "配送路线"],
            ["任务状态", "定位", "巡检结果", "异常告警", "配送状态"],
            ["机器人任务", "异常识别", "路线执行", "配送完成率"],
            ["运维人员", "保卫人员", "配送班组", "医院管理者"],
            ["移动端", "控制室电脑端"]),
        new CustomerDataSystem(
            "其他物联设备",
            ["智慧卫生间", "智慧食堂系统", "无人零售物联系统"],
            ["厕位传感器", "空气质量传感器", "耗材传感器", "油烟监测", "食安监测"],
            ["卫生间", "食堂后厨", "公共服务区"],
            ["厕位状态", "空气质量", "耗材余量", "油烟状态", "设备运行状态"],
            ["智慧卫生间服务状态", "明厨亮灶联动", "食安强化监测", "无人零售状态"],
            ["保洁班组", "食堂管理人员", "总务处管理者", "医院管理者"],
            ["移动端", "控制室电脑端"])
    ];

    private static CompetitorModule[] BuildCompetitorModules() =>
    [
        new CompetitorModule("智慧医院运行保障系统基础服务模块", ["个人工作台", "用户管理", "角色权限管理", "统一登录管理", "工单管理", "消息推送管理", "数据安全管理", "日志管理", "人员信息管理", "空间信息管理", "组织信息管理", "资产信息管理", "岗位信息管理"]),
        new CompetitorModule("可视化数据驾驶舱管理系统", ["综合服务", "品质管理", "设备安全", "能耗管理"]),
        new CompetitorModule("智能移动应用终端系统", ["配置注册管理", "消息管理", "一站式服务管理", "统一报警管理", "设备运维管理", "机电安全监测", "数据统计分析", "职工服务管理"]),
        new CompetitorModule("智能基础运行资产台账管理系统", ["资产分类管理", "资产台账管理", "资产维修管理"]),
        new CompetitorModule("基础运行设备设施使用运维系统", ["工作日历", "消息管理", "报修管理", "巡检/保养管理", "计划管理"]),
        new CompetitorModule("后勤供料配件耗材库智能管理系统", ["基础信息管理", "仓库管理", "采购决策", "耗材精细化", "统计分析"]),
        new CompetitorModule("智慧电梯运行监测管理系统", ["运行实时监测", "报警管理", "可视报警求助", "统计分析"]),
        new CompetitorModule("医用气体预警监测管理系统", ["氧气系统监测", "压缩空气系统监测", "负压真空系统监测", "特殊气体汇流排监测", "报警接收与处置"]),
        new CompetitorModule("供配电监测管理系统", ["运行总览", "电力监测", "数据报表"]),
        new CompetitorModule("给排水监测管理系统", ["运行总览", "运行监测", "APP远程监控"]),
        new CompetitorModule("冷热站监测管理系统", ["运行总览", "运行监测", "App远程监控"]),
        new CompetitorModule("环境质量监测管理系统", ["运行总览", "运行监测", "监测分区配置", "环境区间配置"]),
        new CompetitorModule("污水站监测管理系统", ["实时监测", "报警处理"]),
        new CompetitorModule("智能一站式服务综合管理系统", ["一站式服务调度中心", "维修管理（含移动维修）", "移动报修", "工程仓库管理", "报表管理", "大屏管理"]),
        new CompetitorModule("智慧保洁服务管理系统", ["工作日历", "任务概览", "计划管理", "任务管理", "应急保洁", "统计分析管理"]),
        new CompetitorModule("医疗废弃物综合管理系统", ["收集总览", "数据可视化大屏", "医废全生命周期监管", "暂存站监管", "医废扎带管理", "统计分析"]),
        new CompetitorModule("可视化空间运维管理系统", ["建筑空间管理", "空间平面图", "空间使用管理", "空间总计分析", "空间租赁管理"]),
        new CompetitorModule("综合能耗智能监管系统", ["用能总览", "实时监控", "告警管理", "用能分析", "报表管理", "成本管理", "配置管理"]),
        new CompetitorModule("后勤业务集成管理系统", ["业务集成", "数据联动", "统一入口"]),
        new CompetitorModule("服务品质集成管理系统", ["服务品质", "考核评价", "报告分析"])
    ];

    private static ImplementationPhase[] BuildImplementationPhases() =>
    [
        new ImplementationPhase(1, "基础平台与工作台", ["个人工作台", "消息", "待办", "权限", "登录日志"]),
        new ImplementationPhase(2, "一站式服务与工单", ["报修", "派单", "接单", "挂单", "转单", "完工", "验收", "评价", "超时提醒"]),
        new ImplementationPhase(3, "资产台账与巡检保养", ["设备设施", "空间", "岗位", "人员", "计划工单", "巡检/保养"]),
        new ImplementationPhase(4, "客户物联数据接入模型", ["强电", "暖通", "给排水", "医气", "环境", "污水", "时序字段"]),
        new ImplementationPhase(5, "专项系统", ["电梯", "医气", "供配电", "给排水", "冷热站", "环境质量", "污水站", "医废", "能耗"]),
        new ImplementationPhase(6, "综合管理", ["合同", "质量", "考核", "服务品质", "报表", "运营分析"])
    ];
}
