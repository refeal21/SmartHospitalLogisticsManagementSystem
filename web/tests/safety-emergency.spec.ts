import { expect, test } from '@playwright/test'

test('safety emergency links fire security events to BIM alarms and dispatch', async ({ page }) => {
  await page.goto('/#safety-emergency')

  await expect(page.getByTestId('safety-emergency-board')).toBeVisible()
  await expect(page.getByTestId('safety-emergency-board')).toContainText('SAFE-FIRE-OPD-1F')
  await expect(page.getByTestId('safety-emergency-board')).toContainText('SAFE-SEC-ER-ACCESS')
  await expect(page.getByTestId('safety-emergency-board')).toContainText('FIRE-SMOKE-OPD-1F-01')
  await expect(page.getByTestId('safety-emergency-board')).toContainText('SEC-ACCESS-ER-01')
  await expect(page.getByTestId('safety-emergency-board')).toContainText('BIM-SEC-OPD-1F-FIRE')
  await expect(page.getByTestId('safety-emergency-board')).toContainText('PPT')

  await page.getByTestId('safety-emergency-ingest-fire').click()
  await expect(page.getByTestId('safety-emergency-board')).toContainText(/ALM-FIRE|火警|应急响应/)

  await page.getByTestId('safety-emergency-convert-alarm').click()
  await expect(page.getByTestId('safety-emergency-board')).toContainText('WO-ALM')

  await page.getByTestId('safety-emergency-open-dispatch').click()
  await expect(page.getByTestId('dispatch-board')).toBeVisible()
  await expect(page.getByTestId('dispatch-board')).toContainText('WO-ALM')
})
