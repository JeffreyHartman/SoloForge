# Journal State Preservation Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Make the Journal editor feel continuous during mid-session use — preserve cursor and scroll position across navigation, and preserve them when tools append content to the active note.

**Architecture:** Three co-located changes in the Vue frontend. Wrap the App-level view switcher in `<KeepAlive>` so the Journal mounts once and survives nav. Teach `WysiwygEditor` to detect pure appends (new content is old content plus a suffix) and insert only the suffix at document end instead of rebuilding the whole document. Add a sticky-scroll watcher in `NoteEditor` that auto-scrolls to the bottom only when the user was already near the bottom. Two pure helpers (`isPureAppend`, `isNearBottom`) are extracted to a utility file for unit testing.

**Tech Stack:** Vue 3 (Composition API), TypeScript, Tiptap (ProseMirror-based editor), `@tiptap/markdown`, Vitest (unit), Playwright (e2e).

**Reference spec:** `docs/superpowers/specs/2026-04-18-journal-state-preservation-design.md`

---

## File Structure

| File | Purpose | Status |
|---|---|---|
| `web/soloforge-web/src/components/journal/editorState.ts` | Pure helpers: `isPureAppend`, `isNearBottom`. | Create |
| `web/soloforge-web/src/components/journal/__tests__/editorState.test.ts` | Vitest unit tests for the pure helpers. | Create |
| `web/soloforge-web/src/components/journal/WysiwygEditor.vue` | Add `contentKey` prop; rewrite `watch(() => props.content)` body to branch: note switch → full rebuild; pure append → `insertContentAt`; else → full rebuild fallback. | Modify |
| `web/soloforge-web/src/components/notes/NoteEditor.vue` | Pass `:content-key="activeNotePath"` to `<WysiwygEditor>`. Add template ref on scroll container. Add sync-flush watcher that captures `wasNearBottom` before content change and schedules `scrollTop = scrollHeight` via `requestAnimationFrame`. Migrate `onMounted`/`onUnmounted` to `onActivated`/`onDeactivated` so keep-alive deactivation still triggers `flushSave` and removes the keydown listener. Expose `reloadActiveNote` on `window.__soloforgeReloadActiveNote` under `import.meta.env.DEV` for e2e tests. | Modify |
| `web/soloforge-web/src/App.vue` | Wrap the view switch cascade in `<KeepAlive>`. No other structural changes. | Modify |
| `web/soloforge-web/e2e/journal-state-preservation.spec.ts` | Playwright e2e covering nav round-trip, append-preserves-cursor, sticky scroll at bottom, sticky scroll when scrolled up, note switch still replaces, save flush on view switch. | Create |

Other view files (`DashboardView`, `ToolsView`, `AdventureView`, `HistoryView`) contain no `onMounted`/`onUnmounted` hooks per a repo grep — no migration needed.

---

## Task 1: Pure helpers with unit tests (TDD)

**Files:**
- Create: `web/soloforge-web/src/components/journal/editorState.ts`
- Create: `web/soloforge-web/src/components/journal/__tests__/editorState.test.ts`

- [ ] **Step 1: Write the failing unit tests**

Create `web/soloforge-web/src/components/journal/__tests__/editorState.test.ts`:

```typescript
import { describe, it, expect } from 'vitest'
import { isPureAppend, isNearBottom } from '../editorState'

describe('isPureAppend', () => {
  it('returns true when old is empty and new has content', () => {
    expect(isPureAppend('', 'hello')).toBe(true)
  })

  it('returns false when old equals new', () => {
    expect(isPureAppend('hello', 'hello')).toBe(false)
  })

  it('returns true when new starts with old and is longer', () => {
    expect(isPureAppend('hello', 'hello world')).toBe(true)
  })

  it('returns false when new does not start with old', () => {
    expect(isPureAppend('hello', 'goodbye world')).toBe(false)
  })

  it('returns false when new is shorter than old', () => {
    expect(isPureAppend('hello world', 'hello')).toBe(false)
  })

  it('returns false when new is empty and old is not', () => {
    expect(isPureAppend('hello', '')).toBe(false)
  })

  it('returns false when both are empty', () => {
    expect(isPureAppend('', '')).toBe(false)
  })
})

describe('isNearBottom', () => {
  const threshold = 80

  it('returns true at exact bottom (remaining distance 0)', () => {
    // scrollHeight 1000, clientHeight 80, scrolled to 920 → 0 remaining
    expect(isNearBottom(920, 1000, 80, threshold)).toBe(true)
  })

  it('returns true within threshold', () => {
    // 20 remaining, threshold 80
    expect(isNearBottom(900, 1000, 80, threshold)).toBe(true)
  })

  it('returns true right at threshold boundary (exclusive upper)', () => {
    // 79 remaining, threshold 80 → still true
    expect(isNearBottom(841, 1000, 80, threshold)).toBe(true)
  })

  it('returns false beyond threshold', () => {
    // 820 remaining, threshold 80 → false
    expect(isNearBottom(100, 1000, 80, threshold)).toBe(false)
  })

  it('returns false for NaN scrollTop', () => {
    expect(isNearBottom(NaN, 1000, 80, threshold)).toBe(false)
  })

  it('returns false for NaN scrollHeight', () => {
    expect(isNearBottom(100, NaN, 80, threshold)).toBe(false)
  })

  it('returns false for NaN clientHeight', () => {
    expect(isNearBottom(100, 1000, NaN, threshold)).toBe(false)
  })
})
```

