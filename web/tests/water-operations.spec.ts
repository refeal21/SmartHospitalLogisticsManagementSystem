import { expect, test } from '@playwright/test'

test('water operations specialty links water supply sewage alarms and dispatch', async ({ page }) => {
  await page.goto('/#water-operations')

  await expect(page.getByTestId('water-operations-board')).toBeVisible()
  await expect(page.getByTestId('water-operations-board')).toContainText('WATER-SYS-B1-PUMP')
  await expect(page.getByTestId('water-operations-board')).toContainText('WATER-PUMP-B1-01')
  await expect(page.getByTestId('water-operations-board')).toContainText('SEWAGE-STATION-01')
  await expect(page.getByTestId('water-operations-board')).toContainText('BIM-ENE-B1-PUMP')
  await expect(page.getByTestId('water-operations-board')).toContainText('BIM-LOG-B1-SEWAGE')
  await expect(page.getByTestId('water-operations-board')).toContainText('PPT')

  await page.getByTestId('water-operations-ingest-critical').click()
  await expect(page.getByTestId('water-operations-board')).toContainText(/ALM-WATER|WO-ALM-/)

  await page.getByTestId('water-operations-convert-workorder').click()
  await page.getByTestId('water-operations-open-dispatch').click()

  await expect(page.getByTestId('work-order-list')).toBeVisible()
  await expect(page.getByTestId('work-order-list')).toContainText(/WO-ALM-/)
  await expect(page.getByTestId('work-order-detail')).toContainText('BIM-ENE-B1-PUMP')
})
