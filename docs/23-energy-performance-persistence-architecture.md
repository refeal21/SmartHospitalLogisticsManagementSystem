# 能耗与运行绩效 V1 纵切

## 定位

能耗与运行绩效不是单独的展示大屏，而是医院后勤平台的运营管理层。V1 把供配电、冷热站、给排水/污水站、物联告警、工单调度和巡检保养聚合到同一个可追溯工作台，用于回答三个运营问题：

- 哪些区域正在异常耗能或产生异常成本。
- 异常是否已经进入告警、工单和责任班组闭环。
- 哪些节能建议可以转化为巡检、调参、维修或管理动作。

## 资料来源

- 北建院：强电、暖通空调、给排水和污水站的客户系统、点位、采集字段、安装位置和使用角色。
- 中科医信：综合能耗、运行绩效、告警处置、工单闭环和服务品质的功能颗粒度。
- PPT：BIM 智慧运维平台的综合监测、能耗运营、后勤服务闭环和空间定位要求。

功能细节优先使用上述资料。AI 只用于结构化、补齐工程边界和生成可验证实现，不替代业务来源。

## 领域模型

- `EnergyPerformanceArea`：能耗绩效区域，绑定 BIM 位置、主计量点、责任班组、关联专项系统、基线能耗、当前能耗、成本单价和来源证据。
- `EnergyPerformanceBoard`：能耗工作台，聚合区域、点位、活动告警、未闭环工单、节能建议、运行绩效指标和 KPI。
- `EnergyPerformanceAreaDetail`：区域详情，返回关联点位、告警、工单、趋势、建议、绩效指标和来源证据。
- `EnergySavingRecommendation`：节能建议，记录异常原因、预估节能率、责任班组、关联区域和可执行动作。
- `OperationPerformanceMetric`：运行绩效指标，用于关联 SLA、告警响应、设备健康和能耗效率。

## 持久化边界

V1 使用 SQLite 最小真实持久化，新增 `energy_performance_areas` 表。该表只保存运营区域主数据和关联键：

- `area_code`
- `name`
- `system`
- `campus/building/floor/room/bim_element_id`
- `primary_meter_point_code`
- `responsible_team`
- `related_system_code`
- `status`
- `baseline_consumption`
- `current_consumption`
- `cost_rate`
- `source_evidence`

时序读数、活动告警、工单、资产和巡检任务不重复写入能耗表，分别继续由 IoT、告警、工单和资产巡检模块管理。能耗层通过点位编码、BIM 构件、责任班组和专项系统编码进行聚合。

默认数据库路径为 `data/smart-hospital-energy.db`，可通过 `SMART_HOSPITAL_LOGISTICS_ENERGY_DB` 覆盖。

## API

- `GET /api/operations/energy-performance-board`
- `GET /api/operations/energy-performance-areas/{areaCode}`

API 必须返回来源证据，并至少能追溯到北建院、中科医信或 PPT，不能只返回 AI 生成描述。

## 前端闭环

能耗与运行绩效页支持 `#energy-performance`。页面必须能看到：

- `ENE-POWER-B1`、`ENE-HVAC-B1`、`ENE-WATER-B1`
- `PWR-LV-B1-IN-01`、`HVAC-CHW-B1-02`、`WATER-PUMP-B1-01`
- `BIM-ENE-B1-PDU` 等 BIM 位置
- 来源证据、异常成本、节能建议和运行绩效指标

页面支持模拟异常用能。异常读数进入 IoT/告警服务后，能耗工作台刷新异常状态；用户可以继续进入供配电专项，由专项页完成告警转 `WO-ALM-*` 工单并进入调度池。

## 后续扩展

- 增加分时电价、科室分摊、楼栋/楼层/系统级同比环比和预算偏差。
- 将能耗建议转化为计划任务、巡检策略或自动化控制建议，并进入审批流。
- 生产化阶段把 SQLite 主数据迁移到 PostgreSQL；高频时序读数进入独立时序库或遥测平台。
- 与平台级运营闭环结合，形成合同服务质量、SLA、能耗成本和节能收益的一体化绩效看板。
