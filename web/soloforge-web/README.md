# SoloForge Web

Vue 3 + TypeScript + Vite frontend for SoloForge.

## Development

```bash
# Install dependencies
npm install

# Start dev server (Vite on :5173, proxies /api to :5137)
npm run dev

# Type-check and build for production
npm run build
```

The API backend must be running for full functionality. Use `../../start-dev.sh` from the repo root to launch both together.

## Testing

### Unit Tests (Vitest)

```bash
# Run all unit tests
npm run test

# Run in watch mode (re-runs on file changes)
npm run test:watch

# Run with coverage report
npm run test:coverage
```

**Config:** `vitest.config.ts` (separate from `vite.config.ts` to avoid loading tailwind/proxy in tests)

**Test files** are colocated with source in `__tests__/` directories:
- `src/composables/__tests__/` — composable logic tests
- `src/components/journal/tiptap/__tests__/` — Tiptap extension/utility tests

**Stack:** Vitest + jsdom. No `@vue/test-utils` needed — composables are tested by calling functions and asserting on reactive return values.

### E2E Tests (Playwright)

```bash
# Run e2e tests (auto-launches API + Vite dev server)
npm run test:e2e
```

**Config:** `playwright.config.ts`

**Test files:** `e2e/`

## Project Structure

```
src/
  composables/           # Vue 3 composables (state + logic)
    __tests__/           # Vitest unit tests for composables
  components/            # Vue components organized by feature
    journal/tiptap/      # Tiptap WYSIWYG editor extensions
      __tests__/         # Vitest unit tests for Tiptap utilities
  types/                 # TypeScript interfaces and type definitions
  views/                 # Top-level view components
  tools/                 # Tool navigation registry
  data/                  # Static data (name lists)
e2e/                     # Playwright end-to-end tests
```
