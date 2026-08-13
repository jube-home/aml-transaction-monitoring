#!/usr/bin/env bash
#
# restore-drill.sh — prove pgBackRest backups by restoring them into a
# disposable, fully isolated scratch container. Never touches the live cluster.
#
# Usage:
#   ./restore-drill.sh full                                # restore latest, replay all WAL
#   ./restore-drill.sh pit "2026-07-22 10:45:00+03:00"     # point-in-time recovery
#
#   BACKUP_SET=20260722-104300F ./restore-drill.sh full    # restore a specific backup label
#   KEEP=y ./restore-drill.sh full                         # keep the scratch container afterwards
#
# Safety properties (why this cannot hurt the live cluster):
#   * The scratch container runs with --network none — it cannot reach
#     Patroni, etcd, HAProxy, or anything else.
#   * The repo is mounted READ-ONLY — the drill physically cannot write to it.
#   * The restore is made with --archive-mode=off (and started with
#     archive_mode=off again, belt and braces) so the restored instance can
#     never push WAL or a new timeline back into the repository.
#   * PGDATA lives inside the scratch container's own filesystem and vanishes
#     with it.
#
set -euo pipefail

# ---- configuration -----------------------------------------------------------
STANZA="postgres-cluster"
REPO_HOST_PATH="/mnt/pgbackrest"                       # repo on the host (local now, NAS in prod)
CONF_HOST_PATH="$HOME/jube-cluster/pgbackrest/pgbackrest.conf"
PGBACKREST_LOCK_PATH="${PGBACKREST_LOCK_PATH:-/var/lib/postgresql/tmp/pgbackrest-lock}"  # explicit, hardened container may not have a writable default
CONTAINER_FILTER="patroni"                             # used to discover the image to drill with
DRILL_NAME="pgbr-restore-drill"
RESTORE_PGDATA="/restore/pgdata"                       # inside the scratch container
MODE="${1:-full}"                                      # full | pit
TARGET="${2:-}"                                        # required for pit, e.g. "2026-07-22 10:45:00+03:00"
BACKUP_SET="${BACKUP_SET:-}"                           # optional --set=<label>
KEEP="${KEEP:-n}"                                      # y = keep scratch container after success
LOCK_FILE="/tmp/pgbackrest-restore-drill.lock"
LOG_DIR="${LOG_DIR:-$HOME/jube-cluster/pgbackrest-logs}"
RECOVERY_TIMEOUT_S=1800                                # max wait for WAL replay to finish
VERIFY_SQL="${VERIFY_SQL:-SELECT datname, pg_size_pretty(pg_database_size(datname)) AS size FROM pg_database WHERE NOT datistemplate ORDER BY datname;}"
# ------------------------------------------------------------------------------

mkdir -p "$LOG_DIR"
LOG_FILE="$LOG_DIR/restore-drill-$(date +%Y%m%d-%H%M%S).log"

log() { printf '%s [%s] %s\n' "$(date -Is)" "$1" "${*:2}" | tee -a "$LOG_FILE"; }
die() { log ERROR "$@"; log ERROR "scratch container '$DRILL_NAME' left in place for inspection"; exit 1; }

case "$MODE" in
  full) ;;
  pit)  [[ -n "$TARGET" ]] || { log ERROR "pit mode needs a target, e.g.: ./restore-drill.sh pit \"2026-07-22 10:45:00+03:00\""; exit 1; } ;;
  *)    log ERROR "invalid mode '$MODE' (expected full|pit)"; exit 1 ;;
esac

[[ -d "$REPO_HOST_PATH" ]] || { log ERROR "repo path $REPO_HOST_PATH does not exist on this host"; exit 1; }
[[ -f "$CONF_HOST_PATH" ]] || { log ERROR "pgbackrest.conf not found at $CONF_HOST_PATH"; exit 1; }

exec 9>"$LOCK_FILE"
flock -n 9 || { log ERROR "another drill appears to be running"; exit 1; }

# ---- 1. discover the image from the running cluster (guarantees version match)
IMAGE="${PATRONI_IMAGE:-$(docker ps --filter "name=${CONTAINER_FILTER}" --format '{{.Image}}' | head -1)}"
[[ -n "$IMAGE" ]] || die "could not discover Patroni image (no running containers match '${CONTAINER_FILTER}' and PATRONI_IMAGE not set)"
log INFO "drill image: $IMAGE"
log INFO "mode: $MODE${TARGET:+ target: $TARGET}${BACKUP_SET:+ backup-set: $BACKUP_SET}"

# ---- 2. launch the isolated scratch container --------------------------------
docker rm -f "$DRILL_NAME" >/dev/null 2>&1 || true
docker run -d --name "$DRILL_NAME" \
  --network none \
  --entrypoint sleep \
  -v "$REPO_HOST_PATH":/var/lib/pgbackrest:ro \
  -v "$CONF_HOST_PATH":/etc/pgbackrest/pgbackrest.conf:ro \
  "$IMAGE" infinity >>"$LOG_FILE" 2>&1 \
  || die "failed to start scratch container"
log INFO "scratch container up (network: none, repo: read-only)"

DEXEC=(docker exec -u postgres "$DRILL_NAME")

docker exec "$DRILL_NAME" sh -c "mkdir -p $RESTORE_PGDATA '$PGBACKREST_LOCK_PATH' && chown -R postgres:postgres /restore '$PGBACKREST_LOCK_PATH' && chmod 700 $RESTORE_PGDATA" \
  || die "could not prepare $RESTORE_PGDATA and lock path"

