#!/usr/bin/env bash
set -euo pipefail

ROOT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
TEST_PROJECT_DIR="$ROOT_DIR/IntegrationTests"
RESULTS_DIR=""

if ! command -v allure >/dev/null 2>&1; then
  echo "Allure CLI is not installed or not on PATH."
  echo "Install with: npm install -g allure-commandline"
  exit 1
fi

if [[ "${1:-}" != "--no-test" ]]; then
  echo "Generating latest Allure results first..."
  "$ROOT_DIR/scripts/generate-allure-report.sh"
fi

RESULTS_DIR="$(find "$TEST_PROJECT_DIR/bin" -type d -name allure-results | sort | tail -n 1)"
if [[ -z "$RESULTS_DIR" ]]; then
  echo "No allure-results directory found."
  echo "Run scripts/generate-allure-report.sh first."
  exit 1
fi

echo "Starting Allure live server..."
echo "Press Ctrl+C to stop."
allure serve "$RESULTS_DIR"
