# SoloForge Application Analysis

## Current State Summary

SoloForge is a .NET console application implementing the **Mythic Game Master Emulator 2nd Edition** system. It's a solo role-playing game assistant that uses randomization and oracle mechanics to generate adventures without a human GM.

**Current Status**: ~40% feature complete with core mechanics implemented

---

## ✅ IMPLEMENTED FEATURES

### 1. **Fate Check System** (Complete)
- Full 9x9 Fate Chart (9 odds levels × 9 chaos factors)
- Odds levels: Impossible → Nearly Impossible → Very Unlikely → Unlikely → 50/50 → Likely → Very Likely → Nearly Certain → Certain
- Exceptional Yes/No detection with special threshold handling
- Random Event triggering on doubles (11, 22, 33, etc.) when digit ≤ Chaos Factor
- d100 roll mechanics with proper threshold comparison

### 2. **Random Event System** (Mostly Complete)
- Event Focus Table with 12 categories (1d100):
  - Remote Event, Ambiguous Event, New NPC, NPC Action, NPC Negative/Positive
  - Move Toward/Away from Thread, Close Thread, PC Negative/Positive, Current Context
- Action generation combining two word lists (action1.txt + action2.txt)
- Proper file loading with fallback paths

### 3. **Scene Check System** (Complete)
- d10 roll mechanics
- Three scene types: Normal Scene, Altered Scene, Interrupt Scene
- Scene Adjustment Table (10 entries) for altered scenes
- Random Event integration for interrupt scenes

### 4. **UI/Menu System** (Complete)
- Main menu with 7 options (Fate Check, Random Event, Scene Check, NPC Generator, Dice Roller, Settings, Quit)
- Hotkey support (F, R, C, N, D, S, Q)
- Number key shortcuts (1-5)
- Session panel showing: Engine, Theme, Chaos Factor
- Chaos Factor adjustment (+/- controls)
- Spectre.Console integration for rich terminal UI

---

## ❌ NOT YET IMPLEMENTED

### Core Mythic 2e Features Missing:

1. **Meaning Tables** (Critical)
   - Action Descriptors (100 entries)
   - Descriptor 1 & 2 (100 entries each)
   - Element Meanings: Locations, Characters, Objects, etc.
   - Used to interpret Random Events and generate details

2. **NPC Generator** (High Priority)
   - NPC Statistics Table (for generating NPC attributes)
   - NPC Behavior Table (for determining NPC actions)
   - Character creation and management

3. **Adventure Lists & Threads** (High Priority)
   - Characters List (for tracking NPCs)
   - Threads List (for tracking goals/missions)
   - List management UI
   - Random selection from lists when needed

4. **Fate Check Modifiers** (Medium Priority)
   - Fate Check Answer modifiers
   - Conditional modifiers based on context

5. **Advanced Scene Features** (Medium Priority)
   - Keyed Scenes (prepared scene templates)
   - Adventure Journal (session logging)
   - Thread Progress Tracking
   - Scene structure guidance

6. **Dice Roller** (Low Priority)
   - Generic dice rolling (dX notation)
   - Multiple dice support
   - Modifier support

7. **Settings/Preferences** (Low Priority)
   - Theme selection
   - Engine selection
   - Chaos flavor variants (Low/Mid/No-Chaos charts)
   - Save/load preferences

---

## Architecture Overview

**Project Structure**:
```
src/SoloForge.Console/
├── Program.cs (Main app, UI, menu handling)
├── Models/ (Data structures)
│   ├── FateCheckResult.cs
│   ├── RandomEventResult.cs
│   └── SceneCheckResult.cs
├── Engines/Mythic2e/ (Game logic)
│   ├── FateCheck.cs
│   ├── RandomEvent.cs
│   ├── SceneCheck.cs
│   └── Odds.cs
└── Tests/ (Test directory - appears empty)

data/
├── mythic2e.md (Complete 2e rulebook)
├── action1.txt (100 action verbs)
└── action2.txt (100 action nouns)
```

**Key Design Patterns**:
- Static utility classes for game mechanics (FateCheck, RandomEvent, SceneCheck)
- Record types for immutable result objects
- Enum for Odds with extension methods for display names
- Spectre.Console for rich terminal rendering

---

## Next Steps for Full Parity

**Priority 1 (Core Gameplay)**:
1. Implement Meaning Tables system
2. Build Adventure Lists/Threads management
3. Create NPC Generator with Statistics & Behavior tables

**Priority 2 (Enhanced Features)**:
4. Add Fate Check Modifiers
5. Implement Keyed Scenes
6. Build Adventure Journal

**Priority 3 (Polish)**:
7. Dice Roller
8. Settings/Preferences
9. Save/Load functionality

---

## Detailed Feature Breakdown

### Meaning Tables (Pages 47-106 in mythic2e.md)

Critical for interpreting Random Events. Provide narrative inspiration:

**ACTION DESCRIPTORS** (100 entries)
- Adverbs describing HOW something happens
- Examples: Adventurously, Aggressively, Anxiously, Beautifully, Boldly, etc.

**DESCRIPTOR 1 & 2** (100 entries each)
- Adjectives for appearance/quality
- Examples: Abnormal, Amusing, Artificial, Beautiful, Bizarre, Broken, etc.

**ELEMENT MEANINGS** (Multiple tables)
- Locations, Characters, Objects, Emotions, etc.

### Adventure Lists System

Players maintain lists randomly consulted:
1. **Characters List** - NPCs (rolled for NPC events)
2. **Threads List** - Goals/missions (rolled for Thread events)
3. **Adventure Features List** - Optional prepared elements

### NPC Generator

**NPC Statistics Table** - Generates NPC attributes
**NPC Behavior Table** - Determines NPC actions

### Fate Check Modifiers

Conditional modifiers adjusting Fate Check results based on narrative context.

### Keyed Scenes

Pre-prepared scene templates that can be randomly selected or used as seeds.

### Adventure Journal

Session logging tracking scenes, NPCs, threads, and discoveries.

### Thread Progress Tracking

Visual progress tracks for long-term goals showing movement toward completion.
