#!/usr/bin/env bash
set -euo pipefail

ROOT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
REPORT_DIR="$ROOT_DIR/IntegrationTests/TestResults"
REPORT_NAME="test-results.html"

mkdir -p "$REPORT_DIR"

echo "Running tests and generating HTML report..."
dotnet test "$ROOT_DIR/BackendIntegrationAutomation.slnx" --logger "html;LogFileName=$REPORT_NAME"

echo "HTML report generated at: $REPORT_DIR/$REPORT_NAME"
