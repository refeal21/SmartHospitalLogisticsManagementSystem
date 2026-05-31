import { useEffect, useMemo, useState } from 'react'
import './App.css'

type OperationDomain = 'Foundation' | 'LogisticsService' | 'Environment' | 'IntegratedManagement'
type Priority = 'Low' | 'Normal' | 'High' | 'Critical'
type WorkOrderStatus =
  | 'New'
  | 'Dispatched'
  | 'Accepted'
  | 'InProgress'
  | 'Suspended'
  | 'Transferred'
  | 'PendingAcceptance'
  | 'PendingEvaluation'
  | 'Closed'
  | 'Escalated'
type FacilityStatus = 'Normal' | 'Warning' | 'Fault' | 'Maintenance'
type SignalStatus = 'Normal' | 'Warning' | 'Critical'
type WorkOrderTransitionAction =
  | 'Accept'
  | 'Suspend'
  | 'Transfer'
  | 'Complete'
  | 'AcceptCompletion'
  | 'RejectCompletion'
  | 'Evaluate'
  | 'Escalate'

type SpatialLocation = {
  campus: string
  building: string
  floor: string
  room: string
  bimElementId: string
}

type WorkOrder = {
  workOrderNo: string
  title: string
  serviceType: string
  priority: Priority
  status: WorkOrderStatus
  location: SpatialLocation
  responsibleTeam: string
  createdAt: string
  slaDueAt: string
}

type FacilityAsset = {
  assetCode: string
  name: string
  system: string
  location: SpatialLocation
  status: FacilityStatus
  lastSignalAt: string
  currentRisk: string
}

type EnvironmentSignal = {
  signalCode: string
  name: string
  metric: string
  value: number
  unit: string
  status: SignalStatus
  location: SpatialLocation
  collectedAt: string
}

type OperationsDashboard = {
  generatedAt: string
  assets: FacilityAsset[]
  workOrders: WorkOrder[]
  environmentSignals: EnvironmentSignal[]
  openWorkOrders: number
  escalatedWorkOrders: number
  riskAssets: number
  warningSignals: number
}

type NavigationGroup = {
  code: string
  name: string
  items: string[]
}

type LogisticsFeatureGroup = {
  code: string
  name: string
  domain: OperationDomain
  primaryModuleCount: number
  secondaryItemCount: number
  modules: string[]
}

type LogisticsWorkflow = {
  code: string
  name: string
  steps: string[]
}

type WorkbenchMetric = {
  code: string
  name: string
  value: string
  trend: string
  tone: string
}

type TeamLoad = {
  teamName: string
  domain: string
  activeTasks: number
  capacity: number
  recommendation: string
}

type FeatureEvidence = {
  featureName: string
  sources: string[]
  evidenceSummary: string
}

type CustomerDataSystem = {
  major: string
  subsystems: string[]
  sensors: string[]
  locations: string[]
  dataFields: string[]
  desiredData: string[]
  roles: string[]
  endpoints: string[]
}

type CompetitorModule = {
  name: string
  featureAreas: string[]
}

type ImplementationPhase = {
  order: number
  name: string
  capabilities: string[]
}

type LogisticsBlueprint = {
  pptReportedPrimaryModuleTotal: number
  pptReportedSecondaryItemTotal: number
  navigationGroups: NavigationGroup[]
  featureGroups: LogisticsFeatureGroup[]
  workflows: LogisticsWorkflow[]
  workbenchMetrics: WorkbenchMetric[]
  teamLoads: TeamLoad[]
  featureEvidence: FeatureEvidence[]
  customerDataSystems: CustomerDataSystem[]
  competitorModules: CompetitorModule[]
  implementationPhases: ImplementationPhase[]
}

type WorkOrderTimelineEntry = {
  occurredAt: string
  operator: string
  action: string
  fromStatus: WorkOrderStatus
  toStatus: WorkOrderStatus
  remark: string
  rating?: number | null
}

type DispatchRecommendation = {
  workOrderNo: string
  recommendedTeam: string
  reason: string
  priority: Priority
  slaMinutesRemaining: number
}

type SlaRiskSummary = {
  openWorkOrders: number
  overdueWorkOrders: number
  dueSoonWorkOrders: number
  escalatedWorkOrders: number
  highestRiskLevel: string
  highestRiskWorkOrderNo?: string | null
}

type WorkOrderDetail = {
  workOrder: WorkOrder
  location: SpatialLocation
  sourceEvidence: FeatureEvidence[]
  timeline: WorkOrderTimelineEntry[]
  slaRiskLevel: string
  slaMinutesRemaining: number
  allowedActions: string[]
}

type DispatchBoard = {
  workOrders: WorkOrder[]
  teamLoads: TeamLoad[]
  recommendations: DispatchRecommendation[]
  slaRisk: SlaRiskSummary
}

type AssetCriticality = 'Low' | 'Medium' | 'High' | 'LifeSafety'
type MaintenanceTaskType = 'Inspection' | 'PreventiveMaintenance' | 'Calibration' | 'SafetyCheck'
type MaintenanceTaskStatus =
  | 'Planned'
  | 'Due'
  | 'Overdue'
  | 'Completed'
  | 'RequiresRepair'
  | 'ConvertedToWorkOrder'
type MaintenanceOutcome = 'Normal' | 'Abnormal' | 'Skipped'

type AssetLedgerItem = {
  assetCode: string
  name: string
  system: string
  criticality: AssetCriticality
  location: SpatialLocation
  status: FacilityStatus
  ownerTeam: string
  manufacturer: string
  model: string
  commissionedOn: string
  maintenanceStrategy: string
  healthScore: number
  currentRisk: string
  sourceTags: string[]
}

type InspectionChecklistResult = {
  code: string
  result: string
  remark: string
}

type MaintenancePlan = {
  planCode: string
  assetCode: string
  name: string
  taskType: MaintenanceTaskType
  cycleDays: number
  nextDueAt: string
  responsibleTeam: string
  checklistTemplate: { code: string; name: string; standard: string; required: boolean }[]
  sourceEvidence: FeatureEvidence[]
}

type MaintenanceTask = {
  taskNo: string
  planCode: string
  assetCode: string
  title: string
  taskType: MaintenanceTaskType
  status: MaintenanceTaskStatus
  priority: Priority
  scheduledAt: string
  dueAt: string
  responsibleTeam: string
  checklistResults: InspectionChecklistResult[]
  outcome?: MaintenanceOutcome | null
  completedAt?: string | null
  completedBy?: string | null
  workOrderNo?: string | null
}

type AssetLifecycleEvent = {
  occurredAt: string
  assetCode: string
  eventType: string
  operator: string
  summary: string
}

type AssetMaintenanceKpi = {
  totalAssets: number
  riskAssets: number
  dueTasks: number
  overdueTasks: number
  preventiveCompletionRate: number
  averageHealthScore: number
}

type MaintenanceGeneratedWorkOrder = {
  workOrderNo: string
  title: string
  serviceType: string
  priority: Priority
  status: WorkOrderStatus
  location: SpatialLocation
  responsibleTeam: string
  createdAt: string
}

type AssetMaintenanceBoard = {
  generatedAt: string
  assets: AssetLedgerItem[]
  plans: MaintenancePlan[]
  dueTasks: MaintenanceTask[]
  lifecycleEvents: AssetLifecycleEvent[]
  sourceEvidence: FeatureEvidence[]
  kpis: AssetMaintenanceKpi
}

type AssetMaintenanceDetail = {
  asset: AssetLedgerItem
  plans: MaintenancePlan[]
  tasks: MaintenanceTask[]
  lifecycle: AssetLifecycleEvent[]
  sourceEvidence: FeatureEvidence[]
}

type MaintenanceTaskOperationResult = {
  succeeded: boolean
  errorMessage?: string | null
  task?: MaintenanceTask | null
  generatedWorkOrder?: MaintenanceGeneratedWorkOrder | null
  notFound: boolean
}

type IotSystemCategory =
  | 'StrongElectric'
  | 'Hvac'
  | 'WaterSupplyDrainage'
  | 'MedicalGas'
  | 'EnvironmentQuality'
  | 'Sewage'
type TelemetryRiskLevel = 'Normal' | 'Warning' | 'Critical'
type MonitoringAlarmStatus = 'New' | 'Acknowledged' | 'ConvertedToWorkOrder' | 'Closed'
type ThresholdDirection = 'Above' | 'Below' | 'OutsideRange'

type IotSystemProfile = {
  category: IotSystemCategory
  name: string
  subsystems: string[]
  roles: string[]
  endpoints: string[]
}

type IotMetricDefinition = {
  code: string
  name: string
  unit: string
  dataType: string
  sourceField: string
}

type IotMonitoringPoint = {
  pointCode: string
  name: string
  category: IotSystemCategory
  location: SpatialLocation
  deviceCode: string
  protocolAdapter: string
  metrics: IotMetricDefinition[]
  sourceEvidence: FeatureEvidence[]
}

type TelemetryThresholdRule = {
  pointCode: string
  metricCode: string
  direction: ThresholdDirection
  warningMin?: number | null
  warningMax?: number | null
  criticalMin?: number | null
  criticalMax?: number | null
  ruleSummary: string
}

type TelemetryReading = {
  pointCode: string
  metricCode: string
  value: number
  unit: string
  collectedAt: string
  riskLevel: TelemetryRiskLevel
  ruleSummary: string
}

type MonitoringAlarmEvent = {
  alarmNo: string
  pointCode: string
  metricCode: string
  title: string
  riskLevel: TelemetryRiskLevel
  status: MonitoringAlarmStatus
  location: SpatialLocation
  value: number
  unit: string
  triggeredAt: string
  ruleSummary: string
  responsibleTeam: string
  workOrderNo?: string | null
  acknowledgedBy?: string | null
  acknowledgedAt?: string | null
  lastRemark?: string | null
}

type MonitoringAlarmBoard = {
  generatedAt: string
  alarms: MonitoringAlarmEvent[]
  sourceEvidence: FeatureEvidence[]
}

type IotPointDetail = {
  point: IotMonitoringPoint
  thresholdRules: TelemetryThresholdRule[]
  recentReadings: TelemetryReading[]
  sourceEvidence: FeatureEvidence[]
}

type IotIntegrationCatalog = {
  generatedAt: string
  systems: IotSystemProfile[]
  points: IotMonitoringPoint[]
  thresholdRules: TelemetryThresholdRule[]
  sourceEvidence: FeatureEvidence[]
}

type SpatialPointKind = 'workOrder' | 'asset' | 'alarm' | 'iot'
type SpatialPointTone = 'workorder' | 'asset' | 'alert' | 'normal'

type SpatialOperationPoint = {
  id: string
  kind: SpatialPointKind
  entityId: string
  title: string
  subtitle: string
  location: SpatialLocation
  statusLabel: string
  actionLabel: string
  tone: SpatialPointTone
}

type TelemetryIngestionResult = {
  succeeded: boolean
  errorMessage?: string | null
  pointCode: string
  metricCode: string
  riskLevel: TelemetryRiskLevel
  ruleSummary: string
  reading?: TelemetryReading | null
  notFound: boolean
}

type WorkspacePage = 'overview' | 'dispatch' | 'assets' | 'iot' | 'alerts' | 'spatial' | 'evidence'

const locations = {
  lobby: {
    campus: '同仁亦庄院区',
    building: '门诊医技楼',
    floor: 'F1',
    room: '共享大厅',
    bimElementId: 'BIM-OPD-F1-LOBBY',
  },
  waste: {
    campus: '同仁亦庄院区',
    building: '后勤楼',
    floor: 'F1',
    room: '医废暂存间',
    bimElementId: 'BIM-LOG-F1-WASTE',
  },
  ward: {
    campus: '同仁亦庄院区',
    building: '住院楼',
    floor: 'F8',
    room: '眼科病区',
    bimElementId: 'BIM-IPD-F8-WARD',
  },
  energy: {
    campus: '同仁亦庄院区',
    building: '能源中心',
    floor: 'B1',
    room: '冷站机房',
    bimElementId: 'BIM-ENE-B1-CHILLER',
  },
}

const sourceEvidence: FeatureEvidence[] = [
  {
    featureName: '工单全流程管理',
    sources: ['中科医信', 'PPT'],
    evidenceSummary: '竞品明确报修、派单、接单、挂单、转单、完工、验收、评价；PPT要求一站式服务闭环。',
  },
  {
    featureName: '供配电监测',
    sources: ['北建院', '中科医信', 'PPT'],
    evidenceSummary: '客户数据包含电压、电流、功率、功率因数、频率、电度、谐波；竞品有供配电监测专项。',
  },
  {
    featureName: '医疗废弃物管理',
    sources: ['中科医信', 'PPT'],
    evidenceSummary: '竞品覆盖收集、暂存站、扎带、全生命周期监管；PPT环境监管域要求医废处置。',
  },
  {
    featureName: '综合能耗监管',
    sources: ['中科医信', 'PPT'],
    evidenceSummary: '竞品包含用能总览、实时监控、告警、分析、报表、成本和配置。',
  },
]

const localWorkOrders: WorkOrder[] = [
  {
    workOrderNo: 'WO-20260530-0001',
    title: '医废暂存间负压异常处置',
    serviceType: '环境应急',
    priority: 'Critical',
    status: 'Escalated',
    location: locations.waste,
    responsibleTeam: '未派工',
    createdAt: '2026-05-30T08:48:00+08:00',
    slaDueAt: '2026-05-30T09:48:00+08:00',
  },
  {
    workOrderNo: 'WO-20260530-0002',
    title: '门诊大厅医梯运行异响巡检',
    serviceType: '设备维修',
    priority: 'High',
    status: 'Dispatched',
    location: locations.lobby,
    responsibleTeam: '电梯维保组',
    createdAt: '2026-05-30T09:04:00+08:00',
    slaDueAt: '2026-05-30T11:30:00+08:00',
  },
  {
    workOrderNo: 'WO-20260530-0003',
    title: '眼科病区被服补给',
    serviceType: '后勤配送',
    priority: 'Normal',
    status: 'Accepted',
    location: locations.ward,
    responsibleTeam: '被服配送组',
    createdAt: '2026-05-30T09:12:00+08:00',
    slaDueAt: '2026-05-30T12:30:00+08:00',
  },
  {
    workOrderNo: 'WO-20260530-0004',
    title: '冷站机房夜间节能策略复核',
    serviceType: '能耗优化',
    priority: 'Normal',
    status: 'PendingAcceptance',
    location: locations.energy,
    responsibleTeam: '能源管理组',
    createdAt: '2026-05-30T05:30:00+08:00',
    slaDueAt: '2026-05-30T13:30:00+08:00',
  },
]

