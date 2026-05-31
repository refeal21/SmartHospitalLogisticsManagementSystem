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

  test('客户物联点位接入支持字段查看和异常读数判定', async ({ page }) => {
    await page.goto('/')

    await expect(page.getByRole('heading', { name: '客户物联点位接入' })).toBeVisible()
    await expect(page.getByRole('heading', { name: '点位目录' })).toBeVisible()
    await expect(page.getByRole('heading', { name: '时序字段' })).toBeVisible()
    await expect(page.getByRole('heading', { name: '阈值与读数' })).toBeVisible()

    await page.getByRole('button', { name: /MEDGAS-O2-8F/ }).click()
    await expect(page.getByText('BIM-IPD-F8-WARD').first()).toBeVisible()
    await expect(page.getByText('氧气压力 / 医气压力').first()).toBeVisible()

    await page.getByRole('button', { name: '模拟异常读数' }).click()
    await expect(page.getByText('最新风险：严重')).toBeVisible()
    await expect(page.getByText('MEDGAS-O2-8F / pressure')).toBeVisible()
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
