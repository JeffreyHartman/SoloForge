# PRD: Combat / Initiative Tracker
**Project:** SoloForge  
**Feature:** Simple Initiative Tracker  
**Status:** Ready for development  
**Last updated:** 2026-03-16

---

## Overview

Solo tabletop RPG sessions break down during combat. The player has to context-switch out of their journal to manage turn order, track HP, and monitor conditions — often jumping between separate browser tabs or paper notes. This feature brings a system-agnostic combat tracker into SoloForge so the player stays in one tool for the entire session.

---

## Goals

- Allow the player to track a full combat encounter (participants, initiative, HP, AC, conditions, turn order) without leaving SoloForge.
- Keep the tracker system-agnostic. No rules assumptions, no stat lookups. It is a digital version of a paper tracker.
- Surface the tracker in two places: as a standalone tool on the Tools page, and as a collapsible panel on the Journal page so it can be open alongside active session notes.
- Persist tracker state across reloads so an interrupted session can be resumed without data loss.

---

## Implementation Boundary

This feature lives entirely in the web frontend project (`web/soloforge-web`). Do not create any new API endpoints, C# models, or backend services. The tracker state is stored in local browser storage only. The only existing backend integration is the character name autocomplete, which should use whatever existing frontend service already queries the Adventure/Characters data — no new API work required for that either.

---

## Out of Scope (this version)

- Dying / death saves mechanic. Players will manage this manually in session.
- Stat block import from notes.
- Encounter saving / loading by name.
- NPC behavior oracle integration.
- Any system-specific rules (D&D, Ironsworn, etc.).
- Keyboard shortcuts (cursor may be active in journal text area).

---

## Where It Lives

### Tools Page
Add a new section to the Tools page sidebar called **Trackers**. The first entry is **Simple Initiative Tracker**. It opens the full-featured tracker as a primary view, the same way existing tools like Fate Check and Meaning work today.

### Journal Page — Right Panel
Add a **Combat** toggle button to the journal tab bar (alongside existing controls in that area). Clicking it opens a resizable right-side panel containing a compact version of the tracker. The journal content area shrinks to accommodate the panel.

**Panel behaviour:**
- When the combat panel opens, the notes file browser sidebar should auto-collapse to give the journal and tracker enough room. The user can manually re-expand the notes sidebar if they want.
- When the combat panel closes, the notes sidebar should restore to its previous state.
- The panel is resizable via a drag handle on its left edge (the boundary between journal and tracker).
- The panel state (open or closed) does not need to persist across page loads. It opens closed by default each session.
- The tracker data inside the panel is the same persisted state as the Tools page tracker — they share one instance of the data.

---

## Combatant Data Model

Each combatant row holds:

| Field | Notes |
|---|---|
| Type | PC or NPC/Enemy. Controls row colour. |
| Initiative | Integer. Nullable until entered or rolled. |
| Name | Free text. |
| Current HP | Integer. |
| Max HP | Integer. |
| AC | Integer. Nullable. |
| Conditions | Free text. No structured tags for now. |
| Status | active or dead. See Status section below. |

---

## Features

### Adding Combatants
- An **Add Combatant** button appends a new empty row.
- New rows default to NPC/Enemy type.
- The **Name field** supports live autocomplete against the Characters list from the Adventure screen. As the user types, matching character names are suggested. Selecting a suggestion fills the name. For now this only fills the name — no stat block import.

