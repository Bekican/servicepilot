#!/bin/sh
set -eu

require() {
    eval "value=\${$1:-}"
    if [ -z "$value" ]; then
        echo "Required restore configuration is missing: $1" >&2
        exit 1
    fi
}

for variable in PGHOST PGDATABASE PGUSER PGPASSWORD DEPLOYMENT_ENVIRONMENT \
    RESTIC_REPOSITORY RESTIC_PASSWORD AWS_ACCESS_KEY_ID AWS_SECRET_ACCESS_KEY \
    KNOWLEDGE_SOURCE_PATH; do
    require "$variable"
done

case "$PGDATABASE:$PGUSER:$DEPLOYMENT_ENVIRONMENT" in
    *[!A-Za-z0-9_:-]*)
        echo "Database identifiers and environment name may contain only letters, digits, underscore and hyphen." >&2
        exit 1
        ;;
esac

work_directory="$(mktemp -d /work/servicepilot-restore.XXXXXX)"
restic_log="/tmp/servicepilot-restic-command.$$"
verification_database="servicepilot_restore_verify_$(date -u +%Y%m%d%H%M%S)_$$"
database_created=false

run_restic() {
    action="$1"
    shift
    if ! restic "$@" >"$restic_log" 2>&1; then
        echo "Restic $action failed. Command output was suppressed to protect repository configuration." >&2
        return 1
    fi
    rm -f "$restic_log"
}

cleanup() {
    if [ "$database_created" = "true" ]; then
        dropdb --if-exists --force "$verification_database" >/dev/null 2>&1 || true
    fi
    rm -rf "$work_directory" "$restic_log"
}
trap cleanup EXIT HUP INT TERM

echo "Downloading the latest encrypted off-site staging snapshot."
run_restic restore restore latest \
    --host "servicepilot-${DEPLOYMENT_ENVIRONMENT}" \
    --target "$work_directory"

archive_path="$(find "$work_directory" -type f -name '*.dump' | head -n 1)"
if [ -z "$archive_path" ]; then
    echo "The restored Restic snapshot did not contain a PostgreSQL archive." >&2
    exit 1
fi

metadata_path="$archive_path.metadata.json"
checksum_path="$archive_path.sha256"
if [ ! -f "$metadata_path" ] || [ ! -f "$checksum_path" ]; then
    echo "The archive metadata or checksum file is missing." >&2
    exit 1
fi

(cd "$(dirname "$archive_path")" && sha256sum -c "$(basename "$checksum_path")")
pg_restore --list "$archive_path" >/dev/null

createdb --template=template0 "$verification_database"
database_created=true
pg_restore --exit-on-error --no-owner --no-privileges \
    --dbname="$verification_database" "$archive_path"
psql --dbname="$verification_database" --set=ON_ERROR_STOP=1 --command='ANALYZE;' >/dev/null

restored_snapshot="$(psql --dbname="$verification_database" --tuples-only --no-align --set=ON_ERROR_STOP=1 <<'SQL'
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
  'reminders', (SELECT COUNT(*) FROM reminders),
  'knowledgeDocuments', (SELECT COUNT(*) FROM knowledge_documents),
  'knowledgeChunks', (SELECT COUNT(*) FROM knowledge_document_chunks)
)::text;
SQL
)"

expected_snapshot="$(jq -c '.snapshot' "$metadata_path")"
actual_snapshot="$(printf '%s' "$restored_snapshot" | jq -c '.')"
if [ "$expected_snapshot" != "$actual_snapshot" ]; then
    echo "Restore completed, but schema or business-table counts differ from metadata." >&2
    exit 1
fi

knowledge_manifest_path="$(find "$work_directory" -type f -name 'knowledge-files.sha256' | head -n 1)"
restored_knowledge_root="$work_directory${KNOWLEDGE_SOURCE_PATH:-/source/knowledge}"
if [ -z "$knowledge_manifest_path" ] || [ ! -d "$restored_knowledge_root" ]; then
    echo "The restored snapshot did not contain knowledge document storage." >&2
    exit 1
fi

echo "Verifying restored knowledge document checksums."
if [ -s "$knowledge_manifest_path" ]; then
    (cd "$restored_knowledge_root" && sha256sum -c "$knowledge_manifest_path") >/dev/null
fi

storage_key_list="$work_directory/active-knowledge-storage-keys.txt"
psql --dbname="$verification_database" --tuples-only --no-align --set=ON_ERROR_STOP=1 \
    --command="SELECT storage_key FROM knowledge_documents WHERE status <> 'Deleted' ORDER BY storage_key;" \
    > "$storage_key_list"
while IFS= read -r storage_key; do
    [ -z "$storage_key" ] && continue
    if [ ! -f "$restored_knowledge_root/$storage_key" ]; then
        echo "Restored database references a missing knowledge document." >&2
        exit 1
    fi
done < "$storage_key_list"

echo "Off-site restore drill passed in isolated database $verification_database."
