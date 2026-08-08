#!/bin/sh
set -eu

repository_root="$(CDPATH= cd -- "$(dirname -- "$0")/.." && pwd)"
environment_file="${SERVICEPILOT_ENV_FILE:-/etc/servicepilot/staging.env}"
release_file="${SERVICEPILOT_RELEASE_FILE:-}"

[ -f "$environment_file" ] || { echo "Runtime environment file not found: $environment_file" >&2; exit 1; }

case "$(uname -s)" in
    MINGW*|MSYS*)
        echo "WARNING: POSIX secret-file permissions cannot be verified on Windows." >&2
        ;;
    *)
        permissions="$(stat -c '%a' "$environment_file")"
        case "$permissions" in
            400|600) ;;
            *)
                echo "Runtime environment file must have mode 400 or 600; found $permissions." >&2
                exit 1
                ;;
        esac
        ;;
esac

has_value() {
    grep -q "^$1=..*" "$environment_file"
}

require_value() {
    if ! has_value "$1"; then
        echo "Required runtime configuration is missing: $1" >&2
        exit 1
    fi
}

get_value() {
    sed -n "s/^$1=//p" "$environment_file" | tr -d '\r'
}

require_boolean() {
    value="$(get_value "$1")"
    case "$value" in
        true|false) ;;
        *) echo "$1 must be true or false." >&2; exit 1 ;;
    esac
}

require_digest() {
    variable="$1"
    value="$2"
    if ! printf '%s' "$value" | grep -Eq '^[A-Za-z0-9._/:@-]+@sha256:[0-9a-fA-F]{64}$'; then
        echo "$variable must be an immutable sha256 registry reference." >&2
        exit 1
    fi
    digest="${value##*@sha256:}"
    if printf '%s' "$digest" | grep -Eq '^([0-9a-fA-F])\1{63}$'; then
        echo "$variable still contains an example digest." >&2
        exit 1
    fi
}

for variable in DEPLOYMENT_TIER COMPOSE_PROJECT_NAME OFFSITE_BACKUP_ENABLED OBSERVABILITY_ENABLED \
    SMTP_MODE PUBLIC_DOMAIN ACME_EMAIL DOTNET_ENVIRONMENT_NAME POSTGRES_DB \
    POSTGRES_USER POSTGRES_PASSWORD JWT_SIGNING_KEY POSTGRES_IMAGE \
    MAILPIT_IMAGE CADDY_IMAGE SMTP_HOST SMTP_PORT SMTP_ENABLE_SSL \
    SMTP_FROM_ADDRESS SMTP_FROM_NAME; do
    require_value "$variable"
done

require_boolean OFFSITE_BACKUP_ENABLED
require_boolean OBSERVABILITY_ENABLED
require_boolean SMTP_ENABLE_SSL

tier="$(get_value DEPLOYMENT_TIER)"
project_name="$(get_value COMPOSE_PROJECT_NAME)"
if ! printf '%s' "$project_name" | grep -Eq '^[a-z0-9][a-z0-9_-]{1,62}$'; then
    echo "COMPOSE_PROJECT_NAME must be a lowercase Docker Compose project name." >&2
    exit 1
fi
case "$project_name" in
    *"$tier"*) ;;
    *) echo "COMPOSE_PROJECT_NAME must identify its deployment tier." >&2; exit 1 ;;
esac

case "$tier" in
    staging)
        [ "$(get_value DOTNET_ENVIRONMENT_NAME)" = "Staging" ] || {
            echo "Staging requires DOTNET_ENVIRONMENT_NAME=Staging." >&2; exit 1;
        }
        ;;
    production)
        [ "$(get_value DOTNET_ENVIRONMENT_NAME)" = "Production" ] || {
            echo "Production requires DOTNET_ENVIRONMENT_NAME=Production." >&2; exit 1;
        }
        [ "$(get_value OFFSITE_BACKUP_ENABLED)" = "true" ] || {
            echo "Production requires encrypted off-site backups." >&2; exit 1;
        }
        [ "$(get_value OBSERVABILITY_ENABLED)" = "true" ] || {
            echo "Production requires hosted observability." >&2; exit 1;
        }
        [ "$(get_value SMTP_MODE)" = "external" ] || {
            echo "Production requires external SMTP; Mailpit is staging-only." >&2; exit 1;
        }
        ;;
    *) echo "DEPLOYMENT_TIER must be staging or production." >&2; exit 1 ;;
