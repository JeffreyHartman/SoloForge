# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## What is SoloForge

SoloForge is a solo tabletop RPG toolkit implementing the Mythic 2e game master emulator. It has two interfaces: a .NET Web API backend and a Vue 3 web frontend.

## Build and Run

```bash
# Build all projects (solution file: SoloForge.slnx)
dotnet build

# Run the API backend (defaults to http://localhost:5137)
dotnet run --project src/SoloForge.Api

# Run API + Vue frontend together (API on :5137, Vite dev server on :5173)
./start-dev.sh

# Vue frontend only (from web/soloforge-web/)
npm --prefix web/soloforge-web run dev
```

## Tests

```bash
# Run all .NET tests (xunit + FluentAssertions + Moq)
dotnet test

# Run a single .NET test
dotnet test tests/SoloForge.Core.Tests --filter "FullyQualifiedName~Namespace.Class.Method"

# Run frontend unit tests (Vitest + jsdom)
npm --prefix web/soloforge-web run test

# Run frontend unit tests in watch mode
npm --prefix web/soloforge-web run test:watch

# Run frontend unit tests with coverage
npm --prefix web/soloforge-web run test:coverage

# Run Playwright e2e tests (from web/soloforge-web/)
# Requires both API and Vite dev server — start-dev.sh is launched automatically
npm --prefix web/soloforge-web run test:e2e
```

- .NET test project: `tests/SoloForge.Core.Tests`
- Frontend unit tests: `web/soloforge-web/src/**/__tests__/` (Vitest + jsdom)
- E2E test directory: `web/soloforge-web/e2e/` (Playwright + Chromium)

## Lint / Format

No dedicated formatter configured. Treat `dotnet build` warnings as lint signal.

## Project Structure

```
SoloForge.slnx                  # Solution (slnx format)
src/
  SoloForge.Core/               # Shared library: engines, models, services (no UI)
  SoloForge.Api/                # ASP.NET minimal API backend (references Core)
web/soloforge-web/              # Vue 3 + Vite + Tailwind CSS frontend
tests/
  SoloForge.Core.Tests/         # xunit tests (references Core)
data/                           # Word tables (.txt), quicksets.json, themes.json
templates/                      # Markdown templates for log entry rendering
```

All projects target **net10.0**.

## Architecture

### SoloForge.Core (shared library)
The engine and service logic lives here, consumed by the Api project:
- **Engines/Mythic2e/**: Pure game logic — `FateCheck`, `SceneCheck`, `RandomEvent`, `MeaningEngine`, `Odds`. No UI, deterministic except for `Random.Shared`.
- **Models/**: DTOs and records — `CampaignData`, `LogEntry`, `AdventureState`, `DiceExpression`, result types.
- **Services/**: `CampaignService`, `HistoryService`, `JournalService`, `TableService`, `QuickSetService`, `DiceRoller`, `AppLogger` (Serilog). Many use the singleton pattern (`Lazy<T>` + `.Instance`).
- **Core/**: `Session` — mutable session state (chaos factor, engine, theme).

### SoloForge.Api
ASP.NET minimal API. All routes defined in `Program.cs` as `app.Map*` calls. Services registered via DI. Proxied by the Vite dev server at `/api`.

### Web Frontend (web/soloforge-web)
Vue 3 + TypeScript + Tailwind CSS 4 + Vite. Composables in `src/composables/` wrap API calls. Views in `src/views/`, components organized by feature in `src/components/`.

### Data Flow
Engine calls produce result records -> `HistoryService.AddEntry` logs them -> `JournalService` appends to the campaign journal -> `CampaignService.Save()` persists to `saves/`.

## C# Conventions

- File-scoped namespaces, `sealed` services, `record` for immutable types
- `_camelCase` private fields, PascalCase public members, `var` when type is obvious
- Braces on new lines, 4-space indentation
- `System.Text.Json` with camelCase policy for persistence
- `Random.Shared` for all RNG in engine classes
- `AppLogger.ForContext<T>()` for structured Serilog logging
- `AppContext.BaseDirectory` for runtime path resolution, `Path.Combine` for paths
- Guard clauses for parameter validation
- Singleton pattern: `private static readonly Lazy<T> _instance` + `Instance`
- Using directives ordered: System, third-party, then SoloForge

## Vue / Frontend Conventions

- Always include appropriate ARIA attributes (`aria-label`, `aria-pressed`, `aria-expanded`, `role`, `tabindex`, etc.) on interactive elements — buttons, toggles, selects, custom controls. Low effort, keeps the app accessible.
- Sanitize all `v-html` bindings with DOMPurify
- Use CSS custom properties (`var(--color-*)`) for theming, not Tailwind `dark:` variants

## Frontend Unit Testing (Vitest)

Tests live in colocated `__tests__/` directories next to source files. Config: `web/soloforge-web/vitest.config.ts`.

**Test locations:**
- `web/soloforge-web/src/composables/__tests__/` — composable unit tests
- `web/soloforge-web/src/components/journal/tiptap/__tests__/` — Tiptap utility tests

**When to add unit tests:**
- Any new composable with business logic (validation, data transformation, state management)
- Pure utility functions (formatters, parsers, encoders)
- Bug fixes in composable logic — add a regression test

**When NOT to unit test (use e2e instead):**
- Thin API wrappers with no branching logic (e.g., `useCampaign`, `useHistory`, `useAdventure`)
- Vue component rendering behavior
- Multi-composable orchestration (e.g., `useToolActions`)

**How to write them:**
- Use explicit imports: `import { describe, it, expect } from 'vitest'`
- For composables with module-level singleton state (refs declared outside the function), use `vi.resetModules()` + dynamic `await import(...)` in `beforeEach` to isolate tests
- For localStorage-dependent composables, create a mock storage with `vi.stubGlobal('localStorage', mockStorage)` before the dynamic import
- For timer-based composables (debounce, auto-dismiss), use `vi.useFakeTimers()` and `vi.advanceTimersByTimeAsync()`
- For API-dependent composables, mock with `vi.mock('../useApi')` and `vi.mocked(apiSend).mockResolvedValue(...)`
- Vue `ref()`/`computed()` work standalone in Vitest without a Vue app instance — no `@vue/test-utils` mount needed for composable tests

## E2E Testing (Playwright)

Tests live in `web/soloforge-web/e2e/`. Config: `web/soloforge-web/playwright.config.ts`.

**When to add e2e tests:**
- Any bug fix involving async behavior, race conditions, or UI state that depends on timing
- New features involving multi-step user interactions (tab switching, navigation, save/load flows)
- Regressions that are hard to catch with unit tests alone (content syncing between editor modes, auto-save)

**How to write them:**
- Use API helpers (`page.request.post/get/delete`) for test setup/teardown — faster than UI interactions
- Use ARIA selectors (`role`, `aria-label`, `aria-selected`) for element targeting — they're stable and already present on all interactive elements
- For race condition tests, use `page.route()` to intercept and delay API responses to simulate slow networks
- Keep tests in `Edit` mode (textarea) unless specifically testing WYSIWYG behavior — textarea assertions are simpler and more reliable
- See `e2e/notes-navigation.spec.ts` as the reference pattern for test structure, helpers, and setup/teardown

## Data and Templates

- `TableService` auto-discovers `.txt` tables in `data/` and `data/elements/` (case-insensitive IDs)
- `TemplateService` loads markdown from `templates/`; placeholders use `{Field}` and `{?Field}...{/Field}`
- Campaign state persisted as JSON in `saves/`; models must stay backward-compatible (add fields with defaults)
