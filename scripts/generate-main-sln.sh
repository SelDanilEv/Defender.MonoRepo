#!/bin/bash

. $(dirname "$0")/all_systems.sh
. $(dirname "$0")/all_libs.sh

SOLUTION_NAME="Defender.Core"
SOLUTION_DIR="$(pwd)"
SRC_DIR="$SOLUTION_DIR/src"
SOLUTION="$SRC_DIR/$SOLUTION_NAME.sln"
shopt -s nullglob

rm -f "$SOLUTION" "$SOLUTION_DIR/$SOLUTION_NAME.sln" "$SOLUTION_DIR/$SOLUTION_NAME.slnx" "$SRC_DIR/$SOLUTION_NAME.slnx"
mkdir -p "$SRC_DIR"
dotnet new sln -n "$SOLUTION_NAME" --format sln -o "$SRC_DIR"

solution_folder_name() {
    echo "${1##*/}"
}

add_projects_from() {
    local folders=("$@")
    for folder in "${folders[@]}"; do
        local path="$SRC_DIR/$folder"
        local folder_name
        folder_name=$(solution_folder_name "$folder")
        for proj in "$path"/src/*/*.csproj; do
            dotnet sln "$SOLUTION" add "$proj" --solution-folder "$folder_name"
        done
    done
}

add_libraries() {
    local folders=("$@")
    for folder in "${folders[@]}"; do
        local path="$SRC_DIR/$folder"
        local folder_name
        folder_name=$(solution_folder_name "$folder")
        for proj in "$path"/src/*/*.csproj; do
            dotnet sln "$SOLUTION" remove "$proj" > /dev/null 2>&1 || true
            dotnet sln "$SOLUTION" add "$proj" --solution-folder "Libraries/$folder_name"
        done
    done
}

add_projects_from "${all_systems[@]}"
add_libraries "${all_libs[@]}"