#!/usr/bin/env bash
#
# backup.sh — defensive pgBackRest backup wrapper for a Patroni cluster
# running in Docker.
#
# Usage:
#   ./backup.sh [full|diff|incr]                 (default: full)
#   RUN_DEEP_VERIFY=y ./backup.sh diff           (also checksum the repo)
#   HEALTHCHECK_URL=https://hc-ping.com/<uuid> ./backup.sh full
#
# What it does, in order:
#   1. Takes an exclusive lock so two backups can never overlap
#   2. Finds the current primary by asking Postgres itself (pg_is_in_recovery),
#      and aborts if it finds zero or more than one primary (split-brain guard)
#   3. Runs `pgbackrest check` as a pre-flight — fails fast if the stanza,
#      repo, or archiving is broken *before* spending hours on a backup
#   4. Runs the backup (no -t/-i, so it is cron-safe)
#   5. Verifies the result from `pgbackrest info --output=json`:
#      newest backup must be error-free, recent, and reported by pgBackRest
#   6. Optionally deep-verifies the repository (RUN_DEEP_VERIFY=y)
#   7. Pings HEALTHCHECK_URL on success, HEALTHCHECK_URL/fail on failure,
#      and exits non-zero on any failure so cron/monitoring can alert
#
set -euo pipefail

# ---- configuration -----------------------------------------------------------
STANZA="postgres-cluster"
CONTAINER_FILTER="patroni"        # docker ps --filter name=<this> must match your Patroni containers
PG_USER="postgres"
PGBACKREST_CONF="/etc/pgbackrest/pgbackrest.conf"
PGBACKREST_LOCK_PATH="${PGBACKREST_LOCK_PATH:-/var/lib/postgresql/tmp/pgbackrest-lock}"  # explicit, hardened container may not have a writable default
BACKUP_TYPE="${1:-full}"
LOCK_FILE="/tmp/pgbackrest-backup.lock"
LOG_DIR="${LOG_DIR:-$HOME/jube-cluster/pgbackrest-logs}"
MAX_BACKUP_AGE_MIN=30             # newest backup's stop time must be within this many minutes
ARCHIVE_CHECK="${ARCHIVE_CHECK:-y}"
ARCHIVE_TIMEOUT="120"
RUN_DEEP_VERIFY="${RUN_DEEP_VERIFY:-n}"   # "y" runs `pgbackrest verify` (checksums repo files; slow)
HEALTHCHECK_URL="${HEALTHCHECK_URL:-}"    # dead-man's-switch ping URL; empty = disabled
NAS_MARKER="${NAS_MARKER:-}"              # e.g. ".on-nas" — production tripwire, see notes; empty = disabled
LOG_KEEP_DAYS=30                  # prune wrapper logs older than this
# ------------------------------------------------------------------------------

# All container commands run as the postgres user — matching how the stanza
# was created — so socket, lock-path, and repo permissions line up.
DEXEC=(docker exec -u "$PG_USER")
PGBR=(pgbackrest --config="$PGBACKREST_CONF" --lock-path="$PGBACKREST_LOCK_PATH" --stanza="$STANZA")

mkdir -p "$LOG_DIR"
LOG_FILE="$LOG_DIR/backup-$(date +%Y%m%d-%H%M%S).log"
find "$LOG_DIR" -name 'backup-*.log' -mtime +"$LOG_KEEP_DAYS" -delete 2>/dev/null || true

log() { printf '%s [%s] %s\n' "$(date -Is)" "$1" "${*:2}" | tee -a "$LOG_FILE"; }

ping_hc() {  # ping_hc ok|fail — best-effort, never affects exit status
  [[ -n "$HEALTHCHECK_URL" ]] || return 0
  local url="$HEALTHCHECK_URL"
  [[ "$1" == "fail" ]] && url="$HEALTHCHECK_URL/fail"
  curl -fsS -m 10 --retry 3 -o /dev/null "$url" 2>>"$LOG_FILE" || log WARN "healthcheck ping failed"
}

die() { log ERROR "$@"; ping_hc fail; exit 1; }

case "$BACKUP_TYPE" in
  full|diff|incr) ;;
  *) die "invalid backup type '$BACKUP_TYPE' (expected full|diff|incr)" ;;
esac

command -v docker >/dev/null || die "docker not found in PATH"
command -v python3 >/dev/null || die "python3 not found in PATH (needed to parse pgbackrest info JSON)"

