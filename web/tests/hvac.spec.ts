import { expect, test } from '@playwright/test'

test('HVAC specialty links chilled-water point asset alarms and dispatch', async ({ page }) => {
  await page.goto('/#hvac')

  await expect(page.getByTestId('hvac-board')).toBeVisible()
  await expect(page.getByTestId('hvac-board')).toContainText('HVAC-LOOP-B1-CHW')
  await expect(page.getByTestId('hvac-board')).toContainText('HVAC-CHW-B1-02')
  await expect(page.getByTestId('hvac-board')).toContainText('CHW-B1-02')
  await expect(page.getByTestId('hvac-board')).toContainText('BIM-ENE-B1-CHILLER')
  await expect(page.getByTestId('hvac-board')).toContainText('PPT')

  await page.getByTestId('hvac-ingest-critical').click()
  await expect(page.getByTestId('hvac-board')).toContainText(/ALM-HVAC|WO-ALM-/)

  await page.getByTestId('hvac-convert-workorder').click()
  await page.getByTestId('hvac-open-dispatch').click()

  await expect(page.getByTestId('work-order-list')).toBeVisible()
  await expect(page.getByTestId('work-order-list')).toContainText(/WO-ALM-/)
  await expect(page.getByTestId('work-order-detail')).toContainText('BIM-ENE-B1-CHILLER')
})
