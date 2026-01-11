#!/bin/bash

set -e

SERVICE="${1:-}"
IMAGE_TAG="${2:-}"
BRANCH="${3:-}"
VALUES_DIR="helm/service-template"

if [ -z "$SERVICE" ]; then
    echo "Error: service parameter is required"
    exit 1
fi

if [ -z "$IMAGE_TAG" ]; then
    echo "Error: image_tag parameter is required"
    exit 1
fi

if [ -z "$BRANCH" ]; then
    echo "Error: branch parameter is required"
    exit 1
fi

VALID_SERVICES=("portal" "user-management" "wallet" "risk-games" "notification" "job-scheduler" "identity" "budget-tracker")

if [ "$SERVICE" != "ALL" ]; then
    VALID=false
    for valid_service in "${VALID_SERVICES[@]}"; do
        if [ "$SERVICE" == "$valid_service" ]; then
            VALID=true
            break
        fi
    done
    
    if [ "$VALID" != "true" ]; then
        echo "Error: Invalid service '$SERVICE'. Valid services are: ${VALID_SERVICES[*]} or ALL"
        exit 1
    fi
fi

if ! command -v yq &> /dev/null; then
    echo "Installing yq..."
    YQ_VERSION="v4.40.5"
    YQ_BINARY="yq_linux_amd64"
    wget -q "https://github.com/mikefarah/yq/releases/download/${YQ_VERSION}/${YQ_BINARY}" -O /tmp/yq
    chmod +x /tmp/yq
    sudo mv /tmp/yq /usr/local/bin/yq
    echo "yq installed successfully"
fi

MODIFIED_FILES=()

if [ "$SERVICE" == "ALL" ]; then
    for service in "${VALID_SERVICES[@]}"; do
        VALUES_FILE="${VALUES_DIR}/values-${service}.yaml"
        
        if [ ! -f "$VALUES_FILE" ]; then
            echo "Error: Values file not found: $VALUES_FILE"
            exit 1
        fi
        
        yq '.image.tag = "'"$IMAGE_TAG"'"' -i "$VALUES_FILE"
        MODIFIED_FILES+=("$VALUES_FILE")
        echo "Updated $VALUES_FILE with tag: $IMAGE_TAG"
    done
else
    VALUES_FILE="${VALUES_DIR}/values-${SERVICE}.yaml"
    
    if [ ! -f "$VALUES_FILE" ]; then
        echo "Error: Values file not found: $VALUES_FILE"
        exit 1
    fi
    
    yq '.image.tag = "'"$IMAGE_TAG"'"' -i "$VALUES_FILE"
    MODIFIED_FILES+=("$VALUES_FILE")
    echo "Updated $VALUES_FILE with tag: $IMAGE_TAG"
fi

echo ""
echo "Modified files:"
for file in "${MODIFIED_FILES[@]}"; do
    echo "  - $file"
done