const localTeamLoads: TeamLoad[] = [
  { teamName: '环境监管班组', domain: '环境监管', activeTasks: 11, capacity: 12, recommendation: '医废负压告警优先派工' },
  { teamName: '综合维修班', domain: '一站式服务', activeTasks: 18, capacity: 24, recommendation: '可承接一般维修与巡检工单' },
  { teamName: '电梯维保组', domain: '设备设施', activeTasks: 6, capacity: 8, recommendation: '保留困人事件应急余量' },
  { teamName: '能源管理组', domain: '基础运行', activeTasks: 9, capacity: 16, recommendation: '适合承接夜间节能策略复核' },
]

const localBlueprint: LogisticsBlueprint = {
  pptReportedPrimaryModuleTotal: 29,
  pptReportedSecondaryItemTotal: 156,
  navigationGroups: [
    { code: 'WORKBENCH', name: '运营工作台', items: ['后勤首页', '待办中心', '风险告警'] },
    { code: 'SERVICE', name: '一站式服务', items: ['服务受理', '工单调度', '任务执行', '验收回访', '服务评价'] },
    { code: 'FACILITY', name: '设备设施', items: ['设备台账', '巡检保养', '维修记录', '备件库存', '电梯专项'] },
    { code: 'SPATIAL', name: 'BIM 空间', items: ['空间台账', '楼层视图', '设备点位', '告警点位', '工单点位'] },
    { code: 'ENVIRONMENT', name: '环境监管', items: ['环境点位', '预警池', '报警策略', '医废处置', '智慧卫生间'] },
    { code: 'MANAGEMENT', name: '综合管理', items: ['质量标准', '合同管理', '考核管理', '人员班组', '运营分析', '能耗成本'] },
    { code: 'GOVERNANCE', name: '系统治理', items: ['角色权限', '流程配置', 'SLA 配置', '字典配置', '审计日志'] },
  ],
  featureGroups: [
    {
      code: 'FOUNDATION',
      name: '基础运行',
      domain: 'Foundation',
      primaryModuleCount: 10,
      secondaryItemCount: 56,
      modules: ['设备设施管理', '智慧电力', '智能机房', '智慧照明', '给排水系统管理', '能源管理', '暖通空调管理', '医用气体管理', '电梯管理', '食堂基础设施'],
    },
    {
      code: 'LOGISTICS_SERVICE',
      name: '后勤服务',
      domain: 'LogisticsService',
      primaryModuleCount: 8,
      secondaryItemCount: 43,
      modules: ['一站式服务中心', '话务系统', '膳食服务管理', '工装被服管理', '房屋空间管理', '后勤资产实物管理', '物资配送管理', '太平间管理'],
    },
    {
      code: 'ENVIRONMENT',
      name: '环境监管',
      domain: 'Environment',
      primaryModuleCount: 3,
      secondaryItemCount: 23,
      modules: ['室内外环境管理', '智慧卫生间管理', '医疗废弃物收集处置管理'],
    },
    {
      code: 'INTEGRATED',
      name: '综合管理',
      domain: 'IntegratedManagement',
      primaryModuleCount: 8,
      secondaryItemCount: 30,
      modules: ['质量体系', '合同管理', '考核管理', '运营分析', '人员管理', '运行管理', '能耗成本管理', '碳排双控'],
    },
  ],
  workflows: [
    { code: 'SERVICE_LOOP', name: '服务闭环', steps: ['服务请求', '受理登记', '工单池', '派工', '执行反馈', '验收回访', '评价归档'] },
    { code: 'FACILITY_LOOP', name: '设施运行闭环', steps: ['设备台账', '监测巡检', '异常发现', '维修保养', '生命周期记录'] },
    { code: 'ENVIRONMENT_LOOP', name: '环境监管闭环', steps: ['点位采集', '阈值策略', '预警池', '处置工单', '策略优化'] },
    { code: 'QUALITY_COST_LOOP', name: '质量成本闭环', steps: ['质量标准', '过程采集', '结果评价', '问题整改', '考核结算'] },
  ],
  workbenchMetrics: [
    { code: 'TODAY_ORDERS', name: '今日工单', value: '126', trend: '+18', tone: 'blue' },
    { code: 'SLA_RISK', name: '超时风险', value: '7', trend: '+2', tone: 'orange' },
    { code: 'ACTIVE_ALERTS', name: '运行告警', value: '12', trend: '-4', tone: 'red' },
    { code: 'ASSET_HEALTH', name: '设备完好率', value: '98.2%', trend: '+0.6%', tone: 'green' },
  ],
  teamLoads: localTeamLoads,
  featureEvidence: sourceEvidence,
  customerDataSystems: [
    {
      major: '强电系统',
      subsystems: ['变电室智能配电系统', '多功能远传电表系统', '电力系统', '照明', '防雷接地'],
      sensors: ['多功能测量仪表', '电能质量仪表', '三相多功能电表'],
      locations: ['变电室', '楼层配电柜', '配电分盘'],
      dataFields: ['电压', '电流', '有功功率', '功率因数', '频率', '电度', '谐波'],
      desiredData: ['温湿度', '浪涌保护器状态', '照明设备数量'],
      roles: ['电工班工作人员', '总务处管理者', '医院管理者'],
      endpoints: ['控制室电脑端', '移动端'],
    },
    {
      major: '供暖空调系统',
      subsystems: ['热源及供暖水系统', '通风系统', '冷源及空调水系统', '空气处理机组', '新风机组', '净化空调系统'],
      sensors: ['温湿度传感器', 'CO2浓度传感器', '压差传感器', '空气质量传感器'],
      locations: ['送风口', '水盘管处', '室内点位', '过滤器处'],
      dataFields: ['供回水温', '压力', '流量', '能耗', '启停状态', '故障状态'],
      desiredData: ['冷源状态', '空调水系统状态', '室内空气质量'],
      roles: ['暖通班工作人员', '总务处管理者', '医院管理者'],
      endpoints: ['控制室电脑端', '移动端'],
    },
    {
      major: '其他物联设备',
      subsystems: ['智慧卫生间', '智慧食堂系统', '无人零售物联系统'],
      sensors: ['厕位传感器', '空气质量传感器', '耗材传感器', '油烟监测'],
      locations: ['卫生间', '食堂后厨', '公共服务区'],
      dataFields: ['厕位状态', '空气质量', '耗材余量', '油烟状态'],
      desiredData: ['智慧卫生间服务状态', '食安强化监测'],
      roles: ['保洁班组', '食堂管理人员', '总务处管理者'],
      endpoints: ['移动端', '控制室电脑端'],
    },
  ],
  competitorModules: [
    { name: '智慧医院运行保障系统基础服务模块', featureAreas: ['个人工作台', '用户管理', '角色权限管理', '统一登录管理', '工单管理', '消息推送管理'] },
    { name: '医疗废弃物综合管理系统', featureAreas: ['收集总览', '数据可视化大屏', '医废全生命周期监管', '暂存站监管', '医废扎带管理'] },
    { name: '综合能耗智能监管系统', featureAreas: ['用能总览', '实时监控', '告警管理', '用能分析', '报表管理', '成本管理'] },
    { name: '智能一站式服务综合管理系统', featureAreas: ['一站式服务调度中心', '维修管理', '移动报修', '工程仓库管理', '报表管理'] },
  ],
  implementationPhases: [
    { order: 1, name: '基础平台与工作台', capabilities: ['个人工作台', '消息', '待办', '权限', '登录日志'] },
    { order: 2, name: '一站式服务与工单', capabilities: ['报修', '派单', '接单', '挂单', '转单', '完工', '验收', '评价', '超时提醒'] },
    { order: 3, name: '资产台账与巡检保养', capabilities: ['设备设施', '空间', '岗位', '人员', '计划工单', '巡检/保养'] },
    { order: 4, name: '客户物联数据接入模型', capabilities: ['强电', '暖通', '给排水', '医气', '环境', '污水', '时序字段'] },
    { order: 5, name: '专项系统', capabilities: ['电梯', '医气', '供配电', '给排水', '冷热站', '环境质量', '污水站', '医废', '能耗'] },
    { order: 6, name: '综合管理', capabilities: ['合同', '质量', '考核', '服务品质', '报表', '运营分析'] },
  ],
}

const localDashboard: OperationsDashboard = {
  generatedAt: '2026-05-30T09:30:00+08:00',
  openWorkOrders: 4,
  escalatedWorkOrders: 1,
  riskAssets: 2,
  warningSignals: 2,
  assets: [
    {
      assetCode: 'ELV-OPD-01',
      name: '门诊楼 1 号医梯',
      system: '电梯管理',
      location: locations.lobby,
      status: 'Warning',
      lastSignalAt: '2026-05-30T09:22:00+08:00',
      currentRisk: '运行频次突增，建议提前巡检曳引系统',
    },
    {
      assetCode: 'WASTE-F1-01',
      name: '医废暂存间负压设备',
      system: '医疗废弃物',
      location: locations.waste,
      status: 'Fault',
      lastSignalAt: '2026-05-30T09:25:00+08:00',
      currentRisk: '负压值低于阈值，已触发应急工单',
    },
  ],
  workOrders: localWorkOrders,
  environmentSignals: [
    {
      signalCode: 'ENV-WASTE-PRESSURE',
      name: '医废暂存间负压',
      metric: 'pressure',
      value: -3.2,
      unit: 'Pa',
      status: 'Critical',
      location: locations.waste,
      collectedAt: '2026-05-30T09:27:00+08:00',
    },
    {
      signalCode: 'ENV-OPD-CO2',
      name: '门诊大厅 CO2',
      metric: 'co2',
      value: 860,
      unit: 'ppm',
      status: 'Warning',
      location: locations.lobby,
      collectedAt: '2026-05-30T09:26:00+08:00',
    },
  ],
}

const localDispatchBoard: DispatchBoard = {
  workOrders: localWorkOrders,
  teamLoads: localTeamLoads,
  recommendations: [
    { workOrderNo: 'WO-20260530-0001', recommendedTeam: '环境监管班组', reason: '环境应急按医废负压风险优先派工', priority: 'Critical', slaMinutesRemaining: 18 },
    { workOrderNo: 'WO-20260530-0002', recommendedTeam: '电梯维保组', reason: '医梯异响按设备专项班组派工', priority: 'High', slaMinutesRemaining: 120 },
    { workOrderNo: 'WO-20260530-0003', recommendedTeam: '综合维修班', reason: '后勤配送可由综合服务班组承接', priority: 'Normal', slaMinutesRemaining: 180 },
  ],
  slaRisk: {
    openWorkOrders: 4,
    overdueWorkOrders: 0,
    dueSoonWorkOrders: 1,
    escalatedWorkOrders: 1,
    highestRiskLevel: 'High',
    highestRiskWorkOrderNo: 'WO-20260530-0001',
  },
}

const assetSourceEvidence: FeatureEvidence[] = [
  {
    featureName: '设备设施资产台账',
    sources: ['中科医信', 'PPT'],
    evidenceSummary: '竞品功能树要求资产分类、资产台账、资产维修管理；PPT 要求基础运行设备设施全生命周期管理。',
  },
  {
    featureName: '巡检保养计划',
    sources: ['中科医信', 'PPT'],
    evidenceSummary: '竞品功能树要求工作日历、巡检/保养、计划管理；PPT 要求设备监测预警与运维处置联动。',
  },
  {
    featureName: '医用气体监测',
    sources: ['北建院', '中科医信', 'PPT'],
    evidenceSummary: '客户数据、竞品功能和 PPT 均涉及医用气体压力、阀箱、报警处置和维修闭环。',
  },
  {
    featureName: '巡检保养异常转工单',
    sources: ['中科医信', 'PPT', '北建院'],
    evidenceSummary: '巡检/保养发现异常后生成维修工单，保留资产、空间、责任班组和来源证据，并进入一站式调度闭环。',
  },
]

