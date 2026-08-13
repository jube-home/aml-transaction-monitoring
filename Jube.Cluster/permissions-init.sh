#!/bin/bash
# permissions-init.sh — run from the jube-cluster directory.
#
# Docker's data-root and containerd state now live inside this directory, so
# every recursive operation below is scoped to an explicit allow-list. Nothing
# here touches docker/ or containerd/: Docker manages its own ownership,
# SELinux types and per-container MCS categories under those trees.

set -uo pipefail

BASE_DIR="$(pwd)"

# Trees this script must never touch. images/ stays excluded as before.
EXCLUDE=(docker containerd images)

# Trees this script is responsible for.
MANAGED=(haproxy patroni pgbackrest redis jemp-files pfx)

echo "Applying SELinux and UID permissions to $BASE_DIR"
echo "Excluding: ${EXCLUDE[*]}"

# --- SELinux file contexts ---------------------------------------------------
# The previous blanket rule "$BASE_DIR(/.*)?" also matched docker/ and
# containerd/, so any restorecon or filesystem relabel would have overwritten
# Docker's labels. Drop it, then register only the managed subtrees.
sudo semanage fcontext -d "$BASE_DIR(/.*)?" 2>/dev/null || true

for dir in "${MANAGED[@]}"; do
    [ -d "$BASE_DIR/$dir" ] || continue
    sudo semanage fcontext -a -t container_file_t "$BASE_DIR/$dir(/.*)?" 2>/dev/null \
        || sudo semanage fcontext -m -t container_file_t "$BASE_DIR/$dir(/.*)?"
    sudo restorecon -Rv "$BASE_DIR/$dir"
done

# --- Ownership ---------------------------------------------------------------
# pgBackRest config (read by postgres UID 999 inside the Patroni containers)
sudo chown -R 70:"$(id -g)" "$BASE_DIR/pgbackrest"
sudo chmod -R 775 "$BASE_DIR/pgbackrest"

# Redis/Sentinel and Patroni/Postgres (UID 999)
sudo chown -R 999:"$(id -g)" "$BASE_DIR/redis"
sudo chmod -R 775 "$BASE_DIR/redis"
sudo chown -R 70:"$(id -g)" "$BASE_DIR/patroni"

# HAProxy (UID 99)
sudo chown -R 99:"$(id -g)" "$BASE_DIR/haproxy"

# --- Broad permissions on everything except the excluded trees ---------------
prune=()
for dir in "${EXCLUDE[@]}"; do prune+=( -not -name "$dir" ); done
sudo find "$BASE_DIR" -mindepth 1 -maxdepth 1 "${prune[@]}" -exec chmod -R 775 {} +

# --- Execution bits ----------------------------------------------------------
for script in deploy.sh database.sh remove.sh permissions-clean.sh hard-reset.sh; do
    [ -f "$BASE_DIR/$script" ] && sudo chmod +x "$BASE_DIR/$script"
done

# --- Container labels --------------------------------------------------------
# Applied per managed directory rather than recursively over $BASE_DIR (Swarm
# ignores the :Z volume flag, so the labels have to be set on disk). A
# recursive chcon from the top would rewrite the type and MCS level across
# Docker's overlay2 and containerd trees — the MCS flattening in particular
# would collapse the per-container categories that isolate containers.
if command -v sestatus &>/dev/null && sestatus | grep -q "SELinux status:.*enabled"; then
    echo "[INFO] SELinux detected and enabled — applying container labels..."

    for dir in "${MANAGED[@]}"; do
        [ -d "$BASE_DIR/$dir" ] || continue
        sudo chcon -Rt container_file_t -l s0 "$BASE_DIR/$dir" \
            || { echo "[ERROR] Failed to label $BASE_DIR/$dir"; exit 1; }
    done

    # Top-level files only (compose file, SQL, README). -maxdepth 1 -type f
    # means the excluded directories are never descended into.
    sudo find "$BASE_DIR" -mindepth 1 -maxdepth 1 -type f \
        -exec chcon -t container_file_t -l s0 {} + \
        || { echo "[ERROR] Failed to label top-level files"; exit 1; }

    echo "[INFO] SELinux labels applied successfully."
else
    echo "[INFO] SELinux not enabled — skipping container label configuration."
fi