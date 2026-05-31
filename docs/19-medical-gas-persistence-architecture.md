# 医用气体专项 V1 纵切

## 定位

医用气体专项不是独立演示页，而是基础运行保障中的高风险业务域。V1 先把住院 8F 医用气体分区打通到底层模型：BIM 空间、氧气压力点位、分区阀箱资产、巡检任务、物联告警和一站式工单调度必须能互相追溯。

## 资料来源

- 北建院：医用气体系统、传感器、安装位置、采集字段、角色和使用端。
- 中科医信：医用气体预警监测、报警接收与处置、巡检保养、工单闭环颗粒度。
- PPT：BIM 智慧运维平台集成边界、基础运行保障和后勤服务闭环。

## 领域模型

- `MedicalGasZone`：医气分区，绑定 BIM 位置、介质类型、责任班组、压力点位和阀箱资产。
- `MedicalGasBoard`：专项看板，聚合分区、点位、阀箱资产、活动告警、未闭环工单和待办巡检任务。
- `MedicalGasZoneDetail`：分区详情，返回点位详情、资产详情、告警、工单、巡检任务和来源证据。
- `MedicalGasBoardKpi`：分区数、异常分区、活动告警、未闭环工单、待办巡检任务。

## 持久化

V1 使用 SQLite 最小真实持久化，新增 `medical_gas_zones` 表。表中只保存专项主数据和关联键：

- `zone_code`
- `name`
- `department`
- `campus/building/floor/room/bim_element_id`
- `supply_types`
- `responsible_team`
- `pressure_point_code`
- `valve_asset_code`
- `status`
- `risk_summary`
- `source_evidence`

活动告警、工单、巡检任务不重复保存到医气表，分别来自 IoT、告警、工单、资产巡检模块，通过点位编码、资产编码、BIM 构件和责任班组聚合。

## API

- `GET /api/operations/medical-gas-board`
- `GET /api/operations/medical-gas-zones/{zoneCode}`

## 前端闭环

医气专项页必须能看到 `MG-ZONE-IPD-8F`、`MEDGAS-O2-8F`、`MEDGAS-IPD-8F`、`MT-20260530-0002` 和来源证据。页面支持模拟压力异常、告警转工单、进入调度池，生成的 `WO-ALM-*` 必须能在工单调度池继续派工、接单和闭环。