const localAssetMaintenanceBoard: AssetMaintenanceBoard = {
  generatedAt: '2026-05-30T09:30:00+08:00',
  assets: [
    {
      assetCode: 'WASTE-F1-01',
      name: '医废暂存间负压设备',
      system: '医疗废物',
      criticality: 'LifeSafety',
      location: locations.waste,
      status: 'Fault',
      ownerTeam: '环境监管班组',
      manufacturer: 'Mock厂商',
      model: 'NP-200',
      commissionedOn: '2023-08-18',
      maintenanceStrategy: '每日巡检 + 异常转工单',
      healthScore: 52,
      currentRisk: '负压低于阈值，需要联动医废处置工单',
      sourceTags: ['中科医信', 'PPT'],
    },
    {
      assetCode: 'MEDGAS-IPD-8F',
      name: '住院 8F 医用气体分区阀箱',
      system: '医用气体',
      criticality: 'LifeSafety',
      location: locations.ward,
      status: 'Maintenance',
      ownerTeam: '医气维保人员',
      manufacturer: 'Mock厂商',
      model: 'MGV-8F',
      commissionedOn: '2022-05-09',
      maintenanceStrategy: '周巡检 + 压力异常闭环',
      healthScore: 68,
      currentRisk: '计划保养中，需关注氧气压力波动',
      sourceTags: ['北建院', '中科医信', 'PPT'],
    },
    {
      assetCode: 'CHW-B1-02',
      name: '冷站 2 号冷冻泵',
      system: '暖通空调',
      criticality: 'High',
      location: locations.energy,
      status: 'Normal',
      ownerTeam: '暖通班工作人员',
      manufacturer: 'Mock厂商',
      model: 'CHW-P-02',
      commissionedOn: '2021-11-12',
      maintenanceStrategy: '月度保养 + 能耗趋势复核',
      healthScore: 91,
      currentRisk: '运行稳定，维持计划保养',
      sourceTags: ['北建院', '中科医信', 'PPT'],
    },
  ],
  plans: [
    {
      planCode: 'MP-MEDGAS-VALVE',
      assetCode: 'MEDGAS-IPD-8F',
      name: '医用气体分区阀箱周巡检',
      taskType: 'Inspection',
      cycleDays: 7,
      nextDueAt: '2026-05-30T09:20:00+08:00',
      responsibleTeam: '医气维保人员',
      checklistTemplate: [
        { code: 'CHK-PRESSURE', name: '氧气压力', standard: '压力在上下限范围内', required: true },
        { code: 'CHK-VALVE', name: '阀门状态', standard: '阀门开闭和标识正常', required: true },
      ],
      sourceEvidence: assetSourceEvidence,
    },
  ],
  dueTasks: [
    {
      taskNo: 'MT-20260530-0002',
      planCode: 'MP-MEDGAS-VALVE',
      assetCode: 'MEDGAS-IPD-8F',
      title: '住院 8F 医用气体分区阀箱周巡检',
      taskType: 'Inspection',
      status: 'Overdue',
      priority: 'Critical',
      scheduledAt: '2026-05-30T08:30:00+08:00',
      dueAt: '2026-05-30T09:20:00+08:00',
      responsibleTeam: '医气维保人员',
      checklistResults: [],
    },
  ],
  lifecycleEvents: [
    {
      occurredAt: '2026-05-30T06:30:00+08:00',
      assetCode: 'MEDGAS-IPD-8F',
      eventType: '计划保养',
      operator: '医气维保人员',
      summary: '完成分区阀箱压力表校准',
    },
  ],
  sourceEvidence: assetSourceEvidence,
  kpis: {
    totalAssets: 3,
    riskAssets: 2,
    dueTasks: 1,
    overdueTasks: 1,
    preventiveCompletionRate: 80,
    averageHealthScore: 70,
  },
}

const iotSourceEvidence: FeatureEvidence[] = [
  {
    featureName: '客户物联数据接入模型',
    sources: ['北建院', 'PPT'],
    evidenceSummary: '北建院调研数据定义系统、传感器、点位、可采集字段和角色；PPT 定义 BIM 智慧运维集成边界。',
  },
  {
    featureName: '医用气体监测',
    sources: ['北建院', '中科医信', 'PPT'],
    evidenceSummary: '氧气、压缩空气、负压真空、压力流量与报警处置是客户数据和竞品功能的共同项。',
  },
]

const localIotCatalog: IotIntegrationCatalog = {
  generatedAt: '2026-05-30T09:30:00+08:00',
  systems: [
    { category: 'StrongElectric', name: '强电系统', subsystems: ['变配电', '远传电表', '照明'], roles: ['电工班工作人员'], endpoints: ['控制室电脑端', '移动端'] },
    { category: 'Hvac', name: '供暖空调系统', subsystems: ['冷热源', '空调水', '新风机组'], roles: ['暖通班工作人员'], endpoints: ['控制室电脑端', '移动端'] },
    { category: 'MedicalGas', name: '医用气体系统', subsystems: ['氧气', '压缩空气', '负压真空'], roles: ['医气维保人员'], endpoints: ['控制室电脑端', '移动端'] },
    { category: 'EnvironmentQuality', name: '环境质量系统', subsystems: ['CO2', '温湿度'], roles: ['环境监管班组'], endpoints: ['控制室电脑端', '移动端'] },
  ],
  points: [
    {
      pointCode: 'MEDGAS-O2-8F',
      name: '住院 8F 氧气压力监测点',
      category: 'MedicalGas',
      location: locations.ward,
      deviceCode: 'MEDGAS-O2-8F',
      protocolAdapter: 'medical-gas-adapter',
      metrics: [
        { code: 'pressure', name: '氧气压力', unit: 'MPa', dataType: 'decimal', sourceField: '医气压力' },
        { code: 'flow', name: '氧气流量', unit: 'm3/h', dataType: 'decimal', sourceField: '医气流量' },
      ],
      sourceEvidence: iotSourceEvidence,
    },
    {
      pointCode: 'PWR-LV-B1-IN-01',
      name: 'B1 低压进线柜多功能电表',
      category: 'StrongElectric',
      location: locations.energy,
      deviceCode: 'METER-LV-001',
      protocolAdapter: 'modbus-adapter',
      metrics: [
        { code: 'voltage', name: '电压', unit: 'V', dataType: 'decimal', sourceField: '电压' },
        { code: 'current', name: '电流', unit: 'A', dataType: 'decimal', sourceField: '电流' },
        { code: 'kwh', name: '电度', unit: 'kWh', dataType: 'decimal', sourceField: '电度' },
      ],
      sourceEvidence: iotSourceEvidence,
    },
  ],
  thresholdRules: [
    {
      pointCode: 'MEDGAS-O2-8F',
      metricCode: 'pressure',
      direction: 'Below',
      warningMin: 0.38,
      criticalMin: 0.35,
      ruleSummary: '医用氧气压力低于阈值需告警处置',
    },
  ],
  sourceEvidence: iotSourceEvidence,
}

const alarmSourceEvidence: FeatureEvidence[] = [
  {
    featureName: '环境监管预警池',
    sources: ['PPT', '中科医信'],
    evidenceSummary: 'PPT 要求点位采集、阈值策略、预警池、报警策略和处置闭环；竞品包含统一报警和专项报警处理。',
  },
  {
    featureName: '客户物联告警联动',
    sources: ['北建院', 'PPT', '中科医信'],
    evidenceSummary: '客户物联读数触发阈值后进入预警池，确认后生成真实处置工单并进入一站式调度池。',
  },
]

const localMonitoringAlarmBoard: MonitoringAlarmBoard = {
  generatedAt: '2026-05-30T09:30:00+08:00',
  alarms: [
    {
      alarmNo: 'ALM-MEDGAS-O2-8F-PRESSURE-20260530102800',
      pointCode: 'MEDGAS-O2-8F',
      metricCode: 'pressure',
      title: '住院 8F 氧气压力监测点压力异常',
      riskLevel: 'Critical',
      status: 'New',
      location: locations.ward,
      value: 0.31,
      unit: 'MPa',
      triggeredAt: '2026-05-30T10:28:00+08:00',
      ruleSummary: '医用氧气压力低于阈值需告警处置',
      responsibleTeam: '医气维保人员',
      lastRemark: '由物联阈值规则自动生成预警事件',
    },
    {
      alarmNo: 'ALM-WASTE-F1-NEGATIVE-PRESSURE-20260530092700',
      pointCode: 'WASTE-F1-01',
      metricCode: 'negativePressure',
      title: '医废暂存间负压异常',
      riskLevel: 'Critical',
      status: 'Acknowledged',
      location: locations.waste,
      value: -3.2,
      unit: 'Pa',
      triggeredAt: '2026-05-30T09:27:00+08:00',
      ruleSummary: '医废暂存间负压低于生命安全阈值',
      responsibleTeam: '环境监管班组',
      lastRemark: '环境监管班组已确认，待转处置工单',
    },
  ],
  sourceEvidence: alarmSourceEvidence,
}

const statusLabels: Record<WorkOrderStatus | FacilityStatus | SignalStatus, string> = {
  New: '新建',
  Dispatched: '已派工',
  Accepted: '处理中',
  InProgress: '处理中',
  Suspended: '已挂单',
  Transferred: '已转单',
  PendingAcceptance: '待验收',
  PendingEvaluation: '待评价',
  Closed: '已关闭',
  Escalated: '已升级',
  Normal: '正常',
  Warning: '预警',
  Fault: '故障',
  Maintenance: '保养中',
  Critical: '严重',
}

const priorityLabels: Record<Priority, string> = {
  Low: '低',
  Normal: '普通',
  High: '高',
  Critical: '紧急',
}

const maintenanceStatusLabels: Record<MaintenanceTaskStatus, string> = {
  Planned: '计划中',
  Due: '待巡检',
  Overdue: '已逾期',
  Completed: '已完成',
  RequiresRepair: '需维修',
  ConvertedToWorkOrder: '已转工单',
}

const criticalityLabels: Record<AssetCriticality, string> = {
  Low: '一般',
  Medium: '重要',
  High: '高',
  LifeSafety: '生命安全',
}

const iotCategoryLabels: Record<IotSystemCategory, string> = {
  StrongElectric: '强电',
  Hvac: '暖通',
  WaterSupplyDrainage: '给排水',
  MedicalGas: '医气',
  EnvironmentQuality: '环境',
  Sewage: '污水',
}

const telemetryRiskLabels: Record<TelemetryRiskLevel, string> = {
  Normal: '正常',
  Warning: '预警',
  Critical: '严重',
}

const alarmStatusLabels: Record<MonitoringAlarmStatus, string> = {
  New: '新告警',
  Acknowledged: '已确认',
  ConvertedToWorkOrder: '已转工单',
  Closed: '已关闭',
}

const pageProfiles: Record<WorkspacePage, { title: string; summary: string }> = {
  overview: {
    title: '后勤运营总览',
    summary: '总览保留跨模块态势；具体办事请从左侧进入工单、资产、物联或 BIM 工作台。',
  },
  dispatch: {
    title: '工单调度工作台',
    summary: '聚焦一站式服务：工单池、派工建议、工单详情、状态流转和处理记录。',
  },
  assets: {
    title: '资产巡检工作台',
    summary: '聚焦设备设施：资产台账、BIM 位置、巡检任务、异常转工单和生命周期记录。',
  },
  iot: {
    title: '物联点位工作台',
    summary: '聚焦客户调研数据：系统分类、点位目录、时序字段、阈值规则和异常读数。',
  },
  alerts: {
    title: '环境预警处置工作台',
    summary: '聚焦物联告警：预警池、告警详情、确认处置、转工单和调度池联动。',
  },
  spatial: {
    title: 'BIM 空间运维工作台',
    summary: '聚焦空间定位：设备、告警、工单和班组负载在同一空间语境里联动。',
  },
  evidence: {
    title: '来源追溯与建设蓝图',
    summary: '查看 PPT、北建院客户数据和竞品功能树如何约束系统能力边界。',
  },
}

const serviceWorkflowTabs = ['服务受理', '工单调度', '任务执行', '验收回访', '服务评价'] as const
type ServiceWorkflowTab = (typeof serviceWorkflowTabs)[number]
const spatialMenuItems = ['空间台账', '楼层视图', '设备点位', '告警点位', '工单点位']

const apiBase = import.meta.env.VITE_API_BASE_URL ?? 'http://localhost:5248'

function pageFromMenuItem(item: string): WorkspacePage {
  if (item.includes('首页') || item.includes('待办')) {
    return 'overview'
  }

  if (item.includes('风险告警') || item.includes('预警') || item.includes('报警策略') || item.includes('医废处置')) {
    return 'alerts'
  }

  if (
    item.includes('BIM') ||
    item.includes('空间') ||
    item.includes('楼层') ||
    spatialMenuItems.some((menuItem) => item.includes(menuItem))
  ) {
    return 'spatial'
  }

  if (item.includes('服务') || item.includes('工单') || item.includes('任务') || item.includes('验收')) {
    return 'dispatch'
  }

  if (
    item.includes('设备') ||
    item.includes('巡检') ||
    item.includes('维修') ||
    item.includes('备件') ||
    item.includes('电梯') ||
    item.includes('暖通') ||
    item.includes('给排水') ||
    item.includes('医气')
  ) {
    return 'assets'
  }

  if (
    item.includes('环境') ||
    item.includes('预警') ||
    item.includes('报警') ||
    item.includes('医废') ||
    item.includes('卫生间')
  ) {
    return 'iot'
  }

  return 'evidence'
}

