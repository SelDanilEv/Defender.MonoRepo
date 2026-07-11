#!/usr/bin/env bash
set -euo pipefail

chart="helm/service-template"
travel_values="$chart/values-travel-calendar.yaml"

image_value() {
  local key="$1"
  local file="$2"

  awk -v key="$key" '
    /^image:[[:space:]]*$/ { in_image = 1; next }
    in_image && /^[^[:space:]]/ { exit }
    in_image && $0 ~ "^[[:space:]]+" key ":[[:space:]]*" {
      sub("^[[:space:]]+" key ":[[:space:]]*", "")
      sub(/[[:space:]]+#.*$/, "")
      gsub(/[\047\042]/, "")
      print
      exit
    }
  ' "$file"
}

for values in "$chart"/values-*.yaml; do
  echo "Linting $values"
  helm lint "$chart" --values "$values"

  rendered="$(mktemp)"
  trap 'rm -f "$rendered"' EXIT
  helm template delivery-validation "$chart" --values "$values" --show-only templates/deployment.yaml > "$rendered"

  digest="$(image_value digest "$values")"
  if [[ -n "$digest" ]]; then
    repository="$(image_value repository "$values")"
    if [[ ! "$digest" =~ ^sha256:[a-f0-9]{64}$ ]]; then
      echo "Invalid image.digest in $values: $digest" >&2
      exit 1
    fi

    expected_image="image: \"$repository@$digest\""
    if ! grep -Fq "$expected_image" "$rendered"; then
      echo "Digest image did not render immutably for $values; expected $expected_image" >&2
      exit 1
    fi
  fi

  rm -f "$rendered"
  trap - EXIT
done

travel_tag="$(image_value tag "$travel_values")"
travel_digest="$(image_value digest "$travel_values")"
travel_repository="$(image_value repository "$travel_values")"

if [[ "$travel_tag" == "latest" ]]; then
  echo "Travel Calendar must not use image.tag latest" >&2
  exit 1
fi

if [[ ! "$travel_digest" =~ ^sha256:[a-f0-9]{64}$ ]]; then
  echo "Travel Calendar must use a sha256 image.digest" >&2
  exit 1
fi

rendered="$(mktemp)"
trap 'rm -f "$rendered"' EXIT
helm template travel-calendar-validation "$chart" --values "$travel_values" --show-only templates/deployment.yaml > "$rendered"
expected_travel_image="image: \"$travel_repository@$travel_digest\""

if ! grep -Fq "$expected_travel_image" "$rendered"; then
  echo "Travel Calendar did not render immutable image $expected_travel_image" >&2
  exit 1
fi

if grep -Fq "image: \"$travel_repository:latest\"" "$rendered"; then
  echo "Travel Calendar rendered latest image tag" >&2
  exit 1
fi

echo "Helm delivery validation passed."