- [ ] **Step 2: Run tests to confirm they fail**

Run: `npm --prefix web/soloforge-web run test -- --run editorState`

Expected: FAIL with "Failed to resolve import '../editorState'" or similar (module does not exist yet).

- [ ] **Step 3: Create the implementation file**

Create `web/soloforge-web/src/components/journal/editorState.ts`:

```typescript
/**
 * Returns true if `newContent` equals `oldContent` plus a non-empty suffix.
 * Used by WysiwygEditor to detect when an external content update is a
 * pure append (e.g., a tool writing a roll result to the end of the note)
 * so the editor can insert just the new suffix at the document end
 * instead of rebuilding the entire document from scratch.
 */
export function isPureAppend(oldContent: string, newContent: string): boolean {
  if (newContent === oldContent) return false
  if (newContent.length <= oldContent.length) return false
  return newContent.startsWith(oldContent)
}

/**
 * Returns true if the scroll container is within `threshold` pixels of its
 * bottom edge. Used by NoteEditor for sticky-scroll behavior: auto-scroll
 * to the new bottom when content is appended only if the user was already
 * near the bottom before the append, so we don't yank the view when the
 * user is scrolled up reading earlier content.
 */
export function isNearBottom(
  scrollTop: number,
  scrollHeight: number,
  clientHeight: number,
  threshold: number,
): boolean {
  if (!Number.isFinite(scrollTop) || !Number.isFinite(scrollHeight) || !Number.isFinite(clientHeight)) {
    return false
  }
  return scrollHeight - scrollTop - clientHeight < threshold
}
```

- [ ] **Step 4: Run tests to confirm they pass**

Run: `npm --prefix web/soloforge-web run test -- --run editorState`

Expected: all 14 tests pass (7 for `isPureAppend`, 7 for `isNearBottom`).

- [ ] **Step 5: Commit**

```bash
git add web/soloforge-web/src/components/journal/editorState.ts web/soloforge-web/src/components/journal/__tests__/editorState.test.ts
git commit -m "feat(journal): add editorState helpers for append detection and sticky scroll

isPureAppend returns true when new content equals old content plus a
non-empty suffix — used to detect when a tool-driven content update can
be inserted at document end instead of triggering a full editor rebuild.

isNearBottom returns true when a scroll container is within a threshold
of its bottom — used for sticky-scroll auto-follow behavior when content
is appended while the user is already near the end."
```

---

## Task 2: Wire contentKey + append detection in WysiwygEditor

**Files:**
- Modify: `web/soloforge-web/src/components/journal/WysiwygEditor.vue`

- [ ] **Step 1: Add the import for `isPureAppend`**

Open `web/soloforge-web/src/components/journal/WysiwygEditor.vue`. In the `<script setup>` block at the top, add the import alongside the existing imports (after the `WikiLinkMark` / `createWikiLinkSuggestion` imports):

```typescript
import { isPureAppend } from './editorState'
```

- [ ] **Step 2: Add `contentKey` to the props definition**

Locate the `withDefaults(defineProps<{ ... }>(), { ... })` block near the top of `<script setup>`. Add `contentKey: string` to the props type:

```typescript
const props = withDefaults(defineProps<{
  content: string | undefined
  contentKey: string
  fontStyle: Record<string, string | undefined>
  disabled: boolean
  placeholder: string
  allPaths?: string[]
  enhanced?: boolean
}>(), {
  enhanced: true,
})
```

- [ ] **Step 3: Add the `lastContentKey` tracking ref**

Immediately after the `const isUpdatingFromProp = ref(false)` line, add:

```typescript
const lastContentKey = ref(props.contentKey)
```

- [ ] **Step 4: Replace the content watcher with the branching version**

Find the existing block:

