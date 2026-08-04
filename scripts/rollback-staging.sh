#!/bin/sh
set -eu

repository_root="$(CDPATH= cd -- "$(dirname -- "$0")/.." && pwd)"
compose_file="${SERVICEPILOT_COMPOSE_FILE:-$repository_root/compose.staging.yaml}"
environment_file="${SERVICEPILOT_ENV_FILE:-/etc/servicepilot/staging.env}"
state_directory="${SERVICEPILOT_STATE_DIR:-$repository_root/.deploy-state/staging}"
current_release="$state_directory/current-release.env"
previous_release="$state_directory/previous-release.env"

[ -f "$previous_release" ] || { echo "No previous staging release is available." >&2; exit 1; }

SERVICEPILOT_ENV_FILE="$environment_file" \
SERVICEPILOT_COMPOSE_FILE="$compose_file" \
    /bin/sh "$repository_root/scripts/validate-staging-environment.sh"

temporary_release="$state_directory/rollback-release.env"
trap 'rm -f "$temporary_release"' EXIT HUP INT TERM

compose_with_release() {
    release_file="$1"
    shift
    docker compose --env-file "$environment_file" --env-file "$release_file" \
        -f "$compose_file" "$@"
}

compose_with_release "$previous_release" up -d --no-deps --force-recreate api worker web caddy

domain="$(sed -n 's/^STAGING_DOMAIN=//p' "$environment_file" | tr -d '\r')"
deadline="$(($(date +%s) + 180))"
rollback_ready=false
until curl --fail --silent --show-error --max-time 10 \
    "https://$domain/ops/api/ready" >/dev/null; do
    if [ "$(date +%s)" -ge "$deadline" ]; then
        break
    fi
    sleep 3
done
if curl --fail --silent --show-error --max-time 10 \
    "https://$domain/ops/api/ready" >/dev/null; then
    rollback_ready=true
fi

if [ "$rollback_ready" != "true" ]; then
    echo "Previous release failed readiness; restoring the recorded current images." >&2
    compose_with_release "$current_release" up -d --no-deps --force-recreate api worker web caddy || true
    exit 1
fi

cp "$current_release" "$temporary_release"
cp "$previous_release" "$current_release"
cp "$temporary_release" "$previous_release"

echo "Previous application images restored. PostgreSQL and migrations were not changed."
