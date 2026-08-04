#!/bin/sh
set -eu

usage() {
    echo "Usage: $0 <release-manifest.json>" >&2
    exit 2
}

[ "$#" -eq 1 ] || usage

repository_root="$(CDPATH= cd -- "$(dirname -- "$0")/.." && pwd)"
compose_file="${SERVICEPILOT_COMPOSE_FILE:-$repository_root/compose.staging.yaml}"
environment_file="${SERVICEPILOT_ENV_FILE:-/etc/servicepilot/staging.env}"
state_directory="${SERVICEPILOT_STATE_DIR:-$repository_root/.deploy-state/staging}"
manifest_path="$1"
current_release="$state_directory/current-release.env"
previous_release="$state_directory/previous-release.env"
candidate_release="$state_directory/candidate-release.env"

[ -f "$manifest_path" ] || { echo "Release manifest not found." >&2; exit 1; }
command -v jq >/dev/null 2>&1 || { echo "jq is required." >&2; exit 1; }

SERVICEPILOT_ENV_FILE="$environment_file" \
SERVICEPILOT_COMPOSE_FILE="$compose_file" \
    /bin/sh "$repository_root/scripts/validate-staging-environment.sh"

release_id="$(jq -r '.releaseId // empty' "$manifest_path")"
case "$release_id" in
    ''|*[!A-Za-z0-9._-]*)
        echo "releaseId is missing or contains unsupported characters." >&2
        exit 1
        ;;
esac

mkdir -p "$state_directory"
chmod 700 "$state_directory"
: > "$candidate_release"
chmod 600 "$candidate_release"
printf 'RELEASE_ID=%s\n' "$release_id" >> "$candidate_release"

for mapping in \
    'SERVICEPILOT_API_IMAGE:.images.api' \
    'SERVICEPILOT_WEB_IMAGE:.images.web' \
    'SERVICEPILOT_WORKER_IMAGE:.images.worker' \
    'SERVICEPILOT_MIGRATOR_IMAGE:.images.migrator'; do
    variable="${mapping%%:*}"
    query="${mapping#*:}"
    image="$(jq -r "$query // empty" "$manifest_path")"
    if ! printf '%s' "$image" | grep -Eq '^[A-Za-z0-9._/:@-]+@sha256:[0-9a-fA-F]{64}$'; then
        echo "$variable must be an immutable sha256 registry reference." >&2
        rm -f "$candidate_release"
        exit 1
    fi
    printf '%s=%s\n' "$variable" "$image" >> "$candidate_release"
done

compose_with_release() {
    release_file="$1"
    shift
    docker compose --env-file "$environment_file" --env-file "$release_file" \
        -f "$compose_file" "$@"
}

rollback_started=false
had_current_release=false
if [ -f "$current_release" ]; then
    had_current_release=true
fi
cleanup() {
    rm -f "$candidate_release"
}
trap cleanup EXIT HUP INT TERM

compose_with_release "$candidate_release" config --quiet
compose_with_release "$candidate_release" pull postgres mailpit caddy backup api worker web migrator

if [ -f "$current_release" ]; then
    echo "Creating mandatory encrypted off-site backup before migration."
    compose_with_release "$current_release" up -d postgres mailpit
    compose_with_release "$current_release" --profile operations run --rm backup
else
    echo "Initial deployment has no existing application database to protect; pre-deployment backup is not applicable."
    compose_with_release "$candidate_release" up -d postgres mailpit
fi

echo "Applying forward-only migrations."
compose_with_release "$candidate_release" --profile operations run --rm migrator

if [ -f "$current_release" ]; then
    cp "$current_release" "$previous_release"
fi
cp "$candidate_release" "$current_release"
rollback_started=true

if ! compose_with_release "$current_release" up -d --no-deps --force-recreate api worker web caddy; then
    deployment_error=true
else
    deployment_error=false
fi

if [ "$deployment_error" = "false" ]; then
    deadline="$(($(date +%s) + 180))"
    until curl --fail --silent --show-error --max-time 10 \
        "https://$(sed -n 's/^STAGING_DOMAIN=//p' "$environment_file" | tr -d '\r')/ops/api/ready" \
        >/dev/null; do
        if [ "$(date +%s)" -ge "$deadline" ]; then
            deployment_error=true
            break
        fi
        sleep 3
    done
fi

if [ "$deployment_error" = "true" ]; then
    if [ "$rollback_started" = "true" ] && [ -f "$previous_release" ]; then
        echo "Candidate failed readiness; restoring previous application images." >&2
        cp "$previous_release" "$current_release"
        compose_with_release "$current_release" up -d --no-deps --force-recreate api worker web caddy || true
    elif [ "$had_current_release" = "false" ]; then
        rm -f "$current_release"
    fi
    echo "Staging deployment failed. PostgreSQL was not automatically restored." >&2
    exit 1
fi

echo "Staging release $release_id is ready."
