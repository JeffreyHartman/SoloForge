# Design: Journal State Preservation ("Obsidian-feel" polish)

**Project:** SoloForge
**Feature:** Journal editor state preservation across navigation, appends, and scroll
**Status:** Ready for implementation
**Last updated:** 2026-04-18

---

## Overview

The Journal view currently loses editor state whenever the user navigates away (cursor position, scroll position, open tabs, editor instance are all destroyed) and whenever a tool appends content to the active note (cursor and scroll reset because the full document is rebuilt from the markdown string). The combination makes the editor feel clunky compared to dedicated markdown editors like Obsidian.

This design addresses three specific, independently-scoped fixes that together restore a continuous-editing feel during a solo session:

1. Preserve the Journal view across navigation with Vue's `<KeepAlive>`.
2. Detect pure appends in the WYSIWYG editor and insert at document end instead of rebuilding the document.
3. Auto-scroll to bottom after an append, but only when the user was already near the bottom (sticky-scroll convention).

---

## Goals

- Navigating Journal → Tools → Journal preserves the editor instance, cursor position, scroll position, and open tabs.
- Appending roll results or oracle output to the active note does not move the cursor from its current position.
- When the user is near the bottom of the note, appended content auto-scrolls into view.
- When the user is scrolled up reading earlier content, appended content does not yank the scroll position.
- Pending auto-saves still flush correctly when navigating away from Journal.
- No regressions in note switching, WYSIWYG/Edit mode toggling, or multi-tab behavior.

## Non-Goals

- No new editor features (no outline, no search, no backlinks, no command palette).
- No rewrite of the Tiptap extensions or markdown pipeline.
- No introduction of Vue Router (stay with the current `currentView` ref + `<component :is>` pattern).
- No migration of other views' lifecycle behavior beyond what's needed for `<KeepAlive>` correctness.
- No audit or fixes of other rough edges across the app (deferred to the separate Journal Polish Audit, tracked in `2026-04-18-journal-polish-audit-plan.md`).

---

## Architecture

Three co-located changes in `web/soloforge-web`:

### 1. `<KeepAlive>` around view switching in `App.vue`

Replace the current `v-if`/`v-else-if` cascade over distinct view components with a single `<component :is>` inside `<KeepAlive>`. A small map holds `viewName → component`, so the template becomes a single dynamic component element with conditional props. All five views are cached after first mount.

Per-view lifecycle hooks that previously ran on every visit (via `onMounted`/`onUnmounted`) move to `onActivated`/`onDeactivated`. The only hook that needs migration is `NoteEditor.vue`'s `onUnmounted(flushSave)`. Other views will be scanned during implementation; any that depend on refreshing data on each visit migrate to `onActivated`.

### 2. Append detection in `WysiwygEditor.vue`

The existing `watch(() => props.content, ...)` block currently calls `editor.commands.setContent(...)` on every external change. This rebuilds the document and resets cursor + scroll. The new behavior:

- Add a new `contentKey: string` prop to `WysiwygEditor`. `NoteEditor.vue` passes `activeNotePath` as the key. When the key changes, the content change is known to be a note switch and must take the full-rebuild path.
- Track `lastContentKey` in a ref inside `WysiwygEditor`, initialized from `props.contentKey` at create time.
- In the watcher:
  - Compute `currentMarkdown = editor.getMarkdown()` (already done).
  - If `newContent === currentMarkdown`, skip (already done).
  - If `props.contentKey !== lastContentKey`: force `setContent` path, update `lastContentKey`. This handles note switches unambiguously.
  - Else if `newContent.startsWith(currentMarkdown)` and `newContent.length > currentMarkdown.length`: extract `suffix = newContent.slice(currentMarkdown.length)` and call `editor.commands.insertContentAt(editor.state.doc.content.size, suffix, { contentType: 'markdown' })`. Wrap in the existing `isUpdatingFromProp` guard. (Options shape matches the existing `setContent(..., { contentType: 'markdown' })` usage in the same file.)
  - Otherwise: fall back to the existing `setContent` path.

The prefix match is exact string comparison. If markdown normalization produces an unexpected mismatch, the watcher falls through to `setContent` — same behavior as today, no regression.

### 3. Sticky scroll in `NoteEditor.vue`

The scroll container is the `overflow-y-auto` wrapper `<div>` around `<WysiwygEditor>` in `NoteEditor.vue`. Add a template ref to it.

