#!/usr/bin/env bash
# Usage: [FILTER="Ns.FixtureA;Ns.FixtureB"] Tools/BuildScripts/run-tests.sh [EditMode|PlayMode ...]   (default: EditMode then PlayMode)
# FILTER limits the run to those fixtures/tests (semicolon separated) - use it while iterating, run unfiltered before commit.
# Set UNITY to the Editor exe if it differs from the default install path.
UNITY="${UNITY:-/c/Program Files/Unity/Hub/Editor/6000.6.4f1/Editor/Unity.exe}"
mkdir -p Builds
if tasklist //FI "IMAGENAME eq Unity.exe" 2>/dev/null | grep -q Unity.exe; then
  echo "A Unity Editor is already running; close it first (two Editors cannot share the project)."; exit 2
fi
rc=0
for P in "${@:-EditMode PlayMode}"; do
  for PLAT in $P; do
    rm -f "Builds/$PLAT.xml"
    args=(-batchmode -nographics -projectPath . -runTests -testPlatform "$PLAT" -testResults "Builds/$PLAT.xml" -logFile "Builds/$PLAT.log")
    [ -n "$FILTER" ] && args+=(-testFilter "$FILTER")
    [ "$PLAT" = EditMode ] && args+=(-runSynchronously)   # no domain-reload round trips for pure EditMode tests
    start=$SECONDS
    "$UNITY" "${args[@]}"
    echo "== $PLAT exit=$? in $((SECONDS - start))s"
    grep -E "error CS" "Builds/$PLAT.log" | head -5
    grep -oE '<test-run [^>]*>' "Builds/$PLAT.xml" | grep -oE ' (result|total|passed|failed)="[^"]*"' | tr '\n' ' '; echo
    grep -E '<test-case [^>]*result="Failed"' "Builds/$PLAT.xml" | grep -oE 'fullname="[^"]*"' | head -10
    # slowest test cases, to spot where the time goes
    grep -oE '<test-case [^>]*>' "Builds/$PLAT.xml" | sed -E 's/.* fullname="([^"]*)".* duration="([^"]*)".*/\2 \1/' | sort -rn | head -5
    grep -q 'result="Passed"' <(grep -oE '<test-run [^>]*>' "Builds/$PLAT.xml") || rc=1
    # aborted PlayMode runs leave scratch scenes behind
    rm -f Assets/InitTestScene*.unity Assets/InitTestScene*.unity.meta
  done
done
exit $rc
