#!/usr/bin/env bash
set -euo pipefail

ROOT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
TEST_PROJECT_DIR="$ROOT_DIR/IntegrationTests"
REPORT_DIR="$TEST_PROJECT_DIR/TestResults/allure-report"

if ! command -v allure >/dev/null 2>&1; then
  echo "Allure CLI is not installed or not on PATH."
  echo "Install with: npm install -g allure-commandline"
  exit 1
fi

rm -rf "$REPORT_DIR"

echo "Running tests to generate Allure results..."
dotnet test "$ROOT_DIR/BackendIntegrationAutomation.slnx"

RESULTS_DIR="$(find "$TEST_PROJECT_DIR/bin" -type d -name allure-results | sort | tail -n 1)"
if [[ -z "$RESULTS_DIR" ]]; then
  echo "No allure-results directory found after test run."
  exit 1
fi

echo "Generating Allure dashboard report..."
allure generate "$RESULTS_DIR" --clean -o "$REPORT_DIR"

echo "Allure report generated at: $REPORT_DIR"