function initialMenuItem(): string {
  if (typeof window === 'undefined') {
    return '后勤首页'
  }

  const hashValue = decodeURIComponent(window.location.hash.replace(/^#/, ''))
  return hashValue || '后勤首页'
}

function serviceTabFromMenuItem(item: string): ServiceWorkflowTab | null {
  const exactTab = serviceWorkflowTabs.find((tab) => item.includes(tab))
  if (exactTab) {
    return exactTab
  }

  if (item.includes('工单')) {
    return '工单调度'
  }

  if (item.includes('任务')) {
    return '任务执行'
  }

  if (item.includes('验收')) {
    return '验收回访'
  }

  return null
}

function App() {
  const [, setDashboard] = useState(localDashboard)
  const [blueprint, setBlueprint] = useState(localBlueprint)
  const [dispatchBoard, setDispatchBoard] = useState(localDispatchBoard)
  const [selectedDetail, setSelectedDetail] = useState(() => buildLocalDetail(localWorkOrders[0]))
  const [assetBoard, setAssetBoard] = useState(localAssetMaintenanceBoard)
  const [selectedAssetDetail, setSelectedAssetDetail] = useState(() => buildLocalAssetDetail('MEDGAS-IPD-8F'))
  const [lastMaintenanceResult, setLastMaintenanceResult] = useState<MaintenanceTaskOperationResult | null>(null)
  const [convertedMaintenanceDetail, setConvertedMaintenanceDetail] = useState<WorkOrderDetail | null>(null)
  const [iotCatalog, setIotCatalog] = useState(localIotCatalog)
  const [selectedIotPoint, setSelectedIotPoint] = useState(() => buildLocalIotPointDetail('MEDGAS-O2-8F'))
  const [lastTelemetryResult, setLastTelemetryResult] = useState<TelemetryIngestionResult | null>(null)
  const [alarmBoard, setAlarmBoard] = useState(localMonitoringAlarmBoard)
  const [selectedAlarm, setSelectedAlarm] = useState<MonitoringAlarmEvent | null>(
    () => localMonitoringAlarmBoard.alarms[0] ?? null,
  )
  const [convertedAlarmDetail, setConvertedAlarmDetail] = useState<WorkOrderDetail | null>(null)
  const [selectedSpatialPointId, setSelectedSpatialPointId] = useState<string | null>(null)
  const [source, setSource] = useState<'api' | 'local'>('local')
  const [activeMenuItem, setActiveMenuItem] = useState(() => initialMenuItem())
  const [activePage, setActivePage] = useState<WorkspacePage>(() => pageFromMenuItem(initialMenuItem()))
  const [activeServiceTab, setActiveServiceTab] = useState<ServiceWorkflowTab>(
    () => serviceTabFromMenuItem(initialMenuItem()) ?? '工单调度',
  )

  useEffect(() => {
    function syncPageFromHash() {
      const menuItem = initialMenuItem()
      setActiveMenuItem(menuItem)
      setActivePage(pageFromMenuItem(menuItem))
      setActiveServiceTab(serviceTabFromMenuItem(menuItem) ?? '工单调度')
    }

    syncPageFromHash()
    window.addEventListener('hashchange', syncPageFromHash)
    return () => window.removeEventListener('hashchange', syncPageFromHash)
  }, [])

  useEffect(() => {
    const controller = new AbortController()

    Promise.all([
      fetch(`${apiBase}/api/operations/dashboard`, { signal: controller.signal }),
      fetch(`${apiBase}/api/logistics/blueprint`, { signal: controller.signal }),
      fetch(`${apiBase}/api/operations/dispatch-board`, { signal: controller.signal }),
      fetch(`${apiBase}/api/operations/asset-maintenance-board`, { signal: controller.signal }),
      fetch(`${apiBase}/api/operations/iot-catalog`, { signal: controller.signal }),
      fetch(`${apiBase}/api/operations/monitoring-alarms`, { signal: controller.signal }),
    ])
      .then(async ([
        dashboardResponse,
        blueprintResponse,
        boardResponse,
        assetBoardResponse,
        iotCatalogResponse,
        alarmBoardResponse,
      ]) => {
        if (
          !dashboardResponse.ok ||
          !blueprintResponse.ok ||
          !boardResponse.ok ||
          !assetBoardResponse.ok ||
          !iotCatalogResponse.ok ||
          !alarmBoardResponse.ok
        ) {
          throw new Error('Logistics API unavailable')
        }

        const board = (await boardResponse.json()) as DispatchBoard
        const maintenanceBoard = (await assetBoardResponse.json()) as AssetMaintenanceBoard
        const fetchedIotCatalog = (await iotCatalogResponse.json()) as IotIntegrationCatalog
        const fetchedAlarmBoard = (await alarmBoardResponse.json()) as MonitoringAlarmBoard
        setDashboard((await dashboardResponse.json()) as OperationsDashboard)
        setBlueprint((await blueprintResponse.json()) as LogisticsBlueprint)
        setDispatchBoard(board)
        setAssetBoard(maintenanceBoard)
        setIotCatalog(fetchedIotCatalog)
        setAlarmBoard(fetchedAlarmBoard)
        setSelectedAlarm(fetchedAlarmBoard.alarms[0] ?? null)
        setSource('api')

        const firstWorkOrderNo = board.workOrders[0]?.workOrderNo
        if (firstWorkOrderNo) {
          const detailResponse = await fetch(`${apiBase}/api/operations/work-orders/${firstWorkOrderNo}`, {
            signal: controller.signal,
          })
          if (detailResponse.ok) {
            setSelectedDetail((await detailResponse.json()) as WorkOrderDetail)
          }
        }

        const firstAssetCode =
          maintenanceBoard.dueTasks[0]?.assetCode ?? maintenanceBoard.assets[0]?.assetCode
        if (firstAssetCode) {
          const assetDetailResponse = await fetch(`${apiBase}/api/operations/assets/${firstAssetCode}/maintenance`, {
            signal: controller.signal,
          })
          if (assetDetailResponse.ok) {
            setSelectedAssetDetail((await assetDetailResponse.json()) as AssetMaintenanceDetail)
          }
        }

        const firstPointCode =
          fetchedIotCatalog.points.find((point) => point.category === 'MedicalGas')?.pointCode ??
          fetchedIotCatalog.points[0]?.pointCode
        if (firstPointCode) {
          const pointResponse = await fetch(`${apiBase}/api/operations/iot-points/${firstPointCode}`, {
            signal: controller.signal,
          })
          if (pointResponse.ok) {
            setSelectedIotPoint((await pointResponse.json()) as IotPointDetail)
          }
        }
      })
      .catch(() => {
        setSource('local')
      })

    return () => controller.abort()
  }, [])

  const sortedWorkOrders = useMemo(
    () =>
      [...dispatchBoard.workOrders].sort((left, right) => {
        const weight: Record<Priority, number> = { Low: 1, Normal: 2, High: 3, Critical: 4 }
        return weight[right.priority] - weight[left.priority]
      }),
    [dispatchBoard.workOrders],
  )

  const selectedRecommendation = dispatchBoard.recommendations.find(
    (recommendation) => recommendation.workOrderNo === selectedDetail.workOrder.workOrderNo,
  )
  const detailedSecondaryTotal = blueprint.featureGroups.reduce((total, group) => total + group.secondaryItemCount, 0)
  const selectedAssetDueTasks = assetBoard.dueTasks.filter(
    (task) => task.assetCode === selectedAssetDetail.asset.assetCode,
  )
  const pageProfile = pageProfiles[activePage]
  const activeAlarm = selectedAlarm ?? alarmBoard.alarms[0] ?? null
  const selectedAlarmEvidence = alarmBoard.sourceEvidence.length > 0 ? alarmBoard.sourceEvidence : alarmSourceEvidence
  const latestEvaluation = [...selectedDetail.timeline].reverse().find((entry) => entry.rating)
  const spatialPoints = useMemo<SpatialOperationPoint[]>(
    () => [
      ...dispatchBoard.workOrders.map((order) => ({
        id: `workOrder:${order.workOrderNo}`,
        kind: 'workOrder' as const,
        entityId: order.workOrderNo,
        title: order.title,
        subtitle: `${order.workOrderNo} / ${priorityLabels[order.priority]}`,
        location: order.location,
        statusLabel: statusLabels[order.status],
        actionLabel: '打开关联工单',
        tone: order.priority === 'Critical' || order.status === 'Escalated' ? 'workorder' as const : 'normal' as const,
      })),
      ...assetBoard.assets.map((asset) => ({
        id: `asset:${asset.assetCode}`,
        kind: 'asset' as const,
        entityId: asset.assetCode,
        title: asset.name,
        subtitle: `${asset.assetCode} / ${asset.system}`,
        location: asset.location,
        statusLabel: statusLabels[asset.status],
        actionLabel: '打开资产台账',
        tone: asset.status === 'Fault' || asset.criticality === 'LifeSafety' ? 'asset' as const : 'normal' as const,
      })),
      ...alarmBoard.alarms
        .filter((alarm) => alarm.status !== 'Closed')
        .map((alarm) => ({
          id: `alarm:${alarm.alarmNo}`,
          kind: 'alarm' as const,
          entityId: alarm.alarmNo,
          title: alarm.title,
          subtitle: `${telemetryRiskLabels[alarm.riskLevel]} / ${alarmStatusLabels[alarm.status]}`,
          location: alarm.location,
          statusLabel: alarmStatusLabels[alarm.status],
          actionLabel: '打开预警处置',
          tone: 'alert' as const,
        })),
      ...iotCatalog.points.map((point) => ({
        id: `iot:${point.pointCode}`,
        kind: 'iot' as const,
        entityId: point.pointCode,
        title: point.name,
        subtitle: `${point.pointCode} / ${iotCategoryLabels[point.category]}`,
        location: point.location,
        statusLabel: point.protocolAdapter,
        actionLabel: '打开物联点位',
        tone: 'normal' as const,
      })),
    ],
    [alarmBoard.alarms, assetBoard.assets, dispatchBoard.workOrders, iotCatalog.points],
  )
  const selectedSpatialPoint =
    spatialPoints.find((point) => point.id === selectedSpatialPointId) ?? spatialPoints[0]

  function openWorkspacePage(item: string) {
    const nextPage = pageFromMenuItem(item)
    setActiveMenuItem(item)
    setActivePage(nextPage)
    if (nextPage === 'dispatch') {
      setActiveServiceTab(serviceTabFromMenuItem(item) ?? '工单调度')
    }
    window.history.replaceState(null, '', `#${encodeURIComponent(item)}`)
    window.scrollTo({ top: 0, behavior: 'smooth' })
  }

  function openServiceWorkflowTab(tab: ServiceWorkflowTab) {
    setActivePage('dispatch')
    setActiveMenuItem(tab)
    setActiveServiceTab(tab)
    window.history.replaceState(null, '', `#${encodeURIComponent(tab)}`)
  }

  async function openSpatialLinkedObject(point: SpatialOperationPoint) {
    if (point.kind === 'workOrder') {
      await loadDetail(point.entityId)
      openServiceWorkflowTab('工单调度')
      return
    }

    if (point.kind === 'asset') {
      await loadAssetDetail(point.entityId)
      openWorkspacePage('设备台账')
      return
    }

    if (point.kind === 'alarm') {
      selectMonitoringAlarm(point.entityId)
      openWorkspacePage('预警池')
      return
    }

    await loadIotPointDetail(point.entityId)
    openWorkspacePage('环境点位')
  }

  async function createServiceIntakeWorkOrder() {
    const localOrder: WorkOrder = {
      workOrderNo: 'WO-SR-20260531-0001',
      title: '门诊大厅空调异常服务请求',
      serviceType: '综合维修',
      priority: 'High',
      status: 'New',
      location: {
        campus: '同仁亦庄院区',
        building: '门诊医技楼',
        floor: '1F',
        room: '共享大厅',
        bimElementId: 'BIM-OPD-F1-HALL',
      },
      responsibleTeam: '待调度',
      createdAt: '2026-05-31T10:00:00+08:00',
      slaDueAt: '2026-05-31T12:00:00+08:00',
    }
    const localDetail = buildLocalDetail(localOrder)

    try {
      const serviceRequestResponse = await fetchWithTimeout(`${apiBase}/api/operations/service-requests`, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({
          sourceType: 'Manual',
          requesterName: '门诊护士站',
          requesterDepartment: '门诊部',
          serviceType: '综合维修',
          priority: 'High',
          description: '门诊大厅空调异常，候诊区温度偏高',
          location: localOrder.location,
        }),
      })

      if (serviceRequestResponse.ok) {
        const serviceRequest = (await serviceRequestResponse.json()) as { requestNo: string }
        const conversionResponse = await fetchWithTimeout(
          `${apiBase}/api/operations/service-requests/${serviceRequest.requestNo}/convert`,
          {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({
              acceptedBy: '一站式受理员',
              remark: '信息完整，生成待派工单',
            }),
          },
        )

        if (conversionResponse.ok) {
          applyGeneratedWorkOrderDetail((await conversionResponse.json()) as WorkOrderDetail)
          return
        }
      }
    } catch {
      // API 不可用时使用本地种子流程，保证前端演示和离线测试仍能闭环。
    }

    applyGeneratedWorkOrderDetail(localDetail)
  }

  function applyGeneratedWorkOrderDetail(
    detail: WorkOrderDetail,
    options: { openDispatch?: boolean } = { openDispatch: true },
  ) {
    const acceptedOrder = detail.workOrder
    const recommendedTeam =
      acceptedOrder.responsibleTeam && !['待调度', '未派工'].includes(acceptedOrder.responsibleTeam)
        ? acceptedOrder.responsibleTeam
        : '综合维修班'

    setDispatchBoard((current) => {
      const hadOrder = current.workOrders.some((order) => order.workOrderNo === acceptedOrder.workOrderNo)
      const existingOrders = current.workOrders.filter((order) => order.workOrderNo !== acceptedOrder.workOrderNo)
      const existingRecommendations = current.recommendations.filter(
        (recommendation) => recommendation.workOrderNo !== acceptedOrder.workOrderNo,
      )

      return {
        ...current,
        workOrders: [acceptedOrder, ...existingOrders],
        recommendations: [
          {
            workOrderNo: acceptedOrder.workOrderNo,
            recommendedTeam,
            reason: `${acceptedOrder.serviceType}按空间、专业和SLA风险派给${recommendedTeam}`,
            priority: acceptedOrder.priority,
            slaMinutesRemaining: 120,
          },
          ...existingRecommendations,
        ],
        slaRisk: {
          ...current.slaRisk,
          openWorkOrders: existingOrders.length + 1,
          dueSoonWorkOrders: hadOrder ? current.slaRisk.dueSoonWorkOrders : current.slaRisk.dueSoonWorkOrders + 1,
          highestRiskLevel: current.slaRisk.highestRiskLevel === 'High' ? 'High' : 'Medium',
          highestRiskWorkOrderNo: current.slaRisk.highestRiskWorkOrderNo ?? acceptedOrder.workOrderNo,
        },
      }
    })
    setDashboard((current) => ({
      ...current,
      openWorkOrders: current.workOrders.some((order) => order.workOrderNo === acceptedOrder.workOrderNo)
        ? current.openWorkOrders
        : current.openWorkOrders + 1,
      workOrders: [acceptedOrder, ...current.workOrders.filter((order) => order.workOrderNo !== acceptedOrder.workOrderNo)],
    }))
    setSelectedDetail(detail)
    if (options.openDispatch ?? true) {
      openServiceWorkflowTab('工单调度')
    }
  }

  async function loadDetail(workOrderNo: string, forceApi = false) {
    if (source === 'api' || forceApi) {
      const response = await fetch(`${apiBase}/api/operations/work-orders/${workOrderNo}`)
      if (response.ok) {
        setSelectedDetail((await response.json()) as WorkOrderDetail)
        return
      }
    }

    const order = dispatchBoard.workOrders.find((item) => item.workOrderNo === workOrderNo)
    if (order) {
      setSelectedDetail(buildLocalDetail(order))
    }
  }

  async function loadAssetDetail(assetCode: string, forceApi = false) {
    if (source === 'api' || forceApi) {
      const response = await fetch(`${apiBase}/api/operations/assets/${assetCode}/maintenance`)
      if (response.ok) {
        setSelectedAssetDetail((await response.json()) as AssetMaintenanceDetail)
        return
      }
    }

    setSelectedAssetDetail(buildLocalAssetDetail(assetCode))
  }

  async function completeMaintenanceTask(task: MaintenanceTask, convertToWorkOrder: boolean) {
    const checklistResults: InspectionChecklistResult[] =
      task.assetCode === 'MEDGAS-IPD-8F'
        ? [
            { code: 'CHK-PRESSURE', result: '异常', remark: '氧气压力低于下限' },
            { code: 'CHK-VALVE', result: '正常', remark: '阀门状态正常' },
          ]
        : [{ code: 'CHK-NEGATIVE-PRESSURE', result: '异常', remark: '负压低于阈值' }]

    if (source === 'api') {
      const response = await fetch(`${apiBase}/api/operations/maintenance-tasks/${task.taskNo}/complete`, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({
          operator: task.responsibleTeam,
          outcome: convertToWorkOrder ? 'Abnormal' : 'Normal',
          remark: convertToWorkOrder ? '巡检发现异常，生成维修工单闭环' : '巡检项目全部正常',
          checklistResults,
          convertToWorkOrder,
        }),
      })
      if (response.ok) {
        applyMaintenanceResult((await response.json()) as MaintenanceTaskOperationResult)
        return
      }
    }

    applyMaintenanceResult(buildLocalMaintenanceResult(task, convertToWorkOrder, checklistResults))
  }

  async function loadIotPointDetail(pointCode: string, forceApi = false) {
    if (source === 'api' || forceApi) {
      const response = await fetch(`${apiBase}/api/operations/iot-points/${pointCode}`)
      if (response.ok) {
        setSelectedIotPoint((await response.json()) as IotPointDetail)
        return
      }
    }

    setSelectedIotPoint(buildLocalIotPointDetail(pointCode))
  }

  async function refreshMonitoringAlarms(select?: { pointCode: string; metricCode: string }) {
    if (source !== 'api') {
      return
    }

    const response = await fetch(`${apiBase}/api/operations/monitoring-alarms`)
    if (!response.ok) {
      return
    }

    const board = (await response.json()) as MonitoringAlarmBoard
    setAlarmBoard(board)
    const nextAlarm = select
      ? board.alarms.find((alarm) => alarm.pointCode === select.pointCode && alarm.metricCode === select.metricCode)
      : board.alarms[0]
    setSelectedAlarm(nextAlarm ?? board.alarms[0] ?? null)
  }

  function selectMonitoringAlarm(alarmNo: string) {
    const alarm = alarmBoard.alarms.find((item) => item.alarmNo === alarmNo)
    if (alarm) {
      setSelectedAlarm(alarm)
      if (alarm.workOrderNo && convertedAlarmDetail?.workOrder.workOrderNo !== alarm.workOrderNo) {
        setConvertedAlarmDetail(null)
      }
    }
  }

  async function acknowledgeSelectedAlarm() {
    if (!activeAlarm || activeAlarm.status === 'Closed' || activeAlarm.status === 'ConvertedToWorkOrder') {
      return
    }

    if (source === 'api') {
      const response = await fetch(`${apiBase}/api/operations/monitoring-alarms/${activeAlarm.alarmNo}/acknowledge`, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ operator: activeAlarm.responsibleTeam, remark: '预警池确认，准备处置闭环' }),
      })
      if (response.ok) {
        applyAlarmUpdate((await response.json()) as MonitoringAlarmEvent)
        return
      }
    }

    applyAlarmUpdate({
      ...activeAlarm,
      status: 'Acknowledged',
      acknowledgedBy: activeAlarm.responsibleTeam,
      acknowledgedAt: new Date().toISOString(),
      lastRemark: '预警池确认，准备处置闭环',
    })
  }

  async function convertSelectedAlarmToWorkOrder() {
    if (!activeAlarm || activeAlarm.status === 'Closed') {
      return
    }

    if (source === 'api') {
      const response = await fetch(`${apiBase}/api/operations/monitoring-alarms/${activeAlarm.alarmNo}/convert-to-work-order`, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({
          operator: '预警池调度员',
          targetTeam: activeAlarm.responsibleTeam,
          remark: '物联告警确认后转入一站式工单调度',
        }),
      })
      if (response.ok) {
        const convertedAlarm = (await response.json()) as MonitoringAlarmEvent
        applyAlarmUpdate(convertedAlarm)
        const detail = convertedAlarm.workOrderNo
          ? await loadWorkOrderDetailForAlarm(convertedAlarm.workOrderNo, convertedAlarm)
          : null
        if (detail) {
          setConvertedAlarmDetail(detail)
          applyGeneratedWorkOrderDetail(detail, { openDispatch: false })
        }
        return
      }
    }

    const convertedAlarm: MonitoringAlarmEvent = {
      ...activeAlarm,
      status: 'ConvertedToWorkOrder',
      workOrderNo: activeAlarm.workOrderNo ?? buildAlarmWorkOrderNo(activeAlarm),
      acknowledgedBy: activeAlarm.acknowledgedBy ?? activeAlarm.responsibleTeam,
      acknowledgedAt: activeAlarm.acknowledgedAt ?? new Date().toISOString(),
      lastRemark: '物联告警确认后转入一站式工单调度',
    }
    const detail = buildLocalAlarmWorkOrderDetail(convertedAlarm)
    applyAlarmUpdate(convertedAlarm)
    setConvertedAlarmDetail(detail)
    applyGeneratedWorkOrderDetail(detail, { openDispatch: false })
  }

  async function loadWorkOrderDetailForAlarm(workOrderNo: string, alarm: MonitoringAlarmEvent) {
    const response = await fetch(`${apiBase}/api/operations/work-orders/${workOrderNo}`)
    if (response.ok) {
      return (await response.json()) as WorkOrderDetail
    }

    return buildLocalAlarmWorkOrderDetail(alarm)
  }

  async function openConvertedAlarmWorkOrder() {
    const workOrderNo = activeAlarm?.workOrderNo ?? convertedAlarmDetail?.workOrder.workOrderNo
    if (!workOrderNo) {
      return
    }

    const detail =
      convertedAlarmDetail?.workOrder.workOrderNo === workOrderNo
        ? convertedAlarmDetail
        : source === 'api'
          ? await loadWorkOrderDetailForAlarm(workOrderNo, activeAlarm!)
          : null

    if (detail) {
      setSelectedDetail(detail)
    }
    openServiceWorkflowTab('工单调度')
  }

  async function ingestCriticalTelemetry() {
    const metricCode = selectedIotPoint.point.metrics[0]?.code ?? 'pressure'
    const value = selectedIotPoint.point.pointCode.includes('ENV') ? 1300 : 0.31
    const unit = selectedIotPoint.point.metrics[0]?.unit ?? ''

    if (source === 'api') {
      const response = await fetch(`${apiBase}/api/operations/iot-readings`, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({
          pointCode: selectedIotPoint.point.pointCode,
          metricCode,
          value,
          unit,
          collectedAt: new Date().toISOString(),
        }),
      })
      if (response.ok) {
        const result = (await response.json()) as TelemetryIngestionResult
        applyTelemetryResult(result)
        await refreshMonitoringAlarms({ pointCode: result.pointCode, metricCode: result.metricCode })
        return
      }
    }

    const localResult = buildLocalTelemetryResult(selectedIotPoint.point, metricCode, value, unit)
    applyTelemetryResult(localResult)
    if (localResult.reading) {
      applyAlarmUpdate(buildLocalAlarmFromTelemetry(selectedIotPoint.point, localResult.reading))
    }
  }

  async function dispatchSelected() {
    const teamName = selectedRecommendation?.recommendedTeam ?? '综合维修班'
    if (source === 'api') {
      const response = await fetch(`${apiBase}/api/operations/work-orders/${selectedDetail.workOrder.workOrderNo}/dispatch`, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ teamName, dispatcher: '调度员', remark: '按SLA风险和专业班组派工' }),
      })
      if (response.ok) {
        applyDetail(
          withTimelineEntry(
            (await response.json()) as WorkOrderDetail,
            '派工',
            '调度员',
            '按SLA风险和专业班组派工',
            'Dispatched',
            teamName,
          ),
        )
        return
      }
    }

    applyLocalTransition('派工', '调度员', '按SLA风险和专业班组派工', 'Dispatched', teamName)
  }

  async function transitionSelected(
    action: WorkOrderTransitionAction,
    label: string,
    nextStatus: WorkOrderStatus,
    rating?: number,
  ) {
    const operator =
      action === 'Evaluate'
        ? '服务对象'
        : selectedDetail.workOrder.responsibleTeam && selectedDetail.workOrder.responsibleTeam !== '未派工'
        ? selectedDetail.workOrder.responsibleTeam
        : '调度员'

    if (source === 'api') {
      const response = await fetch(`${apiBase}/api/operations/work-orders/${selectedDetail.workOrder.workOrderNo}/transition`, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ action, operator, remark: label, rating }),
      })
      if (response.ok) {
        applyDetail((await response.json()) as WorkOrderDetail)
        return
      }
    }

    applyLocalTransition(label, operator, label, nextStatus, undefined, rating)
  }

  function applyLocalTransition(
    action: string,
    operator: string,
    remark: string,
    nextStatus: WorkOrderStatus,
    responsibleTeam = selectedDetail.workOrder.responsibleTeam,
    rating?: number,
  ) {
    const updatedOrder = {
      ...selectedDetail.workOrder,
      status: nextStatus,
      responsibleTeam,
    }
    const updatedDetail = {
      ...selectedDetail,
      workOrder: updatedOrder,
      timeline: [
        ...selectedDetail.timeline,
        {
          occurredAt: new Date().toISOString(),
          operator,
          action,
          fromStatus: selectedDetail.workOrder.status,
          toStatus: nextStatus,
          remark,
          rating: rating ?? null,
        },
      ],
    }
    applyDetail(updatedDetail)
  }

  function applyDetail(detail: WorkOrderDetail) {
    setSelectedDetail(detail)
    setDispatchBoard((current) => ({
      ...current,
      workOrders: current.workOrders.map((order) =>
        order.workOrderNo === detail.workOrder.workOrderNo ? detail.workOrder : order,
      ),
    }))
  }

  function withTimelineEntry(
    detail: WorkOrderDetail,
    action: string,
    operator: string,
    remark: string,
    nextStatus: WorkOrderStatus,
    responsibleTeam = detail.workOrder.responsibleTeam,
  ): WorkOrderDetail {
    if (detail.timeline.some((entry) => entry.action === action)) {
      return detail
    }

    return {
      ...detail,
      workOrder: {
        ...detail.workOrder,
        status: nextStatus,
        responsibleTeam,
      },
      timeline: [
        ...detail.timeline,
        {
          occurredAt: new Date().toISOString(),
          operator,
          action,
          fromStatus: detail.workOrder.status,
          toStatus: nextStatus,
          remark,
        },
      ],
    }
  }

  function applyMaintenanceResult(result: MaintenanceTaskOperationResult) {
    if (!result.succeeded || !result.task) {
      return
    }

    const completedTask = result.task
    setLastMaintenanceResult(result)
    if (result.generatedWorkOrder) {
      const detail = buildMaintenanceWorkOrderDetail(result.generatedWorkOrder, completedTask)
      setConvertedMaintenanceDetail(detail)
      applyGeneratedWorkOrderDetail(detail, { openDispatch: false })
    } else {
      setConvertedMaintenanceDetail(null)
    }
    setAssetBoard((current) => ({
      ...current,
      dueTasks: current.dueTasks
        .map((task) => (task.taskNo === completedTask.taskNo ? completedTask : task))
        .filter((task) => task.status !== 'Completed' && task.status !== 'ConvertedToWorkOrder'),
      lifecycleEvents: [
        {
          occurredAt: completedTask.completedAt ?? new Date().toISOString(),
          assetCode: completedTask.assetCode,
          eventType: result.generatedWorkOrder ? '异常转工单' : '完成巡检',
          operator: completedTask.completedBy ?? completedTask.responsibleTeam,
          summary: result.generatedWorkOrder
            ? `${completedTask.title} 已生成 ${result.generatedWorkOrder.workOrderNo}`
            : `${completedTask.title} 已完成`,
        },
        ...current.lifecycleEvents,
      ],
      kpis: {
        ...current.kpis,
        dueTasks: Math.max(current.kpis.dueTasks - 1, 0),
        overdueTasks: completedTask.status === 'ConvertedToWorkOrder'
          ? Math.max(current.kpis.overdueTasks - 1, 0)
          : current.kpis.overdueTasks,
      },
    }))
    setSelectedAssetDetail((current) => ({
      ...current,
      tasks: current.tasks.map((task) => (task.taskNo === completedTask.taskNo ? completedTask : task)),
      lifecycle: [
        {
          occurredAt: completedTask.completedAt ?? new Date().toISOString(),
          assetCode: completedTask.assetCode,
          eventType: result.generatedWorkOrder ? '异常转工单' : '完成巡检',
          operator: completedTask.completedBy ?? completedTask.responsibleTeam,
          summary: result.generatedWorkOrder
            ? `${completedTask.title} 已生成 ${result.generatedWorkOrder.workOrderNo}`
            : `${completedTask.title} 已完成`,
        },
        ...current.lifecycle,
      ],
    }))
  }

  function openConvertedMaintenanceWorkOrder() {
    if (!convertedMaintenanceDetail) {
      return
    }

    setSelectedDetail(convertedMaintenanceDetail)
    openServiceWorkflowTab('工单调度')
  }

  function applyAlarmUpdate(updatedAlarm: MonitoringAlarmEvent) {
    setSelectedAlarm(updatedAlarm)
    setAlarmBoard((current) => {
      const remaining = current.alarms.filter((alarm) => alarm.alarmNo !== updatedAlarm.alarmNo)
      return {
        ...current,
        alarms: [updatedAlarm, ...remaining].sort((left, right) => {
          const statusWeight: Record<MonitoringAlarmStatus, number> = {
            New: 4,
            Acknowledged: 3,
            ConvertedToWorkOrder: 2,
            Closed: 1,
          }
          return statusWeight[right.status] - statusWeight[left.status] ||
            new Date(right.triggeredAt).getTime() - new Date(left.triggeredAt).getTime()
        }),
      }
    })
  }

  function applyTelemetryResult(result: TelemetryIngestionResult) {
    if (!result.succeeded || !result.reading) {
      return
    }

    setLastTelemetryResult(result)
    setSelectedIotPoint((current) => ({
      ...current,
      recentReadings: [result.reading!, ...current.recentReadings].slice(0, 12),
    }))
  }

  return (
    <main className="admin-shell">
      <aside className="side-nav" aria-label="后勤系统功能菜单">
        <div className="logo-lockup">
          <span className="logo-mark">L</span>
          <div>
            <strong>LOGO</strong>
            <span>智慧后勤</span>
          </div>
        </div>

        <nav className="nav-groups">
          {blueprint.navigationGroups.map((group) => (
            <section className="nav-group" key={group.code}>
              <p>{group.name}</p>
              {group.items.map((item) => (
                <button
                  aria-pressed={activeMenuItem === item}
                  className={activeMenuItem === item ? 'active' : ''}
                  key={item}
                  type="button"
                  onClick={() => openWorkspacePage(item)}
                >
                  {item}
                </button>
              ))}
            </section>
          ))}
        </nav>
      </aside>

      <section className="admin-main">
        <header className="top-toolbar">
          <div>
            <span className="tenant">同仁亦庄院区</span>
            <h1>医院后勤管理工作台</h1>
          </div>
          <div className="toolbar-actions">
            <label>
              <span>搜索</span>
              <input placeholder="工单 / 设备 / 空间" />
            </label>
            <div className="sync-state">
              <span className={source === 'api' ? 'dot online' : 'dot'} />
              {source === 'api' ? 'API 实时数据' : '本地业务种子数据'}
            </div>
          </div>
        </header>

        <section className="metric-strip" aria-label="运营指标">
          {blueprint.workbenchMetrics.map((metric) => (
            <article className={`metric-card ${metric.tone}`} key={metric.code}>
              <span>{metric.name}</span>
              <strong>{metric.value}</strong>
              <em>{metric.trend}</em>
            </article>
          ))}
          <article className="metric-card cyan">
            <span>未闭环工单</span>
            <strong>{dispatchBoard.slaRisk.openWorkOrders}</strong>
            <em>待跟踪</em>
          </article>
        </section>

        <section className="page-context">
          <div>
            <span>当前业务页</span>
            <h2>{pageProfile.title}</h2>
            <p>{pageProfile.summary}</p>
          </div>
        </section>

        <section className="content-grid" data-active-page={activePage}>
          <section className="panel dispatch-panel">
            <PanelHeader title="工单调度中心" meta="按 SLA / 优先级 / 班组负载派工" />
            <div className="service-workflow-header">
              <div>
                <span>一站式服务流程</span>
                <h2>{activeServiceTab}</h2>
                <p>
                  {activeServiceTab === '工单调度'
                    ? '按 SLA、风险等级、专业班组负载和 BIM 空间位置处理今日后勤工单。'
                    : '围绕一站式服务闭环处理当前页面的主对象、操作和流转结果。'}
                </p>
              </div>
              <div className="service-workflow-tabs" role="tablist" aria-label="一站式服务流程">
                {serviceWorkflowTabs.map((tab) => (
                  <button
                    aria-selected={tab === activeServiceTab}
                    className={tab === activeServiceTab ? 'active' : ''}
                    key={tab}
                    onClick={() => openServiceWorkflowTab(tab)}
                    role="tab"
                    type="button"
                  >
                    {tab}
                  </button>
                ))}
              </div>
            </div>
            {activeServiceTab === '服务受理' && (
              <div className="service-stage-grid" data-testid="service-intake-panel">
                <section className="stage-card">
                  <h2>请求来源</h2>
                  <ul>
                    <li>电话受理、移动报修、现场登记</li>
                    <li>物联告警、巡检异常、BIM 空间入口</li>
                    <li>科室联系人、空间、设备和问题描述</li>
                  </ul>
                </section>
                <section className="stage-card primary">
                  <h2>服务请求登记</h2>
                  <p>登记请求、补齐空间和设备，生成待派工单。</p>
                  <dl className="detail-list">
                    <div>
                      <dt>示例来源</dt>
                      <dd>门诊医技楼共享大厅现场报修</dd>
                    </div>
                    <div>
                      <dt>建议分类</dt>
                      <dd>综合维修 / 高优先级 / 需调度确认</dd>
                    </div>
                    <div>
                      <dt>空间绑定</dt>
                      <dd>门诊医技楼 · 共享大厅</dd>
                    </div>
                  </dl>
                  <button type="button" onClick={createServiceIntakeWorkOrder}>
                    生成待派工单
                  </button>
                </section>
                <section className="stage-card">
                  <h2>受理校验</h2>
                  <p>检查联系人、空间、设备、服务类型和重复工单，减少无效派工。</p>
                </section>
              </div>
            )}

            {activeServiceTab === '工单调度' && (
              <div className="dispatch-workbench">
              <section>
                <h2>工单池</h2>
                <div className="work-order-list" data-testid="work-order-list">
                  {sortedWorkOrders.map((order) => (
                    <button
                      className={`work-order-card ${selectedDetail.workOrder.workOrderNo === order.workOrderNo ? 'selected' : ''}`}
                      key={order.workOrderNo}
                      type="button"
                      onClick={() => void loadDetail(order.workOrderNo)}
                    >
                      <strong>{order.title}</strong>
                      <span>{order.workOrderNo} / {priorityLabels[order.priority]} / 状态：{statusLabels[order.status]}</span>
                      <small>{order.location.building} · {order.location.room}</small>
                    </button>
                  ))}
                </div>
              </section>

              <section className="recommendation-panel">
                <h2>派工建议</h2>
                <strong>{selectedRecommendation?.recommendedTeam ?? '综合维修班'}</strong>
                <p>{selectedRecommendation?.reason ?? '按工单类型和班组负载推荐'}</p>
                <button type="button" onClick={() => void dispatchSelected()}>
                  派工到{selectedRecommendation?.recommendedTeam ?? '综合维修班'}
                </button>
              </section>

              <section className="detail-panel" data-testid="work-order-detail">
                <h2>工单详情</h2>
                <div className="detail-title">
                  <strong>{selectedDetail.workOrder.title}</strong>
                  <span>状态：{statusLabels[selectedDetail.workOrder.status]}</span>
                </div>
                <dl className="detail-list">
                  <div>
                    <dt>位置</dt>
                    <dd>{selectedDetail.location.building} · {selectedDetail.location.room}</dd>
                  </div>
                  <div>
                    <dt>BIM</dt>
                    <dd>{selectedDetail.location.bimElementId}</dd>
                  </div>
                  <div>
                    <dt>SLA</dt>
                    <dd>SLA 风险：{selectedDetail.slaRiskLevel} / 剩余 {selectedDetail.slaMinutesRemaining} 分钟</dd>
                  </div>
                  <div>
                    <dt>来源</dt>
                    <dd>{selectedDetail.sourceEvidence.map((evidence) => evidence.featureName).join('、')}</dd>
                  </div>
                </dl>
                <div className="action-bar" data-testid="work-order-actions">
                  <button type="button" onClick={() => void transitionSelected('Accept', '接单', 'Accepted')}>
                    接单处理
                  </button>
                  <button type="button" onClick={() => void transitionSelected('Suspend', '挂单', 'Suspended')}>
                    挂单
                  </button>
                  <button type="button" onClick={() => void transitionSelected('Complete', '完工', 'PendingAcceptance')}>
                    完工
                  </button>
                </div>
              </section>

              <section className="timeline-panel" data-testid="work-order-timeline">
                <h2>流转记录</h2>
                <ol>
                  {selectedDetail.timeline.map((entry) => (
                    <li key={`${entry.occurredAt}-${entry.action}-${entry.toStatus}`}>
                      <strong>{entry.action}</strong>
                      <span>{entry.operator} · {statusLabels[entry.fromStatus]} → {statusLabels[entry.toStatus]}</span>
                      <small>{entry.remark}</small>
                    </li>
                  ))}
                </ol>
              </section>
            </div>
            )}

            {activeServiceTab === '任务执行' && (
              <div className="service-stage-grid" data-testid="task-execution-panel">
                <section className="stage-card">
                  <h2>我的任务</h2>
                  <p>{selectedDetail.workOrder.title}</p>
                  <small>{selectedDetail.location.building} · {selectedDetail.location.room}</small>
                </section>
                <section className="stage-card primary">
                  <h2>现场处置</h2>
                  <p>班组人员在这里接单、记录到场、补充现场说明，并提交完工证据。</p>
                  <div className="action-bar">
                    <button type="button" onClick={() => void transitionSelected('Accept', '接单', 'Accepted')}>
                      接单
                    </button>
                    <button type="button" onClick={() => void transitionSelected('Suspend', '挂单', 'Suspended')}>
                      挂单
                    </button>
                    <button type="button" onClick={() => void transitionSelected('Complete', '完工', 'PendingAcceptance')}>
                      完工
                    </button>
                  </div>
                </section>
                <section className="stage-card">
                  <h2>异常动作</h2>
                  <p>缺备件、跨专业协同或现场条件不足时，挂单和转单必须写明原因。</p>
                </section>
              </div>
            )}

            {activeServiceTab === '验收回访' && (
              <div className="service-stage-grid" data-testid="acceptance-review-panel">
                <section className="stage-card">
                  <h2>待验收工单</h2>
                  <p>{selectedDetail.workOrder.workOrderNo}</p>
                  <small>{selectedDetail.workOrder.title}</small>
                  <small>状态：{statusLabels[selectedDetail.workOrder.status]}</small>
                </section>
                <section className="stage-card primary">
                  <h2>验收判断</h2>
                  <p>核对完工说明、SLA 结果、现场证据和服务对象反馈，未达标可驳回整改。</p>
                  <div className="action-bar">
                    <button type="button" onClick={() => void transitionSelected('AcceptCompletion', '验收', 'PendingEvaluation')}>
                      验收通过
                    </button>
                    <button type="button" onClick={() => void transitionSelected('RejectCompletion', '驳回', 'InProgress')}>
                      驳回整改
                    </button>
                  </div>
                </section>
                <section className="stage-card">
                  <h2>回访要点</h2>
                  <p>确认问题是否解决、现场是否恢复、是否有重复故障或投诉风险。</p>
                </section>
              </div>
            )}

            {activeServiceTab === '服务评价' && (
              <div className="service-stage-grid" data-testid="service-evaluation-panel">
                <section className="stage-card">
                  <h2>评价与投诉</h2>
                  <p>服务对象对已关闭或待评价工单进行评分、反馈和投诉标记。</p>
                  <dl className="detail-list">
                    <div>
                      <dt>工单</dt>
                      <dd>{selectedDetail.workOrder.workOrderNo}</dd>
                    </div>
                    <div>
                      <dt>状态</dt>
                      <dd>状态：{statusLabels[selectedDetail.workOrder.status]}</dd>
                    </div>
                    <div>
                      <dt>评价</dt>
                      <dd>{latestEvaluation?.rating ? `${latestEvaluation.rating} 星` : '待服务对象评价'}</dd>
                    </div>
                  </dl>
                  <button
                    disabled={selectedDetail.workOrder.status !== 'PendingEvaluation'}
                    type="button"
                    onClick={() => void transitionSelected('Evaluate', '评价', 'Closed', 5)}
                  >
                    五星评价并归档
                  </button>
                </section>
                <section className="stage-card primary">
                  <h2>服务品质沉淀</h2>
                  <p>SLA 达成、满意度、驳回、投诉和重复故障进入服务品质和考核指标。</p>
                  <dl className="detail-list">
                    <div>
                      <dt>SLA</dt>
                      <dd>{dispatchBoard.slaRisk.openWorkOrders} 个未闭环工单仍需跟踪</dd>
                    </div>
                    <div>
                      <dt>风险</dt>
                      <dd>{dispatchBoard.slaRisk.highestRiskLevel} / {dispatchBoard.slaRisk.highestRiskWorkOrderNo}</dd>
                    </div>
                  </dl>
                </section>
                <section className="stage-card">
                  <h2>整改入口</h2>
                  <p>低分评价或投诉应生成质量问题，并能追溯到班组、供应商和合同。</p>
                </section>
              </div>
            )}
          </section>

          <section className="panel asset-maintenance-panel">
            <PanelHeader title="资产台账与巡检保养" meta="台账 / BIM位置 / 计划任务 / 异常转工单" />
            <div className="asset-maintenance-workbench">
              <section>
                <h2>设备设施台账</h2>
                <div className="asset-list">
                  {assetBoard.assets.map((asset) => (
                    <button
                      className={`asset-card ${selectedAssetDetail.asset.assetCode === asset.assetCode ? 'selected' : ''}`}
                      key={asset.assetCode}
                      type="button"
                      onClick={() => void loadAssetDetail(asset.assetCode)}
                    >
                      <strong>{asset.name}</strong>
                      <span>{asset.assetCode} / {asset.system} / {criticalityLabels[asset.criticality]}</span>
                      <small>健康度 {asset.healthScore} / {statusLabels[asset.status]}</small>
                    </button>
                  ))}
                </div>
              </section>

              <section className="asset-detail-panel">
                <h2>资产详情</h2>
                <div className="detail-title">
                  <strong>{selectedAssetDetail.asset.name}</strong>
                  <span>{selectedAssetDetail.asset.assetCode}</span>
                </div>
                <dl className="detail-list">
                  <div>
                    <dt>BIM</dt>
                    <dd>{selectedAssetDetail.asset.location.bimElementId}</dd>
                  </div>
                  <div>
                    <dt>位置</dt>
                    <dd>{selectedAssetDetail.asset.location.building} / {selectedAssetDetail.asset.location.room}</dd>
                  </div>
                  <div>
                    <dt>策略</dt>
                    <dd>{selectedAssetDetail.asset.maintenanceStrategy}</dd>
                  </div>
                  <div>
                    <dt>风险</dt>
                    <dd>{selectedAssetDetail.asset.currentRisk}</dd>
                  </div>
                </dl>
              </section>

              <section className="maintenance-task-panel">
                <h2>巡检任务</h2>
                {(selectedAssetDueTasks.length > 0 ? selectedAssetDueTasks : selectedAssetDetail.tasks).map((task) => (
                  <article className="maintenance-task-card" key={task.taskNo}>
                    <div>
                      <strong>{task.title}</strong>
                      <span>{task.taskNo} / {maintenanceStatusLabels[task.status]} / {priorityLabels[task.priority]}</span>
                    </div>
                    {task.workOrderNo ? <em>{task.workOrderNo}</em> : null}
                    <button
                      type="button"
                      disabled={task.status === 'Completed' || task.status === 'ConvertedToWorkOrder'}
                      onClick={() => void completeMaintenanceTask(task, true)}
                    >
                      异常完成并转工单
                    </button>
                  </article>
                ))}
                {lastMaintenanceResult?.generatedWorkOrder ? (
                  <div className="generated-workorder" data-testid="maintenance-generated-workorder">
                    <strong>已转工单</strong>
                    <span>{lastMaintenanceResult.generatedWorkOrder.workOrderNo}</span>
                    <small>{lastMaintenanceResult.generatedWorkOrder.title}</small>
                    <small>BIM：{convertedMaintenanceDetail?.location.bimElementId ?? lastMaintenanceResult.generatedWorkOrder.location.bimElementId}</small>
                    <small>责任班组：{lastMaintenanceResult.generatedWorkOrder.responsibleTeam}</small>
                    <small>
                      来源：{convertedMaintenanceDetail?.sourceEvidence.map((evidence) => evidence.featureName).join('、') ?? '巡检保养异常转工单'}
                    </small>
                    <button type="button" onClick={openConvertedMaintenanceWorkOrder}>
                      进入调度池
                    </button>
                  </div>
                ) : null}
              </section>

              <section className="asset-lifecycle-panel">
                <h2>生命周期记录</h2>
                <ol>
                  {selectedAssetDetail.lifecycle.slice(0, 5).map((item) => (
                    <li key={`${item.occurredAt}-${item.eventType}`}>
                      <strong>{item.eventType}</strong>
                      <span>{item.operator}</span>
                      <small>{item.summary}</small>
                    </li>
                  ))}
                </ol>
              </section>
            </div>
          </section>

          <section className="panel iot-panel">
            <PanelHeader title="客户物联点位接入" meta="系统 / 点位 / 字段 / 阈值 / 读数风险" />
            <div className="iot-workbench">
              <section>
                <h2>系统覆盖</h2>
                <div className="iot-system-grid">
                  {iotCatalog.systems.map((system) => (
                    <article key={system.category}>
                      <strong>{system.name}</strong>
                      <span>{system.subsystems.slice(0, 3).join('、')}</span>
                    </article>
                  ))}
                </div>
              </section>

              <section>
                <h2>点位目录</h2>
                <div className="iot-point-list" data-testid="iot-point-list">
                  {iotCatalog.points.map((point) => (
                    <button
                      className={`iot-point-card ${selectedIotPoint.point.pointCode === point.pointCode ? 'selected' : ''}`}
                      key={point.pointCode}
                      type="button"
                      onClick={() => void loadIotPointDetail(point.pointCode)}
                    >
                      <strong>{point.name}</strong>
                      <span>{point.pointCode} / {iotCategoryLabels[point.category]}</span>
                      <small>{point.protocolAdapter} / {point.location.bimElementId}</small>
                    </button>
                  ))}
                </div>
              </section>

              <section className="iot-detail-panel" data-testid="iot-point-detail">
                <h2>时序字段</h2>
                <div className="detail-title">
                  <strong>{selectedIotPoint.point.name}</strong>
                  <span>{selectedIotPoint.point.deviceCode}</span>
                </div>
                <div className="module-tags">
                  {selectedIotPoint.point.metrics.map((metric) => (
                    <span key={metric.code}>{metric.name} / {metric.sourceField}</span>
                  ))}
                </div>
                <dl className="detail-list">
                  <div>
                    <dt>BIM</dt>
                    <dd>{selectedIotPoint.point.location.bimElementId}</dd>
                  </div>
                  <div>
                    <dt>来源</dt>
                    <dd>{selectedIotPoint.sourceEvidence.flatMap((item) => item.sources).join('、')}</dd>
                  </div>
                </dl>
                <button type="button" onClick={() => void ingestCriticalTelemetry()}>
                  模拟异常读数
                </button>
              </section>

              <section className="iot-reading-panel" data-testid="iot-reading-panel">
                <h2>阈值与读数</h2>
                {selectedIotPoint.thresholdRules.map((rule) => (
                  <article key={`${rule.pointCode}-${rule.metricCode}`}>
                    <strong>{rule.metricCode}</strong>
                    <span>{rule.ruleSummary}</span>
                  </article>
                ))}
                {selectedIotPoint.recentReadings.slice(0, 4).map((reading) => (
                  <article className={`reading-card ${reading.riskLevel}`} key={`${reading.collectedAt}-${reading.metricCode}`}>
                    <strong>{reading.metricCode}: {reading.value} {reading.unit}</strong>
                    <span>{telemetryRiskLabels[reading.riskLevel]}</span>
                    <small>{reading.ruleSummary}</small>
                  </article>
                ))}
                {lastTelemetryResult ? (
                  <div className="generated-workorder">
                    <strong>最新风险：{telemetryRiskLabels[lastTelemetryResult.riskLevel]}</strong>
                    <span>{lastTelemetryResult.pointCode} / {lastTelemetryResult.metricCode}</span>
                  </div>
                ) : null}
              </section>
            </div>
          </section>

          <section className="panel spatial-panel">
            <PanelHeader title="BIM 空间业务定位" meta="设备 / 告警 / 工单同图层" />
            {activePage === 'spatial' ? (
              <>
                <div className="spatial-workbench">
                  <section>
                    <h2>楼层业务图层</h2>
                    <div className="floor-map" data-testid="spatial-floor-map">
                      {spatialPoints.map((point) => (
                        <button
                          className={`map-node ${point.tone} ${selectedSpatialPoint?.id === point.id ? 'selected' : ''}`}
                          key={point.id}
                          type="button"
                          onClick={() => setSelectedSpatialPointId(point.id)}
                        >
                          <strong>{point.title}</strong>
                          <span>{point.subtitle}</span>
                          <small>{point.location.bimElementId}</small>
                        </button>
                      ))}
                      <span className="map-line" />
                    </div>
                  </section>

                  <section className="spatial-detail-panel" data-testid="spatial-point-detail">
                    <h2>空间点位详情</h2>
                    {selectedSpatialPoint ? (
                      <>
                        <div className="detail-title">
                          <strong>{selectedSpatialPoint.title}</strong>
                          <span>{selectedSpatialPoint.statusLabel}</span>
                        </div>
                        <dl className="detail-list">
                          <div>
                            <dt>对象</dt>
                            <dd>{selectedSpatialPoint.entityId}</dd>
                          </div>
                          <div>
                            <dt>位置</dt>
                            <dd>{selectedSpatialPoint.location.building} / {selectedSpatialPoint.location.room}</dd>
                          </div>
                          <div>
                            <dt>BIM</dt>
                            <dd>{selectedSpatialPoint.location.bimElementId}</dd>
                          </div>
                        </dl>
                        <button type="button" onClick={() => void openSpatialLinkedObject(selectedSpatialPoint)}>
                          {selectedSpatialPoint.actionLabel}
                        </button>
                      </>
                    ) : (
                      <p>暂无空间点位</p>
                    )}
                  </section>
                </div>
                <div className="space-summary">
                  <div>
                    <strong>{dispatchBoard.workOrders.length} 个工单点位</strong>
                    <span>调度池工单按 BIM 构件定位</span>
                  </div>
                  <div>
                    <strong>{assetBoard.assets.length} 个资产点位</strong>
                    <span>设备设施台账与空间绑定</span>
                  </div>
                  <div>
                    <strong>{alarmBoard.alarms.length} 个告警点位</strong>
                    <span>物联阈值事件可转处置工单</span>
                  </div>
                </div>
              </>
            ) : (
              <>
                <div className="floor-map">
                  <span className="map-node workorder">医废间工单</span>
                  <span className="map-node alert">门诊医梯</span>
                  <span className="map-node normal">眼科病区</span>
                  <span className="map-node normal">冷站机房</span>
                  <span className="map-line" />
                </div>
                <div className="space-summary">
                  {assetBoard.assets.map((asset) => (
                    <div key={asset.assetCode}>
                      <strong>{asset.name}</strong>
                      <span>{asset.location.bimElementId}</span>
                    </div>
                  ))}
                </div>
              </>
            )}
          </section>

          <section className="panel alert-panel">
            <PanelHeader title="环境预警池" meta="物联告警 / BIM位置 / 转工单 / 调度联动" />
            <div className="monitoring-alert-workbench">
              <section>
                <div className="panel-subhead">
                  <h2>告警池</h2>
                  <button type="button" onClick={() => void ingestCriticalTelemetry()}>
                    模拟异常读数
                  </button>
                </div>
                <div className="monitoring-alarm-list" data-testid="monitoring-alarm-list">
                  {alarmBoard.alarms.length > 0 ? (
                    alarmBoard.alarms.map((alarm) => (
                      <button
                        aria-label={`告警 ${alarm.title}`}
                        className={`monitoring-alarm-card ${activeAlarm?.alarmNo === alarm.alarmNo ? 'selected' : ''}`}
                        key={alarm.alarmNo}
                        type="button"
                        onClick={() => selectMonitoringAlarm(alarm.alarmNo)}
                      >
                        <strong>{alarm.title}</strong>
                        <span>{alarm.pointCode} / {alarm.metricCode} / {alarmStatusLabels[alarm.status]}</span>
                        <small>{alarm.location.building} · {alarm.location.room}</small>
                      </button>
                    ))
                  ) : (
                    <div className="empty-state">
                      <strong>暂无实时告警</strong>
                      <span>可通过物联异常读数生成预警事件。</span>
                    </div>
                  )}
                </div>
              </section>

              <section className="monitoring-alarm-detail" data-testid="monitoring-alarm-detail">
                <h2>告警详情</h2>
                {activeAlarm ? (
                  <>
                    <div className="detail-title">
                      <strong>{activeAlarm.title}</strong>
                      <span>{alarmStatusLabels[activeAlarm.status]}</span>
                    </div>
                    <dl className="detail-list">
                      <div>
                        <dt>点位</dt>
                        <dd>{activeAlarm.pointCode} / {activeAlarm.metricCode}</dd>
                      </div>
                      <div>
                        <dt>BIM</dt>
                        <dd>{activeAlarm.location.bimElementId}</dd>
                      </div>
                      <div>
                        <dt>读数</dt>
                        <dd>{activeAlarm.value} {activeAlarm.unit} / {telemetryRiskLabels[activeAlarm.riskLevel]}</dd>
                      </div>
                      <div>
                        <dt>规则</dt>
                        <dd>{activeAlarm.ruleSummary}</dd>
                      </div>
                      <div>
                        <dt>来源</dt>
                        <dd>{selectedAlarmEvidence.map((evidence) => evidence.featureName).join('、')}</dd>
                      </div>
                      <div>
                        <dt>工单</dt>
                        <dd>{activeAlarm.workOrderNo ?? '尚未转工单'}</dd>
                      </div>
                    </dl>
                    <div className="action-bar">
                      <button
                        type="button"
                        disabled={activeAlarm.status !== 'New'}
                        onClick={() => void acknowledgeSelectedAlarm()}
                      >
                        确认告警
                      </button>
                      <button
                        type="button"
                        disabled={activeAlarm.status === 'ConvertedToWorkOrder' || activeAlarm.status === 'Closed'}
                        onClick={() => void convertSelectedAlarmToWorkOrder()}
                      >
                        转处置工单
                      </button>
                      <button
                        type="button"
                        disabled={!activeAlarm.workOrderNo && !convertedAlarmDetail}
                        onClick={() => void openConvertedAlarmWorkOrder()}
                      >
                        进入调度池
                      </button>
                    </div>
                  </>
                ) : (
                  <div className="empty-state">
                    <strong>请选择告警</strong>
                    <span>告警确认、转工单和调度联动会在这里闭环。</span>
                  </div>
                )}
              </section>

              <section className="monitoring-alarm-flow">
                <h2>处置链路</h2>
                <ol>
                  <li>物联读数触发阈值</li>
                  <li>预警池确认责任班组</li>
                  <li>转入一站式工单调度</li>
                  <li>派工、接单、完工、验收</li>
                </ol>
                {convertedAlarmDetail ? (
                  <div className="generated-workorder">
                    <strong>已联动调度池</strong>
                    <span>{convertedAlarmDetail.workOrder.workOrderNo}</span>
                    <small>{convertedAlarmDetail.workOrder.title}</small>
                  </div>
                ) : null}
              </section>

              <section className="monitoring-alarm-evidence">
                <h2>来源依据</h2>
                <div className="source-tags">
                  {selectedAlarmEvidence.flatMap((evidence) => evidence.sources).map((sourceName, index) => (
                    <span key={`${sourceName}-${index}`}>{sourceName}</span>
                  ))}
                </div>
                <p>{selectedAlarmEvidence.map((evidence) => evidence.evidenceSummary).join(' ')}</p>
              </section>
            </div>
          </section>

          <section className="panel team-panel">
            <PanelHeader title="班组负载" meta="辅助派工建议" />
            <div className="team-list">
              {dispatchBoard.teamLoads.map((team) => (
                <article className="team-card" key={team.teamName}>
                  <div>
                    <strong>{team.teamName}</strong>
                    <span>{team.domain}</span>
                  </div>
                  <div className="progress">
                    <span style={{ width: `${Math.round((team.activeTasks / team.capacity) * 100)}%` }} />
                  </div>
                  <small>{team.activeTasks}/{team.capacity} · {team.recommendation}</small>
                </article>
              ))}
            </div>
          </section>

          <section className="panel module-panel">
            <PanelHeader
              title="PPT 功能矩阵落地"
              meta={`${blueprint.pptReportedPrimaryModuleTotal} 个一级模块 / PPT 总览 ${blueprint.pptReportedSecondaryItemTotal} 个子项`}
            />
            <p className="matrix-note">明细页可见 {detailedSecondaryTotal} 个子项；差异保留为后续原始清单校准项。</p>
            <div className="module-grid">
              {blueprint.featureGroups.map((group) => (
                <article className={`module-card ${group.domain}`} key={group.code}>
                  <div>
                    <strong>{group.name}</strong>
                    <span>{group.primaryModuleCount} 个一级模块 · {group.secondaryItemCount} 个明细子项</span>
                  </div>
                  <div className="module-tags">
                    {group.modules.map((module) => (
                      <span key={module}>{module}</span>
                    ))}
                  </div>
                </article>
              ))}
            </div>
          </section>

          <section className="panel evidence-panel">
            <PanelHeader title="来源可追溯功能目录" meta="客户数据 > 竞品功能树 > PPT 架构" />
            <div className="source-grid">
              {blueprint.featureEvidence.map((item) => (
                <article className="source-card" key={item.featureName}>
                  <div className="source-card-head">
                    <strong>{item.featureName}</strong>
                    <div className="source-tags">
                      {item.sources.map((sourceName) => (
                        <span key={sourceName}>{sourceName}</span>
                      ))}
                    </div>
                  </div>
                  <p>{item.evidenceSummary}</p>
                </article>
              ))}
            </div>
          </section>

          <section className="panel catalog-panel">
            <PanelHeader title="北建院客户数据目录" meta="系统 / 传感器 / 字段 / 角色 / 使用端" />
            <div className="catalog-grid">
              {blueprint.customerDataSystems.map((system) => (
                <article className="catalog-card" key={system.major}>
                  <strong>{system.major}</strong>
                  <span>{system.subsystems.slice(0, 4).join('、')}</span>
                  <dl>
                    <div>
                      <dt>字段</dt>
                      <dd>{system.dataFields.slice(0, 6).join('、')}</dd>
                    </div>
                    <div>
                      <dt>角色</dt>
                      <dd>{system.roles.slice(0, 3).join('、')}</dd>
                    </div>
                  </dl>
                </article>
              ))}
            </div>
          </section>

          <section className="panel competitor-panel">
            <PanelHeader title="竞品功能颗粒度" meta="成熟后勤模块拆成功能对象" />
            <div className="competitor-grid">
              {blueprint.competitorModules.map((module) => (
                <article className="competitor-card" key={module.name}>
                  <strong>{module.name}</strong>
                  <div className="module-tags">
                    {module.featureAreas.slice(0, 6).map((feature) => (
                      <span key={feature}>{feature}</span>
                    ))}
                  </div>
                </article>
              ))}
            </div>
          </section>

          <section className="panel phase-panel">
            <PanelHeader title="V1实施顺序" meta="先业务闭环，再专项扩展" />
            <div className="phase-list">
              {blueprint.implementationPhases.map((phase) => (
                <article className="phase-card" key={phase.order}>
                  <span>{phase.order}</span>
                  <div>
                    <strong>{phase.name}</strong>
                    <p>{phase.capabilities.join('、')}</p>
                  </div>
                </article>
              ))}
            </div>
          </section>

          <section className="panel workflow-panel">
            <PanelHeader title="业务闭环" meta="从办事到考核归档" />
            <div className="workflow-grid">
              {blueprint.workflows.map((workflow) => (
                <article key={workflow.code}>
                  <strong>{workflow.name}</strong>
                  <ol>
                    {workflow.steps.map((step) => (
                      <li key={step}>{step}</li>
                    ))}
                  </ol>
                </article>
              ))}
            </div>
          </section>
        </section>
      </section>
    </main>
  )
}