# ---- 1. single-instance lock -------------------------------------------------
exec 9>"$LOCK_FILE"
flock -n 9 || die "another backup appears to be running (lock held: $LOCK_FILE)"
log INFO "lock acquired, backup type: $BACKUP_TYPE"

# ---- 2. locate the primary (and only one primary) ----------------------------
mapfile -t CANDIDATES < <(docker ps --filter "name=${CONTAINER_FILTER}" --format '{{.Names}}')
[[ ${#CANDIDATES[@]} -gt 0 ]] || die "no running containers match filter 'name=${CONTAINER_FILTER}'"
log INFO "candidate containers: ${CANDIDATES[*]}"

PRIMARY=""
for c in "${CANDIDATES[@]}"; do
  in_recovery="$("${DEXEC[@]}" "$c" psql -U "$PG_USER" -tAc 'SELECT pg_is_in_recovery()' 2>/dev/null | tr -d '[:space:]' || true)"
  case "$in_recovery" in
    f)
      if [[ -n "$PRIMARY" ]]; then
        die "multiple primaries detected ('$PRIMARY' and '$c') — possible split-brain, refusing to back up"
      fi
      PRIMARY="$c"
      ;;
    t)  log INFO "$c is a replica" ;;
    *)  log WARN "$c did not answer pg_is_in_recovery() cleanly (got '${in_recovery:-<empty>}') — skipping" ;;
  esac
done
[[ -n "$PRIMARY" ]] || die "no primary found among: ${CANDIDATES[*]}"
log INFO "primary is: $PRIMARY"

# ---- 2b. NAS tripwire (production) -------------------------------------------
# If NAS_MARKER is set, the container must be able to see that file in the
# repo. Its absence means the repo path is bare local disk — the NAS mount is
# missing or the bind went stale — and backing up would quietly write to the
# wrong place. Create the marker ONCE while the NAS is properly mounted:
#   sudo touch /mnt/pgbackrest/.on-nas
if [[ -n "$NAS_MARKER" ]]; then
  if ! "${DEXEC[@]}" "$PRIMARY" test -e "/var/lib/pgbackrest/$NAS_MARKER"; then
    die "NAS marker '/var/lib/pgbackrest/$NAS_MARKER' not visible in container — repo is not on the NAS (mount missing or stale bind); refusing to back up to the wrong storage"
  fi
  log INFO "NAS marker present — repo storage confirmed"
fi

# ---- 3. pre-flight: stanza + archiving sanity check --------------------------
log INFO "running pre-flight 'pgbackrest check'"
if ! "${DEXEC[@]}" "$PRIMARY" "${PGBR[@]}" \
      --archive-timeout="$ARCHIVE_TIMEOUT" check >>"$LOG_FILE" 2>&1; then
  die "pre-flight 'pgbackrest check' failed — fix the stanza/repo/archiving before backing up (see $LOG_FILE)"
fi
log INFO "pre-flight check passed"

# ---- 4. run the backup -------------------------------------------------------
START_EPOCH="$(date +%s)"
log INFO "starting $BACKUP_TYPE backup of stanza '$STANZA'"
if ! "${DEXEC[@]}" "$PRIMARY" "${PGBR[@]}" \
      --type="$BACKUP_TYPE" \
      --archive-check="$ARCHIVE_CHECK" \
      --archive-timeout="$ARCHIVE_TIMEOUT" \
      --log-level-console=info \
      backup >>"$LOG_FILE" 2>&1; then
  die "pgbackrest backup FAILED (see $LOG_FILE)"
fi
log INFO "backup command completed in $(( $(date +%s) - START_EPOCH ))s"

# ---- 5. verify the result from pgbackrest's own metadata ---------------------
# --log-level-console=error: the conf sets console logging to info, which would
# prepend INFO lines to stdout and corrupt the JSON.
INFO_JSON="$("${DEXEC[@]}" "$PRIMARY" "${PGBR[@]}" --log-level-console=error info --output=json 2>>"$LOG_FILE")" \
  || die "could not retrieve 'pgbackrest info' after backup"

# Hand the JSON to python via a temp file: python reads its *program* from
# stdin here (the heredoc), so piping the JSON to stdin as well cannot work —
# the heredoc wins and the pipe is silently discarded.
INFO_TMP="$(mktemp)"
trap 'rm -f "$INFO_TMP"' EXIT
printf '%s' "$INFO_JSON" > "$INFO_TMP"

