#!/usr/bin/env bash
set -euo pipefail

PROJECT_ID="${1:-bizflow-b8207}"
ROOT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"

cd "$ROOT_DIR"

echo "[firestore] Switching to hybrid rules (dev + prod)..."
cp "firestore.rules.hybrid" "firestore.rules"

echo "[firestore] Deploying rules to project: $PROJECT_ID"
npx -y firebase-tools@latest deploy --only firestore:rules --project "$PROJECT_ID"

echo "[firestore] Done. Active file: firestore.rules <- firestore.rules.hybrid"