function buildLocalDetail(order: WorkOrder): WorkOrderDetail {
  return {
    workOrder: order,
    location: order.location,
    sourceEvidence: order.workOrderNo.startsWith('WO-ALM-')
      ? [alarmSourceEvidence[1], sourceEvidence[0]]
      : order.workOrderNo.startsWith('WO-MT-')
      ? [assetSourceEvidence[3], assetSourceEvidence[1], sourceEvidence[0]]
      : order.serviceType.includes('环境')
      ? [sourceEvidence[0], sourceEvidence[2]]
      : [sourceEvidence[0]],
    timeline: [
      {
        occurredAt: order.createdAt,
        operator: '系统',
        action: '创建',
        fromStatus: 'New',
        toStatus: order.status,
        remark: order.workOrderNo.startsWith('WO-MT-')
          ? '来自巡检保养异常转工单的模拟工单'
          : '来自一站式服务或监测告警的模拟工单',
      },
    ],
    slaRiskLevel: order.priority === 'Critical' || order.status === 'Escalated' ? 'High' : 'Medium',
    slaMinutesRemaining: order.workOrderNo === 'WO-20260530-0001' ? 18 : 120,
    allowedActions: ['Dispatch', 'Accept', 'Suspend', 'Complete'],
  }
}

function buildLocalAssetDetail(assetCode: string): AssetMaintenanceDetail {
  const asset =
    localAssetMaintenanceBoard.assets.find((item) => item.assetCode === assetCode) ??
    localAssetMaintenanceBoard.assets[0]
  const plans = localAssetMaintenanceBoard.plans.filter((plan) => plan.assetCode === asset.assetCode)
  const tasks = localAssetMaintenanceBoard.dueTasks.filter((task) => task.assetCode === asset.assetCode)
  const lifecycle = localAssetMaintenanceBoard.lifecycleEvents.filter((item) => item.assetCode === asset.assetCode)

  return {
    asset,
    plans,
    tasks,
    lifecycle,
    sourceEvidence: assetSourceEvidence,
  }
}

