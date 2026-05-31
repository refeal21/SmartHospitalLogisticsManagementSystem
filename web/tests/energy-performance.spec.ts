import { expect, test } from '@playwright/test'

test('energy performance aggregates specialty systems and links anomalies to operations', async ({ page }) => {
  await page.goto('/#energy-performance')

  await expect(page.getByTestId('energy-performance-board')).toBeVisible()
  await expect(page.getByTestId('energy-performance-board')).toContainText('ENE-POWER-B1')
  await expect(page.getByTestId('energy-performance-board')).toContainText('ENE-HVAC-B1')
  await expect(page.getByTestId('energy-performance-board')).toContainText('ENE-WATER-B1')
  await expect(page.getByTestId('energy-performance-board')).toContainText('PWR-LV-B1-IN-01')
  await expect(page.getByTestId('energy-performance-board')).toContainText('HVAC-CHW-B1-02')
  await expect(page.getByTestId('energy-performance-board')).toContainText('BIM-ENE-B1-PDU')
  await expect(page.getByTestId('energy-performance-board')).toContainText('PPT')

  await page.getByTestId('energy-performance-ingest-anomaly').click()
  await expect(page.getByTestId('energy-performance-board')).toContainText(/ALM-PWR|异常成本|节能建议/)

  await page.getByTestId('energy-performance-open-power').click()
  await expect(page.getByTestId('power-distribution-board')).toBeVisible()
  await expect(page.getByTestId('power-distribution-board')).toContainText('PWR-CIRCUIT-B1-LV-IN')
})
