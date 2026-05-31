# 暖通/冷热站专项 V1 纵切

## 定位

暖通/冷热站专项属于医院基础运行保障的能源与舒适性核心域。V1 先把 `HVAC-CHW-B1-02` 这个冷站冷冻泵点位打通为真实业务对象：BIM 冷站机房位置、冷冻水循环回路、冷冻泵资产、巡检任务、压力阈值告警和一站式工单调度必须互相追溯。

## 资料来源

- 北建院：供暖空调系统、冷热源、空调水、新风机组、压力、流量、供回水温度、能耗、启停状态、故障状态、暖通班/第三方服务方/总务处角色。
- 中科医信：冷热站运行监测、暖通专项、巡检保养、报警接收处置和工单闭环颗粒度。
- PPT：BIM 智慧运维平台集成边界、基础运行保障、物联监测预警和后勤服务闭环。

## 领域模型

- `HvacLoop`：暖通冷冻水回路，绑定 BIM 位置、责任班组、BAS 点位、冷冻泵资产、监测指标和来源证据。
- `HvacBoard`：专项看板，聚合回路、暖通点位、冷站资产、活动告警、未闭环工单和待办巡检任务。
- `HvacLoopDetail`：回路详情，返回点位详情、资产详情、告警、工单、巡检任务和来源证据。
- `HvacBoardKpi`：回路数、异常回路、活动告警、未闭环工单、待办巡检任务。

## 持久化

V1 使用 SQLite 最小真实持久化，新增 `hvac_cooling_loops` 表。表中只保存专项主数据和关联键：

- `loop_code`
- `name`
- `system`
- `campus/building/floor/room/bim_element_id`
- `responsible_team`
- `monitoring_point_code`
- `asset_code`
- `status`
- `monitored_metrics`
- `risk_summary`
- `source_evidence`

活动告警、工单、读数和巡检任务不重复保存到暖通专项表，分别来自 IoT、告警、工单、资产巡检模块，通过点位编码、资产编码、BIM 构件和责任班组聚合。

## API

- `GET /api/operations/hvac-board`
- `GET /api/operations/hvac-cooling-loops/{loopCode}`

## 前端闭环

暖通专项页必须能看到 `HVAC-LOOP-B1-CHW`、`HVAC-CHW-B1-02`、`CHW-B1-02`、`BIM-ENE-B1-CHILLER` 和来源证据。页面支持模拟压力异常、告警转 `WO-ALM-*` 工单、进入调度池，生成工单必须保留 BIM 位置和来源证据，并能继续派工、接单和闭环。

## 后续扩展

- 扩展冷冻水、冷却水、热水、空调箱、新风机组和末端风阀回路。
- 将暖通读数纳入综合能耗、峰谷策略、夜间节能和运行绩效分析，但不在本表重复保存时序数据。
- 生产化时把 SQLite 最小持久化迁移到 PostgreSQL 主数据表，并将高频时序读数进入独立时序库。