function buildMaintenanceWorkOrderDetail(
  generatedWorkOrder: MaintenanceGeneratedWorkOrder,
  task: MaintenanceTask,
): WorkOrderDetail {
  const isMedicalGas = task.assetCode.includes('MEDGAS') || generatedWorkOrder.serviceType.includes('医')
  const workOrder: WorkOrder = {
    ...generatedWorkOrder,
    slaDueAt: addMinutesToIso(generatedWorkOrder.createdAt, generatedWorkOrder.priority === 'Critical' ? 60 : 240),
  }

  return {
    workOrder,
    location: generatedWorkOrder.location,
    sourceEvidence: [
      assetSourceEvidence[3],
      ...(isMedicalGas ? [assetSourceEvidence[2]] : []),
      assetSourceEvidence[1],
      sourceEvidence[0],
    ],
    timeline: [
      {
        occurredAt: generatedWorkOrder.createdAt,
        operator: task.completedBy ?? task.responsibleTeam,
        action: '巡检异常建单',
        fromStatus: 'New',
        toStatus: generatedWorkOrder.status,
        remark: `${task.title}发现异常，转入一站式工单调度池`,
      },
    ],
    slaRiskLevel: generatedWorkOrder.priority === 'Critical' ? 'High' : 'Medium',
    slaMinutesRemaining: generatedWorkOrder.priority === 'Critical' ? 60 : 240,
    allowedActions: ['Dispatch', 'Accept', 'Suspend', 'Complete'],
  }
}

