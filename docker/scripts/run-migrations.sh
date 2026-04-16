#!/bin/bash
# =============================================================================
# BizFlow Migration Runner
# =============================================================================
# Applies pending SQL migration files to the running MySQL container.
# Tracks applied migrations in a `schema_migrations` table so each file
# is executed exactly once — same pattern as Flyway/Liquibase but zero deps.
#
# Usage (manual):
#   cd /opt/bizflow/bizflow-app
#   bash docker/scripts/run-migrations.sh
#
# Called automatically by the CI/CD deploy-be.yml after each deploy.
# =============================================================================

set -euo pipefail

CONTAINER="bizflow-mysql"
MIGRATIONS_DIR="./database/migrations"

# ---------------------------------------------------------------------------
# Load credentials from .env.prod if not already set in environment
# ---------------------------------------------------------------------------
if [ -f ".env.prod" ]; then
  # shellcheck disable=SC2046
  export $(grep -v '^#' .env.prod | grep -E '^MYSQL_' | xargs)
fi

DB="${MYSQL_DATABASE:-bizflow_db}"
DB_PASS="${MYSQL_PASSWORD:?MYSQL_PASSWORD is required}"
ROOT_PASS="${MYSQL_ROOT_PASSWORD:?MYSQL_ROOT_PASSWORD is required}"

# ---------------------------------------------------------------------------
# Apply pending migrations in alphabetical order.
# Each .sql file already contains an INSERT INTO __MigrationHistory at the end.
# We check that table to skip already-applied files.
# ---------------------------------------------------------------------------
APPLIED=0

for file in $(ls "${MIGRATIONS_DIR}"/*.sql 2>/dev/null | sort); do
  filename=$(basename "$file")
  # MigrationId stored without extension (e.g. "001_create_initial_schema")
  migration_id="${filename%.sql}"

  count=$(docker exec -i "$CONTAINER" mysql \
    -uroot -p"${ROOT_PASS}" \
    --default-character-set=utf8mb4 \
    -D "$DB" \
    -sNe "SELECT COUNT(*) FROM \`__MigrationHistory\` WHERE \`MigrationId\` = '${migration_id}';" 2>/dev/null || echo "0")

  if [ "${count}" -eq "1" ]; then
    continue
  fi

  echo "[migrate] Applying: ${filename} ..."

  docker exec -i "$CONTAINER" mysql \
    -uroot -p"${ROOT_PASS}" \
    --default-character-set=utf8mb4 \
    -D "$DB" < "$file"

  echo "[migrate] Applied:  ${filename}"
  APPLIED=$((APPLIED + 1))
done

if [ "$APPLIED" -eq "0" ]; then
  echo "[migrate] No pending migrations. Database is up to date."
else
  echo "[migrate] Done. Applied ${APPLIED} migration(s)."
fi