Add a `watch(activeNoteContent, ..., { flush: 'sync' })` in `NoteEditor.vue` that fires **before** Vue updates the DOM. The watcher captures `wasNearBottom` using the scroll container's current geometry:

```
wasNearBottom = (scrollHeight - scrollTop - clientHeight) < 80
```

After Vue's DOM update (inside a `nextTick` chained from the same watcher, or a second watcher with default flush), if `wasNearBottom` was `true`, set `scrollContainer.scrollTop = scrollContainer.scrollHeight`. If `false`, do nothing — user keeps their position.

Threshold: 80px. Simple constant, not exposed to users.

`WysiwygEditor` does not need to emit an `append` event — `NoteEditor` already has `activeNoteContent` via `useNotes()` and can watch it directly.

---

## Components Touched

Three files. No new files except tests.

| File | Change |
|---|---|
| `web/soloforge-web/src/App.vue` | Replace `v-if` cascade with `<KeepAlive><component :is>`. Build view registry map. Scan other views for `onMounted` side effects; migrate any stragglers to `onActivated`. |
| `web/soloforge-web/src/components/journal/WysiwygEditor.vue` | Add `contentKey: string` prop. Track `lastContentKey` ref. Modify existing `watch(() => props.content, ...)`: on `contentKey` change, force `setContent`; else try append; else fall back to `setContent`. |
| `web/soloforge-web/src/components/notes/NoteEditor.vue` | Pass `:content-key="activeNotePath"` to `<WysiwygEditor>`. Add template ref on scroll container. Add sticky-scroll watcher on `activeNoteContent` (sync flush for capture + default flush for scroll). Migrate `onUnmounted(flushSave)` → `onDeactivated(flushSave)`, keep `onUnmounted(flushSave)` as backup for hard teardowns. |

Two small pure helpers extracted into a new util file `web/soloforge-web/src/components/journal/editorState.ts`:

- `isPureAppend(oldContent: string, newContent: string): boolean`
- `isNearBottom(scrollTop: number, scrollHeight: number, clientHeight: number, threshold: number): boolean`

These are imported by `WysiwygEditor` and `NoteEditor` respectively, and tested as pure functions.

---

## Data Flow

### A. User navigates Journal → Tools → Journal

**Before:** Journal component destroyed on leave, rebuilt on return — scroll at top, cursor lost, tabs reload, editor reinitialized from markdown string.

**After:** Journal component hidden but preserved in memory. On return, it is re-shown as-is. Editor instance, scroll position, cursor, open tabs all preserved. `onActivated` fires (currently a no-op; reserved for future needs like refreshing stale data).

### B. User is editing a note; a tool appends a roll result

**Before:** `useToolActions` appends markdown to `activeNoteContent`. WYSIWYG watcher calls `setContent` on the new string. Document rebuilt. Cursor resets to document start. Scroll jumps to top.

**After:**
1. Tool action appends to `activeNoteContent` (unchanged).
2. `NoteEditor`'s sync-flush watcher captures `wasNearBottom` from the scroll container's pre-update position.
3. `WysiwygEditor`'s watcher detects the prefix match and calls `insertContentAt(docEnd, suffix)`. Existing DOM nodes are untouched; only new ones are appended.
4. After DOM update, if `wasNearBottom`, `NoteEditor` sets `scrollTop = scrollHeight`.
5. If user was scrolled up, nothing scrolls.

### C. User switches between open note tabs

**Before:** `activeNoteContent` changes to the other note's content. Watcher calls `setContent` — correct behavior.

**After:** `contentKey` (active note path) changes. Watcher takes the explicit note-switch branch: forces `setContent`, updates `lastContentKey`. Correct behavior, and immune to the edge case where one note's content is a prefix of another's.

### D. Auto-journal writes to session-log note while a different note is active

Routing of auto-journal through `useNotes` is unchanged. If session-log is not the active note, the active editor is not touched. If session-log is active, the append path fires instead of the rebuild path — cursor preserved.

### E. Non-append content changes

If a future tool does a non-append write (replace, clear), the prefix match fails and the `setContent` fallback handles it. Current tools do not appear to do this; verified during implementation.

---

## Error Handling & Edge Cases

