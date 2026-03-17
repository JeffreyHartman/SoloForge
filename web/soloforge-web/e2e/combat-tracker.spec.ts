import { test, expect, type Page } from '@playwright/test'

// ---------------------------------------------------------------------------
// Helpers
// ---------------------------------------------------------------------------

async function goToTools(page: Page) {
  await page.locator('button[aria-label="Tools"]').click()
}

async function selectCombatTracker(page: Page) {
  await page.locator('button').filter({ hasText: 'Combat Tracker' }).click()
}

async function goToJournal(page: Page) {
  await page.locator('button[aria-label="Journal"]').click()
  await page.locator('div[role="tree"][aria-label="Notes tree"]').waitFor({ timeout: 5_000 })
}

async function addCombatant(page: Page) {
  await page.locator('button[aria-label="Add combatant"]').click()
}

function combatantRows(page: Page) {
  return page.locator('div[role="listitem"][aria-label*="Combatant"]')
}

function nameInput(page: Page, index: number) {
  return combatantRows(page).nth(index).locator('input[aria-label="Combatant name"]')
}

function acInput(page: Page, index: number) {
  return combatantRows(page).nth(index).locator('input[aria-label="Armor class"]')
}

/** Click-to-edit HP field: clicks the button to reveal input, fills, then presses Enter. */
async function setHp(page: Page, index: number, label: string, value: string) {
  const row = combatantRows(page).nth(index)
  const btn = row.locator(`button[aria-label="${label}"]`)
  await btn.click()
  const input = row.locator(`input[aria-label="${label}"]`)
  await input.fill(value)
  await input.press('Enter')
}

async function setCurrentHp(page: Page, index: number, value: string) {
  await setHp(page, index, 'Current HP for combatant', value)
}

async function setMaxHp(page: Page, index: number, value: string) {
  await setHp(page, index, 'Max HP for combatant', value)
}

/** Returns the displayed current HP text (from the button label). */
function currentHpDisplay(page: Page, index: number) {
  return combatantRows(page).nth(index).locator('button[aria-label="Current HP for combatant"]')
}

function maxHpDisplay(page: Page, index: number) {
  return combatantRows(page).nth(index).locator('button[aria-label="Max HP for combatant"]')
}

// ---------------------------------------------------------------------------
// Setup
// ---------------------------------------------------------------------------

test.beforeEach(async ({ page }) => {
  await page.goto('/')
  await page.evaluate(() => localStorage.removeItem('soloforge-combat-tracker'))
  await page.reload()
  await page.waitForLoadState('networkidle')
  await goToTools(page)
  await selectCombatTracker(page)
})

// ---------------------------------------------------------------------------
// Tests
// ---------------------------------------------------------------------------

test('add and populate combatants', async ({ page }) => {
  await addCombatant(page)
  await addCombatant(page)

  // Fill in first combatant
  await nameInput(page, 0).fill('Goblin')
  await setMaxHp(page, 0, '12')
  await setCurrentHp(page, 0, '12')
  await acInput(page, 0).fill('13')

  // Fill in second combatant
  await nameInput(page, 1).fill('Orc')
  await setMaxHp(page, 1, '30')
  await setCurrentHp(page, 1, '30')
  await acInput(page, 1).fill('16')

  // Verify values are shown
  await expect(nameInput(page, 0)).toHaveValue('Goblin')
  await expect(currentHpDisplay(page, 0)).toHaveText('12')
  await expect(maxHpDisplay(page, 0)).toHaveText('12')
  await expect(nameInput(page, 1)).toHaveValue('Orc')
  await expect(acInput(page, 1)).toHaveValue('16')
  await expect(combatantRows(page)).toHaveCount(2)
})

test('turn cycle increments round on wrap', async ({ page }) => {
  for (let i = 0; i < 3; i++) {
    await addCombatant(page)
    await nameInput(page, i).fill(`Fighter ${i + 1}`)
    await setCurrentHp(page, i, '10')
    await setMaxHp(page, i, '10')
  }

  const nextBtn = page.locator('button[aria-label="Next turn"]')
  const roundDisplay = page.locator('button[aria-label="Edit round number"]')

  // Start combat
  await nextBtn.click()
  await expect(roundDisplay).toHaveText('1')

  // Advance through all 3 combatants
  await nextBtn.click() // Fighter 2
  await nextBtn.click() // Fighter 3
  await nextBtn.click() // Wrap to Fighter 1, round 2

  await expect(roundDisplay).toHaveText('2')
})

