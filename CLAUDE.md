# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Build & Run Commands

```bash
# Build the project
dotnet build

# Run the application
dotnet run --project src/SoloForge.Console

# Clean build artifacts
dotnet clean
```

No automated tests exist yet. Manual testing through the console UI.

## Project Overview

SoloForge is a .NET 10.0 console application implementing the **Mythic Game Master Emulator 2nd Edition** - a solo role-playing game assistant using randomization and oracle mechanics.

## Architecture

### Screen Controller Pattern
All UI screens implement `IScreen` with a `Run()` method that returns the next screen or null. `BaseScreen` provides shared functionality (header rendering, key input, clipboard, quick rolls). Navigation flows by returning screens from `Run()`.

### Dependency Injection
- **Singletons**: `Session`, `AdventureStateManager`, `HistoryService`, `CampaignService`, `ClipboardService`, `TemplateService`, `JournalService`
- **Transients**: All Screen types

### Static Engine Classes
`FateCheck`, `RandomEvent`, `SceneCheck`, `MeaningEngine` in `Engines/Mythic2e/` are pure static utility classes with game logic. They use `Random.Shared` and have no UI dependencies.

### Key Directories
- `Core/` - IScreen interface and BaseScreen
- `Engines/Mythic2e/` - Pure game logic (fate chart, random events, scene checks)
- `Models/` - Immutable record types for results and data
- `Services/` - Application services (persistence, state, clipboard)
- `Screens/` - UI screens implementing IScreen
- `UI/` - MythicUi static factory for Spectre.Console components
- `data/` (repo root) - Game tables auto-discovered at runtime

### Data & Persistence
- Campaigns saved as JSON in `saves/` with markdown journals
- `CampaignService` orchestrates all persistence
- `TableService` auto-discovers `.txt` files in `data/` and `data/elements/`

## Code Conventions

- **C# 14**: Primary constructors, collection expressions `[]`, `field` keyword
- **Nullable reference types** enabled
- **Records** with init-only properties for immutable result types
- **Serilog** structured logging via `AppLogger` static helper
- **Spectre.Console** for rich terminal UI

## Key Files

- `Program.cs` - DI setup, logging init, main screen loop
- `Core/BaseScreen.cs` - Common screen functionality, Session class
- `Services/CampaignService.cs` - Campaign persistence
- `UI/MythicUi.cs` - Standardized UI component factory
