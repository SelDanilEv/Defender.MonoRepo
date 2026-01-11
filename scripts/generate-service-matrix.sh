#!/bin/bash

SCRIPT_DIR="$(cd "$(dirname "$0")" && pwd)"
. "$SCRIPT_DIR/all_systems.sh"

get_short_name() {
    "$SCRIPT_DIR/map-service-name.sh" "$1"
}

for service in "${all_systems[@]}"; do
    short_name=$(get_short_name "$service")
    
    if [ "$service" == "Defender.Portal" ]; then
        dockerfile="Dockerfile.Portal"
        project_type="WebUI"
    else
        dockerfile="Dockerfile.Service"
        project_type="WebApi"
    fi
    
    echo "          - name: \"defender.$short_name\""
    echo "            dockerfile: \"$dockerfile\""
    echo "            project_type: \"$project_type\""
    echo "            service_dir: \"$service\""
done
