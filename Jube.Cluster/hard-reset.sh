#!/bin/bash

# ─────────────────────────────────────────────
#  DANGER: This script will DESTROY and
#  reinitialise the jube-cluster Docker swarm.
#  All running services will be removed.
# ─────────────────────────────────────────────

echo ""
echo "  ⚠️  WARNING: DESTRUCTIVE OPERATION"
echo "  ════════════════════════════════════════"
echo "  This will perform a full reset of the"
echo "  jube-cluster swarm in the following order:"
echo ""
echo "  [DESTROY]"
echo "    1. Remove the 'jube-cluster' Docker stack"
echo "    2. Remove all jube-cluster volumes"
echo "    3. Force-leave the Docker swarm"
echo "    4. Prune all Docker networks"
echo "    5. Delete /var/lib/docker/swarm"
echo "    6. Restart Docker"
echo "  ════════════════════════════════════════"
echo ""
read -r -p "  Type CONFIRM to proceed, or anything else to abort: " response
echo ""

if [ "$response" != "CONFIRM" ]; then
  echo "  Aborted. No changes were made."
  exit 1
fi

echo "  Proceeding..."
echo ""

output=$(docker stack rm jube-cluster 2>&1)
echo "$output"
if ! echo "$output" | grep -q "Nothing found in stack: jube-cluster"; then
  sleep 15
fi

docker volume ls -q --filter name=jube-cluster | xargs -r docker volume rm
docker swarm leave --force
docker network prune -f
sudo rm -rf /var/lib/docker/swarm
sudo systemctl restart docker