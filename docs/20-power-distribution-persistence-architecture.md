# 供配电/强电专项 V1 纵切

## 定位

供配电/强电专项属于基础运行保障，不是展示卡片。V1 先把 `PWR-LV-B1-IN-01` 这个低压进线电表点位打通为真实业务对象：BIM 变配电室位置、低压进线回路、配电柜资产、巡检任务、电压阈值告警和一站式工单调度必须互相追溯。

## 资料来源

- 北建院：强电系统、变配电室/配电柜点位、电压、电流、有功功率、无功功率、视在功率、功率因数、频率、电度、谐波、温湿度、浪涌保护器状态、照明数量、光照度，以及电工班/总务处/医院管理角色。
- 中科医信：供配电监测管理系统，包含运行总览、电力监测、数据报表；同时要求设备台账、巡检保养、告警处置和工单闭环颗粒度。
- PPT：BIM 智慧运维平台集成边界、基础运行保障、物联监测预警和后勤服务闭环。

## 领域模型

- `PowerDistributionCircuit`：供配电回路，绑定 BIM 位置、责任班组、电表点位、配电柜资产、监测指标和来源证据。
- `PowerDistributionBoard`：专项看板，聚合回路、强电点位、配电资产、活动告警、未闭环工单和待办巡检任务。
- `PowerDistributionCircuitDetail`：回路详情，返回点位详情、资产详情、告警、工单、巡检任务和来源证据。
- `PowerDistributionBoardKpi`：回路数、异常回路、活动告警、未闭环工单、待办巡检任务。

## 持久化

V1 使用 SQLite 最小真实持久化，新增 `power_distribution_circuits` 表。表中只保存专项主数据和关联键：

- `circuit_code`
- `name`
- `system`
- `campus/building/floor/room/bim_element_id`
- `responsible_team`
- `meter_point_code`
- `asset_code`
- `status`
- `monitored_metrics`
- `risk_summary`
- `source_evidence`

活动告警、工单、读数和巡检任务不重复保存到供配电专项表，分别来自 IoT、告警、工单、资产巡检模块，通过点位编码、资产编码、BIM 构件和责任班组聚合。

## API

- `GET /api/operations/power-distribution-board`
- `GET /api/operations/power-distribution-circuits/{circuitCode}`

## 前端闭环

供配电专项页必须能看到 `PWR-CIRCUIT-B1-LV-IN`、`PWR-LV-B1-IN-01`、`PWR-LV-B1-IN-CAB`、`BIM-ENE-B1-PDU` 和来源证据。页面支持模拟电压异常、告警转 `WO-ALM-*` 工单、进入调度池，生成工单必须保留 BIM 位置和来源证据，并能继续派工、接单和闭环。

## 后续扩展

- 扩展馈线回路、楼层配电柜、照明回路和防雷接地点。
- 将强电读数纳入综合能耗与电能质量分析，但不在本表重复保存时序数据。
- 生产化时把 SQLite 最小持久化迁移到 PostgreSQL 主数据表，并将时序读数进入独立时序库。
