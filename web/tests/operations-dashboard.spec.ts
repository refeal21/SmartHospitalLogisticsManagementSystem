import { expect, test } from '@playwright/test'

test.describe('医院后勤 BIM 智慧运维功能型后台', () => {
  test('呈现可办事的后勤管理菜单和工作台', async ({ page }) => {
    await page.goto('/')

    await expect(
      page.getByRole('heading', { name: '医院后勤管理工作台' }),
    ).toBeVisible()
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

  test('移动端不会出现横向溢出并保留核心办事入口', async ({ page }) => {
    await page.setViewportSize({ width: 390, height: 844 })
    await page.goto('/')

    await expect(
      page.getByRole('heading', { name: '医院后勤管理工作台' }),
    ).toBeVisible()
    await expect(page.getByRole('link', { name: '工单调度' })).toBeVisible()
    await expect(page.getByRole('heading', { name: '工单调度中心' })).toBeVisible()
    await expect(page.getByRole('heading', { name: '环境预警池' })).toBeVisible()

    const hasHorizontalOverflow = await page.evaluate(
      () => document.documentElement.scrollWidth > window.innerWidth + 1,
    )
    expect(hasHorizontalOverflow).toBe(false)
  })
})
