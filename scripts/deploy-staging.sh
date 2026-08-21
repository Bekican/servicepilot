#!/bin/sh
set -eu

usage() {
    echo "Usage: $0 <release-manifest.json>" >&2
    exit 2
}

[ "$#" -eq 1 ] || usage

repository_root="$(CDPATH= cd -- "$(dirname -- "$0")/.." && pwd)"
environment_file="${SERVICEPILOT_ENV_FILE:-/etc/servicepilot/staging.env}"
state_directory="${SERVICEPILOT_STATE_DIR:-$repository_root/.deploy-state/staging}"
manifest_path="$1"
current_release="$state_directory/current-release.env"
previous_release="$state_directory/previous-release.env"
candidate_release="$state_directory/candidate-release.env"
compose_script="$repository_root/scripts/staging-compose.sh"

[ -f "$manifest_path" ] || { echo "Release manifest not found." >&2; exit 1; }
for command in docker jq curl getent; do
    command -v "$command" >/dev/null 2>&1 || { echo "$command is required." >&2; exit 1; }
done
docker info >/dev/null 2>&1 || { echo "Docker Engine is unavailable." >&2; exit 1; }

available_kib="$(df -Pk "$repository_root" | awk 'NR == 2 {print $4}')"
if [ -z "$available_kib" ] || [ "$available_kib" -lt 10485760 ]; then
    echo "At least 10 GiB of free disk space is required before deployment." >&2
    exit 1
fi

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

cleanup() {
    rm -f "$candidate_release"
}
trap cleanup EXIT HUP INT TERM

for mapping in \
    'SERVICEPILOT_API_IMAGE:.images.api' \
    'SERVICEPILOT_WEB_IMAGE:.images.web' \
    'SERVICEPILOT_WORKER_IMAGE:.images.worker' \
    'SERVICEPILOT_MIGRATOR_IMAGE:.images.migrator' \
    'SERVICEPILOT_BACKUP_IMAGE:.images.backup'; do
    variable="${mapping%%:*}"
    query="${mapping#*:}"
    image="$(jq -r "$query // empty" "$manifest_path")"
    printf '%s=%s\n' "$variable" "$image" >> "$candidate_release"
done

SERVICEPILOT_ENV_FILE="$environment_file" \
SERVICEPILOT_RELEASE_FILE="$candidate_release" \
    /bin/sh "$repository_root/scripts/validate-staging-environment.sh"

domain="$(sed -n 's/^PUBLIC_DOMAIN=//p' "$environment_file" | tr -d '\r')"
getent ahosts "$domain" >/dev/null 2>&1 || {
    echo "PUBLIC_DOMAIN does not resolve yet: $domain" >&2
    exit 1
}

compose_with_release() {
    release_file="$1"
    shift
    SERVICEPILOT_ENV_FILE="$environment_file" \
    SERVICEPILOT_RELEASE_FILE="$release_file" \
        /bin/sh "$compose_script" "$@"
}

wait_for_public_readiness() {
    deadline="$(($(date +%s) + 180))"
    while :; do
        api_ready=false
        knowledge_ready=false
        web_ready=false
        curl --fail --silent --show-error --max-time 10 \
            "https://$domain/ops/api/ready" >/dev/null 2>&1 && api_ready=true
        curl --fail --silent --show-error --max-time 10 \
            "https://$domain/ops/api/knowledge" >/dev/null 2>&1 && knowledge_ready=true
        curl --fail --silent --show-error --max-time 10 \
            "https://$domain/ops/web/ready" >/dev/null 2>&1 && web_ready=true
        if [ "$api_ready" = "true" ] && [ "$knowledge_ready" = "true" ] \
            && [ "$web_ready" = "true" ]; then
            return 0
        fi
        [ "$(date +%s)" -lt "$deadline" ] || return 1
        sleep 3
    done
}

wait_for_worker_health() {
    release_file="$1"
    deadline="$(($(date +%s) + 90))"
    while :; do
        container_id="$(compose_with_release "$release_file" ps -q worker)"
        if [ -n "$container_id" ]; then
            status="$(docker inspect --format '{{if .State.Health}}{{.State.Health.Status}}{{else}}{{.State.Status}}{{end}}' "$container_id" 2>/dev/null || true)"
            [ "$status" = "healthy" ] && return 0
            [ "$status" = "unhealthy" ] && return 1
        fi
        [ "$(date +%s)" -lt "$deadline" ] || return 1
        sleep 3
    done
}

compose_with_release "$candidate_release" pull postgres caddy ollama api worker web migrator
if grep -q '^SMTP_MODE=mailpit$' "$environment_file"; then
    compose_with_release "$candidate_release" pull mailpit
fi
if grep -q '^OBSERVABILITY_ENABLED=true$' "$environment_file"; then
    compose_with_release "$candidate_release" pull otel-collector
fi
if grep -q '^OFFSITE_BACKUP_ENABLED=true$' "$environment_file"; then
    compose_with_release "$candidate_release" --profile operations pull backup
fi

had_current_release=false
if [ -f "$current_release" ]; then
    had_current_release=true
fi

start_foundation() {
    release_file="$1"
    if grep -q '^SMTP_MODE=mailpit$' "$environment_file"; then
        compose_with_release "$release_file" up -d postgres ollama mailpit
    else
        compose_with_release "$release_file" up -d postgres ollama
    fi
}

if [ "$had_current_release" = "true" ]; then
    if grep -q '^OFFSITE_BACKUP_ENABLED=true$' "$environment_file"; then
        echo "Creating mandatory encrypted off-site backup before migration."
        start_foundation "$current_release"
        compose_with_release "$current_release" --profile operations run --rm backup
    else
        echo "WARNING: pre-migration off-site backup is disabled; staging must remain disposable." >&2
        start_foundation "$current_release"
    fi
else
    echo "Initial deployment has no existing application database to protect."
    start_foundation "$candidate_release"
fi

echo "Applying forward-only migrations."
compose_with_release "$candidate_release" --profile operations run --rm migrator

echo "Preparing local Qwen models and secured document storage."
compose_with_release "$candidate_release" --profile operations run --rm ollama-model-init
compose_with_release "$candidate_release" run --rm knowledge-storage-init

if [ "$had_current_release" = "true" ]; then
    cp "$current_release" "$previous_release"
fi
cp "$candidate_release" "$current_release"

deployment_error=false
if ! compose_with_release "$current_release" up -d --no-deps --force-recreate api worker web caddy; then
    deployment_error=true
elif ! wait_for_worker_health "$current_release"; then
    echo "Worker failed its process health check." >&2
    deployment_error=true
elif ! wait_for_public_readiness; then
    echo "Public API or Web readiness timed out." >&2
    deployment_error=true
fi

if [ "$deployment_error" = "true" ]; then
    if [ -f "$previous_release" ]; then
        echo "Candidate failed readiness; restoring previous application images." >&2
        cp "$previous_release" "$current_release"
        compose_with_release "$current_release" up -d --no-deps --force-recreate api worker web caddy || true
    elif [ "$had_current_release" = "false" ]; then
        rm -f "$current_release"
    fi
    echo "Deployment failed. PostgreSQL and forward migrations were not automatically reverted." >&2
    exit 1
fi

echo "$release_id is ready at https://$domain."
