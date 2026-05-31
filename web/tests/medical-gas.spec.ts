import { expect, test } from '@playwright/test'

test('medical gas specialty links points assets alarms and dispatch', async ({ page }) => {
  await page.goto('/#medical-gas')

  await expect(page.getByTestId('medical-gas-board')).toBeVisible()
  await expect(page.getByTestId('medical-gas-board')).toContainText('MG-ZONE-IPD-8F')
  await expect(page.getByTestId('medical-gas-board')).toContainText('MEDGAS-O2-8F')
  await expect(page.getByTestId('medical-gas-board')).toContainText('MEDGAS-IPD-8F')
  await expect(page.getByTestId('medical-gas-board')).toContainText('MT-20260530-0002')
  await expect(page.getByTestId('medical-gas-board')).toContainText('PPT')

  await page.getByTestId('medical-gas-ingest-critical').click()
  await expect(page.getByTestId('medical-gas-board')).toContainText(/ALM-MEDGAS|WO-ALM-/)

  await page.getByTestId('medical-gas-convert-workorder').click()
  await page.getByTestId('medical-gas-open-dispatch').click()

  await expect(page.getByTestId('work-order-list')).toBeVisible()
  await expect(page.getByTestId('work-order-list')).toContainText(/WO-ALM-/)
  await expect(page.getByTestId('work-order-detail')).toContainText('BIM-IPD-F8-WARD')
})