VERIFY_OUT="$(python3 - "$MAX_BACKUP_AGE_MIN" "$BACKUP_TYPE" "$INFO_TMP" <<'PYEOF'
import json, sys, time

def fail(msg):
    print(msg)          # stdout, so the wrapper's $() captures it for the error message
    raise SystemExit(1)

max_age_min = int(sys.argv[1])
requested_type = sys.argv[2]

with open(sys.argv[3]) as f:
    raw = f.read()
start = raw.find("[")
if start < 0:
    fail(f"FAIL: pgbackrest info returned no JSON: {raw[:200]!r}")
data = json.loads(raw[start:])
stanza = data[0]
if stanza.get("status", {}).get("code", -1) != 0:
    fail(f"FAIL: stanza status is not ok: {stanza.get('status')}")

backups = stanza.get("backup", [])
if not backups:
    fail("FAIL: no backups listed in repository")

latest = backups[-1]
label = latest.get("label", "?")

if latest.get("error", True):
    fail(f"FAIL: newest backup {label} is flagged with errors: {latest.get('error-list')}")

stop = latest["timestamp"]["stop"]
age_min = (time.time() - stop) / 60
if age_min > max_age_min:
    fail(f"FAIL: newest backup {label} is {age_min:.0f} min old — the backup just run is not the newest in the repo")

btype = latest.get("type", "?")
# pgBackRest silently upgrades diff/incr to full when no prior full exists — that's fine, but say so.
note = "" if btype == requested_type else f" (requested {requested_type}, pgBackRest ran {btype})"

size = latest.get("info", {}).get("repository", {}).get("delta", 0)
print(f"OK: {label} type={btype}{note} repo-delta={size/1024/1024:.1f}MiB age={age_min:.1f}min")
PYEOF
)" || die "post-backup verification failed: $VERIFY_OUT"
log INFO "verification: $VERIFY_OUT"

# ---- 6. optional deep verify (checksums files in the repo; slow) -------------
if [[ "$RUN_DEEP_VERIFY" == "y" ]]; then
  log INFO "running deep 'pgbackrest verify' (this can take a while)"
  if ! "${DEXEC[@]}" "$PRIMARY" "${PGBR[@]}" verify >>"$LOG_FILE" 2>&1; then
    die "deep verify FAILED — repository may be corrupt (see $LOG_FILE)"
  fi
  log INFO "deep verify passed"
fi

log INFO "backup completed and verified successfully"
ping_hc ok
exit 0

# ------------------------------------------------------------------------------
# Crontab (crontab -e on the host that runs the backups):
#
#   See crontab.txt alongside this script. In production also set
#   NAS_MARKER=.on-nas there (after touching the marker file once on the
#   mounted NAS) so a missing/stale mount fails the run instead of silently
#   writing to local disk.
#
# The script pings HEALTHCHECK_URL itself on success and <url>/fail on failure,
# so no curl needed in the crontab. Configure the healthcheck to expect a ping
# every 24h with a grace period of a few hours; silence = alert.
#
# Retention lives in pgbackrest.conf: repo1-retention-full=2 keeps two weekly
# fulls (~8–14 days of PITR). Raise to 4–5 if a client context needs ~30 days.
#
# Notes
#
# * archive-check stays ON: with it off a "successful" backup can be
#   unrestorable to a consistent state if the WAL needed to reach consistency
#   never made it to the archive. Override explicitly if you must:
#     ARCHIVE_CHECK=n ./backup.sh full
#
# * Production repo is on the NAS. Two hard requirements:
#   (1) The NAS must be mounted on ALL hosts that can host the primary, at the
#       same path, BEFORE docker starts — order docker.service after the mount
#       unit, or the containers bind an empty underlay (the stale-bind failure).
#   (2) Prefer NFS or, better, the NAS's S3/MinIO service (repo1-type=s3) over
#       CIFS — CIFS's POSIX/fsync semantics are a poor fit for a pgBackRest
#       repo. With repo1-type=s3, requirement (1) disappears entirely.
#
# * All docker exec calls run -u postgres and pass --config explicitly,
#   matching stanza-create. No -t/-i anywhere: TTYs break under cron.
#
# * Progress: tail the newest file in $LOG_DIR.
#
# * A backup is proven by a restore. Schedule a monthly restore drill into a
#   scratch container; until one has succeeded, treat the repo as unproven.
# ------------------------------------------------------------------------------