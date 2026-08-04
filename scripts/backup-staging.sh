#!/bin/sh
set -eu

repository_root="$(CDPATH= cd -- "$(dirname -- "$0")/.." && pwd)"
compose_file="${SERVICEPILOT_COMPOSE_FILE:-$repository_root/compose.staging.yaml}"
environment_file="${SERVICEPILOT_ENV_FILE:-/etc/servicepilot/staging.env}"
state_directory="${SERVICEPILOT_STATE_DIR:-$repository_root/.deploy-state/staging}"
current_release="$state_directory/current-release.env"

[ -f "$current_release" ] || { echo "No successfully deployed staging release was recorded." >&2; exit 1; }

SERVICEPILOT_ENV_FILE="$environment_file" \
SERVICEPILOT_COMPOSE_FILE="$compose_file" \
    /bin/sh "$repository_root/scripts/validate-staging-environment.sh"

docker compose --env-file "$environment_file" --env-file "$current_release" \
    -f "$compose_file" --profile operations run --rm backup
