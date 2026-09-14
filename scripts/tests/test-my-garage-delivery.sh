#!/usr/bin/env bash
set -euo pipefail

repo_root="$(cd "$(dirname "$0")/../.." && pwd)"
values="$repo_root/helm/service-template/values-car.yaml"
application="$repo_root/helm/argocd-applications/dev/car-service-app.yaml"
generator="$repo_root/scripts/generate-argocd-apps.sh"
operations="$repo_root/docs/OPERATIONS-GUIDE.md"
workflow_readme="$repo_root/.github/workflows/README.md"
root_readme="$repo_root/README.md"
project_overview="$repo_root/docs/PROJECT-OVERVIEW.md"
car_readme="$repo_root/src/Defender.CarService/README.md"

if grep -Eq '^[[:space:]]+tag:[[:space:]]+latest[[:space:]]*$' "$values" \
    && grep -Eq '^    automated:[[:space:]]*$' "$application"; then
    echo "FAIL mutable CarService image tag has automated ArgoCD sync" >&2
    exit 1
fi

grep -Eq 'generate_argocd_app "Defender\.CarService" "car-service" "values-car\.yaml" "false"' "$generator"

for document in "$operations" "$workflow_readme" "$root_readme" "$project_overview" "$car_readme"; do
    if grep -Eiq 'CarService.*(manual|sync).*until|auto-sync.*disabled until|latest.*manual until|after promotion.*(sync|auto-sync)|promotion.*(enables|turns on|activates) .*sync' "$document"; then
        echo "FAIL conditional CarService sync wording in $document" >&2
        exit 1
    fi
    grep -Fq 'CarService manual-sync exception' "$document"
    grep -Fq 'publish an immutable CarService tag' "$document"
    grep -Fq 'promotion commits values-car.yaml' "$document"
    grep -Fq 'manual ArgoCD sync' "$document"
    grep -Fq 'current-task' "$document"
    grep -Fq 'deployment approval' "$document"
    grep -Fq 'CarService always uses manual ArgoCD sync' "$document"
    grep -Fiq 'immutable promoted tags remain manual' "$document"
done

if grep -Eiq 'CarService remains manual until|promotion (enables|turns on|activates) .*auto-sync|promotion.*auto-sync (enabled|on)' "$project_overview"; then
    echo "FAIL CarService promotion wording implies automated sync" >&2
    exit 1
fi

grep -Fq 'CarService always remains manual' "$project_overview"
grep -Fq 'immutable image promotion' "$project_overview"
grep -Fq 'updates values only and does not enable auto-sync' "$project_overview"

echo "MY_GARAGE_DELIVERY_PASS"