esac

domain="$(get_value PUBLIC_DOMAIN)"
if ! printf '%s' "$domain" | grep -Eq '^[A-Za-z0-9]([A-Za-z0-9.-]*[A-Za-z0-9])?$' \
    || printf '%s' "$domain" | grep -Eqi '(^|\.)example\.(com|net|org)$|localhost'; then
    echo "PUBLIC_DOMAIN must be a real DNS hostname." >&2
    exit 1
fi

email="$(get_value ACME_EMAIL)"
if ! printf '%s' "$email" | grep -Eq '^[^[:space:]@]+@[^[:space:]@]+\.[^[:space:]@]+$'; then
    echo "ACME_EMAIL is invalid." >&2
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
if [ "${#database_password}" -lt 24 ] || [ "${#jwt_key}" -lt 32 ]; then
    echo "Database or JWT secret does not meet the minimum length." >&2
    exit 1
fi

require_digest POSTGRES_IMAGE "$(get_value POSTGRES_IMAGE)"
require_digest MAILPIT_IMAGE "$(get_value MAILPIT_IMAGE)"
require_digest CADDY_IMAGE "$(get_value CADDY_IMAGE)"

smtp_mode="$(get_value SMTP_MODE)"
case "$smtp_mode" in
    mailpit)
        [ "$tier" = "staging" ] && [ "$(get_value SMTP_HOST)" = "mailpit" ] || {
            echo "SMTP_MODE=mailpit is allowed only in staging with SMTP_HOST=mailpit." >&2; exit 1;
        }
        ;;
    external)
        require_value SMTP_USERNAME
        require_value SMTP_PASSWORD
        [ "$(get_value SMTP_ENABLE_SSL)" = "true" ] || {
            echo "External SMTP requires TLS." >&2; exit 1;
        }
        ;;
    *) echo "SMTP_MODE must be mailpit or external." >&2; exit 1 ;;
esac

if [ "$(get_value OBSERVABILITY_ENABLED)" = "true" ]; then
    for variable in OTEL_COLLECTOR_IMAGE OTEL_BACKEND_ENDPOINT OTEL_BACKEND_AUTHORIZATION; do
        require_value "$variable"
    done
    require_digest OTEL_COLLECTOR_IMAGE "$(get_value OTEL_COLLECTOR_IMAGE)"
    case "$(get_value OTEL_BACKEND_ENDPOINT)" in
        https://*) ;;
        *) echo "OTEL_BACKEND_ENDPOINT must use HTTPS." >&2; exit 1 ;;
    esac
fi

if [ "$(get_value OFFSITE_BACKUP_ENABLED)" = "true" ]; then
    for variable in RESTIC_REPOSITORY RESTIC_PASSWORD AWS_ACCESS_KEY_ID AWS_SECRET_ACCESS_KEY; do
        require_value "$variable"
    done
    restic_password="$(get_value RESTIC_PASSWORD)"
    [ "${#restic_password}" -ge 32 ] || {
        echo "RESTIC_PASSWORD must contain at least 32 characters." >&2; exit 1;
    }
    case "$(get_value RESTIC_REPOSITORY)" in
        s3:https://*) ;;
        *) echo "RESTIC_REPOSITORY must use an HTTPS S3-compatible endpoint." >&2; exit 1 ;;
    esac
fi

if [ -n "$release_file" ]; then
    [ -f "$release_file" ] || { echo "Runtime release file not found." >&2; exit 1; }
    for variable in SERVICEPILOT_API_IMAGE SERVICEPILOT_WEB_IMAGE \
        SERVICEPILOT_WORKER_IMAGE SERVICEPILOT_MIGRATOR_IMAGE \
        SERVICEPILOT_BACKUP_IMAGE; do
        value="$(sed -n "s/^$variable=//p" "$release_file" | tr -d '\r')"
        require_digest "$variable" "$value"
    done
    SERVICEPILOT_ENV_FILE="$environment_file" \
    SERVICEPILOT_RELEASE_FILE="$release_file" \
        /bin/sh "$repository_root/scripts/staging-compose.sh" \
        --profile operations config --quiet
fi

if [ "$(get_value OFFSITE_BACKUP_ENABLED)" != "true" ]; then
    echo "WARNING: encrypted off-site backup is disabled; this environment must not hold customer data." >&2
fi

echo "Runtime environment configuration is valid for $tier."
