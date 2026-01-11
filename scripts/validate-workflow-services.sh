#!/bin/bash

set -e

. $(dirname "$0")/all_systems.sh

REPO_ROOT="$(cd "$(dirname "$0")/.." && pwd)"
WORKFLOWS=(
    "${REPO_ROOT}/.github/workflows/docker-build-publish.yml"
    "${REPO_ROOT}/.github/workflows/promote-image-tag.yml"
)

ERRORS=0

for workflow in "${WORKFLOWS[@]}"; do
    if [ ! -f "$workflow" ]; then
        echo "Warning: Workflow file not found: $workflow"
        continue
    fi

    echo "Checking: $(basename "$workflow")"
    
    for service in "${all_systems[@]}"; do
        if ! grep -q "\"$service\"" "$workflow"; then
            echo "  ERROR: Service '$service' not found in workflow"
            ERRORS=$((ERRORS + 1))
        fi
    done
    
    if ! grep -q '"ALL"' "$workflow"; then
        echo "  ERROR: 'ALL' option not found in workflow"
        ERRORS=$((ERRORS + 1))
    fi
done

if [ $ERRORS -eq 0 ]; then
    echo "✓ All workflows are in sync with all_systems.sh"
    exit 0
else
    echo "✗ Found $ERRORS error(s)"
    echo ""
    echo "To update workflows, run:"
    echo "  ./scripts/generate-service-options.sh"
    echo "and copy the output to the 'options:' section in each workflow file."
    exit 1
fi
