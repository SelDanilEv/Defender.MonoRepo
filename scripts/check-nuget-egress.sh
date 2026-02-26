#!/usr/bin/env bash

set -euo pipefail

# Verifies outbound HTTPS access required for NuGet restore/test.
ENDPOINTS=(
  "https://api.nuget.org/v3/index.json"
  "https://www.nuget.org"
)

check_with_curl() {
  local endpoint="$1"
  curl \
    --fail \
    --silent \
    --show-error \
    --location \
    --output /dev/null \
    --connect-timeout 5 \
    --max-time 20 \
    --retry 2 \
    --retry-delay 2 \
    "$endpoint"
}

check_with_wget() {
  local endpoint="$1"
  wget \
    --quiet \
    --tries=3 \
    --timeout=20 \
    --spider \
    "$endpoint"
}

echo "Running NuGet egress preflight..."

if ! command -v curl >/dev/null 2>&1 && ! command -v wget >/dev/null 2>&1; then
  echo "ERROR: Neither 'curl' nor 'wget' is available to perform egress checks."
  exit 1
fi

failed=0

for endpoint in "${ENDPOINTS[@]}"; do
  echo "Checking ${endpoint}"
  if command -v curl >/dev/null 2>&1; then
    if check_with_curl "$endpoint"; then
      echo "OK: ${endpoint}"
    else
      echo "ERROR: Unable to reach ${endpoint} over HTTPS."
      failed=1
    fi
  else
    if check_with_wget "$endpoint"; then
      echo "OK: ${endpoint}"
    else
      echo "ERROR: Unable to reach ${endpoint} over HTTPS."
      failed=1
    fi
  fi
done

if [ "$failed" -ne 0 ]; then
  echo "NuGet egress preflight failed. Ensure outbound HTTPS (443) to *.nuget.org is allowed."
  exit 1
fi

echo "NuGet egress preflight passed."
