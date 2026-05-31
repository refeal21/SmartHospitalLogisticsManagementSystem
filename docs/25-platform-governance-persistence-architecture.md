# 平台级治理与运营闭环 V1 持久化架构

## 目标

平台级治理不是新增一个静态综合管理菜单，而是把一站式服务、资产巡检、能耗、消防安防、告警和工单结果上卷为可审计的治理项。V1 只做最小真实纵切：看板、治理项详情、整改动作登记、审计轨迹、与调度池联动。

## 资料来源

- PPT：综合管理、质量闭环、成本/能耗、安全应急与 BIM 时空底座联动。
- 北建院：强电、暖通、给排水、消防安防等客户系统数据需要上卷为管理指标。
- 中科医信：质量体系、合同管理、考核管理、服务品质、运营分析等成熟功能颗粒度。

## 领域模型

- `PlatformGovernanceControl`：治理项，保留指标、目标值、当前值、责任角色、相关模块、状态和来源证据。
- `PlatformGovernanceAction`：整改动作，记录责任角色、登记人、状态、到期时间和可选工单号。
- `PlatformGovernanceAuditEntry`：审计轨迹，记录动作、人员、时间和摘要。
- `PlatformGovernanceBoard`：聚合治理项、动作、相关工单、相关告警、绩效指标和 KPI。
- `PlatformGovernanceControlDetail`：单个治理项的动作、告警、工单和审计上下文。

## 持久化边界

V1 新增 SQLite 表：

- `platform_governance_controls`
- `platform_governance_actions`
- `platform_governance_audit_trail`

只持久化治理主数据、动作和审计记录。工单、告警、能耗、安全应急等仍由各自专项服务维护，治理服务只做聚合引用，避免重复保存跨域事实。

默认环境变量：

- `SMART_HOSPITAL_LOGISTICS_GOVERNANCE_DB`

未设置时落到 `data/smart-hospital-governance.db`。

## API

- `GET /api/operations/platform-governance-board`
- `GET /api/operations/platform-governance-controls/{controlCode}`
- `POST /api/operations/platform-governance-controls/{controlCode}/actions`

动作登记成功后返回更新后的治理项详情，前端可立即看到整改动作和审计记录。

## 前端闭环

新增 hash 页面 `#platform-governance`：

- 显示治理项 KPI、治理项列表、指标目标值、来源证据。
- 展示治理项详情、绩效指标、审计轨迹、整改动作、相关工单和相关告警。
- 支持登记整改动作。
- 支持进入工单调度池，继续查看和处理相关工单。

## 后续扩展

- 接入真实合同、人员班组、外包单位和考核周期。
- 将整改动作升级为可派发任务或计划工单。
- 引入权限、审批、复盘模板和治理看板导出。
- 与生产化 PostgreSQL、审计日志和可观测指标统一。
