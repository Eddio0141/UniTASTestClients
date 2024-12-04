#!/bin/bash

# config
UNITAS_REPO="" # NOTE: fill this in to wherever you cloned UniTAS
PROFILE=debug

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

if [ -d "$TEST_DIR" ]; then
    rm "$TEST_DIR" -r
fi

mkdir -p "$TEST_DIR"
cp "test-runner/target/${PROFILE}/test-runner" "$TEST_DIR"

EXCLUDE_TEST_CLIENTS_REPO_DIRS=(
    "MiscTestGame"
    "nix"
    "test-runner"
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

cp "$UNITAS_BUILD" "${TEST_DIR}/UniTAS" -r

# shellcheck disable=SC2090,SC2086
exec $TEST_DIR/test-runner --github-token "$(gh auth token)" $GAME_ARGS