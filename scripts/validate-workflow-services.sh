#!/bin/bash

set -e

SCRIPT_DIR="$(cd "$(dirname "$0")" && pwd)"
. "$SCRIPT_DIR/all_systems.sh"

REPO_ROOT="$(cd "$SCRIPT_DIR/.." && pwd)"

get_short_name() {
    "$SCRIPT_DIR/map-service-name.sh" "$1"
}

ERRORS=0

echo "Checking: docker-build-publish.yml"
WORKFLOW="${REPO_ROOT}/.github/workflows/docker-build-publish.yml"

if [ ! -f "$WORKFLOW" ]; then
    echo "  ERROR: Workflow file not found"
    exit 1
fi

for service in "${all_systems[@]}"; do
    if ! grep -q "\"$service\"" "$WORKFLOW"; then
        echo "  ERROR: Service '$service' not found in options"
        ERRORS=$((ERRORS + 1))
    fi
    
    short_name=$(get_short_name "$service")
    if ! grep -q "defender.$short_name" "$WORKFLOW"; then
        echo "  ERROR: Service 'defender.$short_name' not found in matrix"
        ERRORS=$((ERRORS + 1))
    fi
    
    if ! grep -q "service_dir: \"$service\"" "$WORKFLOW"; then
        echo "  ERROR: Service directory '$service' not found in matrix"
        ERRORS=$((ERRORS + 1))
    fi
done

if ! grep -q '"ALL"' "$WORKFLOW"; then
    echo "  ERROR: 'ALL' option not found"
    ERRORS=$((ERRORS + 1))
fi

echo ""
echo "Checking: promote-image-tag.yml"
WORKFLOW="${REPO_ROOT}/.github/workflows/promote-image-tag.yml"

if [ ! -f "$WORKFLOW" ]; then
    echo "  ERROR: Workflow file not found"
    exit 1
fi

for service in "${all_systems[@]}"; do
    if ! grep -q "\"$service\"" "$WORKFLOW"; then
        echo "  ERROR: Service '$service' not found in options"
        ERRORS=$((ERRORS + 1))
    fi
done

if ! grep -q '"ALL"' "$WORKFLOW"; then
    echo "  ERROR: 'ALL' option not found"
    ERRORS=$((ERRORS + 1))
fi

echo ""
if [ $ERRORS -eq 0 ]; then
    echo "✓ All workflows are in sync with all_systems.sh"
    exit 0
else
    echo "✗ Found $ERRORS error(s)"
    echo ""
    echo "To update workflows, run:"
    echo "  ./scripts/update-workflow-services.sh"
    exit 1
fi