```typescript
// Watch for external content changes (e.g., auto-appended roll results, tab switches)
watch(() => props.content, (newContent) => {
  if (!editor.value) return
  // Skip if Tiptap already shows this content (avoids cursor reset on feedback loops)
  const currentMarkdown = editor.value.getMarkdown()
  if (newContent === currentMarkdown) return

  isUpdatingFromProp.value = true
  editor.value.commands.setContent(newContent ?? '', { contentType: 'markdown' })
  nextTick(() => {
    isUpdatingFromProp.value = false
  })
})
```

Replace it with:

```typescript
// Watch for external content changes (e.g., auto-appended roll results, tab switches).
// Three branches:
//   1. contentKey changed → note switch, force full rebuild.
//   2. Content is an append (old is a prefix of new) → insert only the suffix at
//      doc end, preserving cursor and existing DOM nodes.
//   3. Otherwise → full rebuild (fallback for replaces, normalization mismatches).
watch(() => props.content, (newContent) => {
  if (!editor.value) return
  const currentMarkdown = editor.value.getMarkdown()
  if (newContent === currentMarkdown) return

  isUpdatingFromProp.value = true

  if (props.contentKey !== lastContentKey.value) {
    editor.value.commands.setContent(newContent ?? '', { contentType: 'markdown' })
    lastContentKey.value = props.contentKey
  } else if (isPureAppend(currentMarkdown, newContent ?? '')) {
    const suffix = (newContent ?? '').slice(currentMarkdown.length)
    const endPos = editor.value.state.doc.content.size
    editor.value.commands.insertContentAt(endPos, suffix, { contentType: 'markdown' })
  } else {
    editor.value.commands.setContent(newContent ?? '', { contentType: 'markdown' })
  }

  nextTick(() => {
    isUpdatingFromProp.value = false
  })
})
```

- [ ] **Step 5: Run existing tests and build to confirm no regressions**

Run: `npm --prefix web/soloforge-web run test -- --run`
Expected: all existing tests still pass (172+ previously passing; now plus 14 from Task 1 = 186+).

Run: `npm --prefix web/soloforge-web run build`
Expected: build succeeds. TypeScript should catch any prop-type mismatches — the `contentKey` prop is now required, so the build will fail if any consumer (only `NoteEditor.vue` at this point) doesn't pass it. That's expected — it will be fixed in Task 3.

If the build fails with `Property 'contentKey' is missing` in `NoteEditor.vue`, that's the expected signal. Proceed to Task 3.

- [ ] **Step 6: Commit**

```bash
git add web/soloforge-web/src/components/journal/WysiwygEditor.vue
git commit -m "feat(journal): add contentKey prop and append-detection branch to WysiwygEditor

On external content changes, the watcher now branches:
- If contentKey changed (note switch), force full rebuild via setContent.
- Else if the change is a pure append, insert only the suffix at doc end
  via insertContentAt, preserving cursor and existing DOM nodes.
- Else fall back to full rebuild.

This preserves cursor position when tools append content to the active
note, and disambiguates note switches from coincidental prefix matches."
```

---

## Task 3: Pass contentKey from NoteEditor to WysiwygEditor

**Files:**
- Modify: `web/soloforge-web/src/components/notes/NoteEditor.vue`

- [ ] **Step 1: Add the `:content-key` binding**

Find the `<WysiwygEditor>` element in the `<template>` of `NoteEditor.vue`. The current element looks like:

```vue
<WysiwygEditor
  ref="wysiwygRef"
  :content="activeNoteContent"
  :font-style="fontStyle"
  :disabled="!activeNotePath"
  :enhanced="prefs.enhanced"
  placeholder="Start writing..."
  :all-paths="allPaths"
  :aria-label="`Edit ${activeNoteFileName}`"
  @update:content="activeNoteContent = $event"
  @navigate="handleNavigate"
/>
```

Add `:content-key="activeNotePath ?? ''"` after the `:content` line:

```vue
<WysiwygEditor
  ref="wysiwygRef"
  :content="activeNoteContent"
  :content-key="activeNotePath ?? ''"
  :font-style="fontStyle"
  :disabled="!activeNotePath"
  :enhanced="prefs.enhanced"
  placeholder="Start writing..."
  :all-paths="allPaths"
  :aria-label="`Edit ${activeNoteFileName}`"
  @update:content="activeNoteContent = $event"
  @navigate="handleNavigate"
/>
```

- [ ] **Step 2: Run build to confirm type-check passes**

Run: `npm --prefix web/soloforge-web run build`
Expected: build succeeds with no errors.

- [ ] **Step 3: Run tests to confirm no regressions**

Run: `npm --prefix web/soloforge-web run test -- --run`
Expected: all tests pass (same count as Task 2).

- [ ] **Step 4: Commit**

