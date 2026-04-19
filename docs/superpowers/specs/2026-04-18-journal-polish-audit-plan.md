# Plan: Journal & Tools Polish Audit

**Project:** SoloForge
**Status:** Planned — to be executed in a future fresh brainstorming session
**Last updated:** 2026-04-18

---

## Purpose

Capture a structured audit of rough edges across the Journal, Tools, and related screens that make mid-session use clunky. This is the "option C" from the 2026-04-18 brainstorming session; the narrower "option A" is being implemented first as `2026-04-18-journal-state-preservation-design.md`.

This document is not a spec yet. It is a backlog + seed for a future brainstorming session, to be run in a fresh context window after option A ships.

---

## Scope

Screens in priority order (based on user's mid-session usage):

1. Journal (primary — editor, note vault, tabs, toolbar, combat panel, wiki-links, roll panels).
2. Tools page (oracles, name generator, combat tracker, workspace layout).
3. Adventure (distant third — characters, threads).

Explicitly out of scope for the audit: Dashboard and History (not primary mid-session surfaces).

---

## Process for the Audit Session

When this audit is run, it should:

1. Start with a fresh brainstorming session (new context window).
2. Review this document's **Findings** section below — items logged during the option A implementation.
3. Do a structured pass through each screen looking for rough edges in:
   - Interaction (cursor, focus, scroll, keyboard shortcuts).
   - Visual state (dirty indicators, loading states, empty states).
   - Data flow (stale state, race conditions, save conflicts).
   - Accessibility (ARIA, keyboard nav).
   - Discoverability (hidden features, unclear affordances).
4. Group findings into sub-projects small enough to each have their own spec → plan → implementation cycle.
5. Pick the highest-value sub-project first; others get their own future specs.

---

## Findings

Items logged during the option A implementation that are out of scope for that spec but worth handling in the audit. Append to this section as they surface.

Logged during the `feat/journal-state-preservation` implementation (2026-04-19):

- **Pre-existing flaky/broken e2e tests in `combat-tracker.spec.ts`.** Two tests fail reliably on `main` as well as on the feature branch when run in isolation: `HP quick-math widget adjusts HP` (:153) and `state persists across reload` (:196). Two others (`add and populate combatants`, `turn cycle increments round on wrap`) fail intermittently when run alongside other e2e files but pass in isolation. None of these are caused by the journal state preservation work — verified by running `npm run test:e2e -- combat-tracker` on `main`.
- **`web/soloforge-web/test-results/` is tracked by git** (at least `.last-run.json`) but is a generated directory. Candidate for `.gitignore` entry.
- **WysiwygEditor `insertContentAt` cursor preservation.** The Task 10 e2e test caught that `insertContentAt` defaults to `updateSelection: true`, moving the cursor to the insertion point. Fixed inline on `feat/journal-state-preservation` by passing `updateSelection: false`. Mentioning here because it's a Tiptap API detail worth remembering for any other append-style flows.
- **`onDeactivated` reads `scrollTop = 0`.** KeepAlive-detached elements have `clientHeight = 0`, which causes browsers to clamp `scrollTop` to 0. Discovered during Task 8 e2e development. Fixed by tracking scroll continuously via event listeners on the active elements and restoring on reactivation. The logic is a bit duplicated between the textarea and the WYSIWYG scroll container — could be DRY'd into a small helper in a future pass.
- **Vue import of `onMounted`/`onUnmounted` no longer used in `NoteEditor.vue`.** Cleaned up during Task 5 lifecycle migration; document here as a reminder that `<KeepAlive>` changes the lifecycle contract of every child component, and future additions to the file should default to `onActivated`/`onDeactivated` unless there is a specific reason to use mount/unmount.

---

## Known Candidate Areas (seed list, not exhaustive)

These are hypotheses from the 2026-04-18 brainstorming scan; treat them as starting points for the audit, not conclusions:

- Wiki-link UX: autocomplete behavior, broken-link handling, navigation feedback, backlinks.
- Roll panel enhanced/collapsed toggle quirks and re-roll flow.
- Paste behavior (HTML, tables, rolls from other sources).
- Tab bar overflow and ordering behavior when many notes are open.
- Sidebar tree: rename/delete/nesting edge cases, conflict handling.
- Auto-save feedback: dirty indicator clarity, save conflicts, recovery from failed saves.
- Search across notes (currently missing — is it worth adding in the audit scope?).
- Insert-at-cursor from Tools page (vs. pinned toolbar, which already works).
- Combat tracker panel: resize persistence, data sync with Tools page instance, narrow-width layout.
- Inline dice roll from journal (quick macros, saved rolls).
- Sidebar collapse state / focus mode toggle.
- Mobile / narrow window layout.
- Keyboard shortcuts (mode toggle exists, others may be missing).
- Last event / result banner interaction with journal scrolling.
