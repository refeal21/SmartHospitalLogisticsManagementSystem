import { expect, test } from '@playwright/test'

test('power distribution specialty links strong electric points assets alarms and dispatch', async ({ page }) => {
  await page.goto('/#power-distribution')

  await expect(page.getByTestId('power-distribution-board')).toBeVisible()
  await expect(page.getByTestId('power-distribution-board')).toContainText('PWR-CIRCUIT-B1-LV-IN')
  await expect(page.getByTestId('power-distribution-board')).toContainText('PWR-LV-B1-IN-01')
  await expect(page.getByTestId('power-distribution-board')).toContainText('PWR-LV-B1-IN-CAB')
  await expect(page.getByTestId('power-distribution-board')).toContainText('BIM-ENE-B1-PDU')
  await expect(page.getByTestId('power-distribution-board')).toContainText('PPT')

  await page.getByTestId('power-distribution-ingest-critical').click()
  await expect(page.getByTestId('power-distribution-board')).toContainText(/ALM-PWR|WO-ALM-/)

  await page.getByTestId('power-distribution-convert-workorder').click()
  await page.getByTestId('power-distribution-open-dispatch').click()

  await expect(page.getByTestId('work-order-list')).toBeVisible()
  await expect(page.getByTestId('work-order-list')).toContainText(/WO-ALM-/)
  await expect(page.getByTestId('work-order-detail')).toContainText('BIM-ENE-B1-PDU')
})
