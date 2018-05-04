#!/bin/sh

set -e

MY_DIR=$(dirname "$0")

cd "${MY_DIR}"

ARTIFACTS_DIR="$(pwd)/artifacts"

echo "Output dir: ${ARTIFACTS_DIR}"
mkdir -pv "${ARTIFACTS_DIR}"

echo "Building RabbitLink package"
dotnet pack ./src/RabbitLink --configuration Release -o "${ARTIFACTS_DIR}/"

echo "Building RabbitLink.Serialization.Json package"
dotnet pack ./src/RabbitLink.Serialization.Json --configuration Release -o "${ARTIFACTS_DIR}/"
