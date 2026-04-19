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

/** Toggles the editor to WYSIWYG (preview) mode via Ctrl+E. */
async function toggleToWysiwyg(page: Page) {
  await editorTextarea(page).focus().catch(() => { /* may already be WYSIWYG */ })
  await page.keyboard.press('Control+E')
  await page.locator('.wysiwyg-editor [contenteditable="true"]').waitFor({ timeout: 5_000 })
}

/** Returns the Tiptap contenteditable root. */
function wysiwygRoot(page: Page) {
  return page.locator('.wysiwyg-editor [contenteditable="true"]')
}

/** Appends content to an existing note via the API. */
async function appendToNoteViaApi(page: Page, path: string, suffix: string) {
  const current = await readNoteViaApi(page, path)
  await page.request.put('/api/notes', {
    data: { path, content: current + suffix },
  })
}

/**
 * Calls the DEV-only test hook exposed by NoteEditor to reload the active
 * note's content from the API. Mirrors the path used by useToolActions
 * when a tool appends to the session log.
 */
async function reloadActiveNote(page: Page) {
  await page.evaluate(async () => {
    const fn = (window as unknown as { __soloforgeReloadActiveNote?: () => Promise<void> }).__soloforgeReloadActiveNote
    if (!fn) throw new Error('__soloforgeReloadActiveNote not exposed — is DEV mode on?')
    await fn()
  })
}

/** Returns the scroll state of the WYSIWYG scroll container. */
async function wysiwygScrollState(page: Page): Promise<{ scrollTop: number; scrollHeight: number; clientHeight: number }> {
  return page.evaluate(() => {
    const editor = document.querySelector('.wysiwyg-editor')
    const container = editor?.parentElement as HTMLElement | null
    if (!container) return { scrollTop: 0, scrollHeight: 0, clientHeight: 0 }
    return {
      scrollTop: container.scrollTop,
      scrollHeight: container.scrollHeight,
      clientHeight: container.clientHeight,
    }
  })
}

/** Scrolls the WYSIWYG container to its bottom. */
async function scrollWysiwygToBottom(page: Page) {
  await page.evaluate(() => {
    const editor = document.querySelector('.wysiwyg-editor')
    const container = editor?.parentElement as HTMLElement | null
    if (container) container.scrollTop = container.scrollHeight
  })
}

// ---------------------------------------------------------------------------
// Fixtures
// ---------------------------------------------------------------------------

const NOTE_A = 'E2E State Note A.md'
// A long note so there is room to scroll. 50 paragraphs, one per line.
const LONG_CONTENT = Array.from({ length: 50 }, (_, i) => `Paragraph ${i + 1} of the long test note.`).join('\n\n')

const NOTE_B = 'E2E State Note B.md'
// Note B's content is the same as Note A's first 3 paragraphs — a strict
// prefix of Note A's content. Exercises the contentKey branch.
const SHORT_CONTENT = Array.from({ length: 3 }, (_, i) => `Paragraph ${i + 1} of the long test note.`).join('\n\n')

test.beforeEach(async ({ page }) => {
  await createNoteViaApi(page, NOTE_A, LONG_CONTENT)
  await createNoteViaApi(page, NOTE_B, SHORT_CONTENT)
  await page.goto('/')
  await page.waitForLoadState('networkidle')
  await goToJournal(page)
  await page.locator('div[role="treeitem"]').filter({ hasText: 'E2E State Note A' }).waitFor({ timeout: 5_000 })
})

