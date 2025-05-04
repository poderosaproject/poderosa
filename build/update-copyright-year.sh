#!/bin/bash

if [ "$#" -ne 2 ]; then
    echo "Update copyright year"
    echo "Usage: $0 commit1 commit2"
    exit 1
fi
commit1=$1
commit2=$2

current_year=$(date +"%Y")

cd "$(dirname $(realpath "$0"))/.."

git diff --name-only "$commit1" "$commit2" \
| grep -E '\.cs$' \
| while IFS= read -r filepath; do
    if [[ -f "$filepath" ]]; then
        echo "Update $filepath"
        sed -i -E \
            -e "s/(Copyright\s+[0-9]{4}-)[0-9]{4}/\1${current_year}/" \
            -e "/Copyright\s+${current_year}/! s/(Copyright\s+[0-9]{4})(\s\w)/\1-${current_year}\2/" \
            "$filepath"
        if [[ $? -ne 0 ]]; then
            echo "Failed to update $filepath"
        fi
        unix2dos "$filepath"
    fi
done

echo "Update Plugin/VersionInfo.cs"
sed -i -E \
    -e "s/(Copyright\s+[0-9]{4}-)[0-9]{4}/\1${current_year}/" \
    -e "s/(COPYRIGHT_YEARS.*[0-9]{4}-)[0-9]{4}/\1${current_year}/" \
    Plugin/VersionInfo.cs
unix2dos Plugin/VersionInfo.cs