```bash
git add web/soloforge-web/src/components/notes/NoteEditor.vue
git commit -m "feat(journal): pass contentKey to WysiwygEditor from NoteEditor

Uses the active note path as the content identity. When the user
switches notes, the editor's watcher sees the key change and forces a
full rebuild, immune to the case where one note's content happens to be
a prefix of another's."
```

---

## Task 4: Sticky scroll watcher in NoteEditor

**Files:**
- Modify: `web/soloforge-web/src/components/notes/NoteEditor.vue`

- [ ] **Step 1: Add the import for `isNearBottom`**

In the `<script setup>` block of `NoteEditor.vue`, add the import near the top with the other imports:

```typescript
import { isNearBottom } from '../journal/editorState'
```

- [ ] **Step 2: Add the scroll container template ref**

In `<script setup>`, near the existing `textareaRef` and `wysiwygRef` declarations (around line 16-17):

```typescript
const scrollContainerRef = ref<HTMLElement | null>(null)
```

- [ ] **Step 3: Attach the ref to the scroll container div in the template**

Find this block in the `<template>`:

```vue
<!-- Preview / WYSIWYG mode -->
<div
  v-else
  class="flex h-full flex-col overflow-y-auto rounded-2xl border border-[var(--color-border-primary)] bg-[var(--color-bg-input)] p-4 shadow-sm outline-none transition focus-within:border-[var(--color-text-dimmed)] focus-within:shadow"
  :style="fontStyle"
  @click.self="wysiwygRef?.focusEnd()"
>
```

Add `ref="scrollContainerRef"`:

```vue
<!-- Preview / WYSIWYG mode -->
<div
  v-else
  ref="scrollContainerRef"
  class="flex h-full flex-col overflow-y-auto rounded-2xl border border-[var(--color-border-primary)] bg-[var(--color-bg-input)] p-4 shadow-sm outline-none transition focus-within:border-[var(--color-text-dimmed)] focus-within:shadow"
  :style="fontStyle"
  @click.self="wysiwygRef?.focusEnd()"
>
```

- [ ] **Step 4: Add the sticky scroll watcher**

In `<script setup>`, after the existing `const { prefs } = useJournalPrefs()` line but before the `fontStyle` computed, add:

```typescript
const STICKY_SCROLL_THRESHOLD = 80

// Sticky scroll: when activeNoteContent changes (e.g., a tool appends a roll
// result), auto-scroll to the new bottom only if the user was already near
// the bottom before the change. Sync flush captures the pre-update scroll
// position; rAF schedules the scroll after Tiptap and Vue have both rendered.
watch(activeNoteContent, () => {
  const el = scrollContainerRef.value
  if (!el) return
  const wasNearBottom = isNearBottom(
    el.scrollTop,
    el.scrollHeight,
    el.clientHeight,
    STICKY_SCROLL_THRESHOLD,
  )
  if (!wasNearBottom) return
  requestAnimationFrame(() => {
    const current = scrollContainerRef.value
    if (!current) return
    current.scrollTop = current.scrollHeight
  })
}, { flush: 'sync' })
```

- [ ] **Step 5: Ensure `watch` is imported**

At the top of `<script setup>` in `NoteEditor.vue`, the existing Vue import may or may not already include `watch`. Locate the line:

```typescript
import { ref, computed, onMounted, onUnmounted } from 'vue'
```

Replace with:

```typescript
import { ref, computed, onMounted, onUnmounted, watch } from 'vue'
```

- [ ] **Step 6: Run build and tests**

Run: `npm --prefix web/soloforge-web run build`
Expected: build succeeds.

Run: `npm --prefix web/soloforge-web run test -- --run`
Expected: all tests pass.

- [ ] **Step 7: Commit**

```bash
git add web/soloforge-web/src/components/notes/NoteEditor.vue
git commit -m "feat(journal): add sticky-scroll behavior to NoteEditor

When activeNoteContent changes, capture whether the scroll container was
near the bottom (within 80px) via a sync-flush watcher — this runs before
Vue and Tiptap update the DOM. After the update, if we were near the
bottom, scroll to the new scrollHeight via requestAnimationFrame so the
scroll happens after both Vue and Tiptap have rendered. Users scrolled
up reading earlier content are not yanked to the bottom."
```

---

## Task 5: Migrate NoteEditor lifecycle for KeepAlive

**Files:**
- Modify: `web/soloforge-web/src/components/notes/NoteEditor.vue`

Context: Once `App.vue` wraps views in `<KeepAlive>` (Task 6), `NoteEditor` will not `onUnmounted` when the user navigates away — only when the whole Journal view is truly destroyed. We need `onActivated`/`onDeactivated` to handle nav-away correctly, and we want the keydown listener attached only while Journal is active.

- [ ] **Step 1: Update the Vue import to include activation hooks**

At the top of `<script setup>`, update the import to include `onActivated` and `onDeactivated`:

