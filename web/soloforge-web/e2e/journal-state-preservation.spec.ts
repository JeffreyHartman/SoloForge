import { test, expect, type Page } from '@playwright/test'

// ---------------------------------------------------------------------------
// Helpers
// ---------------------------------------------------------------------------

/** Navigates to the Journal view via the bottom nav. */
async function goToJournal(page: Page) {
  await page.locator('button[aria-label="Journal"]').click()
  await page.locator('div[role="tree"][aria-label="Notes tree"]').waitFor()
}

/** Navigates to the Tools view via the bottom nav. */
async function goToTools(page: Page) {
  await page.locator('button[aria-label="Tools"]').click()
}

/** Creates a note via the API. */
async function createNoteViaApi(page: Page, path: string, content = '') {
  await page.request.post('/api/notes', { data: { path, content } })
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

/** Returns the editor textarea locator (Edit mode). */
function editorTextarea(page: Page) {
  return page.locator('textarea[aria-label*="Edit"]')
}

/** Waits for the save status to show "Saved". */
async function waitForSaved(page: Page) {
  await expect(page.locator('span').filter({ hasText: /^Saved$/ })).toBeVisible({ timeout: 10_000 })
}

// ---------------------------------------------------------------------------
// Fixtures
// ---------------------------------------------------------------------------

const NOTE_A = 'E2E State Note A.md'
// A long note so there is room to scroll. 50 paragraphs, one per line.
const LONG_CONTENT = Array.from({ length: 50 }, (_, i) => `Paragraph ${i + 1} of the long test note.`).join('\n\n')

test.beforeEach(async ({ page }) => {
  await createNoteViaApi(page, NOTE_A, LONG_CONTENT)
  await page.goto('/')
  await page.waitForLoadState('networkidle')
  await goToJournal(page)
  await page.locator('div[role="treeitem"]').filter({ hasText: 'E2E State Note A' }).waitFor({ timeout: 5_000 })
})

test.afterEach(async ({ page }) => {
  await deleteNoteViaApi(page, NOTE_A)
})

// ---------------------------------------------------------------------------
// Tests
// ---------------------------------------------------------------------------

test.describe('Journal state preservation across nav', () => {
  test('nav round-trip preserves editor content and scroll position', async ({ page }) => {
    // Open the long note in Edit mode
    await openNoteInSidebar(page, 'E2E State Note A')
    const editor = editorTextarea(page)
    await expect(editor).toHaveValue(LONG_CONTENT, { timeout: 5_000 })

    // Scroll the textarea to a distinctive position (not top, not bottom)
    await editor.evaluate((el: HTMLTextAreaElement) => { el.scrollTop = 500 })
    const scrollBefore = await editor.evaluate((el: HTMLTextAreaElement) => el.scrollTop)
    expect(scrollBefore).toBeGreaterThan(0)

    // Navigate away and back
    await goToTools(page)
    // Pause briefly so any unmount side-effects would have time to fire
    await page.waitForTimeout(300)
    await goToJournal(page)

    // Same note should still be active; content unchanged
    const editorAfter = editorTextarea(page)
    await expect(editorAfter).toHaveValue(LONG_CONTENT, { timeout: 5_000 })

    // Scroll position preserved (within a small tolerance for rendering jitter)
    const scrollAfter = await editorAfter.evaluate((el: HTMLTextAreaElement) => el.scrollTop)
    expect(Math.abs(scrollAfter - scrollBefore)).toBeLessThan(50)
  })
})
