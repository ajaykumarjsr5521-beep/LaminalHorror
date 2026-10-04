#!/usr/bin/env bash
# Usage: Tools/BuildScripts/run-tests.sh [EditMode|PlayMode ...]   (default: both)
# Set UNITY to the Editor exe if it differs from the default install path.
UNITY="${UNITY:-/c/Program Files/Unity/Hub/Editor/6000.6.4f1/Editor/Unity.exe}"
mkdir -p Builds
rc=0
for P in "${@:-EditMode PlayMode}"; do
  for PLAT in $P; do
    rm -f "Builds/$PLAT.xml"
    "$UNITY" -batchmode -nographics -projectPath . -runTests -testPlatform "$PLAT" -testResults "Builds/$PLAT.xml" -logFile "Builds/$PLAT.log"
    echo "== $PLAT exit=$?"
    grep -E "error CS" "Builds/$PLAT.log" | head -5
    grep -oE '<test-run [^>]*>' "Builds/$PLAT.xml" | grep -oE ' (result|total|passed|failed)="[^"]*"' | tr '\n' ' '; echo
    grep -E '<test-case [^>]*result="Failed"' "Builds/$PLAT.xml" | grep -oE 'fullname="[^"]*"' | head -10
    grep -q 'result="Passed"' <(grep -oE '<test-run [^>]*>' "Builds/$PLAT.xml") || rc=1
  done
done
exit $rc
