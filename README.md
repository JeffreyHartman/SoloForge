# SoloForge

A solo tabletop RPG toolkit implementing the Mythic 2e game master emulator. Run fate checks, scene checks, random events, meaning tables, dice rolls, and manage campaigns — all locally with your files.

## Getting Started

Prerequisites: [.NET 10 SDK](https://dotnet.microsoft.com/download), [Node.js](https://nodejs.org/) (for the web frontend).

```bash
# Install frontend dependencies
npm --prefix web/soloforge-web install

# Start the API and web frontend together
./start-dev.sh
```

Open [http://localhost:5173](http://localhost:5173) in your browser.

## Project Structure

- **src/SoloForge.Core** — Shared library: Mythic 2e engines, models, and services
- **src/SoloForge.Api** — ASP.NET minimal API backend (http://localhost:5137)
- **web/soloforge-web** — Vue 3 + TypeScript + Tailwind CSS frontend
- **data/** — Word tables, quick sets, and theme definitions
- **tests/** — xunit tests

## Commands

```bash
dotnet build              # Build all .NET projects
dotnet test               # Run .NET unit tests
./start-dev.sh            # Run API + frontend dev servers

# E2E tests (Playwright — launches dev servers automatically)
cd web/soloforge-web
npm run test:e2e
```

## How It Works

SoloForge runs entirely on your machine. Campaigns are saved as JSON in `saves/`, and journals are markdown files you can edit with any text editor. No account, no cloud, no internet required.
