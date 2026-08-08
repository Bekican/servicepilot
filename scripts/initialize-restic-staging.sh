#!/bin/sh
set -eu

repository_root="$(CDPATH= cd -- "$(dirname -- "$0")/.." && pwd)"
environment_file="${SERVICEPILOT_ENV_FILE:-/etc/servicepilot/staging.env}"
state_directory="${SERVICEPILOT_STATE_DIR:-$repository_root/.deploy-state/staging}"
current_release="$state_directory/current-release.env"

[ -f "$current_release" ] || { echo "Deploy the initial staging release before initializing Restic." >&2; exit 1; }

[ "$(sed -n 's/^OFFSITE_BACKUP_ENABLED=//p' "$environment_file" | tr -d '\r')" = "true" ] || {
    echo "Encrypted off-site backup is disabled for this environment." >&2; exit 1;
}

SERVICEPILOT_ENV_FILE="$environment_file" \
SERVICEPILOT_RELEASE_FILE="$current_release" \
    /bin/sh "$repository_root/scripts/validate-staging-environment.sh"

SERVICEPILOT_ENV_FILE="$environment_file" \
SERVICEPILOT_RELEASE_FILE="$current_release" \
    /bin/sh "$repository_root/scripts/staging-compose.sh" \
    --profile operations run --rm \
    --entrypoint /bin/sh backup -c \
    'if restic cat config >/dev/null 2>&1; then echo "Restic repository is already initialized."; elif restic init >/dev/null 2>&1; then echo "Restic repository initialized."; else echo "Restic initialization failed; repository details were suppressed." >&2; exit 1; fi'