test.afterEach(async ({ page }) => {
  await deleteNoteViaApi(page, NOTE_A)
  await deleteNoteViaApi(page, NOTE_B)
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

  test('append preserves cursor position in WYSIWYG mode', async ({ page }) => {
    await openNoteInSidebar(page, 'E2E State Note A')
    await editorTextarea(page).waitFor()
    await waitForSaved(page)
    await toggleToWysiwyg(page)

    const root = wysiwygRoot(page)
    await expect(root).toBeVisible()

    // Place cursor in the middle of paragraph 25
    const middleParagraph = root.locator('p').nth(25)
    await middleParagraph.click()

    const cursorBefore = await page.evaluate(() => {
      const sel = window.getSelection()
      if (!sel || sel.rangeCount === 0) return null
      const range = sel.getRangeAt(0)
      return {
        anchorNodeText: range.startContainer.textContent ?? '',
        offset: range.startOffset,
      }
    })
    expect(cursorBefore).not.toBeNull()

    // Append via API, then reload via the test hook (mirrors tool refresh path)
    await appendToNoteViaApi(page, NOTE_A, '\n\nAppended from test.')
    await reloadActiveNote(page)
    await expect(root).toContainText('Appended from test.', { timeout: 5_000 })

    const cursorAfter = await page.evaluate(() => {
      const sel = window.getSelection()
      if (!sel || sel.rangeCount === 0) return null
      const range = sel.getRangeAt(0)
      return {
        anchorNodeText: range.startContainer.textContent ?? '',
        offset: range.startOffset,
      }
    })
    expect(cursorAfter).not.toBeNull()
    expect(cursorAfter!.anchorNodeText).toBe(cursorBefore!.anchorNodeText)
    expect(cursorAfter!.offset).toBe(cursorBefore!.offset)
  })

  test('sticky scroll follows append when user was near the bottom', async ({ page }) => {
    await openNoteInSidebar(page, 'E2E State Note A')
    await editorTextarea(page).waitFor()
    await waitForSaved(page)
    await toggleToWysiwyg(page)

    const root = wysiwygRoot(page)
    await expect(root).toBeVisible()

    await scrollWysiwygToBottom(page)
    const before = await wysiwygScrollState(page)
    expect(before.scrollHeight - before.scrollTop - before.clientHeight).toBeLessThan(80)

    await appendToNoteViaApi(page, NOTE_A, '\n\nFresh append at the bottom.')
    await reloadActiveNote(page)
    await expect(root).toContainText('Fresh append at the bottom.', { timeout: 5_000 })

    await page.waitForTimeout(100) // rAF settle
    const after = await wysiwygScrollState(page)
    expect(after.scrollHeight - after.scrollTop - after.clientHeight).toBeLessThan(80)
    expect(after.scrollHeight).toBeGreaterThan(before.scrollHeight)
  })

  test('sticky scroll does not yank view when user is scrolled up', async ({ page }) => {
    await openNoteInSidebar(page, 'E2E State Note A')
    await editorTextarea(page).waitFor()
    await waitForSaved(page)
    await toggleToWysiwyg(page)

    const root = wysiwygRoot(page)
    await expect(root).toBeVisible()

    await page.evaluate(() => {
      const editor = document.querySelector('.wysiwyg-editor')
      const container = editor?.parentElement as HTMLElement | null
      if (container) container.scrollTop = 0
    })
    const before = await wysiwygScrollState(page)
    expect(before.scrollTop).toBe(0)

    await appendToNoteViaApi(page, NOTE_A, '\n\nAnother append far below.')
    await reloadActiveNote(page)
    await expect(root).toContainText('Another append far below.', { timeout: 5_000 })

    await page.waitForTimeout(100)
    const after = await wysiwygScrollState(page)
    expect(after.scrollTop).toBeLessThan(50) // still at/near top
  })

  test('note switch replaces content even when one is a prefix of the other', async ({ page }) => {
    // Open Note B (short) first
    await openNoteInSidebar(page, 'E2E State Note B')
    await editorTextarea(page).waitFor()
    await expect(editorTextarea(page)).toHaveValue(SHORT_CONTENT, { timeout: 5_000 })
    await toggleToWysiwyg(page)

    // Verify short content shown in WYSIWYG
    const root = wysiwygRoot(page)
    await expect(root).toContainText('Paragraph 3 of the long test note.')
    // Note A's long content has paragraphs beyond 3 — verify they are NOT present
    await expect(root).not.toContainText('Paragraph 4 of the long test note.')

    // Switch to Note A (long, which starts with the same content as B)
    await openNoteInSidebar(page, 'E2E State Note A')
    await expect(root).toContainText('Paragraph 50 of the long test note.', { timeout: 5_000 })

    // Switch back to Note B; verify only the first 3 paragraphs are shown,
    // not the appended paragraphs from A (which would happen if the watcher
    // incorrectly took the append branch on a note switch).
    await openNoteInSidebar(page, 'E2E State Note B')
    await expect(root).toContainText('Paragraph 3 of the long test note.', { timeout: 5_000 })
    await expect(root).not.toContainText('Paragraph 4 of the long test note.')
  })

  test('save flushes when navigating away from Journal', async ({ page }) => {
    await openNoteInSidebar(page, 'E2E State Note A')
    const editor = editorTextarea(page)
    await expect(editor).toHaveValue(LONG_CONTENT, { timeout: 5_000 })

    const newContent = LONG_CONTENT + `\n\nUnique marker ${Date.now()}`
    await editor.fill(newContent)

    // Navigate away immediately, before the auto-save debounce fires
    await goToTools(page)

    // Wait a moment for onDeactivated → flushSave to run
    await page.waitForTimeout(500)

    // Read note content via API; should reflect the edit
    const saved = await readNoteViaApi(page, NOTE_A)
    expect(saved).toBe(newContent)
  })
})
