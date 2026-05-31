import { expect, test } from '@playwright/test'

test.describe('医院后勤 BIM 智慧运维功能型后台', () => {
  test('呈现可办事的后勤管理菜单和工作台', async ({ page }) => {
    await page.goto('/')

    await expect(page.getByRole('heading', { name: '医院后勤管理工作台' })).toBeVisible()
    await expect(page.getByRole('link', { name: '服务受理' })).toBeVisible()
    await expect(page.getByRole('link', { name: '工单调度' })).toBeVisible()
    await expect(page.getByRole('link', { name: '设备台账' })).toBeVisible()
    await expect(page.getByRole('link', { name: '预警池' })).toBeVisible()
    await expect(page.getByRole('link', { name: '合同管理' })).toBeVisible()

    await expect(page.getByRole('heading', { name: '工单调度中心' })).toBeVisible()
    await expect(page.getByText('WO-20260530-0001')).toBeVisible()
    await expect(page.getByText('医废暂存间负压异常处置')).toBeVisible()
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

  test('移动端不出现横向溢出并保留核心办事入口', async ({ page }) => {
    await page.setViewportSize({ width: 390, height: 844 })
    await page.goto('/')

    await expect(page.getByRole('heading', { name: '医院后勤管理工作台' })).toBeVisible()
    await expect(page.getByRole('link', { name: '工单调度' })).toBeVisible()
    await expect(page.getByRole('heading', { name: '工单调度中心' })).toBeVisible()
    await expect(page.getByRole('heading', { name: '环境预警池' })).toBeVisible()
    await expect(page.getByRole('heading', { name: '来源可追溯功能目录' })).toBeVisible()

    const hasHorizontalOverflow = await page.evaluate(
      () => document.documentElement.scrollWidth > window.innerWidth + 1,
    )
    expect(hasHorizontalOverflow).toBe(false)
  })
})
