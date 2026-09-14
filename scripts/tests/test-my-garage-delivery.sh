#!/usr/bin/env bash
set -euo pipefail

repo_root="$(cd "$(dirname "$0")/../.." && pwd)"
values="$repo_root/helm/service-template/values-car.yaml"
application="$repo_root/helm/argocd-applications/dev/car-service-app.yaml"
generator="$repo_root/scripts/generate-argocd-apps.sh"

if grep -Eq '^[[:space:]]+tag:[[:space:]]+latest[[:space:]]*$' "$values" \
    && grep -Eq '^    automated:[[:space:]]*$' "$application"; then
    echo "FAIL mutable CarService image tag has automated ArgoCD sync" >&2
    exit 1
fi

grep -Eq 'generate_argocd_app "Defender\.CarService" "car-service" "values-car\.yaml" "false"' "$generator"

echo "MY_GARAGE_DELIVERY_PASS"