test('dead combatants are skipped during turn advancement', async ({ page }) => {
  for (let i = 0; i < 3; i++) {
    await addCombatant(page)
    await nameInput(page, i).fill(`Warrior ${i + 1}`)
    await setCurrentHp(page, i, '10')
    await setMaxHp(page, i, '10')
  }

  // Mark middle combatant as dead
  const deadToggle = combatantRows(page).nth(1).locator('button[aria-label="Mark as dead"]')
  await deadToggle.click()

  const nextBtn = page.locator('button[aria-label="Next turn"]')

  // Start combat — should be on Warrior 1
  await nextBtn.click()

  // Next should skip Warrior 2 (dead) and go to Warrior 3
  await nextBtn.click()

  // Verify Warrior 3 has the active state
  const warrior3Row = combatantRows(page).nth(2)
  await expect(warrior3Row).toHaveClass(/border-\[var\(--color-text-accent\)\]/)
})

test('HP quick-math widget adjusts HP', async ({ page }) => {
  await addCombatant(page)
  await nameInput(page, 0).fill('Target')
  await setMaxHp(page, 0, '20')
  await setCurrentHp(page, 0, '20')

  // Click current HP to enter edit mode and show +/- buttons
  const hpBtn = currentHpDisplay(page, 0)
  await hpBtn.click()

  // Click minus button to reduce HP
  const reduceBtn = combatantRows(page).nth(0).locator('button[aria-label="Reduce HP"]')
  await reduceBtn.click()

  // HP should now be 19 — check the input value (still in edit mode)
  const hpInput = combatantRows(page).nth(0).locator('input[aria-label="Current HP for combatant"]')
  await expect(hpInput).toHaveValue('19')

  // Click plus button to increase HP
  const increaseBtn = combatantRows(page).nth(0).locator('button[aria-label="Increase HP"]')
  await increaseBtn.click()

  await expect(hpInput).toHaveValue('20')
})

test('drag reorder changes combatant order', async ({ page }) => {
  await addCombatant(page)
  await nameInput(page, 0).fill('Alpha')
  await addCombatant(page)
  await nameInput(page, 1).fill('Beta')
  await addCombatant(page)
  await nameInput(page, 2).fill('Gamma')

  const gammaRow = combatantRows(page).nth(2)
  const alphaRow = combatantRows(page).nth(0)

  await gammaRow.dragTo(alphaRow)

  await expect(nameInput(page, 0)).toHaveValue('Gamma')
  await expect(nameInput(page, 1)).toHaveValue('Alpha')
  await expect(nameInput(page, 2)).toHaveValue('Beta')
})

test('state persists across reload', async ({ page }) => {
  await addCombatant(page)
  await nameInput(page, 0).fill('Persistent Hero')
  await setCurrentHp(page, 0, '25')
  await setMaxHp(page, 0, '25')

  await page.reload()
  await page.waitForLoadState('networkidle')
  await goToTools(page)
  await selectCombatTracker(page)

  await expect(nameInput(page, 0)).toHaveValue('Persistent Hero')
  await expect(currentHpDisplay(page, 0)).toHaveText('25')
  await expect(combatantRows(page)).toHaveCount(1)
})

test('clear with confirmation', async ({ page }) => {
  await addCombatant(page)
  await nameInput(page, 0).fill('Doomed')
  await addCombatant(page)
  await nameInput(page, 1).fill('Also Doomed')

  await page.locator('button[aria-label="Clear combat"]').click()

  // Cancel — data should remain
  await page.locator('button[aria-label="Cancel clear"]').click()
  await expect(combatantRows(page)).toHaveCount(2)

  // Click Clear again and confirm
  await page.locator('button[aria-label="Clear combat"]').click()
  await page.locator('button[aria-label="Confirm clear"]').click()

  await expect(combatantRows(page)).toHaveCount(0)
})

test('journal panel toggle opens and closes combat tracker', async ({ page }) => {
  // Add a combatant on the Tools page first
  await addCombatant(page)
  await nameInput(page, 0).fill('Shared Hero')
  await setCurrentHp(page, 0, '15')
  await setMaxHp(page, 0, '15')

  await goToJournal(page)

  // Open combat panel
  const toggleBtn = page.locator('button[aria-label="Open combat tracker"]')
  await toggleBtn.click()

  const panel = page.locator('div[aria-label="Combat tracker panel"]')
  await expect(panel).toBeVisible()

  // Verify sidebar collapsed
  const sidebarExpandBtn = page.locator('button[aria-label="Open Notes sidebar"]')
  await expect(sidebarExpandBtn).toBeVisible()

  // Verify shared state
  const panelNameInput = panel.locator('input[aria-label="Combatant name"]').first()
  await expect(panelNameInput).toHaveValue('Shared Hero')

  // Close panel
  await page.locator('button[aria-label="Close combat panel"]').click()
  await expect(panel).not.toBeVisible()
})
