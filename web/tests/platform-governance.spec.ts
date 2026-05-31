import { expect, test } from '@playwright/test'

test('platform governance closes management metrics into actions and dispatch context', async ({ page }) => {
  await page.goto('/#platform-governance')

  const board = page.getByTestId('platform-governance-board')
  await expect(board).toBeVisible()
  await expect(board).toContainText('GOV-SLA-ONE-STOP')
  await expect(board).toContainText('GOV-ENERGY-COST')
  await expect(board).toContainText('GOV-SAFETY-EMERGENCY')
  await expect(board).toContainText('PPT')

  await page.getByTestId('platform-governance-record-action').click()
  await expect(board).toContainText(/ACT-GOV-SLA-ONE-STOP|整改动作/)

  await page.getByTestId('platform-governance-open-dispatch').click()
  await expect(page.getByTestId('dispatch-board')).toBeVisible()
})
