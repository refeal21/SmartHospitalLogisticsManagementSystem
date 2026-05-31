# One-Stop Service Productization Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Turn the source-traceable hospital logistics business map into a usable one-stop service and work-order product workflow instead of a module/card display.

**Architecture:** Start from a written module workflow and page contracts, then use tests to drive frontend restructuring. Keep backend API behavior stable unless the page contract exposes a missing workflow field.

**Tech Stack:** Markdown project docs, React + TypeScript + Vite frontend, Playwright E2E tests, .NET application/API tests.

---

## File Structure

- Create: `docs/13-one-stop-service-workflow-and-page-contracts.md`
  - Owns the detailed workflow, page contracts, role responsibilities, state transitions and acceptance flows for service intake, dispatch, execution, acceptance and evaluation.
- Modify: `docs/08-source-traceable-v1-backlog.md`
  - Adds the one-stop service page-contract milestone and frontend acceptance criteria.
- Modify: `web/tests/operations-dashboard.spec.ts`
  - Adds failing E2E coverage for productized navigation and work-order workflow pages.
- Modify: `web/src/App.tsx`
  - Splits the current monolithic presentation into explicit page sections for the one-stop service workflow.
- Modify: `web/src/App.css`
  - Reworks layout toward dense hospital operations pages: stable sidebar, page header, workflow tabs, list/detail/action/timeline panels and responsive behavior.

### Task 1: Write One-Stop Service Page Contracts

**Files:**
- Create: `docs/13-one-stop-service-workflow-and-page-contracts.md`
- Modify: `docs/08-source-traceable-v1-backlog.md`

- [ ] **Step 1: Create the workflow contract document**

Add `docs/13-one-stop-service-workflow-and-page-contracts.md` with these sections:

```markdown
# 一站式服务与工单调度页面契约

## 目标

把一站式服务从“工单卡片展示”推进为可办事的医院后勤核心流程。

## 来源

- 中科医信：调度中心、维修管理、移动维修、移动报修、工程仓库、报表。
- PPT：一站式服务中心、工单管理、服务评价、空间信息联动、AI/知识库。
- 北建院：强电、暖通、给排水、医气、环境、医废等工单来源和专业班组映射。

## 用户与职责

| 角色 | 主要职责 |
| --- | --- |
| 报修人/科室 | 提交服务请求、补充现场信息、确认结果和评价 |
| 一站式服务受理员 | 登记请求、分类定级、补充空间和设备 |
| 后勤调度员 | 查看工单池、按 SLA 和班组负载派工、跟踪超时风险 |
| 班组人员 | 接单、到场、挂单、转单、完工和提交处置证据 |
| 管理者 | 验收、驳回、查看质量和服务表现 |

## 页面契约

| 页面 | 使用者 | 主对象 | 核心操作 | 验收流程 |
| --- | --- | --- | --- | --- |
| 服务受理 | 受理员 | 服务请求 | 登记、分类、定位、生成工单 | 电话/报修信息生成待派工单 |
| 工单调度 | 调度员 | 工单 | 筛选、查看详情、派工、升级 | 选择高风险工单并派给建议班组 |
| 任务执行 | 班组人员 | 执行任务 | 接单、挂单、转单、完工 | 已派工单进入处理中并提交完工 |
| 验收回访 | 管理者/报修人 | 待验收工单 | 验收、驳回、回访 | 完工单通过验收进入评价 |
| 服务评价 | 报修人/管理者 | 已关闭工单 | 评分、投诉、沉淀质量记录 | 评价结果进入服务品质指标 |

## 状态流转

服务请求 -> 新建工单 -> 已派工 -> 处理中 -> 已挂单/已转单 -> 待验收 -> 已关闭。

## 前端结构

一站式服务页面组使用统一骨架：流程导航、筛选工具条、主列表、详情面板、操作面板、流转时间线、结果反馈。

## 验收

- 用户不滚动长页面也能知道当前页面职责。
- 工单调度页可以完成选择工单、查看详情、派工、接单、完工的核心动作。
- 页面内容能追溯到 PPT、北建院或中科医信来源。
```

- [ ] **Step 2: Update backlog milestone**

In `docs/08-source-traceable-v1-backlog.md`, add a row under `已落地纵切`:

```markdown
| 一站式服务页面契约 | 工单全流程管理、消息推送与待办、服务品质管理、可视化空间运维 | 服务受理、工单调度、任务执行、验收回访、服务评价五个页面拥有明确使用者、主对象、核心操作和端到端验收流程 |
```

- [ ] **Step 3: Review the contract**