### PC vs NPC Distinction
- Each row has a type toggle: **PC** or **NPC**.
- PC rows are visually distinguished from NPC rows via row colour (exact colour is the developer's call to match the app theme — the requirement is that the two types are visually distinct at a glance).
- No functional difference between the two types in this version.

### Initiative
- Each row has an initiative input field.
- A dice icon next to the field rolls a d20 for that individual combatant and fills the field.
- A **Roll All** button rolls d20 for every combatant in one action.
- A **Sort** button reorders rows highest-to-lowest by initiative value. Rows with no initiative value go to the bottom.

### HP — Current and Max
- HP is displayed as `current / max` (e.g. `24 / 28`).
- Clicking into the current HP field shows a quick-math widget: a minus button, an adjustable amount field (default 1), and a plus button. This allows fast damage/healing without typing. The current HP field remains directly editable as a normal number input.
- The quick-math widget closes when focus leaves the HP area.
- Current HP can go below 0 via the widget (the player may have reasons to enter a negative number in some systems).
- When current HP reaches 0 (via widget or direct input), the combatant's status automatically changes to **dead**.

### Combatant Status
Two states only: **active** and **dead**.

- **Active:** Normal row appearance.
- **Dead:** Row is dimmed, name has strikethrough, a skull icon is shown. Dead rows are automatically skipped when advancing turns.
- The player can manually toggle a combatant to dead at any time using the skull icon, regardless of HP value. This covers cases where the player wants to manually remove someone from the turn order mid-fight.
- Toggling dead off returns the combatant to active. HP is not changed by toggling status.

### Row Ordering and Drag Reorder
- Rows have a drag handle on the far left (before all other columns).
- The player can drag rows to reorder them manually at any time. This is independent of the Sort button — sort is a one-time action, drag is manual override.

### Turn Tracking
- One row is highlighted as the **active turn**.
- A **Next** button advances to the next active (non-dead) row.
- A **Back** button goes to the previous active row.
- When Next advances past the last row, it wraps to the top and increments the round counter by 1.
- When Back goes before the first row, it does nothing (does not decrement round or wrap).
- The round counter is displayed prominently. The player can click the round number to edit it directly (override). This should be low-profile — not a prominent edit button, just clicking the number itself makes it editable.
- Turn tracking does not start until the player clicks Next for the first time. Before that, no row is highlighted.

### Clear
- A **Clear** button wipes all combatants and resets the round to 1.
- Clicking Clear shows a confirmation prompt before proceeding.

### Persistence
- All tracker state (combatants, round number, current turn) is saved to local browser storage.
- State persists across page reloads and app restarts.
- State is only cleared when the user explicitly clicks Clear and confirms.

---

## Compact Panel Version (Journal)

The right-panel version of the tracker is the same feature, not a stripped-down rebuild. It should reuse the same underlying component(s). The differences are presentational only:

- The Conditions column may be hidden or truncated by default at small panel widths to keep the table readable. The player can widen the panel via the drag handle to reveal it.
- The Add Combatant button and other controls are present but may be more compact.
- No separate data store — the panel shows the same combat state as the Tools page tracker.

---

## What We Are Not Deciding Here

The developer should use their judgement on:
- Exact colours for PC vs NPC rows and active turn highlight, as long as they are consistent with the existing app theme.
- Whether the quick-math HP widget is a popover, inline expansion, or tooltip-style — whichever is cleanest given the existing component patterns.
- Exact placement of the Combat toggle button in the journal tab bar area.
- Internal component architecture and whether this is one component or several.

---

## Testing Requirements

Testing is not optional and should not be left until the end. Write tests alongside the implementation.

### Unit Tests — Vitest

Cover the core tracker logic as pure functions or composables, isolated from the UI. At minimum:

- **Initiative rolling** — rolling a single combatant produces a value between 1 and 20; Roll All updates every combatant.
- **Sort** — correctly orders by initiative descending; combatants with no initiative go to the bottom.
- **Turn advancement** — Next skips dead combatants; wrapping past the last row increments the round; Back does not decrement below round 1.
- **HP logic** — Allow hitpoints to go below 0 (negative numbers allowed to account for dying mechanics); status auto-sets to dead at 0; toggling status does not modify HP.
- **Persistence** — state written to localStorage can be read back and reconstructs correctly.
- **Clear** — clears all combatants and resets round to 1.

### E2E Tests — Playwright

Cover the full user-facing flows in the browser. At minimum:

- **Add and populate combatants** — add two combatants, fill in name, HP, AC, and conditions, verify they appear correctly.
- **Turn cycle** — add three combatants, click Next through all of them, verify round increments to 2 on wrap.
- **Dead skip** — mark the second combatant as dead, verify Next jumps from first directly to third.
- **HP quick-math** — click into an HP field, use the minus button to reduce HP, verify the displayed value updates.
- **Drag reorder** — drag the bottom row to the top, verify the new order is reflected.
- **Persist on reload** — add combatants, reload the page, verify state is restored.
- **Clear with confirmation** — click Clear, cancel the prompt and verify data is intact; click Clear again and confirm, verify tracker is empty.
- **Journal panel toggle** — on the Journal page, click the Combat toggle, verify the panel opens; verify the notes sidebar auto-collapses; click the toggle again and verify the panel closes and the notes sidebar is restored.

### Visual Verification with Playwright MCP

After the E2E tests pass, use the Playwright MCP to do a live visual pass. Launch the dev server, open the tracker in the browser, take screenshots of:

- The tracker with a mixed set of PC and NPC combatants, one dead, one active turn highlighted.
- The Journal page with the combat panel open alongside journal content.
- The combat panel at a narrow width to verify the table degrades gracefully.

Review the screenshots and confirm the visual result matches the intent before marking the feature complete. This is not a pixel-perfect sign-off — it is a sanity check that the layout is not broken, colours are correct, and the tracker is usable at realistic panel widths.

---

## Acceptance Criteria

- A player can add combatants, assign initiative, HP, AC, and conditions without leaving the journal.
- Clicking Next correctly skips dead combatants and increments the round on wrap.
- Rolling and sorting initiative works correctly.
- HP quick-math correctly adds/subtracts from current HP and auto-sets dead at 0.
- Dragging rows reorders them correctly and the new order persists.
- Clearing with confirmation wipes all data.
- State survives a page reload.
- The right panel opens and closes from the journal without navigating away.
- Opening the right panel collapses the notes sidebar; closing it restores the notes sidebar.
- The panel is resizable via its left drag handle.
- The Tools page tracker and Journal panel tracker reflect the same data.
