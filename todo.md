# SoloForge - Project Status & Roadmap

## Implemented Features

### Core Oracle System
- [x] Fate Check with full Mythic 2e fate chart
- [x] Odds selection (Impossible to Certain)
- [x] Chaos Factor integration (1-9 scale)
- [x] Random Event detection on doubles

### Random Events
- [x] Event Focus table (d100)
- [x] Action word pair generation (Action 1 + Action 2)
- [x] Integration with Fate Check triggers
- [x] Character/Thread selection for relevant focus types
- [x] New NPC quick-add prompt

### Scene Management
- [x] Scene Check with chaos factor comparison
- [x] Scene interruption detection (random event trigger)
- [x] Scene alteration detection (adjustment roll)
- [x] Scene Adjustment table

### Discovering Meaning System
- [x] Dynamic table discovery from data/ directory
- [x] Cached table loading via TableService
- [x] Quick Rolls (Action, Description)
- [x] Searchable Element Browser (45+ tables)
- [x] Fusion Roll (combine two different tables)
- [x] NPC Profile preset (8 character attributes)

### Adventure Lists & Tracking
- [x] Character list management (add, edit, remove)
- [x] Thread/Plot list management (add, edit, close, remove)
- [x] Closed threads archive with reopen capability
- [x] Random Event integration (selects from lists)
- [x] Session header displays character/thread counts
- [x] JSON-serializable data model for future save/load

### User Interface
- [x] Spectre.Console rich terminal UI
- [x] Main menu with hotkey support
- [x] Session panel (Engine, Theme, Chaos Factor, Lists)
- [x] Chaos Factor adjustment via +/- keys
- [x] Consistent session header across screens
- [x] Configurable figlet titles on subpages (appsettings.json)
- [x] Centered panels with minimum widths

## In Progress

### Current Sprint
- [ ] Dice Roller implementation
- [ ] Settings menu (theme selection, chaos presets)

## Planned Features

### High Priority (Next)
- [x] Save/Load System (JSON persistence)
- [x] Campaign/Session persistence
- [ ] Allow users to define custom Meaning Quick-Sets via configuration

### Near-Term
- [ ] Scene list with status tracking
- [x] Adventure log/journal
- [ ] Custom table support (user-defined .txt files)

### Medium-Term
- [ ] UNE (Universal NPC Emulator) integration
- [x] Export to markdown
- [x] Session history

### Long-Term / Experimental
- [ ] AI Integration via OpenRouter API
- [ ] Local LLM support (Ollama/Llama.cpp)
- [ ] AI-assisted scene interpretation
- [ ] AI-generated NPC dialogue

## Technical Debt
- [ ] Add unit tests for core engines
- [x] Refactor Program.cs into separate UI components
- [x] Implement proper dependency injection

## Notes
- Data files located in `data/` directory
- Element tables in `data/elements/` subdirectory
- Adding new .txt files to elements folder auto-discovers them at runtime
- AdventureState model is designed for easy JSON serialization
- C# 14 features used: primary constructors, collection expressions, field keyword
