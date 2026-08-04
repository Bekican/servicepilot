#!/bin/sh
set -eu

require() {
    eval "value=\${$1:-}"
    if [ -z "$value" ]; then
        echo "Required backup configuration is missing: $1" >&2
        exit 1
    fi
}

for variable in PGHOST PGDATABASE PGUSER PGPASSWORD DEPLOYMENT_ENVIRONMENT \
    RESTIC_REPOSITORY RESTIC_PASSWORD AWS_ACCESS_KEY_ID AWS_SECRET_ACCESS_KEY; do
    require "$variable"
done

case "$PGDATABASE:$PGUSER:$DEPLOYMENT_ENVIRONMENT" in
    *[!A-Za-z0-9_:-]*)
        echo "Database identifiers and environment name may contain only letters, digits, underscore and hyphen." >&2
        exit 1
        ;;
esac

work_directory="/work/servicepilot-backup"
restic_log="/tmp/servicepilot-restic-command.$$"
verification_database="servicepilot_backup_verify_$(date -u +%Y%m%d%H%M%S)_$$"
database_created=false
rm -rf "$work_directory"
mkdir -p "$work_directory"

cleanup() {
    if [ "$database_created" = "true" ]; then
        dropdb --if-exists --force "$verification_database" >/dev/null 2>&1 || true
    fi
    rm -rf "$work_directory" "$restic_log"
}
trap cleanup EXIT HUP INT TERM

run_restic() {
    action="$1"
    shift
    if ! restic "$@" >"$restic_log" 2>&1; then
        echo "Restic $action failed. Command output was suppressed to protect repository configuration." >&2
        return 1
    fi
    rm -f "$restic_log"
}

timestamp="$(date -u +%Y%m%dT%H%M%SZ)"
archive_name="servicepilot-${DEPLOYMENT_ENVIRONMENT}-${timestamp}.dump"
archive_path="$work_directory/$archive_name"
metadata_path="$archive_path.metadata.json"
checksum_path="$archive_path.sha256"

if ! restic cat config >/dev/null 2>&1; then
    if [ "${RESTIC_AUTO_INIT:-false}" != "true" ]; then
        echo "The Restic repository is unavailable or uninitialized. Initialize it explicitly before backup." >&2
        exit 1
    fi
    echo "Initializing encrypted Restic repository."
    run_restic initialization init
fi

echo "Creating and validating PostgreSQL archive."
pg_dump --format=custom --no-owner --no-privileges --file="$archive_path"
pg_restore --list "$archive_path" >/dev/null

echo "Restoring the archive into an isolated verification database."
createdb --template=template0 "$verification_database"
database_created=true
pg_restore --exit-on-error --no-owner --no-privileges \
    --dbname="$verification_database" "$archive_path"
psql --dbname="$verification_database" --set=ON_ERROR_STOP=1 \
    --command='ANALYZE;' >/dev/null

snapshot="$(psql --dbname="$verification_database" --tuples-only --no-align --set=ON_ERROR_STOP=1 <<'SQL'
SELECT jsonb_build_object(
  'schemaTables', (SELECT COUNT(*) FROM information_schema.tables WHERE table_schema = 'public' AND table_type = 'BASE TABLE'),
  'migrationTablePresent', EXISTS(SELECT 1 FROM information_schema.tables WHERE table_schema = 'public' AND table_name = '__EFMigrationsHistory'),
  'organizations', (SELECT COUNT(*) FROM organizations),
  'users', (SELECT COUNT(*) FROM users),
  'employees', (SELECT COUNT(*) FROM employees),
  'invitations', (SELECT COUNT(*) FROM user_invitations),
  'auditLogs', (SELECT COUNT(*) FROM audit_logs),
  'customers', (SELECT COUNT(*) FROM customers),
  'customerAddresses', (SELECT COUNT(*) FROM customer_addresses),
  'services', (SELECT COUNT(*) FROM services),
  'appointments', (SELECT COUNT(*) FROM appointments),
  'reminders', (SELECT COUNT(*) FROM reminders)
)::text;
SQL
)"

dropdb --if-exists --force "$verification_database"
database_created=false

sha256="$(sha256sum "$archive_path" | awk '{print $1}')"
printf '%s  %s\n' "$sha256" "$archive_name" > "$checksum_path"

jq -n \
    --arg createdAtUtc "$(date -u +%Y-%m-%dT%H:%M:%SZ)" \
    --arg sourceDatabase "$PGDATABASE" \
    --arg postgresVersion "$(psql --tuples-only --no-align --command='SHOW server_version;' | tr -d '[:space:]')" \
    --arg releaseId "${RELEASE_ID:-unknown}" \
    --arg archiveFile "$archive_name" \
    --arg sha256 "$sha256" \
    --argjson archiveBytes "$(wc -c < "$archive_path" | tr -d '[:space:]')" \
    --argjson snapshot "$snapshot" \
    '{schemaVersion: 1, createdAtUtc: $createdAtUtc, sourceDatabase: $sourceDatabase,
      postgresVersion: $postgresVersion, releaseId: $releaseId, archiveFile: $archiveFile,
      archiveBytes: $archiveBytes, sha256: $sha256, snapshot: $snapshot}' \
    > "$metadata_path"

echo "Uploading encrypted backup to the off-site repository."
run_restic upload backup \
    --host "servicepilot-${DEPLOYMENT_ENVIRONMENT}" \
    --tag servicepilot \
    --tag "$DEPLOYMENT_ENVIRONMENT" \
    "$work_directory"

if [ "${RESTIC_APPLY_RETENTION:-true}" = "true" ]; then
    run_restic retention forget \
        --host "servicepilot-${DEPLOYMENT_ENVIRONMENT}" \
        --tag "servicepilot,$DEPLOYMENT_ENVIRONMENT" \
        --keep-daily "${RESTIC_KEEP_DAILY:-14}" \
        --keep-weekly "${RESTIC_KEEP_WEEKLY:-8}" \
        --prune
fi

run_restic verification check

if [ -n "${BACKUP_HEARTBEAT_URL:-}" ]; then
    curl --fail --silent --show-error --retry 3 --max-time 20 \
        --output /dev/null "$BACKUP_HEARTBEAT_URL"
fi

echo "Encrypted off-site backup completed successfully."
