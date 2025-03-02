#!/bin/bash

# config
UNITAS_REPO="" # NOTE: fill this in to wherever you cloned UniTAS
PROFILE=debug
BEPINEX_DIR="" # NOTE: fill this in to wherever you downloaded and extracted BepInEx

# setup stuff
SCRIPT_DIR=$( cd -- "$( dirname -- "${BASH_SOURCE[0]}" )" &> /dev/null && pwd )
TEST_DIR="${SCRIPT_DIR}/test-runner/target/${PROFILE}/test"

if [ ! -d "$UNITAS_REPO" ]; then
    echo "You need to enter a valid directory for UNITAS_REPO"
    exit 1
fi

UNITAS_BUILD="${UNITAS_REPO}/UniTAS/Patcher/bin/Release"
if [ ! -d "$UNITAS_BUILD" ]; then
    echo "You need to build UniTAS with the Release profile"
    exit 1
fi
UNITAS_RS_BUILD="${UNITAS_REPO}/unitas-rs/target/release/libunitas_rs.so"
if [ ! -f "$UNITAS_RS_BUILD" ]; then
    echo "no unitas-rs build found, this will be an error in the future"
fi

if [ -d "$TEST_DIR" ]; then
    rm "$TEST_DIR" -r
fi

mkdir -p "$TEST_DIR"
cp "test-runner/target/${PROFILE}/test-runner" "$TEST_DIR"

EXCLUDE_TEST_CLIENTS_REPO_DIRS=(
    "MiscTestGame"
    "nix"
    "test-runner"
    "hooks"
    "3.0.0f5-base"
    "3.4.0f5-base"
    "UnityShared"
)

GAME_ARGS=""
for GAME_DIR in "$SCRIPT_DIR"/*; do
    if [ ! -d "$GAME_DIR" ]; then
        continue
    fi

    GAME_NAME=$(basename "$GAME_DIR")

    if [[ " ${EXCLUDE_TEST_CLIENTS_REPO_DIRS[*]} " =~ [[:space:]]${GAME_NAME}[[:space:]] ]]; then
        continue
    fi

    # shellcheck disable=SC2089
    GAME_ARGS="-r ${GAME_NAME}=\"${GAME_DIR}/build\" ${GAME_ARGS}"
done

if [ -d "$BEPINEX_DIR" ]; then
    BEPINEX_DIR="--bepinex-path $BEPINEX_DIR"
else
    BEPINEX_DIR=""
fi

cp "$UNITAS_BUILD" "${TEST_DIR}/UniTAS" -r
if [ -f "$UNITAS_RS_BUILD" ]; then
    cp "$UNITAS_RS_BUILD" "${TEST_DIR}/UniTAS/BepInEx/patchers/UniTAS"
fi

# shellcheck disable=SC2090,SC2086
exec $TEST_DIR/test-runner --github-token "$(gh auth token)" $GAME_ARGS $BEPINEX_DIR
