import { test, expect, type Page } from '@playwright/test'

// ---------------------------------------------------------------------------
// Helpers
// ---------------------------------------------------------------------------

/** Navigates to the Journal view via the bottom nav. */
async function goToJournal(page: Page) {
  await page.locator('button[aria-label="Journal"]').click()
  await page.locator('div[role="tree"][aria-label="Notes tree"]').waitFor()
}

/** Creates a note via the API (bypasses UI for speed). */
async function createNoteViaApi(page: Page, path: string, content = '') {
  await page.request.post('/api/notes', {
    data: { path, content },
  })
}

/** Reads a note's content via the API. */
async function readNoteViaApi(page: Page, path: string): Promise<string> {
  const resp = await page.request.get(`/api/notes?path=${encodeURIComponent(path)}`)
  const body = await resp.json()
  return body.content ?? ''
}

/** Deletes a note via the API (ignores errors for cleanup). */
async function deleteNoteViaApi(page: Page, path: string) {
  await page.request.delete(`/api/notes?path=${encodeURIComponent(path)}`).catch(() => {})
}

/** Opens a note by clicking it in the sidebar tree. */
async function openNoteInSidebar(page: Page, name: string) {
  await page.locator('div[role="treeitem"]').filter({ hasText: name }).click()
}

/** Clicks a tab in the tab bar by note name. */
async function clickTab(page: Page, name: string) {
  await page.locator('div[role="tab"]').filter({ hasText: name }).click()
}

/** Returns the editor textarea locator. */
function editorTextarea(page: Page) {
  return page.locator('textarea[aria-label*="Edit"]')
}

/** Waits for save status to show "Saved". */
async function waitForSaved(page: Page) {
  // The status span is inside the toolbar row
  await expect(page.locator('span').filter({ hasText: /^Saved$/ })).toBeVisible({ timeout: 10_000 })
}

// ---------------------------------------------------------------------------
// Test setup & teardown
// ---------------------------------------------------------------------------

const NOTE_A = 'E2E Test Note A.md'
const NOTE_B = 'E2E Test Note B.md'
const CONTENT_A = 'Content for Note A - initial'
const CONTENT_B = 'Content for Note B - initial'

test.beforeEach(async ({ page }) => {
  // Create test notes via API
  await createNoteViaApi(page, NOTE_A, CONTENT_A)
  await createNoteViaApi(page, NOTE_B, CONTENT_B)

  // Load the app and navigate to journal
  await page.goto('/')
  await page.waitForLoadState('networkidle')
  await goToJournal(page)

  // Refresh the sidebar to pick up our test notes
  // Opening the journal view should auto-load the tree
  await page.locator('div[role="treeitem"]').filter({ hasText: 'E2E Test Note A' }).waitFor({ timeout: 5_000 })
})

test.afterEach(async ({ page }) => {
  await deleteNoteViaApi(page, NOTE_A)
  await deleteNoteViaApi(page, NOTE_B)
})

// ---------------------------------------------------------------------------
// Tests
// ---------------------------------------------------------------------------

test.describe('Notes tab navigation race conditions', () => {
  test('content is preserved after rapid tab switching', async ({ page }) => {
    // Open Note A and type distinctive content
    await openNoteInSidebar(page, 'E2E Test Note A')
    const editor = editorTextarea(page)
    await editor.waitFor()

    const marker = 'UNIQUE-MARKER-' + Date.now()
    await editor.fill(marker)

    // Open Note B so both tabs exist
    await openNoteInSidebar(page, 'E2E Test Note B')
    await editorTextarea(page).waitFor()

    // Rapidly switch: A → B → A → B → A
    await clickTab(page, 'E2E Test Note A')
    await clickTab(page, 'E2E Test Note B')
    await clickTab(page, 'E2E Test Note A')
    await clickTab(page, 'E2E Test Note B')
    await clickTab(page, 'E2E Test Note A')

    // Wait for content to stabilize
    await editor.waitFor()
    await expect(editor).toHaveValue(marker, { timeout: 5_000 })

    // Verify Note B does NOT have the marker
    await clickTab(page, 'E2E Test Note B')
    await editor.waitFor()
    await expect(editor).not.toHaveValue(marker)
  })

  test('save completes for the correct path after quick navigation', async ({ page }) => {
    // Open Note A and type new content
    await openNoteInSidebar(page, 'E2E Test Note A')
    const editor = editorTextarea(page)
    await editor.waitFor()

    const newContent = 'Saved content for A - ' + Date.now()
    await editor.fill(newContent)

    // Immediately switch to Note B (before debounce fires)
    await openNoteInSidebar(page, 'E2E Test Note B')
    await editorTextarea(page).waitFor()

    // Wait for saves to complete (flushSave should have fired on tab switch)
    await waitForSaved(page)

    // Verify via API that Note A was saved correctly
    const savedA = await readNoteViaApi(page, NOTE_A)
    expect(savedA).toBe(newContent)

    // Verify Note B was NOT overwritten
    const savedB = await readNoteViaApi(page, NOTE_B)
    expect(savedB).toBe(CONTENT_B)
  })

  test('stale API response does not overwrite active tab content', async ({ page }) => {
    // Intercept Note A loads and add a delay to simulate slow API
    await page.route(/\/api\/notes\?path=.*E2E.*Test.*Note.*A/, async (route) => {
      await new Promise((r) => setTimeout(r, 800))
      await route.continue()
    })

    // Open Note B first (loads instantly, populates cache)
    await openNoteInSidebar(page, 'E2E Test Note B')
    const editor = editorTextarea(page)
    await editor.waitFor()
    await waitForSaved(page)

    // Close Note B's tab cache by navigating away and back to force API loads
    // Click Note A (slow load starts)
    await openNoteInSidebar(page, 'E2E Test Note A')

    // Quickly click Note B before Note A's response arrives
    // Since Note B is cached, it should load instantly
    await clickTab(page, 'E2E Test Note B')

    // Wait a moment for Note A's delayed response to arrive
    await page.waitForTimeout(1200)

    // Assert we're still seeing Note B's content, not Note A's stale response
    await expect(editorTextarea(page)).toHaveValue(CONTENT_B, { timeout: 3_000 })
  })
})
