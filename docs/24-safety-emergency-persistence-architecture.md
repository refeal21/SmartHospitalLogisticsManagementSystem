# 消防/安防/应急联动 V1 纵切

## 定位

消防/安防/应急联动不是重做消防主机、门禁平台或视频平台，而是在医院后勤 BIM 智慧运维平台内承接事件、空间、资产、告警和工单闭环。V1 目标是让火灾自动报警、门禁安防等外部系统事件可以进入同一个后勤调度语境：

- 事件绑定 BIM 位置和责任角色。
- 物联告警可转一站式工单。
- 应急步骤有可执行的确认、通知、派工和复盘链路。
- 后续可扩展到消防水、防火门、可燃气体、电气火灾、视频安防和应急预案。

## 资料来源

- 北建院：火灾自动报警及联动控制系统、可燃气体探测报警、防火门监控、电气火灾监控、公共安全、门禁、视频安防、智能化系统。
- PPT：BIM 智慧运维平台的统一空间底座、事件联动、后勤服务闭环和安全边界。
- 中科医信：统一报警、设备安全、任务处置、移动端消息和工单管理颗粒度。

## 领域模型

- `SafetyEmergencyNode`：消防/安防联动节点，绑定事件类型、BIM 位置、责任班组、监测点位、资产、联动系统和来源证据。
- `SafetyEmergencyBoard`：专项看板，聚合联动节点、监测点、资产、活动告警、未闭环工单、应急步骤和 KPI。
- `SafetyEmergencyNodeDetail`：节点详情，返回点位详情、资产详情、活动告警、工单、应急步骤和来源证据。
- `EmergencyResponseStep`：应急响应步骤，表达确认、通知、派工、复盘等可执行动作。

## 持久化边界

V1 使用 SQLite 最小真实持久化，新增 `safety_emergency_nodes` 表。该表只保存消防/安防联动节点主数据和关联键：

- `node_code`
- `name`
- `event_type`
- `campus/building/floor/room/bim_element_id`
- `responsible_team`
- `monitoring_point_code`
- `asset_code`
- `status`
- `response_level`
- `linked_systems`
- `source_evidence`

高频报警、遥测读数、工单流转和资产明细仍由 IoT、告警、工单、资产巡检模块管理，不重复写入本表。

默认数据库路径为 `data/smart-hospital-safety.db`，可通过 `SMART_HOSPITAL_LOGISTICS_SAFETY_DB` 覆盖。

## API

- `GET /api/operations/safety-emergency-board`
- `GET /api/operations/safety-emergency-nodes/{nodeCode}`

告警生成和转工单沿用既有入口：

- `POST /api/operations/iot-readings`
- `POST /api/operations/monitoring-alarms/{alarmNo}/convert-to-work-order`

## 前端闭环

消防/安防应急页支持 `#safety-emergency`。页面必须能看到：

- `SAFE-FIRE-OPD-1F`
- `SAFE-SEC-ER-ACCESS`
- `FIRE-SMOKE-OPD-1F-01`
- `SEC-ACCESS-ER-01`
- `BIM-SEC-OPD-1F-FIRE`
- 来源证据、应急步骤、告警和工单入口

页面支持模拟火警信号，生成 `ALM-FIRE-*` 告警；告警可转 `WO-ALM-*` 工单，并进入工单调度池继续派工、接单、完工、验收。

## 后续扩展

- 接入消防水系统、防火门、可燃气体、电气火灾和视频安防事件。
- 增加应急预案版本、演练记录、通知确认、现场反馈和复盘报告。
- 与平台级治理结合，形成事件分级、责任追踪、整改闭环、考核和审计。
