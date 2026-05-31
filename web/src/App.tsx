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

const statusLabels: Record<WorkOrderStatus | FacilityStatus | SignalStatus, string> = {
  New: '新建',
  Dispatched: '已派工',
  Accepted: '处理中',
  InProgress: '处理中',
  Suspended: '已挂单',
  Transferred: '已转单',
  PendingAcceptance: '待验收',
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

const apiBase = import.meta.env.VITE_API_BASE_URL ?? 'http://localhost:5248'

function App() {
  const [dashboard, setDashboard] = useState(localDashboard)
  const [blueprint, setBlueprint] = useState(localBlueprint)
  const [dispatchBoard, setDispatchBoard] = useState(localDispatchBoard)
  const [selectedDetail, setSelectedDetail] = useState(() => buildLocalDetail(localWorkOrders[0]))
  const [source, setSource] = useState<'api' | 'local'>('local')

  useEffect(() => {
    const controller = new AbortController()

    Promise.all([
      fetch(`${apiBase}/api/operations/dashboard`, { signal: controller.signal }),
      fetch(`${apiBase}/api/logistics/blueprint`, { signal: controller.signal }),
      fetch(`${apiBase}/api/operations/dispatch-board`, { signal: controller.signal }),
    ])
      .then(async ([dashboardResponse, blueprintResponse, boardResponse]) => {
        if (!dashboardResponse.ok || !blueprintResponse.ok || !boardResponse.ok) {
          throw new Error('Logistics API unavailable')
        }

        const board = (await boardResponse.json()) as DispatchBoard
        setDashboard((await dashboardResponse.json()) as OperationsDashboard)
        setBlueprint((await blueprintResponse.json()) as LogisticsBlueprint)
        setDispatchBoard(board)
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

  async function dispatchSelected() {
    const teamName = selectedRecommendation?.recommendedTeam ?? '综合维修班'
    if (source === 'api') {
      const response = await fetch(`${apiBase}/api/operations/work-orders/${selectedDetail.workOrder.workOrderNo}/dispatch`, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ teamName, dispatcher: '调度员', remark: '按SLA风险和专业班组派工' }),
      })
      if (response.ok) {
        applyDetail((await response.json()) as WorkOrderDetail)
        return
      }
    }

    applyLocalTransition('派工', '调度员', '按SLA风险和专业班组派工', 'Dispatched', teamName)
  }

  async function transitionSelected(action: WorkOrderTransitionAction, label: string, nextStatus: WorkOrderStatus) {
    if (source === 'api') {
      const response = await fetch(`${apiBase}/api/operations/work-orders/${selectedDetail.workOrder.workOrderNo}/transition`, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ action, operator: '环境监管班组', remark: label }),
      })
      if (response.ok) {
        applyDetail((await response.json()) as WorkOrderDetail)
        return
      }
    }

    applyLocalTransition(label, '环境监管班组', label, nextStatus)
  }

  function applyLocalTransition(
    action: string,
    operator: string,
    remark: string,
    nextStatus: WorkOrderStatus,
    responsibleTeam = selectedDetail.workOrder.responsibleTeam,
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
                <a href={`#${item}`} key={item}>
                  {item}
                </a>
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

        <section className="content-grid">
          <section className="panel dispatch-panel">
            <PanelHeader title="工单调度中心" meta="按 SLA / 优先级 / 班组负载派工" />
            <div className="dispatch-workbench">
              <section>
                <h2>工单池</h2>
                <div className="work-order-list">
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

              <section className="detail-panel">
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
                <div className="action-bar">
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

              <section className="timeline-panel">
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
          </section>

          <section className="panel spatial-panel">
            <PanelHeader title="BIM 空间业务定位" meta="设备 / 告警 / 工单同图层" />
            <div className="floor-map">
              <span className="map-node workorder">医废间工单</span>
              <span className="map-node alert">门诊医梯</span>
              <span className="map-node normal">眼科病区</span>
              <span className="map-node normal">冷站机房</span>
              <span className="map-line" />
            </div>
            <div className="space-summary">
              {dashboard.assets.map((asset) => (
                <div key={asset.assetCode}>
                  <strong>{asset.name}</strong>
                  <span>{asset.location.bimElementId}</span>
                </div>
              ))}
            </div>
          </section>

          <section className="panel alert-panel">
            <PanelHeader title="环境预警池" meta="点位异常转处置工单" />
            <div className="alert-list">
              {dashboard.environmentSignals.map((signal) => (
                <article className={`alert-item ${signal.status}`} key={signal.signalCode}>
                  <div>
                    <strong>{signal.name}</strong>
                    <span>{signal.location.building} · {signal.location.room}</span>
                  </div>
                  <em>{signal.value} {signal.unit}</em>
                  <span className={`badge ${signal.status}`}>{statusLabels[signal.status]}</span>
                </article>
              ))}
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
    sourceEvidence: order.serviceType.includes('环境')
      ? [sourceEvidence[0], sourceEvidence[2]]
      : [sourceEvidence[0]],
    timeline: [
      {
        occurredAt: order.createdAt,
        operator: '系统',
        action: '创建',
        fromStatus: 'New',
        toStatus: order.status,
        remark: '来自一站式服务或监测告警的模拟工单',
      },
    ],
    slaRiskLevel: order.priority === 'Critical' || order.status === 'Escalated' ? 'High' : 'Medium',
    slaMinutesRemaining: order.workOrderNo === 'WO-20260530-0001' ? 18 : 120,
    allowedActions: ['Dispatch', 'Accept', 'Suspend', 'Complete'],
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