Run:

```powershell
Select-String -Path 'docs/13-one-stop-service-workflow-and-page-contracts.md','docs/08-source-traceable-v1-backlog.md' -Pattern '占位','待补','未明确'
```

Expected: no matches.

### Task 2: Write Failing Frontend Workflow Tests

**Files:**
- Modify: `web/tests/operations-dashboard.spec.ts`

- [ ] **Step 1: Add E2E tests for page productization**

Add tests that assert:

```typescript
test('一站式服务菜单呈现五个可办事页面', async ({ page }) => {
  await page.goto('/#工单调度');
  await expect(page.getByRole('heading', { name: '工单调度' })).toBeVisible();
  await expect(page.getByRole('tab', { name: '服务受理' })).toBeVisible();
  await expect(page.getByRole('tab', { name: '工单调度' })).toBeVisible();
  await expect(page.getByRole('tab', { name: '任务执行' })).toBeVisible();
  await expect(page.getByRole('tab', { name: '验收回访' })).toBeVisible();
  await expect(page.getByRole('tab', { name: '服务评价' })).toBeVisible();
});

test('工单调度页按照业务骨架呈现列表详情操作和时间线', async ({ page }) => {
  await page.goto('/#工单调度');
  await expect(page.getByTestId('work-order-list')).toBeVisible();
  await expect(page.getByTestId('work-order-detail')).toBeVisible();
  await expect(page.getByTestId('work-order-actions')).toBeVisible();
  await expect(page.getByTestId('work-order-timeline')).toBeVisible();
});
```

- [ ] **Step 2: Run tests to verify red**

Run:

```powershell
cd web
npm run test:e2e -- --grep "一站式服务菜单|业务骨架"
```

Expected: FAIL because workflow tabs and test ids are not implemented yet.

### Task 3: Productize the One-Stop Service Frontend Skeleton

**Files:**
- Modify: `web/src/App.tsx`
- Modify: `web/src/App.css`

- [ ] **Step 1: Implement workflow tabs**

Add explicit one-stop service workflow tabs:

```typescript
const serviceWorkflowTabs = ['服务受理', '工单调度', '任务执行', '验收回访', '服务评价'] as const;
```

Render them as accessible `role="tab"` controls inside the dispatch page header.

- [ ] **Step 2: Add stable work-order panel test ids**

Set these attributes on the dispatch page panels:

```tsx
data-testid="work-order-list"
data-testid="work-order-detail"
data-testid="work-order-actions"
data-testid="work-order-timeline"
```

- [ ] **Step 3: Rework layout copy**

Replace generic “当前业务页” copy with operational page language:

```text
工单调度
按 SLA、风险等级、专业班组负载和 BIM 空间位置处理今日后勤工单。
```

- [ ] **Step 4: Run the targeted E2E tests**

Run:

```powershell
cd web
npm run test:e2e -- --grep "一站式服务菜单|业务骨架"
```

Expected: PASS.

### Task 4: Full Verification and Commit

**Files:**
- All modified files from prior tasks.

- [ ] **Step 1: Run frontend checks**

Run:

```powershell
cd web
npm run lint
npm run build
npm run test:e2e
```

Expected: all pass.

- [ ] **Step 2: Run backend checks**

Run:

```powershell
dotnet build SmartHospitalLogistics.sln
dotnet test SmartHospitalLogistics.sln
```

Expected: all pass.

- [ ] **Step 3: Check ignored materials**

Run:

```powershell
git status --short --ignored
```

Expected: source docs and code files may be modified; raw PPT, Excel, XMind, screenshots, credentials, `node_modules/`, `bin/` and `obj/` are not staged.

- [ ] **Step 4: Commit and push**

Run:

```powershell
git add docs/12-global-business-process-map.md docs/superpowers/specs/2026-05-31-logistics-business-process-map-design.md docs/superpowers/plans/2026-05-31-one-stop-service-productization.md docs/13-one-stop-service-workflow-and-page-contracts.md docs/08-source-traceable-v1-backlog.md web/tests/operations-dashboard.spec.ts web/src/App.tsx web/src/App.css
git commit -m "feat: productize one-stop service workflow" -m "AI-Assisted-By: Codex (GPT-5.5)"
git push
```

Expected: push succeeds on the current `codex/source-traceable-logistics-blueprint` branch.

## Self-Review Notes

- This plan deliberately starts with page contracts before frontend implementation.
- This plan keeps backend stable unless frontend workflow exposes a missing API contract.
- This plan does not commit original PPT, Excel, XMind, screenshots, credentials or internal raw notes.