- **Empty old content, first character typed:** Prefix match is `true` for `"" → "H"`. Append path inserts `"H"` at doc end. Correct.
- **Whitespace/normalization mismatches:** If `useNotes`-stored content and `editor.getMarkdown()` produce different trailing forms, the exact prefix match fails. Falls through to `setContent` — no regression vs. today.
- **Note B's content is a prefix-extension of Note A's content:** Handled explicitly by the `contentKey` branch. Note switch always forces `setContent` regardless of string relationship.
- **Same-tick keystroke + tool append:** Vue batches reactivity. Watcher sees the net change. If the final string is `old + suffix`, append works. If the user's keystroke mutates content between captures, it falls through to `setContent`. Not worse than today.
- **Missing scroll container ref:** Guard with null check; skip the scroll step silently.
- **NaN in scroll measurements (detached element):** `isNearBottom` returns `false`. No scroll. Safe default.
- **`<KeepAlive>` memory:** Five Vue component trees in memory after full session. Estimated a few hundred KB. Non-issue for a local single-user app.
- **Stale-data side effects from `<KeepAlive>`:** Other views may have assumed fresh-mount data refresh. Scan during implementation. User-visible symptom is stale data, easy to catch and migrate to `onActivated`. Not a correctness risk on the journal path.
- **`flushSave` on both deactivated and unmounted:** `onDeactivated` handles nav switches, `onUnmounted` handles hard teardowns (campaign switch, app reload). Both registered; `flushSave` is idempotent against no-dirty state.

---

## Testing

### Unit tests — Vitest

Colocated in `web/soloforge-web/src/components/journal/__tests__/editorState.test.ts`.

**`isPureAppend(oldContent, newContent)`**
- Empty old, non-empty new → `true`.
- Old === new → `false` (no change).
- New starts with old and is longer → `true`.
- New does not start with old → `false`.
- New is shorter than old → `false`.

Note: `contentKey` disambiguation of note-switches is a stateful flow inside `WysiwygEditor` and is covered by the e2e "Note switch still replaces" test.

**`isNearBottom(scrollTop, scrollHeight, clientHeight, threshold)`**
- At exact bottom → `true`.
- Within threshold → `true`.
- Beyond threshold → `false`.
- `NaN` inputs → `false`.

### E2E tests — Playwright

New spec: `web/soloforge-web/e2e/journal-state-preservation.spec.ts`. Follows the pattern in `notes-navigation.spec.ts` — API helpers for setup/teardown, ARIA selectors for stable element targeting.

1. **Nav round-trip preserves editor state.** Open Journal, type content, place cursor mid-document, scroll down. Navigate to Tools. Navigate back to Journal. Assert: content unchanged, cursor at prior position, scroll position preserved.
2. **Append preserves cursor.** Open a note with multi-paragraph content. Place cursor in middle paragraph. Trigger a pinned-toolbar tool that appends a roll result. Assert: cursor still at the prior offset, appended content present at end.
3. **Sticky scroll — at bottom.** Scroll to bottom. Append via tool. Assert: scrolled to new bottom, new content visible.
4. **Sticky scroll — scrolled up.** Scroll to top of a long note. Append via tool. Assert: scroll position unchanged, new content off-screen at bottom.
5. **Note switch still replaces.** Open note A, note B. Switch between them. Assert: content of active tab matches the correct note (regression check for the `setContent` fallback).
6. **Save flushes on view switch.** Type into a note. Navigate to Tools before the debounce fires. Assert via API: content persisted before the nav switch. Covers the `onDeactivated(flushSave)` migration.

### Visual verification with Playwright MCP

After e2e passes, run a live pass:
- Open Journal, perform the nav round-trip, verify feel.
- Watch sticky-scroll behavior with a tool append at bottom and at top.
- Screenshot "before append" and "after append" at both positions.

Sanity check the subjective "Obsidian-feel" before marking complete. This is not pixel-perfect sign-off — it is a check that the editor continuity feels right.

---

## Acceptance Criteria

- Navigating away from and back to Journal preserves: open tabs, active note, cursor position, scroll position, editor selection.
- Appending via a tool while editing does not move the cursor from its current position.
- Appending while near the bottom auto-scrolls to the new bottom.
- Appending while scrolled up does not scroll the view.
- Switching note tabs still correctly swaps content (no regression).
- Auto-save flushes when navigating away from Journal (the view is hidden rather than unmounted, but pending saves still persist).
- All existing unit and e2e tests continue to pass.
- New unit tests for `isPureAppend` and `isNearBottom` pass.
- New e2e spec `journal-state-preservation.spec.ts` passes.
- Live visual pass confirms the editor feels continuous during nav and appends.

---

## Out of Scope / Deferred

Tracked in `2026-04-18-journal-polish-audit-plan.md`. Items discovered during implementation that are out of scope for this spec are appended there instead of expanding this spec.
