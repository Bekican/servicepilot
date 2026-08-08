#!/bin/sh
set -eu

case "$(uname -s)" in
    MINGW*|MSYS*)
        repository_root="$(CDPATH= cd -- "$(dirname -- "$0")/.." && pwd -W)"
        ;;
    *)
        repository_root="$(CDPATH= cd -- "$(dirname -- "$0")/.." && pwd)"
        ;;
esac
environment_file="${SERVICEPILOT_ENV_FILE:-/etc/servicepilot/staging.env}"
release_file="${SERVICEPILOT_RELEASE_FILE:-}"
base_compose="${SERVICEPILOT_COMPOSE_FILE:-$repository_root/compose.staging.yaml}"
observability_compose="${SERVICEPILOT_OBSERVABILITY_COMPOSE_FILE:-$repository_root/compose.staging.observability.yaml}"
backup_compose="${SERVICEPILOT_BACKUP_COMPOSE_FILE:-$repository_root/compose.staging.backup.yaml}"

[ -f "$environment_file" ] || { echo "Runtime environment file not found." >&2; exit 1; }

get_value() {
    sed -n "s/^$1=//p" "$environment_file" | tr -d '\r'
}

case "$(uname -s)" in
    MINGW*|MSYS*) compose_separator=';' ;;
    *) compose_separator=':' ;;
esac

compose_files="$base_compose"
if [ "$(get_value OBSERVABILITY_ENABLED)" = "true" ]; then
    compose_files="$compose_files$compose_separator$observability_compose"
fi
if [ "$(get_value OFFSITE_BACKUP_ENABLED)" = "true" ]; then
    compose_files="$compose_files$compose_separator$backup_compose"
fi

export COMPOSE_FILE="$compose_files"
export COMPOSE_PATH_SEPARATOR="$compose_separator"
if [ "$(get_value SMTP_MODE)" = "mailpit" ]; then
    export COMPOSE_PROFILES=mailpit
else
    unset COMPOSE_PROFILES || true
fi

if [ -n "$release_file" ]; then
    [ -f "$release_file" ] || { echo "Runtime release file not found." >&2; exit 1; }
    exec docker compose --project-directory "$repository_root" \
        --env-file "$environment_file" --env-file "$release_file" "$@"
fi

exec docker compose --project-directory "$repository_root" \
    --env-file "$environment_file" "$@"
