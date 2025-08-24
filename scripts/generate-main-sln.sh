#!/bin/bash

. $(dirname "$0")/all_systems.sh
. $(dirname "$0")/all_libs.sh

SOLUTION="../Defender.Core.sln"

add_projects_from() {
    local folders=("$@")
    for folder in "${folders[@]}"; do
        pushd "$folder" > /dev/null || continue
        for proj in $(find . -name '*.csproj'); do
            dotnet sln "$SOLUTION" add "$proj" --solution-folder "$folder"
        done
        popd > /dev/null
    done
}

add_projects_from "${all_systems[@]}"
add_projects_from "${all_libs[@]}"