PGBIN="$(docker exec "$DRILL_NAME" sh -c 'command -v pg_ctl >/dev/null && dirname "$(command -v pg_ctl)" || ls -d /usr/lib/postgresql/*/bin 2>/dev/null | sort -V | tail -1')"
[[ -n "$PGBIN" ]] || die "could not locate postgres binaries in the image"
log INFO "postgres binaries: $PGBIN"

# ---- 3. restore --------------------------------------------------------------
RESTORE_ARGS=(
  --config=/etc/pgbackrest/pgbackrest.conf
  --lock-path="$PGBACKREST_LOCK_PATH"
  --stanza="$STANZA"
  --pg1-path="$RESTORE_PGDATA"
  --archive-mode=off
  --log-level-console=info
)
[[ -n "$BACKUP_SET" ]] && RESTORE_ARGS+=( --set="$BACKUP_SET" )
if [[ "$MODE" == "pit" ]]; then
  # pause at target so we can inspect before promoting — the drill verifies
  # recovery genuinely stopped at the requested moment
  RESTORE_ARGS+=( --type=time --target="$TARGET" --target-action=pause )
fi

log INFO "running pgbackrest restore"
START_EPOCH="$(date +%s)"
"${DEXEC[@]}" pgbackrest "${RESTORE_ARGS[@]}" restore >>"$LOG_FILE" 2>&1 \
  || die "pgbackrest restore FAILED (see $LOG_FILE)"
log INFO "restore completed in $(( $(date +%s) - START_EPOCH ))s"

# ---- 4. start postgres and let it replay WAL ---------------------------------
log INFO "starting restored postgres (archive_mode=off, socket only)"
"${DEXEC[@]}" "$PGBIN/pg_ctl" -D "$RESTORE_PGDATA" -w -t 300 \
  -l /tmp/drill-postgres.log \
  -o "-c archive_mode=off -c listen_addresses=''" start >>"$LOG_FILE" 2>&1 \
  || die "restored postgres failed to start — container log: docker exec $DRILL_NAME cat /tmp/drill-postgres.log"

psql_scratch() { "${DEXEC[@]}" psql -U postgres -tAc "$1"; }

wait_for() {  # wait_for <description> <sql> <expected> <timeout_s>
  local desc="$1" sql="$2" want="$3" timeout="$4" elapsed=0 got=""
  while (( elapsed < timeout )); do
    got="$(psql_scratch "$sql" 2>/dev/null | tr -d '[:space:]' || true)"
    [[ "$got" == "$want" ]] && return 0
    sleep 5; elapsed=$(( elapsed + 5 ))
  done
  die "timed out after ${timeout}s waiting for: $desc (last value: '${got:-<none>}')"
}

if [[ "$MODE" == "pit" ]]; then
  # recovery should reach the target and PAUSE (still in recovery)
  wait_for "recovery paused at target" "SELECT pg_is_wal_replay_paused()" "t" "$RECOVERY_TIMEOUT_S"
  REPLAYED_AT="$(psql_scratch 'SELECT pg_last_xact_replay_timestamp()')"
  log INFO "recovery paused; last replayed transaction at: ${REPLAYED_AT:-<none>} (requested target: $TARGET)"
  log INFO "promoting"
  psql_scratch "SELECT pg_promote(true, 60)" >/dev/null || die "pg_promote failed"
fi

wait_for "recovery to finish (pg_is_in_recovery = f)" "SELECT pg_is_in_recovery()" "f" "$RECOVERY_TIMEOUT_S"
log INFO "recovery complete — instance promoted on its own timeline"

# ---- 5. verification queries -------------------------------------------------
log INFO "running verification queries"
{
  echo "---- checkpoint / timeline ----"
  "${DEXEC[@]}" "$PGBIN/pg_controldata" "$RESTORE_PGDATA" | grep -E 'checkpoint|TimeLineID' || true
  echo "---- databases restored ----"
  "${DEXEC[@]}" psql -U postgres -c "$VERIFY_SQL"
} | tee -a "$LOG_FILE"

log INFO "RESTORE DRILL PASSED ($MODE${TARGET:+ @ $TARGET})"

# ---- 6. teardown -------------------------------------------------------------
if [[ "$KEEP" == "y" ]]; then
  log INFO "KEEP=y — scratch container '$DRILL_NAME' left running; connect with:"
  log INFO "  docker exec -it -u postgres $DRILL_NAME psql -U postgres"
else
  docker rm -f "$DRILL_NAME" >/dev/null 2>&1 || true
  log INFO "scratch container removed"
fi
exit 0

# ------------------------------------------------------------------------------
# Notes
#
# * VERIFY_SQL: override with a query that proves YOUR data, e.g.
#     VERIFY_SQL="SELECT max(created_date) FROM jube.archive" ./restore-drill.sh full
#   A drill that checks database sizes proves the restore ran; a drill that
#   checks your newest business row proves the restore is USEFUL.
#
# * PITR target format: "YYYY-MM-DD HH:MM:SS+TZ", e.g. "2026-07-22 10:45:00+03:00".
#   The target must fall AFTER the end of the backup being restored — pgBackRest
#   picks the newest backup before the target automatically; use BACKUP_SET to
#   pin a specific one.
#
# * The gold-standard PITR test: insert a canary row on the live primary, note
#   the time, wait a minute (WAL archives), drill to 30s BEFORE the insert and
#   confirm the row is absent, then drill to 30s AFTER and confirm it exists.
#
# * On failure the scratch container is kept for inspection:
#     docker exec pgbr-restore-drill cat /tmp/drill-postgres.log
#     docker rm -f pgbr-restore-drill    # when done
#
# * Monthly cron (first Saturday, after that morning's backup window):
#     0 5 1-7 * 6  /home/richard/jube-cluster/restore-drill.sh full
#   PITR drills are better run by hand — choosing a meaningful target is the
#   point of the exercise.
# ------------------------------------------------------------------------------