function addMinutesToIso(value: string, minutes: number) {
  const date = new Date(value)
  if (Number.isNaN(date.getTime())) {
    return value
  }

  date.setMinutes(date.getMinutes() + minutes)
  return date.toISOString()
}

function buildLocalMaintenanceResult(
  task: MaintenanceTask,
  convertToWorkOrder: boolean,
  checklistResults: InspectionChecklistResult[],
): MaintenanceTaskOperationResult {
  const completedAt = new Date().toISOString()
  const generatedWorkOrder = convertToWorkOrder
    ? {
        workOrderNo: `WO-MT-${task.taskNo.replace('MT-', '')}`,
        title: `${task.title}异常处置`,
        serviceType: '设备巡检异常',
        priority: task.priority,
        status: 'New' as WorkOrderStatus,
        location: buildLocalAssetDetail(task.assetCode).asset.location,
        responsibleTeam: task.responsibleTeam,
        createdAt: completedAt,
      }
    : null

  return {
    succeeded: true,
    errorMessage: null,
    task: {
      ...task,
      status: convertToWorkOrder ? 'ConvertedToWorkOrder' : 'Completed',
      outcome: convertToWorkOrder ? 'Abnormal' : 'Normal',
      completedAt,
      completedBy: task.responsibleTeam,
      checklistResults,
      workOrderNo: generatedWorkOrder?.workOrderNo ?? null,
    },
    generatedWorkOrder,
    notFound: false,
  }
}

