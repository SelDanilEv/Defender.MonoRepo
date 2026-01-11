#!/bin/bash

. $(dirname "$0")/all_systems.sh

echo "          - \"ALL\""
for service in "${all_systems[@]}"; do
    echo "          - \"$service\""
done
