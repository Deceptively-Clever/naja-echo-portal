#!/usr/bin/env bash
set -euo pipefail

export PATH="$HOME/.dotnet/tools:$PATH"

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"

dotnet ef database update \
  --project "$SCRIPT_DIR/backend/src/NajaEcho.Infrastructure" \
  --startup-project "$SCRIPT_DIR/backend/src/NajaEcho.Api"
