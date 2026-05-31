# 给排水/污水站专项 V1 纵切

## 定位

给排水/污水站专项属于医院基础运行保障和合规监管的关键域。V1 先把 `WATER-PUMP-B1-01` 与 `SEWAGE-STATION-01` 两类客户点位打通为真实业务对象：BIM 给水泵房/污水处理站位置、运行单元、设备资产、巡检任务、压力与水质阈值告警、一站式工单调度必须互相追溯。

## 资料来源

- 北建院：给排水系统、污水站监测、给水压力、流量、液位、水质、COD、PH、安装位置、角色和使用端。
- 中科医信：给排水、污水站、环境监管、巡检保养、报警接收处置和工单闭环颗粒度。
- PPT：BIM 智慧运维平台集成边界、基础运行保障、物联监测预警和后勤服务闭环。

## 领域模型

- `WaterOperationsUnit`：水务运行单元，绑定 BIM 位置、责任班组、物联点位、设备资产、监测指标和来源证据。
- `WaterOperationsBoard`：专项看板，聚合给水/污水运行单元、点位、资产、活动告警、未闭环工单和待办巡检任务。
- `WaterOperationsUnitDetail`：单元详情，返回点位详情、资产详情、告警、工单、巡检任务和来源证据。
- `WaterOperationsBoardKpi`：单元数、异常单元、活动告警、未闭环工单、待办巡检任务。

## 持久化

V1 使用 SQLite 最小真实持久化，新增 `water_operations_units` 表。表中只保存专项主数据和关联键：

- `unit_code`
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

活动告警、工单、读数和巡检任务不重复保存到水务专项表，分别来自 IoT、告警、工单、资产巡检模块，通过点位编码、资产编码、BIM 构件和责任班组聚合。

## API

- `GET /api/operations/water-operations-board`
- `GET /api/operations/water-operations-units/{unitCode}`

## 前端闭环

给排水/污水站专项页必须能看到 `WATER-SYS-B1-PUMP`、`WATER-PUMP-B1-01`、`SEWAGE-SYS-B1-TREATMENT`、`SEWAGE-STATION-01`、`BIM-ENE-B1-PUMP`、`BIM-LOG-B1-SEWAGE` 和来源证据。页面支持模拟水务异常、告警转 `WO-ALM-*` 工单、进入调度池，生成工单必须保留 BIM 位置和来源证据，并能继续派工、接单和闭环。

## 后续扩展

- 扩展热水、中水、消防水、排水泵、集水坑和楼层管井。
- 将给水与污水读数纳入综合能耗、用水绩效、合规报表和异常成本分析，但不在本表重复保存时序数据。
- 生产化时把 SQLite 最小持久化迁移到 PostgreSQL 主数据表，并将高频时序读数进入独立时序库。
