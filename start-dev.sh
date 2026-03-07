#!/usr/bin/env bash
set -euo pipefail

root_dir="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"

backend_project="${BACKEND_PROJECT:-"$root_dir/src/SoloForge.Api/SoloForge.Api.csproj"}"
frontend_dir="${FRONTEND_DIR:-"$root_dir/web/soloforge-web"}"

if ! command -v dotnet >/dev/null 2>&1; then
    echo "dotnet not found in PATH" >&2
    exit 127
fi

if ! command -v npm >/dev/null 2>&1; then
    echo "npm not found in PATH" >&2
    exit 127
fi

if [[ ! -f "$backend_project" ]]; then
    echo "Backend project not found: $backend_project" >&2
    exit 2
fi

if [[ ! -f "$frontend_dir/package.json" ]]; then
    echo "Frontend package.json not found: $frontend_dir/package.json" >&2
    exit 2
fi

pids=()

cleanup() {
    for pid in "${pids[@]:-}"; do
        kill "$pid" >/dev/null 2>&1 || true
    done
    wait >/dev/null 2>&1 || true
}

trap cleanup EXIT INT TERM

echo "Starting backend: dotnet run --project $backend_project"
dotnet run --project "$backend_project" &
pids+=("$!")

echo "Starting frontend: npm --prefix $frontend_dir run dev"
npm --prefix "$frontend_dir" run dev &
pids+=("$!")

set +e
wait -n "${pids[@]}"
exit_code=$?
set -e

echo "One process exited (code $exit_code); stopping the other..." >&2
exit "$exit_code"
