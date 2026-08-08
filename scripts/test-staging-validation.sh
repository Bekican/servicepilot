#!/bin/sh
set -eu

repository_root="$(CDPATH= cd -- "$(dirname -- "$0")/.." && pwd)"
temporary_directory="$(mktemp -d)"
trap 'rm -rf "$temporary_directory"' EXIT HUP INT TERM

environment_file="$temporary_directory/runtime.env"
release_file="$temporary_directory/release.env"
digest=0123456789abcdef0123456789abcdef0123456789abcdef0123456789abcdef

prepare_staging() {
    cp "$repository_root/deploy/staging/.env.example" "$environment_file"
    cp "$repository_root/deploy/staging/release.env.example" "$release_file"
    sed -i \
        -e 's/staging\.example\.com/staging.servicepilot.test/g' \
        -e 's/operations@example\.com/operations@servicepilot.test/g' \
        -e 's/replace-with-a-long-random-database-password/database-password-with-more-than-24-characters/g' \
        -e 's/replace-with-at-least-32-random-characters/jwt-signing-key-with-more-than-32-characters/g' \
        -e "s/[a-e]\{64\}/$digest/g" \
        "$environment_file" "$release_file"
    chmod 600 "$environment_file" "$release_file"
}

validate() {
    SERVICEPILOT_ENV_FILE="$environment_file" \
    SERVICEPILOT_RELEASE_FILE="$release_file" \
        /bin/sh "$repository_root/scripts/validate-staging-environment.sh"
}

prepare_staging
validate >/dev/null

sed -i \
    -e 's/DEPLOYMENT_TIER=staging/DEPLOYMENT_TIER=production/' \
    -e 's/COMPOSE_PROJECT_NAME=servicepilot-staging/COMPOSE_PROJECT_NAME=servicepilot-production/' \
    -e 's/DOTNET_ENVIRONMENT_NAME=Staging/DOTNET_ENVIRONMENT_NAME=Production/' \
    "$environment_file"
if validate >/dev/null 2>&1; then
    echo "Production validation accepted disabled safety gates." >&2
    exit 1
fi

sed -i \
    -e 's/OFFSITE_BACKUP_ENABLED=false/OFFSITE_BACKUP_ENABLED=true/' \
    -e 's/OBSERVABILITY_ENABLED=false/OBSERVABILITY_ENABLED=true/' \
    -e 's/SMTP_MODE=mailpit/SMTP_MODE=external/' \
    -e 's/SMTP_HOST=mailpit/SMTP_HOST=smtp.servicepilot.test/' \
    -e 's/SMTP_PORT=1025/SMTP_PORT=587/' \
    -e 's/SMTP_ENABLE_SSL=false/SMTP_ENABLE_SSL=true/' \
    -e 's/^SMTP_USERNAME=$/SMTP_USERNAME=servicepilot/' \
    -e 's/^SMTP_PASSWORD=$/SMTP_PASSWORD=smtp-password-with-more-than-24-characters/' \
    -e 's|^OTEL_BACKEND_ENDPOINT=$|OTEL_BACKEND_ENDPOINT=https://otel.servicepilot.test|' \
    -e 's|^OTEL_BACKEND_AUTHORIZATION=$|OTEL_BACKEND_AUTHORIZATION=Bearer-test-token|' \
    -e 's|^RESTIC_REPOSITORY=$|RESTIC_REPOSITORY=s3:https://s3.servicepilot.test/servicepilot-production|' \
    -e 's/^RESTIC_PASSWORD=$/RESTIC_PASSWORD=restic-password-with-more-than-32-characters/' \
    -e 's/^AWS_ACCESS_KEY_ID=$/AWS_ACCESS_KEY_ID=backup-access-key/' \
    -e 's/^AWS_SECRET_ACCESS_KEY=$/AWS_SECRET_ACCESS_KEY=backup-secret-key/' \
    "$environment_file"
validate >/dev/null

sed -i 's|^POSTGRES_IMAGE=.*|POSTGRES_IMAGE=postgres:17-alpine|' "$environment_file"
if validate >/dev/null 2>&1; then
    echo "Runtime validation accepted a mutable infrastructure image." >&2
    exit 1
fi

echo "Staging and production environment validation tests passed."
