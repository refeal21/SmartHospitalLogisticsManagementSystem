import { expect, test } from '@playwright/test'

test.describe('医院后勤 BIM 智慧运维功能型后台', () => {
  test('呈现可办事的后勤管理菜单和工作台', async ({ page }) => {
    await page.goto('/')

    await expect(page.getByRole('heading', { name: '医院后勤管理工作台' })).toBeVisible()
    await expect(page.getByRole('button', { name: '服务受理' })).toBeVisible()
    await expect(page.getByRole('button', { name: '工单调度' })).toBeVisible()
    await expect(page.getByRole('button', { name: '设备台账' })).toBeVisible()
    await expect(page.getByRole('button', { name: '预警池' })).toBeVisible()
    await expect(page.getByRole('button', { name: '合同管理' })).toBeVisible()

    await expect(page.getByRole('heading', { name: '工单调度中心' })).toBeVisible()
    await expect(page.getByText('WO-20260530-0001')).toBeVisible()
    await expect(page.getByText('医废暂存间负压异常处置').first()).toBeVisible()
    await expect(page.getByRole('heading', { name: 'BIM 空间业务定位' })).toBeVisible()
    await expect(page.getByRole('heading', { name: '环境预警池' })).toBeVisible()
    await expect(page.getByRole('heading', { name: '班组负载' })).toBeVisible()
    await expect(page.getByText('29 个一级模块 / PPT 总览 156 个子项')).toBeVisible()

    await expect(page.getByText(/API 实时数据|本地业务种子数据/)).toBeVisible()
  })

  test('展示PPT、北建院和中科医信驱动的来源追溯矩阵', async ({ page }) => {
    await page.goto('/')

    await expect(page.getByRole('heading', { name: '来源可追溯功能目录' })).toBeVisible()
    await expect(page.getByText('PPT', { exact: true }).first()).toBeVisible()
    await expect(page.getByText('北建院', { exact: true }).first()).toBeVisible()
    await expect(page.getByText('中科医信', { exact: true }).first()).toBeVisible()
    await expect(page.getByText('工单全流程管理', { exact: true }).first()).toBeVisible()
    await expect(page.getByText('供配电监测', { exact: true }).first()).toBeVisible()

    await expect(page.getByRole('heading', { name: '北建院客户数据目录' })).toBeVisible()
    await expect(page.getByText('强电系统', { exact: true }).first()).toBeVisible()
    await expect(page.getByText('供暖空调系统', { exact: true }).first()).toBeVisible()
    await expect(page.getByText('智慧卫生间').first()).toBeVisible()

    await expect(page.getByRole('heading', { name: '竞品功能颗粒度' })).toBeVisible()
    await expect(page.getByText('智慧医院运行保障系统基础服务模块')).toBeVisible()
    await expect(page.getByText('医疗废弃物综合管理系统')).toBeVisible()
    await expect(page.getByText('综合能耗智能监管系统')).toBeVisible()

    await expect(page.getByRole('heading', { name: 'V1实施顺序' })).toBeVisible()
    await expect(page.getByText('基础平台与工作台', { exact: true }).first()).toBeVisible()
    await expect(page.getByText('一站式服务与工单', { exact: true }).first()).toBeVisible()
    await expect(page.getByText('综合管理', { exact: true }).first()).toBeVisible()
  })

  test('一站式工单调度支持派工、接单和详情追踪', async ({ page }) => {
    await page.goto('/')

    await expect(page.getByRole('heading', { name: '工单池' })).toBeVisible()
    await expect(page.getByRole('heading', { name: '派工建议' })).toBeVisible()
    await expect(page.getByRole('heading', { name: '工单详情' })).toBeVisible()
    await expect(page.getByRole('heading', { name: '流转记录' })).toBeVisible()

    await page.getByRole('button', { name: /医废暂存间负压异常处置/ }).click()
    await expect(page.getByText('BIM-LOG-F1-WASTE').first()).toBeVisible()
    await expect(page.getByText('SLA 风险：High')).toBeVisible()
    await expect(page.getByText('工单全流程管理').first()).toBeVisible()

    await page.getByRole('button', { name: '派工到环境监管班组' }).click()
    await expect(page.getByText('状态：已派工', { exact: true })).toBeVisible()
    await expect(page.locator('.timeline-panel').getByText('派工', { exact: true }).first()).toBeVisible()

    await page.getByRole('button', { name: '接单处理' }).click()
    await expect(page.getByText('状态：处理中', { exact: true })).toBeVisible()
    await expect(page.locator('.timeline-panel').getByText('接单', { exact: true }).first()).toBeVisible()
  })

  test('一站式服务菜单呈现五个可办事页面', async ({ page }) => {
    await page.goto('/#工单调度')

    await expect(page.getByRole('heading', { name: '工单调度', exact: true })).toBeVisible()
    await expect(page.getByRole('tab', { name: '服务受理' })).toBeVisible()
    await expect(page.getByRole('tab', { name: '工单调度' })).toBeVisible()
    await expect(page.getByRole('tab', { name: '任务执行' })).toBeVisible()
    await expect(page.getByRole('tab', { name: '验收回访' })).toBeVisible()
    await expect(page.getByRole('tab', { name: '服务评价' })).toBeVisible()
  })

  test('工单调度页按照业务骨架呈现列表详情操作和时间线', async ({ page }) => {
    await page.goto('/#工单调度')

    await expect(page.getByTestId('work-order-list')).toBeVisible()
    await expect(page.getByTestId('work-order-detail')).toBeVisible()
    await expect(page.getByTestId('work-order-actions')).toBeVisible()
    await expect(page.getByTestId('work-order-timeline')).toBeVisible()
  })

  test('一站式服务页签切换到服务受理和任务执行页面', async ({ page }) => {
    await page.goto('/#服务受理')

    await expect(page.getByRole('heading', { name: '服务受理', exact: true })).toBeVisible()
    await expect(page.getByTestId('service-intake-panel')).toBeVisible()
    await expect(page.getByText('登记请求、补齐空间和设备，生成待派工单。')).toBeVisible()

    await page.getByRole('tab', { name: '任务执行' }).click()
    await expect(page.getByRole('heading', { name: '任务执行', exact: true })).toBeVisible()
    await expect(page.getByTestId('task-execution-panel')).toBeVisible()
    await expect(page.getByText('我的任务')).toBeVisible()
  })

  test('验收回访和服务评价页面承接闭环结果', async ({ page }) => {
    await page.goto('/#验收回访')

    await expect(page.getByRole('heading', { name: '验收回访', exact: true })).toBeVisible()
    await expect(page.getByTestId('acceptance-review-panel')).toBeVisible()
    await expect(page.getByText('待验收工单')).toBeVisible()

    await page.getByRole('tab', { name: '服务评价' }).click()
    await expect(page.getByRole('heading', { name: '服务评价', exact: true })).toBeVisible()
    await expect(page.getByTestId('service-evaluation-panel')).toBeVisible()
    await expect(page.getByText('评价与投诉')).toBeVisible()
  })

  test('服务受理生成待派工单并进入调度池', async ({ page }) => {
    await page.goto('/#服务受理')

    await page.getByRole('button', { name: '生成待派工单' }).click()

    await expect(page.getByRole('heading', { name: '工单调度', exact: true })).toBeVisible()
    await expect(page.getByText('WO-SR-20260531-0001')).toBeVisible()
    await expect(page.getByTestId('work-order-detail').getByText('门诊大厅空调异常服务请求')).toBeVisible()
    await expect(page.getByTestId('work-order-detail').getByText('状态：新建', { exact: true })).toBeVisible()
  })

  test('服务请求工单可验收评价并归档闭环', async ({ page }) => {
    await page.goto('/#服务受理')

    await page.getByRole('button', { name: '生成待派工单' }).click()
    await page.getByRole('button', { name: /派工到/ }).click()
    await page.getByRole('button', { name: '接单处理' }).click()
    await page.getByRole('button', { name: '完工' }).click()
    await expect(page.getByTestId('work-order-detail')).toContainText('状态：待验收')

    await page.getByRole('tab', { name: '验收回访' }).click()
    await page.getByRole('button', { name: '验收通过' }).click()
    await expect(page.getByTestId('acceptance-review-panel')).toContainText('状态：待评价')

    await page.getByRole('tab', { name: '服务评价' }).click()
    await expect(page.getByTestId('service-evaluation-panel')).toContainText(/WO-SR-/)
    await page.getByRole('button', { name: '五星评价并归档' }).click()
    await expect(page.getByTestId('service-evaluation-panel')).toContainText('状态：已关闭')
    await expect(page.getByTestId('service-evaluation-panel')).toContainText('5 星')

    await page.getByRole('tab', { name: '工单调度' }).click()
    await expect(page.getByTestId('work-order-detail')).toContainText('状态：已关闭')
    await expect(page.getByTestId('work-order-timeline')).toContainText('评价')
  })

  test('服务受理优先调用API生成待派工单', async ({ page }) => {
    const corsHeaders = {
      'Access-Control-Allow-Headers': 'content-type',
      'Access-Control-Allow-Methods': 'POST, OPTIONS',
      'Access-Control-Allow-Origin': '*',
      'Content-Type': 'application/json',
    }

    await page.route('**/api/operations/service-requests', async (route) => {
      if (route.request().method() === 'OPTIONS') {
        await route.fulfill({ status: 200, headers: corsHeaders })
        return
      }

      await route.fulfill({
        status: 200,
        headers: corsHeaders,
        body: JSON.stringify({
          requestNo: 'SR-API-0001',
          sourceType: 'Manual',
          requesterName: '门诊护士站',
          requesterDepartment: '门诊部',
          serviceType: '综合维修',
          priority: 'High',
          description: 'API受理的门诊大厅空调异常',
          location: {
            campus: '同仁亦庄院区',
            building: '门诊医技楼',
            floor: 'F1',
            room: '共享大厅',
            bimElementId: 'BIM-OPD-F1-HALL',
          },
          status: 'Accepted',
          createdAt: '2026-05-31T10:00:00+08:00',
          convertedWorkOrderNo: null,
        }),
      })
    })

    await page.route('**/api/operations/service-requests/SR-API-0001/convert', async (route) => {
      if (route.request().method() === 'OPTIONS') {
        await route.fulfill({ status: 200, headers: corsHeaders })
        return
      }

      await route.fulfill({
        status: 200,
        headers: corsHeaders,
        body: JSON.stringify({
          workOrder: {
            workOrderNo: 'WO-SR-API-0001',
            title: 'API受理的门诊大厅空调异常',
            serviceType: '综合维修',
            priority: 'High',
            status: 'New',
            location: {
              campus: '同仁亦庄院区',
              building: '门诊医技楼',
              floor: 'F1',
              room: '共享大厅',
              bimElementId: 'BIM-OPD-F1-HALL',
            },
            responsibleTeam: '未派工',
            createdAt: '2026-05-31T10:00:00+08:00',
            slaDueAt: '2026-05-31T12:00:00+08:00',
          },
          location: {
            campus: '同仁亦庄院区',
            building: '门诊医技楼',
            floor: 'F1',
            room: '共享大厅',
            bimElementId: 'BIM-OPD-F1-HALL',
          },
          sourceEvidence: [
            {
              featureName: '一站式服务受理',
              sources: ['中科医信', 'PPT', '北建院'],
              evidenceSummary: 'API受理服务请求后转工单。',
            },
          ],
          timeline: [
            {
              occurredAt: '2026-05-31T10:00:00+08:00',
              operator: '一站式受理员',
              action: '受理建单',
              fromStatus: 'New',
              toStatus: 'New',
              remark: 'API生成待派工单',
              rating: null,
            },
          ],
          slaRiskLevel: 'Medium',
          slaMinutesRemaining: 120,
          allowedActions: ['Dispatch'],
        }),
      })
    })

    await page.goto('/#服务受理')
    await page.getByRole('button', { name: '生成待派工单' }).click()

    await expect(page.getByText('WO-SR-API-0001')).toBeVisible()
    await expect(page.getByTestId('work-order-detail').getByText('API受理的门诊大厅空调异常')).toBeVisible()
  })

  test('资产台账与巡检保养支持异常转工单', async ({ page }) => {
    await page.goto('/')

    await expect(page.getByRole('heading', { name: '资产台账与巡检保养' })).toBeVisible()
    await expect(page.getByRole('heading', { name: '设备设施台账' })).toBeVisible()
    await expect(page.getByRole('heading', { name: '巡检任务' })).toBeVisible()

    await page.getByRole('button', { name: /MEDGAS-IPD-8F/ }).click()
    await expect(page.getByText('BIM-IPD-F8-WARD').first()).toBeVisible()
    await expect(page.getByText('住院 8F 医用气体分区阀箱周巡检').first()).toBeVisible()

    const convertButton = page.getByRole('button', { name: '异常完成并转工单' }).first()
    if (await convertButton.isEnabled()) {
      await convertButton.click()
    }

    await expect(page.getByText('已转工单').first()).toBeVisible()
    await expect(page.getByText(/WO-MT-20260530-0002|WO-MT-20260530-0001/).first()).toBeVisible()
  })

  test('资产巡检异常工单进入调度池并保留来源证据', async ({ page }) => {
    await page.goto('/#设备台账')

    await page.getByRole('button', { name: /MEDGAS-IPD-8F/ }).click()
    const convertButton = page.getByRole('button', { name: '异常完成并转工单' }).first()
    if (await convertButton.isEnabled()) {
      await convertButton.click()
    }

    const generatedWorkOrder = page.getByTestId('maintenance-generated-workorder')
    await expect(generatedWorkOrder).toContainText(/WO-MT-20260530-0002|WO-MT-20260530-0001/)
    await expect(generatedWorkOrder).toContainText('BIM-IPD-F8-WARD')
    await expect(generatedWorkOrder).toContainText('医气维保人员')
    await expect(generatedWorkOrder).toContainText('巡检保养异常转工单')

    await generatedWorkOrder.getByRole('button', { name: '进入调度池' }).click()
    await expect(page.getByRole('heading', { name: '工单调度', exact: true })).toBeVisible()
    await expect(page.getByTestId('work-order-list')).toContainText(/WO-MT-20260530-0002|WO-MT-20260530-0001/)
    await expect(page.getByTestId('work-order-detail')).toContainText('巡检保养异常转工单')

    await page.getByRole('button', { name: /派工到/ }).click()
    await expect(page.getByTestId('work-order-detail')).toContainText('状态：已派工')
  })

  test('客户物联点位接入支持字段查看和异常读数判定', async ({ page }) => {
    await page.goto('/')

    await expect(page.getByRole('heading', { name: '客户物联点位接入' })).toBeVisible()
    await expect(page.getByRole('heading', { name: '点位目录' })).toBeVisible()
    await expect(page.getByRole('heading', { name: '时序字段' })).toBeVisible()
    await expect(page.getByRole('heading', { name: '阈值与读数' })).toBeVisible()

    await page.getByTestId('iot-point-list').getByRole('button', { name: /MEDGAS-O2-8F/ }).click()
    await expect(page.getByText('BIM-IPD-F8-WARD').first()).toBeVisible()
    await expect(page.getByText('氧气压力 / 医气压力').first()).toBeVisible()

    await page.getByTestId('iot-point-detail').getByRole('button', { name: '模拟异常读数' }).click()
    await expect(page.getByText('最新风险：严重')).toBeVisible()
    await expect(page.getByTestId('iot-reading-panel').getByText('MEDGAS-O2-8F / pressure')).toBeVisible()
  })

  test('环境预警池支持告警确认转工单并联动调度池', async ({ page }) => {
    await page.goto('/#预警池')

    await expect(page.getByRole('heading', { name: '环境预警池', exact: true })).toBeVisible()
    await expect(page.getByTestId('monitoring-alarm-list')).toBeVisible()
    await expect(page.getByTestId('monitoring-alarm-detail')).toBeVisible()

    await page.getByTestId('monitoring-alarm-list').getByRole('button', { name: /氧气压力/ }).click()
    await expect(page.getByTestId('monitoring-alarm-detail')).toContainText('BIM-IPD-F8-WARD')
    await expect(page.getByTestId('monitoring-alarm-detail')).toContainText('客户物联告警联动')

    await page.getByRole('button', { name: '确认告警' }).click()
    await expect(page.getByTestId('monitoring-alarm-detail')).toContainText('已确认')

    await page.getByRole('button', { name: '转处置工单' }).click()
    await expect(page.getByTestId('monitoring-alarm-detail')).toContainText(/WO-ALM-/)

    await page.getByRole('button', { name: '进入调度池' }).click()
    await expect(page.getByRole('heading', { name: '工单调度', exact: true })).toBeVisible()
    await expect(page.getByTestId('work-order-list')).toContainText(/WO-ALM-/)
    await expect(page.getByTestId('work-order-detail')).toContainText('客户物联告警联动')
  })

  test('左侧菜单切换到聚焦业务页面而不是所有模块堆叠', async ({ page }) => {
    await page.goto('/')

    await page.getByRole('button', { name: '设备台账' }).click()
    await expect(page.getByRole('heading', { name: '资产台账与巡检保养' })).toBeVisible()
    await expect(page.getByRole('heading', { name: '工单调度中心' })).toBeHidden()

    await page.getByRole('button', { name: '环境点位' }).click()
    await expect(page.getByRole('heading', { name: '客户物联点位接入' })).toBeVisible()
    await expect(page.getByRole('heading', { name: '资产台账与巡检保养' })).toBeHidden()

    await page.getByRole('button', { name: '工单调度' }).click()
    await expect(page.getByRole('heading', { name: '工单调度中心' })).toBeVisible()
    await expect(page.getByRole('heading', { name: '客户物联点位接入' })).toBeHidden()
  })

  test('直接打开带 hash 的业务链接时同步当前页面', async ({ page }) => {
    await page.goto('/#工单调度')

    await expect(page.getByRole('button', { name: '工单调度' })).toHaveAttribute('aria-pressed', 'true')
    await expect(page.getByRole('heading', { name: '工单调度工作台' })).toBeVisible()
    await expect(page.getByRole('heading', { name: '资产台账与巡检保养' })).toBeHidden()
  })

  test('移动端不出现横向溢出并保留核心办事入口', async ({ page }) => {
    await page.setViewportSize({ width: 390, height: 844 })
    await page.goto('/')

    await expect(page.getByRole('heading', { name: '医院后勤管理工作台' })).toBeVisible()
    await expect(page.getByRole('button', { name: '工单调度' })).toBeVisible()
    await expect(page.getByRole('heading', { name: '工单调度中心' })).toBeVisible()
    await expect(page.getByRole('heading', { name: '环境预警池' })).toBeVisible()
    await expect(page.getByRole('heading', { name: '来源可追溯功能目录' })).toBeVisible()

    const hasHorizontalOverflow = await page.evaluate(
      () => document.documentElement.scrollWidth > window.innerWidth + 1,
    )
    expect(hasHorizontalOverflow).toBe(false)
  })
})
