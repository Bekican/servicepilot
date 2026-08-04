#!/bin/sh
set -eu

repository_root="$(CDPATH= cd -- "$(dirname -- "$0")/.." && pwd)"
compose_file="${SERVICEPILOT_COMPOSE_FILE:-$repository_root/compose.staging.yaml}"
environment_file="${SERVICEPILOT_ENV_FILE:-/etc/servicepilot/staging.env}"

if [ ! -f "$environment_file" ]; then
    echo "Staging environment file not found: $environment_file" >&2
    exit 1
fi

permissions="$(stat -c '%a' "$environment_file")"
case "$permissions" in
    400|600) ;;
    *)
        echo "Staging environment file must have mode 400 or 600; found $permissions." >&2
        exit 1
        ;;
esac

required_variables='STAGING_DOMAIN ACME_EMAIL POSTGRES_DB POSTGRES_USER POSTGRES_PASSWORD JWT_SIGNING_KEY SMTP_FROM_ADDRESS SMTP_FROM_NAME SERVICEPILOT_BACKUP_IMAGE RESTIC_REPOSITORY RESTIC_PASSWORD AWS_ACCESS_KEY_ID AWS_SECRET_ACCESS_KEY'
for variable in $required_variables; do
    if ! grep -q "^${variable}=..*" "$environment_file"; then
        echo "Required staging configuration is missing: $variable" >&2
        exit 1
    fi
done

if grep -Eqi '(replace-with|example\.com|ghcr\.io/example)' "$environment_file"; then
    echo "Staging environment still contains example placeholder values." >&2
    exit 1
fi

get_value() {
    sed -n "s/^$1=//p" "$environment_file" | tr -d '\r'
}

domain="$(get_value STAGING_DOMAIN)"
if ! printf '%s' "$domain" | grep -Eq '^[A-Za-z0-9]([A-Za-z0-9.-]*[A-Za-z0-9])?$'; then
    echo "STAGING_DOMAIN is not a valid DNS hostname." >&2
    exit 1
fi

database_name="$(get_value POSTGRES_DB)"
database_user="$(get_value POSTGRES_USER)"
if ! printf '%s:%s' "$database_name" "$database_user" | grep -Eq '^[A-Za-z0-9_]+:[A-Za-z0-9_]+$'; then
    echo "PostgreSQL database and user names may contain only letters, digits and underscore." >&2
    exit 1
fi

database_password="$(get_value POSTGRES_PASSWORD)"
jwt_key="$(get_value JWT_SIGNING_KEY)"
restic_password="$(get_value RESTIC_PASSWORD)"
if [ "${#database_password}" -lt 24 ] || [ "${#jwt_key}" -lt 32 ] || [ "${#restic_password}" -lt 32 ]; then
    echo "Database, JWT or Restic secret does not meet the minimum length." >&2
    exit 1
fi

restic_repository="$(get_value RESTIC_REPOSITORY)"
case "$restic_repository" in
    s3:https://*) ;;
    *)
        echo "RESTIC_REPOSITORY must use an HTTPS S3-compatible endpoint." >&2
        exit 1
        ;;
esac

backup_image="$(get_value SERVICEPILOT_BACKUP_IMAGE)"
if ! printf '%s' "$backup_image" | grep -Eq '@sha256:[0-9a-fA-F]{64}$'; then
    echo "SERVICEPILOT_BACKUP_IMAGE must be pinned by sha256 digest." >&2
    exit 1
fi

docker compose --env-file "$environment_file" -f "$compose_file" config --quiet
echo "Staging environment and Compose model are valid."