function buildLocalIotPointDetail(pointCode: string): IotPointDetail {
  const point = localIotCatalog.points.find((item) => item.pointCode === pointCode) ?? localIotCatalog.points[0]

  return {
    point,
    thresholdRules: localIotCatalog.thresholdRules.filter((rule) => rule.pointCode === point.pointCode),
    recentReadings: [
      {
        pointCode: point.pointCode,
        metricCode: point.metrics[0]?.code ?? 'pressure',
        value: point.pointCode.includes('PWR') ? 228 : 0.39,
        unit: point.metrics[0]?.unit ?? '',
        collectedAt: '2026-05-30T09:25:00+08:00',
        riskLevel: 'Normal',
        ruleSummary: '种子读数正常',
      },
    ],
    sourceEvidence: iotSourceEvidence,
  }
}

function buildLocalTelemetryResult(
  point: IotMonitoringPoint,
  metricCode: string,
  value: number,
  unit: string,
): TelemetryIngestionResult {
  const reading: TelemetryReading = {
    pointCode: point.pointCode,
    metricCode,
    value,
    unit,
    collectedAt: new Date().toISOString(),
    riskLevel: 'Critical',
    ruleSummary: '当前值低于阈值',
  }

  return {
    succeeded: true,
    errorMessage: null,
    pointCode: point.pointCode,
    metricCode,
    riskLevel: 'Critical',
    ruleSummary: reading.ruleSummary,
    reading,
    notFound: false,
  }
}

function buildLocalAlarmFromTelemetry(point: IotMonitoringPoint, reading: TelemetryReading): MonitoringAlarmEvent {
  return {
    alarmNo: `ALM-${point.pointCode}-${reading.metricCode}-${Date.now()}`,
    pointCode: point.pointCode,
    metricCode: reading.metricCode,
    title: `${point.name}${reading.metricCode}异常`,
    riskLevel: reading.riskLevel,
    status: 'New',
    location: point.location,
    value: reading.value,
    unit: reading.unit,
    triggeredAt: reading.collectedAt,
    ruleSummary: reading.ruleSummary,
    responsibleTeam: recommendedTeamForPoint(point),
    lastRemark: '由前端模拟异常读数生成预警事件',
  }
}

function buildLocalAlarmWorkOrderDetail(alarm: MonitoringAlarmEvent): WorkOrderDetail {
  const createdAt = alarm.triggeredAt
  const workOrder: WorkOrder = {
    workOrderNo: alarm.workOrderNo ?? buildAlarmWorkOrderNo(alarm),
    title: `${alarm.title}处置`,
    serviceType: '物联告警处置',
    priority: alarm.riskLevel === 'Critical' ? 'Critical' : 'High',
    status: 'New',
    location: alarm.location,
    responsibleTeam: alarm.responsibleTeam,
    createdAt,
    slaDueAt: addHours(createdAt, alarm.riskLevel === 'Critical' ? 1 : 2),
  }

  return {
    workOrder,
    location: alarm.location,
    sourceEvidence: [alarmSourceEvidence[1], sourceEvidence[0]],
    timeline: [
      {
        occurredAt: createdAt,
        operator: '预警池调度员',
        action: '外部来源建单',
        fromStatus: 'New',
        toStatus: 'New',
        remark: alarm.lastRemark ?? alarm.ruleSummary,
      },
    ],
    slaRiskLevel: alarm.riskLevel === 'Critical' ? 'High' : 'Medium',
    slaMinutesRemaining: alarm.riskLevel === 'Critical' ? 60 : 120,
    allowedActions: ['Dispatch'],
  }
}

function buildAlarmWorkOrderNo(alarm: MonitoringAlarmEvent) {
  return `WO-ALM-${alarm.alarmNo.replace(/^ALM-?/i, '').replace(/[^a-z0-9]+/gi, '-').replace(/^-|-$/g, '').toUpperCase()}`
}

function recommendedTeamForPoint(point: IotMonitoringPoint) {
  if (point.category === 'MedicalGas') {
    return '医气维保人员'
  }

  if (point.category === 'EnvironmentQuality' || point.pointCode.includes('WASTE')) {
    return '环境监管班组'
  }

  if (point.category === 'StrongElectric') {
    return '电工班工作人员'
  }

  return '综合维修班'
}

function addHours(value: string, hours: number) {
  const date = new Date(value)
  date.setHours(date.getHours() + hours)
  return date.toISOString()
}

async function fetchWithTimeout(input: RequestInfo | URL, init: RequestInit = {}, timeoutMs = 1200) {
  const controller = new AbortController()
  const timeout = window.setTimeout(() => controller.abort(), timeoutMs)

  try {
    return await fetch(input, { ...init, signal: controller.signal })
  } finally {
    window.clearTimeout(timeout)
  }
}

function PanelHeader({ title, meta }: { title: string; meta: string }) {
  return (
    <div className="panel-header">
      <h2>{title}</h2>
      <span>{meta}</span>
    </div>
  )
}

export default App
