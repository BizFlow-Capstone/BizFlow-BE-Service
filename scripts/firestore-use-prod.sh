#!/usr/bin/env bash
set -euo pipefail

PROJECT_ID="${1:-bizflow-b8207}"
ROOT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"

cd "$ROOT_DIR"

echo "[firestore] Switching to strict production rules..."
cp "firestore.rules.prod" "firestore.rules"

echo "[firestore] Deploying rules to project: $PROJECT_ID"
npx -y firebase-tools@latest deploy --only firestore:rules --project "$PROJECT_ID"

echo "[firestore] Done. Active file: firestore.rules <- firestore.rules.prod"
