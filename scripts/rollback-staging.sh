#!/bin/sh
set -eu

repository_root="$(CDPATH= cd -- "$(dirname -- "$0")/.." && pwd)"
environment_file="${SERVICEPILOT_ENV_FILE:-/etc/servicepilot/staging.env}"
state_directory="${SERVICEPILOT_STATE_DIR:-$repository_root/.deploy-state/staging}"
current_release="$state_directory/current-release.env"
previous_release="$state_directory/previous-release.env"
temporary_release="$state_directory/rollback-release.env"

[ -f "$current_release" ] || { echo "No current release is recorded." >&2; exit 1; }
[ -f "$previous_release" ] || { echo "No previous release is available." >&2; exit 1; }
trap 'rm -f "$temporary_release"' EXIT HUP INT TERM

SERVICEPILOT_ENV_FILE="$environment_file" \
SERVICEPILOT_RELEASE_FILE="$previous_release" \
    /bin/sh "$repository_root/scripts/validate-staging-environment.sh"

compose_with_release() {
    release_file="$1"
    shift
    SERVICEPILOT_ENV_FILE="$environment_file" \
    SERVICEPILOT_RELEASE_FILE="$release_file" \
        /bin/sh "$repository_root/scripts/staging-compose.sh" "$@"
}

domain="$(sed -n 's/^PUBLIC_DOMAIN=//p' "$environment_file" | tr -d '\r')"
compose_with_release "$previous_release" up -d --no-deps --force-recreate api worker web caddy

deadline="$(($(date +%s) + 180))"
rollback_ready=false
while [ "$(date +%s)" -lt "$deadline" ]; do
    if curl --fail --silent --show-error --max-time 10 \
        "https://$domain/ops/api/ready" >/dev/null 2>&1 \
        && curl --fail --silent --show-error --max-time 10 \
        "https://$domain/ops/web/ready" >/dev/null 2>&1; then
        rollback_ready=true
        break
    fi
    sleep 3
done

if [ "$rollback_ready" != "true" ]; then
    echo "Previous release failed readiness; restoring recorded current images." >&2
    compose_with_release "$current_release" up -d --no-deps --force-recreate api worker web caddy || true
    exit 1
fi

cp "$current_release" "$temporary_release"
cp "$previous_release" "$current_release"
cp "$temporary_release" "$previous_release"

echo "Previous application images restored. PostgreSQL and migrations were not changed."
