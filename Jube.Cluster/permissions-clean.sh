#!/bin/bash
# permissions-clean.sh — run from the jube-cluster directory.
#
# Undoes what permissions-init.sh applied: ownership back to the invoking user,
# directories 755, files 644, shell scripts 755.
#
# docker/ and containerd/ are excluded. Recursing into Docker's data-root would
# reassign the UIDs baked into every image layer and strip the execute bit off
# every binary inside them — not something init can put back.
#
# Intended to be run with the stack down (see remove.sh), since it will pull
# ownership out from under Patroni and Redis while they hold their data dirs.

set -uo pipefail

BASE_DIR="$(pwd)"
OWNER="$(id -un):$(id -gn)"

# Trees this script must never touch. Mirrors permissions-init.sh.
EXCLUDE=(docker containerd images)

# --- Guards ------------------------------------------------------------------
if [ ! -f "$BASE_DIR/docker-compose.yml" ]; then
    echo "[ERROR] No docker-compose.yml in $BASE_DIR — run this from the jube-cluster directory."
    exit 1
fi

if [ "$BASE_DIR" = "/" ] || [ "$BASE_DIR" = "$HOME" ]; then
    echo "[ERROR] Refusing to run against $BASE_DIR."
    exit 1
fi

echo "Resetting ownership and permissions under $BASE_DIR"
echo "Owner: $OWNER"
echo "Excluding: ${EXCLUDE[*]}"

# Build the prune expression. Each excluded path is matched and pruned before
# the real expression is reached, so find never descends into it.
prune=()
for dir in "${EXCLUDE[@]}"; do
    prune+=( -path "$BASE_DIR/$dir" -prune -o )
done

# --- Ownership ---------------------------------------------------------------
sudo find "$BASE_DIR" "${prune[@]}" -exec chown "$OWNER" {} +

# --- Modes -------------------------------------------------------------------
# Directories: 755 (rwxr-xr-x)
sudo find "$BASE_DIR" "${prune[@]}" -type d -exec chmod 755 {} +

# Files: 644 (rw-r--r--)
sudo find "$BASE_DIR" "${prune[@]}" -type f -exec chmod 644 {} +

# Shell scripts back to executable
sudo find "$BASE_DIR" "${prune[@]}" -type f -name "*.sh" -exec chmod 755 {} +

# --- SELinux (optional) ------------------------------------------------------
# Drops the local fcontext rules permissions-init.sh added for the managed
# subtrees and restores their default contexts. Deliberately scoped to those
# paths — any rule or equivalence covering docker/ or containerd/ is left in
# place. Comment this block out if you want a permissions-only reset.
if command -v semanage &>/dev/null; then
    for dir in haproxy patroni pgbackrest redis jemp-files pfx; do
        [ -d "$BASE_DIR/$dir" ] || continue
        sudo semanage fcontext -d "$BASE_DIR/$dir(/.*)?" 2>/dev/null || true
        sudo restorecon -Rv "$BASE_DIR/$dir"
    done
    # Legacy blanket rule, if it is still present.
    sudo semanage fcontext -d "$BASE_DIR(/.*)?" 2>/dev/null || true
fi

echo "[INFO] Reset complete. docker/ and containerd/ untouched."