```typescript
import { ref, computed, onMounted, onUnmounted, onActivated, onDeactivated, watch } from 'vue'
```

- [ ] **Step 2: Replace the `onMounted` / `onUnmounted` block with activation-based lifecycle**

Find the existing block near the bottom of `<script setup>`:

```typescript
onMounted(() => document.addEventListener('keydown', onKeydown))
onUnmounted(() => {
  document.removeEventListener('keydown', onKeydown)
  flushSave()
})
```

Replace with:

```typescript
// Under <KeepAlive>, onActivated/onDeactivated fire on nav in/out while the
// component remains cached. onActivated also fires right after onMounted on
// the first visit, and onDeactivated fires right before onUnmounted on final
// teardown. Using these hooks for the keydown listener means Ctrl+E only
// toggles Journal mode while Journal is the active view.
onActivated(() => {
  document.addEventListener('keydown', onKeydown)
})
onDeactivated(() => {
  document.removeEventListener('keydown', onKeydown)
  flushSave()
})
```

Then update the Vue import to drop the now-unused `onMounted` and `onUnmounted`:

```typescript
import { ref, computed, onActivated, onDeactivated, watch } from 'vue'
```

- [ ] **Step 3: Run build and tests**

Run: `npm --prefix web/soloforge-web run build`
Expected: build succeeds.

Run: `npm --prefix web/soloforge-web run test -- --run`
Expected: all tests pass.

- [ ] **Step 4: Commit**

```bash
git add web/soloforge-web/src/components/notes/NoteEditor.vue
git commit -m "refactor(journal): switch NoteEditor to onActivated/onDeactivated

Under <KeepAlive>, the component remains mounted across view switches.
Move the keydown listener attach/detach and flushSave call to
activation-based hooks so:
- Ctrl+E only toggles mode while Journal is the active view.
- Pending saves still flush when the user navigates away from Journal.
- Both behaviors continue to fire on true unmount because onDeactivated
  runs before onUnmounted during final teardown."
```

---

## Task 6: Wrap view switch in KeepAlive (App.vue)

**Files:**
- Modify: `web/soloforge-web/src/App.vue`

- [ ] **Step 1: Wrap the view cascade in `<KeepAlive>`**

Find the `<main>` block in the `<template>` of `App.vue`:

```vue
<main class="relative mx-auto max-w-[2200px] px-4 pb-12 pt-6">
  <DashboardView
    v-if="currentView === 'dashboard'"
    ...
  />

  <ToolsView v-else-if="currentView === 'tools'" />

  <AdventureView
    v-else-if="currentView === 'adventure'"
    ...
  />

  <JournalView
    v-else-if="currentView === 'journal'"
    ...
  />

  <HistoryView
    v-else-if="currentView === 'history'"
    ...
  />
</main>
```

Wrap the entire cascade in a `<KeepAlive>` element (keep all props bindings as-is):

```vue
<main class="relative mx-auto max-w-[2200px] px-4 pb-12 pt-6">
  <KeepAlive>
    <DashboardView
      v-if="currentView === 'dashboard'"
      ...
    />

    <ToolsView v-else-if="currentView === 'tools'" />

    <AdventureView
      v-else-if="currentView === 'adventure'"
      ...
    />

    <JournalView
      v-else-if="currentView === 'journal'"
      ...
    />

    <HistoryView
      v-else-if="currentView === 'history'"
      ...
    />
  </KeepAlive>
</main>
```

