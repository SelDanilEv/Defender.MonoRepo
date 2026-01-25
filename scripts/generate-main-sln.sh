#!/bin/bash

. $(dirname "$0")/all_systems.sh
. $(dirname "$0")/all_libs.sh

SOLUTION_NAME="Defender.Core"
SOLUTION_DIR="$(pwd)"
SOLUTION="$SOLUTION_DIR/$SOLUTION_NAME.slnx"
shopt -s nullglob

rm -f "$SOLUTION"
dotnet new sln -n "$SOLUTION_NAME"

add_projects_from() {
    local folders=("$@")
    for folder in "${folders[@]}"; do
        for proj in "$folder"/src/*/*.csproj; do
            dotnet sln "$SOLUTION" add "$proj" --solution-folder "$folder"
        done
    done
}

add_libraries() {
    local folders=("$@")
    for folder in "${folders[@]}"; do
        for proj in "$folder"/src/*/*.csproj; do
            dotnet sln "$SOLUTION" remove "$proj" > /dev/null 2>&1 || true
            dotnet sln "$SOLUTION" add "$proj" --solution-folder "Libraries/$folder"
        done
    done
}

add_projects_from "${all_systems[@]}"
add_libraries "${all_libs[@]}"