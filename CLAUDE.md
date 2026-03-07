# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## What is SoloForge

SoloForge is a solo tabletop RPG toolkit implementing the Mythic 2e game master emulator. It has three interfaces: a Terminal.Gui console app, a .NET Web API backend, and a Vue 3 web frontend.

## Build and Run

```bash
# Build all projects (solution file: SoloForge.slnx)
dotnet build

# Run the Terminal.Gui console app
dotnet run --project src/SoloForge.Console

# Run the API backend (defaults to http://localhost:5137)
dotnet run --project src/SoloForge.Api

# Run API + Vue frontend together (API on :5137, Vite dev server on :5173)
./start-dev.sh

# Vue frontend only (from web/soloforge-web/)
npm --prefix web/soloforge-web run dev
```

## Tests

```bash
# Run all tests (xunit + FluentAssertions + Moq)
dotnet test

# Run a single test
dotnet test tests/SoloForge.Console.Tests --filter "FullyQualifiedName~Namespace.Class.Method"
```

Test project: `tests/SoloForge.Console.Tests` — references `SoloForge.Core`.

## Lint / Format

No dedicated formatter configured. Treat `dotnet build` warnings as lint signal.

## Project Structure

```
SoloForge.slnx                  # Solution (slnx format)
src/
  SoloForge.Core/               # Shared library: engines, models, services (no UI)
  SoloForge.Console/            # Terminal.Gui desktop app (references Core)
  SoloForge.Api/                # ASP.NET minimal API backend (references Core)
web/soloforge-web/              # Vue 3 + Vite + Tailwind CSS frontend
tests/
  SoloForge.Console.Tests/      # xunit tests (references Core)
data/                           # Word tables (.txt), quicksets.json, themes.json
```

All projects target **net10.0**.

## Architecture

### SoloForge.Core (shared library)
The engine and service logic lives here, shared by both the Console and Api projects:
- **Engines/Mythic2e/**: Pure game logic — `FateCheck`, `SceneCheck`, `RandomEvent`, `MeaningEngine`, `Odds`. No UI, deterministic except for `Random.Shared`.
- **Models/**: DTOs and records — `CampaignData`, `LogEntry`, `AdventureState`, `DiceExpression`, result types.
- **Services/**: `CampaignService`, `HistoryService`, `JournalService`, `TableService`, `QuickSetService`, `DiceRoller`, `AppLogger` (Serilog), `SettingsService`. Many use the singleton pattern (`Lazy<T>` + `.Instance`).
- **Core/**: `Session` — mutable session state (chaos factor, engine, theme).

### SoloForge.Console
Terminal.Gui desktop app. `Program.cs` wires services and runs `SoloForgeApp`. Views in `Views/` build UI in constructors. Console-only services: `ThemeService`, `ClipboardService`.

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

## Data and Templates

- `TableService` auto-discovers `.txt` tables in `data/` and `data/elements/` (case-insensitive IDs)
- `TemplateService` loads markdown from `templates/`; placeholders use `{Field}` and `{?Field}...{/Field}`
- Campaign state persisted as JSON in `saves/`; models must stay backward-compatible (add fields with defaults)
