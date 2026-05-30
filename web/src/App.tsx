import { useEffect, useMemo, useState } from 'react'
import './App.css'

type OperationDomain =
  | 'Foundation'
  | 'LogisticsService'
  | 'Environment'
  | 'IntegratedManagement'

type Priority = 'Low' | 'Normal' | 'High' | 'Critical'
type WorkOrderStatus =
  | 'New'
  | 'Dispatched'
  | 'InProgress'
  | 'PendingAcceptance'
  | 'Closed'
  | 'Escalated'
type FacilityStatus = 'Normal' | 'Warning' | 'Fault' | 'Maintenance'
type SignalStatus = 'Normal' | 'Warning' | 'Critical'

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

type LogisticsBlueprint = {
  pptReportedPrimaryModuleTotal: number
  pptReportedSecondaryItemTotal: number
  navigationGroups: NavigationGroup[]
  featureGroups: LogisticsFeatureGroup[]
  workflows: LogisticsWorkflow[]
  workbenchMetrics: WorkbenchMetric[]
  teamLoads: TeamLoad[]
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
      system: '医疗废物',
      location: locations.waste,
      status: 'Fault',
      lastSignalAt: '2026-05-30T09:25:00+08:00',
      currentRisk: '负压值低于阈值，已触发应急工单',
    },
  ],
  workOrders: [
    {
      workOrderNo: 'WO-20260530-0001',
      title: '医废暂存间负压异常处置',
      serviceType: '环境应急',
      priority: 'Critical',
      status: 'Escalated',
      location: locations.waste,
      responsibleTeam: '环境监管班组',
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
      status: 'InProgress',
      location: locations.ward,
      responsibleTeam: '被服配送组',
      createdAt: '2026-05-30T09:12:00+08:00',
      slaDueAt: '2026-05-30T12:30:00+08:00',
    },
  ],
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

const localBlueprint: LogisticsBlueprint = {
  pptReportedPrimaryModuleTotal: 29,
  pptReportedSecondaryItemTotal: 156,
  navigationGroups: [
    { code: 'WORKBENCH', name: '运营工作台', items: ['后勤首页', '待办中心', '风险告警'] },
    { code: 'SERVICE', name: '一站式服务', items: ['服务受理', '工单调度', '任务执行', '验收回访', '服务评价'] },
    { code: 'FACILITY', name: '设备设施', items: ['设备台账', '巡检保养', '维修记录', '备件库存', '电梯专项'] },
    { code: 'SPATIAL', name: 'BIM 空间', items: ['空间台账', '楼层视图', '设备点位', '告警点位', '工单点位'] },
    { code: 'ENVIRONMENT', name: '环境监管', items: ['环境点位', '预警池', '报警策略', '医废处置', '智慧卫生间'] },
    { code: 'MANAGEMENT', name: '综合管理', items: ['质量标准', '合同管理', '考核管理', '人员班组', '运营分析', '能耗成本', '碳排双控'] },
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
      modules: ['室内外环境管理', '智慧卫生间管理', '医疗废物收集处置管理'],
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
  teamLoads: [
    { teamName: '综合维修班', domain: '一站式服务', activeTasks: 18, capacity: 24, recommendation: '可承接一般维修与巡检工单' },
    { teamName: '电梯维保组', domain: '设备设施', activeTasks: 6, capacity: 8, recommendation: '保留困人事件应急余量' },
    { teamName: '环境监管班组', domain: '环境监管', activeTasks: 11, capacity: 12, recommendation: '医废负压告警优先派工' },
  ],
}

const statusLabels: Record<WorkOrderStatus | FacilityStatus | SignalStatus, string> = {
  New: '新建',
  Dispatched: '已派工',
  InProgress: '处理中',
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

function App() {
  const [dashboard, setDashboard] = useState(localDashboard)
  const [blueprint, setBlueprint] = useState(localBlueprint)
  const [source, setSource] = useState<'api' | 'local'>('local')

  useEffect(() => {
    const controller = new AbortController()
    const apiBase = import.meta.env.VITE_API_BASE_URL ?? 'http://localhost:5248'

    Promise.all([
      fetch(`${apiBase}/api/operations/dashboard`, { signal: controller.signal }),
      fetch(`${apiBase}/api/logistics/blueprint`, { signal: controller.signal }),
    ])
      .then(async ([dashboardResponse, blueprintResponse]) => {
        if (!dashboardResponse.ok || !blueprintResponse.ok) {
          throw new Error('Logistics API unavailable')
        }

        setDashboard((await dashboardResponse.json()) as OperationsDashboard)
        setBlueprint((await blueprintResponse.json()) as LogisticsBlueprint)
        setSource('api')
      })
      .catch(() => {
        setSource('local')
      })

    return () => controller.abort()
  }, [])

  const sortedWorkOrders = useMemo(
    () =>
      [...dashboard.workOrders].sort((left, right) => {
        const weight: Record<Priority, number> = { Low: 1, Normal: 2, High: 3, Critical: 4 }
        return weight[right.priority] - weight[left.priority]
      }),
    [dashboard.workOrders],
  )

  const detailedSecondaryTotal = blueprint.featureGroups.reduce(
    (total, group) => total + group.secondaryItemCount,
    0,
  )

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
            <strong>{dashboard.openWorkOrders}</strong>
            <em>待跟踪</em>
          </article>
        </section>

        <section className="content-grid">
          <section className="panel dispatch-panel">
            <PanelHeader title="工单调度中心" meta="按 SLA / 优先级 / 班组负载派工" />
            <div className="filter-bar">
              <button type="button">全部</button>
              <button type="button">待派工</button>
              <button type="button">超时风险</button>
              <button type="button">今日到期</button>
            </div>
            <div className="dispatch-table">
              <div className="dispatch-row table-head">
                <span>工单</span>
                <span>类型</span>
                <span>位置</span>
                <span>班组</span>
                <span>状态</span>
                <span>操作</span>
              </div>
              {sortedWorkOrders.map((order) => (
                <div className="dispatch-row" key={order.workOrderNo}>
                  <div>
                    <strong>{order.title}</strong>
                    <small>{order.workOrderNo} / {priorityLabels[order.priority]}</small>
                  </div>
                  <span>{order.serviceType}</span>
                  <span>{order.location.building} · {order.location.room}</span>
                  <span>{order.responsibleTeam}</span>
                  <span className={`badge ${order.status}`}>{statusLabels[order.status]}</span>
                  <button type="button">派工</button>
                </div>
              ))}
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
              {blueprint.teamLoads.map((team) => (
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

function PanelHeader({ title, meta }: { title: string; meta: string }) {
  return (
    <div className="panel-header">
      <h2>{title}</h2>
      <span>{meta}</span>
    </div>
  )
}

export default App
