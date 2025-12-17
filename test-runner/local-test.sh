#!/bin/bash

# config
unitas_repo="" # NOTE: fill this in to wherever you cloned UniTAS
profile=debug
bepinex_dir="" # NOTE: fill this in to wherever you downloaded and extracted BepInEx

# setup stuff
script_dir=$(pwd)
test_dir="${script_dir}/test-runner/target/${profile}/test"
clients_dir="${script_dir}/Clients"

if [ ! -d "$unitas_repo" ]; then
    echo "You need to enter a valid directory for unitas_repo"
    exit 1
fi

unitas_build="${unitas_repo}/UniTAS/Patcher/bin/Release"
if [ ! -d "$unitas_build" ]; then
    echo "You need to build UniTAS with the Release profile"
    exit 1
fi
unitas_rs_build="${unitas_repo}/unitas-rs/target/release/libunitas_rs.so"
if [ ! -f "$unitas_rs_build" ]; then
    echo "No unitas-rs build found"
    exit 1
fi

if [ ! -d "$clients_dir" ]; then
    echo "Run this script from the UniTASTestClients repo base directory"
    exit 1
fi

if [ -d "$test_dir" ]; then
    rm "$test_dir" -r
fi

mkdir -p "$test_dir"
cp "test-runner/target/${profile}/test-runner" "$test_dir"

game_args=""
for game_dir in "$clients_dir"/*; do
    if [ ! -d "$game_dir" ]; then
        continue
    fi

    game_name=$(basename "$game_dir")
    game_dir_build="${game_dir}/build"

    if [ ! -d "$game_dir_build" ]; then
        echo "game ${game_name} doesn't have a build directory, skipping"
        continue
    fi

    # game args are used literally as is
    # shellcheck disable=SC2089
    game_args="-r ${game_name}=\"${game_dir_build}\" ${game_args}"
done

if [ -d "$bepinex_dir" ]; then
    bepinex_dir="--bepinex-path $bepinex_dir"
else
    bepinex_dir=""
fi

cp "$unitas_build" "${test_dir}/UniTAS" -r
if [ -f "$unitas_rs_build" ]; then
    cp "$unitas_rs_build" "${test_dir}/UniTAS/BepInEx/patchers/UniTAS"
fi

# shellcheck disable=SC2090,SC2086
exec $test_dir/test-runner --github-token "$(gh auth token)" $game_args $bepinex_dir
