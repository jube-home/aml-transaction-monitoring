#!/bin/bash

ENV_FILE="$(dirname "$0")/.env"

if [ ! -f "$ENV_FILE" ]; then
    echo "ERROR: .env file not found at $ENV_FILE — aborting."
    exit 1
fi

set -a
source "$ENV_FILE"
set +a

echo "Deploying jube-cluster..."
docker stack deploy -c docker-compose.yml jube-cluster