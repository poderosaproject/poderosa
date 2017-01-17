#!/bin/bash

PROJDIR="$(dirname $0)/.."
DISTBASE="$PROJDIR/dist"

for z in "$DISTBASE"/*.zip; do
  echo "$z";
  gpg --armor --detach-sig "$z"
done
