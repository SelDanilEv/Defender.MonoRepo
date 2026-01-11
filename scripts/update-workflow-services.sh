#!/bin/bash

. $(dirname "$0")/all_systems.sh

echo "Service options for GitHub Actions workflows:"
echo ""
echo "        options:"
./scripts/generate-service-options.sh
echo ""
echo "Copy the output above to replace the 'options:' section in:"
echo "  - .github/workflows/docker-build-publish.yml"
echo "  - .github/workflows/promote-image-tag.yml"
echo ""
echo "Or run: ./scripts/validate-workflow-services.sh to check if workflows are in sync"