(Vue's `<KeepAlive>` component is globally available — no import needed.)

- [ ] **Step 2: Run build**

Run: `npm --prefix web/soloforge-web run build`
Expected: build succeeds.

- [ ] **Step 3: Run all tests**

Run: `npm --prefix web/soloforge-web run test -- --run`
Expected: all tests pass.

- [ ] **Step 4: Manual smoke test in dev server**

Start the dev servers (from repo root):

```bash
./start-dev.sh
```

Open `http://localhost:5173` in a browser. Perform this sequence:

1. Navigate to Journal.
2. Open a note. Type some text. Scroll down if the note is long enough; otherwise just place cursor somewhere in the middle.
3. Navigate to Tools (via the bottom nav bar).
4. Navigate back to Journal.
5. **Verify:** the same note is still open, content unchanged, cursor roughly where you left it, scroll position preserved.

If any of those fail, do not proceed — investigate and fix before committing.

Stop the dev server (`Ctrl+C`).

- [ ] **Step 5: Commit**

```bash
git add web/soloforge-web/src/App.vue
git commit -m "feat(nav): wrap view switch in KeepAlive to preserve view state

Previously the v-if cascade destroyed each view on nav away and rebuilt
it on return, losing editor cursor, scroll position, and open tabs in
Journal. Wrapping in <KeepAlive> keeps each view mounted but hidden,
preserving their internal state across nav. Lifecycle hooks in child
components that depended on onMounted/onUnmounted firing per visit must
use onActivated/onDeactivated instead — already migrated in NoteEditor."
```

---

## Task 7: Verify no other views need lifecycle migration

**Files:**
- Read-only scan: `web/soloforge-web/src/views/*.vue`, plus any view-level components that might have mount-time side effects.

- [ ] **Step 1: Grep the views directory for lifecycle hooks**

Run from repo root:

```bash
grep -rn "onMounted\|onUnmounted\|onActivated\|onDeactivated" web/soloforge-web/src/views/
```

Expected: no matches. If a match appears, inspect that view and determine whether the side effect should re-run on each visit (migrate `onMounted` → `onActivated`) or only once (leave as-is). Commit any migrations.

- [ ] **Step 2: Manual smoke test across all views**

Start the dev servers:

```bash
./start-dev.sh
```

Click through each nav destination in sequence — Dashboard, Tools, Adventure, Journal, History — and verify:

1. Each view renders correctly on first visit.
2. Navigating away and back shows the same state (tabs, form input, scroll position) rather than a reset.
3. No console errors.

If any view misbehaves (e.g., stale data, event listener duplicated), trace to the cause and fix.

Stop the dev server.

- [ ] **Step 3: Commit (only if changes were made)**

If no other views required migration, there is nothing to commit. Skip this step. If you made changes:

```bash
git add <modified files>
git commit -m "refactor(views): migrate view-level mount hooks to activation hooks

Ensures per-visit side effects still fire correctly under <KeepAlive>."
```

---

## Task 8: E2E — Nav round-trip preserves editor state

**Files:**
- Create: `web/soloforge-web/e2e/journal-state-preservation.spec.ts`

- [ ] **Step 1: Create the e2e file with the first test**

Create `web/soloforge-web/e2e/journal-state-preservation.spec.ts`:

```typescript
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
```

- [ ] **Step 2: Run the e2e test**

From `web/soloforge-web/`:

```bash
npm run test:e2e -- journal-state-preservation
```

Playwright will start the dev servers automatically (per `playwright.config.ts`). Expected: the test passes. If it fails, read the Playwright trace (usually printed path to `test-results/…/trace.zip`) to see what the editor state looked like post-nav.

- [ ] **Step 3: Commit**

```bash
git add web/soloforge-web/e2e/journal-state-preservation.spec.ts
git commit -m "test(e2e): verify nav round-trip preserves journal editor state

Opens a long note, scrolls to a known position, navigates to Tools and
back, and asserts content and scroll position are preserved."
```

---

## Task 9: Add a DEV-only test hook for reloading the active note

**Files:**
- Modify: `web/soloforge-web/src/components/notes/NoteEditor.vue`

Context: The e2e tests in Tasks 10-12 need to simulate the real code path that fires when a tool appends content — `notesState.reloadActiveNote()`. That path is normally called from `useToolActions.refreshAfterAction`, which is triggered by running a tool. For e2e, we don't want to depend on the toolbar or a specific tool's side effects. Instead, we expose a single function on `window` under `import.meta.env.DEV` so Playwright can invoke it directly. This hook does not ship to production builds because `DEV` is `false` in production and the conditional is tree-shaken by Vite.

- [ ] **Step 1: Expose the test hook in NoteEditor onActivated**

In `NoteEditor.vue`, locate the `useNotes()` destructure (currently `const { activeNotePath, activeNoteContent, activeNoteFileName, saveStatus, allPaths, openNote, resolveNotePath, flushSave } = useNotes()`) and add `reloadActiveNote` to the destructure:

```typescript
const { activeNotePath, activeNoteContent, activeNoteFileName, saveStatus, allPaths, openNote, resolveNotePath, flushSave, reloadActiveNote } = useNotes()
```

Then, in the `onActivated` hook you added in Task 5, expose the hook:

```typescript
onActivated(() => {
  document.addEventListener('keydown', onKeydown)
  if (import.meta.env.DEV) {
    (window as unknown as { __soloforgeReloadActiveNote?: () => Promise<void> }).__soloforgeReloadActiveNote = reloadActiveNote
  }
})
```

Remove it on deactivation:

```typescript
onDeactivated(() => {
  document.removeEventListener('keydown', onKeydown)
  flushSave()
  if (import.meta.env.DEV) {
    delete (window as unknown as { __soloforgeReloadActiveNote?: () => Promise<void> }).__soloforgeReloadActiveNote
  }
})
```

- [ ] **Step 2: Verify `reloadActiveNote` is exported from `useNotes`**

Run from repo root:

```bash
grep -n "reloadActiveNote" web/soloforge-web/src/composables/useNotes.ts
```

Expected: at least one match showing `reloadActiveNote` as an exported function. (It is already used by `useToolActions.ts`, so it should be there.)

If it is not exported from the composable's returned object, add it. Read the file to confirm the shape.

- [ ] **Step 3: Run build and tests**

Run: `npm --prefix web/soloforge-web run build`
Expected: build succeeds.

Run: `npm --prefix web/soloforge-web run test -- --run`
Expected: all tests pass.

- [ ] **Step 4: Commit**

```bash
git add web/soloforge-web/src/components/notes/NoteEditor.vue
git commit -m "test: expose reloadActiveNote as DEV-only window hook for e2e

Tests need to exercise the same content-refresh path that
useToolActions uses when a tool appends to the active note. Exposing
reloadActiveNote on window under import.meta.env.DEV gives Playwright a
reliable way to trigger that path without depending on toolbar state or
specific tool side effects. The conditional is tree-shaken out of
production builds."
```

---

## Task 10: E2E — Append preserves cursor in WYSIWYG mode

**Files:**
- Modify: `web/soloforge-web/e2e/journal-state-preservation.spec.ts`

- [ ] **Step 1: Add WYSIWYG helpers**

In the helpers section of the spec file (after `waitForSaved`), add:

```typescript
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
```

- [ ] **Step 2: Add the test**

Inside the existing `test.describe('Journal state preservation across nav', () => { ... })` block, append:

```typescript
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
```

- [ ] **Step 3: Run the test**

```bash
npm --prefix web/soloforge-web run test:e2e -- journal-state-preservation
```

Expected: both tests pass (Task 8 test + this new one).

- [ ] **Step 4: Commit**

```bash
git add web/soloforge-web/e2e/journal-state-preservation.spec.ts
git commit -m "test(e2e): verify append preserves cursor in WYSIWYG mode

Clicks a middle paragraph to place the cursor, appends content via the
API, reloads the active note through the DEV test hook (mirroring the
tool-refresh path), and asserts the cursor stayed in the same text node
at the same offset. Exercises the append-detection branch in
WysiwygEditor's watcher."
```

---

## Task 11: E2E — Sticky scroll when at bottom

**Files:**
- Modify: `web/soloforge-web/e2e/journal-state-preservation.spec.ts`

- [ ] **Step 1: Add helper for querying scroll state**

In the helpers section:

```typescript
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
```

- [ ] **Step 2: Add the test**

Inside the existing `test.describe` block:

```typescript
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
```

- [ ] **Step 3: Run the test**

```bash
npm --prefix web/soloforge-web run test:e2e -- journal-state-preservation
```

Expected: all three tests pass.

- [ ] **Step 4: Commit**

```bash
git add web/soloforge-web/e2e/journal-state-preservation.spec.ts
git commit -m "test(e2e): verify sticky scroll auto-follows append when near bottom

Scrolls to bottom, appends content via API + reload, asserts the scroll
position stayed near the new bottom (remaining distance below
threshold)."
```

---

## Task 12: E2E — Sticky scroll does not yank when scrolled up

**Files:**
- Modify: `web/soloforge-web/e2e/journal-state-preservation.spec.ts`

- [ ] **Step 1: Add the test**

Inside the existing `test.describe` block:

```typescript
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
```

- [ ] **Step 2: Run the test**

```bash
npm --prefix web/soloforge-web run test:e2e -- journal-state-preservation
```

Expected: all four tests pass.

- [ ] **Step 3: Commit**

```bash
git add web/soloforge-web/e2e/journal-state-preservation.spec.ts
git commit -m "test(e2e): verify scroll is not yanked on append when scrolled up

Scrolls to top, appends content via API + reload, asserts the scroll
position stayed near the top (did not auto-follow to new content)."
```

---

## Task 13: E2E — Note switch still replaces content

**Files:**
- Modify: `web/soloforge-web/e2e/journal-state-preservation.spec.ts`

Context: regression check for the `contentKey` branch — particularly the edge case where one note's content is a prefix of another's.

- [ ] **Step 1: Add a second note fixture that is a prefix of the first**

Near the top of the file where `NOTE_A` and `LONG_CONTENT` are defined, add a second fixture:

```typescript
const NOTE_B = 'E2E State Note B.md'
// Note B's content is the same as Note A's first 3 paragraphs — a strict
// prefix of Note A's content. Exercises the contentKey branch.
const SHORT_CONTENT = Array.from({ length: 3 }, (_, i) => `Paragraph ${i + 1} of the long test note.`).join('\n\n')
```

Update `beforeEach` to create both notes:

```typescript
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
```

- [ ] **Step 2: Add the test**

Inside the existing `test.describe` block:

```typescript
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
```

- [ ] **Step 3: Run the test**

```bash
npm --prefix web/soloforge-web run test:e2e -- journal-state-preservation
```

Expected: all five tests pass (Tasks 8, 10, 11, 12, plus this one).

- [ ] **Step 4: Commit**

```bash
git add web/soloforge-web/e2e/journal-state-preservation.spec.ts
git commit -m "test(e2e): verify note switch replaces content even on prefix collision

Regression check for the contentKey branch in WysiwygEditor — creates
two notes where one is a strict prefix of the other, switches between
them, and asserts each tab shows the correct content."
```

---

## Task 14: E2E — Save flushes on nav away

**Files:**
- Modify: `web/soloforge-web/e2e/journal-state-preservation.spec.ts`

- [ ] **Step 1: Add the test**

Inside the existing `test.describe` block:

```typescript
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
```

- [ ] **Step 2: Run the test**

```bash
npm --prefix web/soloforge-web run test:e2e -- journal-state-preservation
```

Expected: all six tests pass.

- [ ] **Step 3: Commit**

```bash
git add web/soloforge-web/e2e/journal-state-preservation.spec.ts
git commit -m "test(e2e): verify save flushes when navigating away from Journal

Types into a note, navigates to Tools before the auto-save debounce
fires, and asserts the content was saved via the API. Exercises the
onDeactivated(flushSave) migration."
```

---

## Task 15: Full test suite + manual visual verification

**Files:** none

- [ ] **Step 1: Run the full Vitest suite**

Run: `npm --prefix web/soloforge-web run test -- --run`

Expected: all tests pass (previous count + 14 new from Task 1).

- [ ] **Step 2: Run the full Playwright suite**

From `web/soloforge-web/`:

```bash
npm run test:e2e
```

Expected: all e2e tests pass, including existing `notes-navigation.spec.ts` (regression check) and the new `journal-state-preservation.spec.ts`.

- [ ] **Step 3: Run the .NET build and tests**

From repo root:

```bash
dotnet build
dotnet test
```

Expected: zero warnings, zero errors, all tests pass. (These are unrelated to the change but confirm nothing in the frontend modifications broke the API integration points.)

- [ ] **Step 4: Manual visual pass with Playwright MCP or live browser**

Start the dev servers:

```bash
./start-dev.sh
```

In a browser:

1. **Nav round-trip:** open Journal, open a note, place cursor mid-document, scroll down. Navigate to Tools. Navigate back. Verify the feel is continuous — cursor where you left it, scroll position preserved, same note active.
2. **Append-at-bottom:** scroll to bottom of a note, open a pinned toolbar tool (e.g., Fate Check or a dice roll) and run it so it appends to the session log. Verify the editor auto-scrolls to reveal the appended content and the cursor did not jump.
3. **Append-while-reading:** scroll to the top of a long note, trigger the same tool append, verify the scroll position stays at the top (new content is off-screen below).
4. **Rapid nav:** click through Dashboard → Tools → Journal → Adventure → Journal rapidly. Verify Journal returns to its prior state each time with no flicker or content loss.

If any of these feel wrong, note specifics — a failing feel, not a broken test — and either fix or add to `docs/superpowers/specs/2026-04-18-journal-polish-audit-plan.md` under **Findings** for the future audit.

Stop the dev server.

- [ ] **Step 5: Update the audit plan Findings section if needed**

If Step 4 surfaced rough edges out of scope for this spec (e.g., flicker in other views, combat panel resize quirks, tab bar overflow behavior) — append them to `docs/superpowers/specs/2026-04-18-journal-polish-audit-plan.md` under the **Findings** header. Commit as:

```bash
git add docs/superpowers/specs/2026-04-18-journal-polish-audit-plan.md
git commit -m "docs(audit): log out-of-scope findings from journal polish implementation"
```

---

## Acceptance Criteria Checklist

From the spec. Verify each before declaring the feature done.

- [ ] Navigating away from and back to Journal preserves: open tabs, active note, cursor position, scroll position, editor selection.
- [ ] Appending via a tool while editing does not move the cursor from its current position.
- [ ] Appending while near the bottom auto-scrolls to the new bottom.
- [ ] Appending while scrolled up does not scroll the view.
- [ ] Switching note tabs still correctly swaps content (no regression).
- [ ] Auto-save flushes when navigating away from Journal.
- [ ] All existing unit and e2e tests continue to pass.
- [ ] New unit tests for `isPureAppend` and `isNearBottom` pass.
- [ ] New e2e spec `journal-state-preservation.spec.ts` passes.
- [ ] Live visual pass confirms the editor feels continuous during nav and appends.
