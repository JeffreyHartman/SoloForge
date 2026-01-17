# AGENTS.md

Guidance for agentic coding assistants working in this repository.
Follow these conventions to keep changes consistent and minimal.

## Project Snapshot
- SoloForge is a .NET console application using Terminal.Gui.
- Main app lives in `src/SoloForge.Console` and targets .NET 10.0.
- Runtime data: `data/` word tables, `templates/` markdown snippets.
- User state: `saves/` and `logs/` are generated/ignored.
- Randomization uses `Random.Shared` in engine classes.
- Services coordinate persistence and history updates.

## Build and Run
- Build all projects: `dotnet build`
- Run the console UI: `dotnet run --project src/SoloForge.Console`
- Clean build outputs: `dotnet clean`
- Release build: `dotnet build -c Release`
- If you change data/templates, rebuild is not required.

## Tests
- There are no test projects in this repo yet.
- When tests are added, run all tests with `dotnet test`.
- Run a single test (once a test project exists):
  `dotnet test path/to/Project.Tests.csproj --filter "FullyQualifiedName~Namespace.Class.Method"`
- Alternative filter example: `--filter "DisplayName~TestName"`
- Manual validation today is by running the console UI.

## Lint / Format
- No dedicated linting or formatter is configured in the repo.
- Treat `dotnet build` warnings as lint signal.
- If you install `dotnet-format`, use:
  `dotnet format src/SoloForge.Console/SoloForge.Console.csproj`
- Do not introduce new tooling without discussion.

## Layout and Architecture
- `App/` contains `SoloForgeApp`, the main Terminal.Gui window.
- `Views/` contains UI views; each view builds UI in its constructor.
- `Services/` handles persistence, history, settings, logging.
- `Engines/Mythic2e/` contains pure game logic (no UI).
- `Models/` contains DTOs and record types used across services.
- `Core/` currently holds shared session state types.

## C# Conventions
- Use file-scoped namespaces (one per file).
- Prefer `record` for immutable data and result types.
- Use `init` setters for immutable properties; `set` for mutable state.
- Use `required` for mandatory model fields.
- Favor `sealed` for services and singleton classes.
- Singleton pattern: `private static readonly Lazy<T> _instance` + `Instance`.
- Use collection expressions `[]` for list initialization.
- Prefer `var` when the type is obvious from the RHS.
- Use explicit types for public APIs and when clarity matters.
- Keep using directives ordered: System, third-party, then SoloForge.
- Keep braces on new lines; indent with 4 spaces.
- Keep expressions short; extract helpers for complex logic.
- Favor guard clauses to validate parameters early.

## Naming
- PascalCase for public types, members, and enums.
- camelCase for local variables and parameters.
- _camelCase for private fields.
- Use descriptive names (avoid single-letter variables).
- Enum display names live in extension methods (see `OddsExtensions`).

## Error Handling
- Throw `ArgumentException`/ `ArgumentNullException` for bad inputs.
- Throw `InvalidOperationException` for invalid state transitions.
- Wrap file IO with try/catch; log warnings via `AppLogger`.
- Avoid swallowing exceptions silently unless a fallback is intentional.
- Log with structured templates: `_log.Warning(ex, "... {Path}", path)`.

## Logging
- Use `AppLogger.ForContext<T>()` for service-level logging.
- Use `AppLogger.Logger` for app-wide messages (startup/shutdown).
- Prefer structured logging over string concatenation.
- Avoid logging in tight UI loops.

## UI Guidelines (Terminal.Gui)
- Views inherit from `View` (or `Toplevel` for the main app).
- Build controls in the view constructor or a `BuildUI` method.
- Wire actions with `.Accepting` or `OpenSelectedItem`.
- After layout changes, call `SetNeedsLayout()`.
- Keep label text and status lines short and readable.
- Use `ColorScheme` for emphasis, not hard-coded ANSI.

## Extending Views
- Keep view state private and reset on rerender.
- Use `FrameView` titles to match the active screen name.
- Use `TextField` and `TextView` for user input, set focus explicitly.
- For modal flows, use `Dialog` and exit with `Application.RequestStop()`.
- Dispose replaced views when swapping content panes.
- Update `SessionInfoBar.Refresh()` after session changes.

## Services and Singletons
- Services are instantiated in `Program.cs` and passed down.
- Avoid new global state unless it follows existing singleton patterns.
- Keep IO side effects inside services, not views.
- Use `Lazy<T>` for caches and singleton initialization.
- Prefer `IReadOnlyList`/ `IEnumerable` for exposed collections.

## Engine Logic
- Engine classes should stay UI-free and deterministic aside from RNG.
- Keep helper methods private and static where possible.
- Use `Random.Shared` for dice/event rolls.
- Validate input ranges (e.g., chaos factor 1-9).
- Favor readable tables/arrays over magic numbers in code.

## Session, History, and Persistence
- Update `Session` for chaos/engine/theme changes.
- Append history via `HistoryService.AddEntry`.
- Update the journal via `JournalView.AppendEntry` after changes.
- Persist state by calling `CampaignService.Save()` after edits.
- Do not write to `saves/` in unit tests.

## Data and Templates
- `TableService` auto-discovers `.txt` tables in `data/` and `data/elements`.
- Table IDs are case-insensitive; preserve file names.
- `TemplateService` loads markdown files from `templates/`.
- Template placeholders use `{Field}` and `{?Field}...{/Field}`.
- Keep templates free of hard-coded UI logic.

## JSON and Serialization
- Use `System.Text.Json` for persistence.
- Match `CampaignService`'s `JsonSerializerOptions` (camelCase, indented).
- Keep models backward-compatible; add fields with defaults.

## Configuration
- `appsettings.json` is optional; code should handle missing files.
- Feature flags live under `Features` in settings.
- Don't hardcode file paths; prefer `Path.Combine`.
- Use `AppContext.BaseDirectory` for runtime root discovery.

## Dependencies
- NuGet packages are listed in `SoloForge.Console.csproj`.
- Update package versions deliberately and note breaking changes.
- Avoid adding heavy UI libraries beyond Terminal.Gui.

## Git Hygiene
- Do not commit `logs/`, `saves/`, `bin/`, or `obj/`.
- Keep data files and templates in plain text.
- Avoid renaming public types without updating all usages.

## Cursor / Copilot Rules
- No `.cursor/rules`, `.cursorrules`, or `.github/copilot-instructions.md` found.

## When in Doubt
- Mirror existing patterns in nearby files.
- Keep changes minimal and focused on the request.
- Ask the user when behavior or UX is ambiguous.
