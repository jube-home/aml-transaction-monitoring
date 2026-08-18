---
layout: default
title: Monitoring Reference
nav_order: 3
parent: Deploying with Jube Cluster
grand_parent: Getting Started
---

🚀 Get to pre-production in weeks, not months, with private [training](https://www.jube.io/jube-training) direct from Jube's developer — real sovereignty, zero vendor lock-in.

# Jube Cluster Monitoring Reference

Host-level CPU, RAM, and disk metrics are assumed to be collected at the node level and are not covered here. This
document covers the checks you run inside the stack itself:

1. **Per-container resource checks** — CPU and memory via `docker stats`.
2. **Port availability** — TCP-level probes confirming services are bound and accepting connections.
3. **Application health endpoints** — HTTP-level probes confirming services are operational.
4. **HAProxy traffic monitoring** — frontend byte-rate and status checks.
5. **PostgreSQL internals** — connections, replication lag, long-running queries.
6. **Redis internals** — memory pressure, replication offset, keyspace hit rate.
7. **Patroni internals** — cluster state, leader presence, timeline divergence.
8. **Sentinel internals** — master reachability, quorum, address consistency.
9. **Jube application monitors** — queue saturation, engine liveness, response time, elevation rate.
10. **Container log monitoring** — STDERR for Jube, STDOUT for infrastructure, restart detection.

All checks are bare shell commands. Wire them into whatever collection or alerting mechanism you use.

---

## 1. Host-Level Resource Note

> Suggested baseline thresholds for node-level collection:
>
> | Metric                      | Warning | Critical |
> |-----------------------------|---------|----------|
> | CPU utilisation (5-min avg) | > 70%   | > 90%    |
> | Memory utilisation          | > 75%   | > 90%    |
> | Disk utilisation            | > 75%   | > 90%    |

---

## 2. Per-Container Resource Checks

Run from the Swarm manager:

```bash
# All containers
docker stats --no-stream --format \
  "container={{.Name}} cpu={{.CPUPerc}} mem={{.MemPerc}} mem_usage={{.MemUsage}}"

# Specific container
docker stats --no-stream --format \
  "container={{.Name}} cpu={{.CPUPerc}} mem={{.MemPerc}}" \
  $(docker ps -qf "name=<container-name>")
```

Suggested thresholds:

| Container type                | CPU alert       | Memory alert   |
|--------------------------------|-----------------|-----------------|
| Database (Patroni/PostgreSQL) | > 75% sustained | > 85% of limit |
| Cache (Redis master)          | > 60% sustained | > 80% of limit |
| Consensus (etcd)              | > 50% sustained | > 70% of limit |
| Application (API / engine)    | > 80% sustained | > 85% of limit |
| Application (UI / frontend)   | > 60% sustained | > 75% of limit |
| Background jobs / workers     | > 70% sustained | > 80% of limit |

---

## 3. Port Availability Checks

TCP probes run from inside a container on the same overlay network. If nothing else, monitor port connectivity — it is
the fastest indicator of a dead service.

### 3.1 Generic TCP Port Probe

```bash
docker exec <container> \
  sh -c 'nc -z -w 3 <hostname> <port> && echo "OK:<hostname>:<port>" || echo "FAIL:<hostname>:<port>"'
```

### 3.2 etcd Client and Peer Ports

```bash
# Repeat for each etcd node
docker exec $(docker ps -qf "name=etcd1") \
  sh -c 'nc -z -w 3 etcd1 2379 && echo "OK:etcd1:2379" || echo "FAIL:etcd1:2379"'

docker exec $(docker ps -qf "name=etcd1") \
  sh -c 'nc -z -w 3 etcd1 2380 && echo "OK:etcd1:2380" || echo "FAIL:etcd1:2380"'
```

Deep cluster health (run from inside any etcd container):

```bash
docker exec $(docker ps -qf "name=etcd1") \
  etcdctl \
    --endpoints=http://etcd1:2379,http://etcd2:2379,http://etcd3:2379,http://etcd4:2379,http://etcd5:2379 \
    endpoint health --write-out=table
```

Alert if any member reports `unhealthy` or if fewer than 3 members are reachable.

### 3.3 HAProxy Frontend Ports

```bash
# PostgreSQL primary (read-write)
docker exec $(docker ps -qf "name=haproxy") \
  sh -c 'nc -z -w 3 haproxy 5432 && echo "OK:haproxy:5432" || echo "FAIL:haproxy:5432"'

# PostgreSQL replica (read-only)
docker exec $(docker ps -qf "name=haproxy") \
  sh -c 'nc -z -w 3 haproxy 5433 && echo "OK:haproxy:5433" || echo "FAIL:haproxy:5433"'

# HAProxy stats / admin
docker exec $(docker ps -qf "name=haproxy") \
  sh -c 'nc -z -w 3 haproxy 7000 && echo "OK:haproxy:7000" || echo "FAIL:haproxy:7000"'
```

### 3.4 Patroni REST API

Each Patroni node exposes a health endpoint on port 8008. No database credentials required.

```bash
# Repeat for each Patroni node
docker exec $(docker ps -qf "name=patroni1") \
  sh -c 'wget -qO- --timeout=5 http://patroni1:8008/health 2>/dev/null || echo "FAIL:patroni1:8008"'
```

Expected: `{"state":"running","role":"master"}` for the primary; `{"state":"running","role":"replica"}` for standbys.
Alert if no node reports `master`.

### 3.5 Redis and Sentinel Ports

```bash
# Redis master
docker exec $(docker ps -qf "name=redis-master") \
  sh -c 'nc -z -w 3 redis-master 6379 && echo "OK:redis-master:6379" || echo "FAIL:redis-master:6379"'

# Redis replicas — repeat for each
docker exec $(docker ps -qf "name=redis-replica1") \
  sh -c 'nc -z -w 3 redis-replica1 6379 && echo "OK:redis-replica1:6379" || echo "FAIL:redis-replica1:6379"'

# Redis PING — confirms the instance is serving commands, not just accepting TCP
docker exec $(docker ps -qf "name=redis-master") \
  sh -c 'redis-cli -a "$(cat /run/secrets/REDIS_PASSWORD)" PING 2>/dev/null'
# Expected: PONG

# Sentinel ports — repeat for each
docker exec $(docker ps -qf "name=sentinel1") \
  sh -c 'nc -z -w 3 sentinel1 26379 && echo "OK:sentinel1:26379" || echo "FAIL:sentinel1:26379"'

# Sentinel master discovery
docker exec $(docker ps -qf "name=sentinel1") \
  sh -c 'redis-cli -p 26379 SENTINEL masters 2>/dev/null | grep -c "name" && echo "OK:sentinel-has-master" || echo "FAIL:sentinel-no-master"'
```

Alert if fewer than 3 sentinels are reachable.

---

## 4. Application Health Endpoint Checks

A passing port check with a failing health endpoint means the process is bound but broken.

```bash
# Generic HTTP health check
docker exec <container> \
  sh -c 'wget -qO- --timeout=5 http://localhost:<port><path> 2>/dev/null \
    && echo "OK:<service>:ready" || echo "FAIL:<service>:ready"'
```

For multi-replica services, iterate over all instances:

```bash
for CONTAINER in $(docker ps -qf "name=<service-name>"); do
  RESULT=$(docker exec "$CONTAINER" \
    sh -c 'wget -qO- --timeout=5 http://localhost:<port><path> 2>/dev/null \
      && echo OK || echo FAIL')
  echo "$CONTAINER:<service-name>:$RESULT"
done
```

For services with no HTTP endpoint, fall back to process liveness:

```bash
docker exec $(docker ps -qf "name=<container>") \
  sh -c 'pgrep -f "<process-name>" > /dev/null \
    && echo "OK:<container>:process" || echo "FAIL:<container>:process"'
```

---

## 5. HAProxy Traffic Monitoring

### 5.1 Enable the Stats Endpoint

Ensure `haproxy.cfg` includes:

```
frontend stats
    bind *:7000
    stats enable
    stats uri /stats
    stats refresh 10s
```

Optionally add a Unix socket for `socat`-based queries:

```
global
    stats socket /var/run/haproxy/admin.sock mode 660 level admin
```

### 5.2 Pull Full Stats CSV

HAProxy exposes machine-readable stats at `/stats;csv`:

```bash
docker exec $(docker ps -qf "name=haproxy") \
  sh -c 'wget -qO- http://localhost:7000/stats;csv 2>/dev/null'
```

Key CSV columns (0-indexed):

| Index | Field   | Meaning                              |
|-------|---------|----------------------------------------|
| 0     | pxname  | Proxy name                             |
| 1     | svname  | FRONTEND, BACKEND, or server name      |
| 4     | scur    | Current sessions                       |
| 7     | stot    | Total sessions (cumulative)            |
| 8     | bin     | Bytes in (cumulative)                  |
| 9     | bout    | Bytes out (cumulative)                 |
| 17    | status  | OPEN, UP, DOWN, MAINT, etc.            |
| 48    | req_tot | Total HTTP requests (frontends only)   |

### 5.3 Frontend Status

```bash
# List all frontends and their status
docker exec $(docker ps -qf "name=haproxy") \
  sh -c 'wget -qO- http://localhost:7000/stats;csv 2>/dev/null \
    | awk -F, "$2==\"FRONTEND\" {print $1, $18}"'

# Alert on any frontend not OPEN
docker exec $(docker ps -qf "name=haproxy") \
  sh -c 'wget -qO- http://localhost:7000/stats;csv 2>/dev/null \
    | awk -F, "$2==\"FRONTEND\" && $18!=\"OPEN\" {print \"ALERT:frontend-not-open:\", $1, $18}"'
```

### 5.4 Frontend Byte Counters

```bash
# Snapshot bytes-in, bytes-out, and status for all frontends
docker exec $(docker ps -qf "name=haproxy") \
  sh -c 'wget -qO- http://localhost:7000/stats;csv 2>/dev/null \
    | awk -F, "$2==\"FRONTEND\" {printf \"frontend=%s bin=%s bout=%s status=%s\n\", $1, $9, $10, $18}"'

# Frontends with absolute zero bytes (useful at startup or after a reset)
docker exec $(docker ps -qf "name=haproxy") \
  sh -c 'wget -qO- http://localhost:7000/stats;csv 2>/dev/null \
    | awk -F, "$2==\"FRONTEND\" && $9==\"0\" && $10==\"0\" {print \"WARN:zero-bytes:\", $1}"'

# Frontends with zero sessions
docker exec $(docker ps -qf "name=haproxy") \
  sh -c 'wget -qO- http://localhost:7000/stats;csv 2>/dev/null \
    | awk -F, "$2==\"FRONTEND\" && $8==\"0\" {print \"WARN:zero-sessions:\", $1}"'
```

Cumulative counters that have not advanced between two scrapes indicate a frontend processing no traffic. Take two
snapshots separated by your collection interval and compare.

### 5.5 Via Unix Socket

```bash
# Full stats dump
docker exec $(docker ps -qf "name=haproxy") \
  sh -c 'echo "show stat" | socat stdio /var/run/haproxy/admin.sock'

# Runtime info (uptime, current connections)
docker exec $(docker ps -qf "name=haproxy") \
  sh -c 'echo "show info" | socat stdio /var/run/haproxy/admin.sock'

# Frontend rows only
docker exec $(docker ps -qf "name=haproxy") \
  sh -c 'echo "show stat" | socat stdio /var/run/haproxy/admin.sock \
    | awk -F, "$2==\"FRONTEND\" {print $1, $9, $10, $18}"'
```

### 5.6 Delta Check Script

Takes two snapshots and reports whether each frontend advanced its byte counter:

```bash
#!/usr/bin/env bash

INTERVAL=60  # seconds between samples
SNAP1=$(mktemp)
SNAP2=$(mktemp)

get_frontend_bytes() {
  docker exec $(docker ps -qf "name=haproxy") \
    sh -c 'wget -qO- http://localhost:7000/stats;csv 2>/dev/null \
      | awk -F, "$2==\"FRONTEND\" {print $1, $9}"'
}

get_frontend_bytes > "$SNAP1"
sleep "$INTERVAL"
get_frontend_bytes > "$SNAP2"

while IFS=' ' read -r name bytes1; do
  bytes2=$(grep "^$name " "$SNAP2" | awk '{print $2}')
  if [ -n "$bytes2" ] && [ "$bytes2" -gt "$bytes1" ] 2>/dev/null; then
    echo "OK:$name:traffic_active"
  else
    echo "WARN:$name:no_bytes_in_window"
  fi
done < "$SNAP1"

rm -f "$SNAP1" "$SNAP2"
```

---

## 6. PostgreSQL Internal Monitors

Run from inside any Patroni container (which includes `psql`). All queries connect as the `postgres` superuser.

The principle: connection saturation, replication lag (especially), and runaway queries cover the majority of production
incidents.

### 6.1 Connection Saturation

Connection exhaustion causes all new connections to fail immediately — instant total outage from the application's
perspective.

```bash
docker exec $(docker ps -qf "name=patroni1") \
  psql -U postgres -Atc "
    SELECT
      count(*)                                                                     AS total_connections,
      (SELECT setting::int FROM pg_settings WHERE name = 'max_connections')       AS max_connections,
      round(100.0 * count(*) /
        (SELECT setting::int FROM pg_settings WHERE name = 'max_connections'), 1) AS pct_used
    FROM pg_stat_activity
    WHERE state IS NOT NULL;
  "
```

Thresholds: warn at 75%, critical at 90%.

Connections waiting on locks — non-zero over several minutes signals contention:

```bash
docker exec $(docker ps -qf "name=patroni1") \
  psql -U postgres -Atc "
    SELECT count(*) AS waiting_on_lock
    FROM pg_stat_activity
    WHERE wait_event_type = 'Lock';
  "
```

### 6.2 Replication Lag

Run on the primary. Standbys return no rows from `pg_stat_replication`.

```bash
docker exec $(docker ps -qf "name=patroni1") \
  psql -U postgres -Atc "
    SELECT
      application_name,
      state,
      pg_wal_lsn_diff(pg_current_wal_lsn(), replay_lsn) AS lag_bytes,
      sync_state
    FROM pg_stat_replication
    ORDER BY lag_bytes DESC;
  "
```

Alert if `lag_bytes` exceeds your RPO tolerance (e.g. warn > 64 MB, critical > 256 MB). Alert if the query returns zero
rows — no standbys connected.

### 6.3 Long-Running Queries

Queries running beyond a threshold are a lock pile-up precursor.

```bash
docker exec $(docker ps -qf "name=patroni1") \
  psql -U postgres -Atc "
    SELECT
      pid,
      now() - query_start AS duration,
      state,
      left(query, 120) AS query_snippet
    FROM pg_stat_activity
    WHERE state != 'idle'
      AND query_start < now() - interval '5 minutes'
      AND query NOT ILIKE '%pg_stat_activity%'
    ORDER BY duration DESC;
  "
```

Alert if any query exceeds 10 minutes. Tune the interval to your workload.

### 6.4 Idle Transactions

Open idle-in-transaction connections hold locks and block autovacuum.

```bash
docker exec $(docker ps -qf "name=patroni1") \
  psql -U postgres -Atc "
    SELECT count(*) AS idle_in_transaction
    FROM pg_stat_activity
    WHERE state = 'idle in transaction'
      AND state_change < now() - interval '2 minutes';
  "
```

Alert if count > 0 sustained for more than 5 minutes.

### 6.5 Table Bloat (Weekly Check)

Not a real-time alert — run as a scheduled job. High bloat means autovacuum is not keeping up.

```bash
docker exec $(docker ps -qf "name=patroni1") \
  psql -U postgres -Atc "
    SELECT
      schemaname,
      tablename,
      n_dead_tup,
      n_live_tup,
      round(100.0 * n_dead_tup / nullif(n_live_tup + n_dead_tup, 0), 1) AS dead_pct,
      last_autovacuum
    FROM pg_stat_user_tables
    WHERE n_dead_tup > 10000
    ORDER BY dead_pct DESC
    LIMIT 20;
  "
```

Alert if any table exceeds 20% dead tuples and `last_autovacuum` is more than 24 hours ago.

---

## 7. Redis Internal Monitors

All `redis-cli` commands require the password from the secret store.

The key signals: memory headroom, replication offset divergence, and whether the eviction policy is silently discarding
data.

### 7.1 Memory Pressure

```bash
docker exec $(docker ps -qf "name=redis-master") \
  sh -c 'redis-cli -a "$(cat /run/secrets/REDIS_PASSWORD)" INFO memory 2>/dev/null \
    | grep -E "used_memory_human|used_memory_peak_human|maxmemory_human|mem_fragmentation_ratio|maxmemory_policy"'
```

| Field                       | Alert condition                                                                                     |
|------------------------------|--------------------------------------------------------------------------------------------------------|
| `used_memory` / `maxmemory` | Warn > 75%, critical > 90%                                                                          |
| `mem_fragmentation_ratio`   | Warn if > 1.5 (fragmentation) or < 1.0 (swapping)                                                   |
| `maxmemory_policy`          | Alert if `allkeys-lru` or `allkeys-random` on a persistence store — silent data loss under pressure |

Numeric ratio for scripting:

```bash
docker exec $(docker ps -qf "name=redis-master") \
  sh -c 'redis-cli -a "$(cat /run/secrets/REDIS_PASSWORD)" INFO memory 2>/dev/null \
    | awk -F: "/^used_memory:/{used=$2} /^maxmemory:/{max=$2} END {
        if (max > 0) printf \"%.1f%%\n\", used/max*100
        else print \"no_limit\"
      }"'
```

### 7.2 Replication Offset Divergence

Run on each replica.

```bash
docker exec $(docker ps -qf "name=redis-replica1") \
  sh -c 'redis-cli -a "$(cat /run/secrets/REDIS_PASSWORD)" INFO replication 2>/dev/null \
    | grep -E "role|master_host|master_link_status|master_last_io_seconds_ago|slave_repl_offset|master_repl_offset"'
```

Alert conditions:

- `master_link_status: down` — replica has lost contact with master.
- `master_last_io_seconds_ago` > 30 — replica has not heard from master recently.
- Offset delta exceeds your tolerance (e.g. > 1 MB).

### 7.3 Keyspace Hit Rate

A sustained low hit rate means data is missing from the cache or being evicted too aggressively.

```bash
docker exec $(docker ps -qf "name=redis-master") \
  sh -c 'redis-cli -a "$(cat /run/secrets/REDIS_PASSWORD)" INFO stats 2>/dev/null \
    | awk -F: "/keyspace_hits/{hits=$2} /keyspace_misses/{misses=$2} END {
        total = hits + misses;
        if (total > 0) printf \"hit_rate=%.1f%%\n\", hits/total*100
        else print \"hit_rate=no_data\"
      }"'
```

Alert if hit rate drops below 80% sustained over 10 minutes. Cold starts will always show low — establish a baseline
first.

### 7.4 Evictions and Rejected Connections

```bash
docker exec $(docker ps -qf "name=redis-master") \
  sh -c 'redis-cli -a "$(cat /run/secrets/REDIS_PASSWORD)" INFO stats 2>/dev/null \
    | grep -E "evicted_keys|rejected_connections|total_commands_processed"'
```

- `evicted_keys` incrementing means memory is full and data is being silently discarded. Any non-zero rate warrants
  investigation.
- `rejected_connections` > 0 means `maxclients` has been hit.

---

## 8. Patroni Internal Monitors

Patroni's REST API requires no database credentials and works regardless of whether PostgreSQL itself is up — making it
the most reliable interface for cluster state.

All endpoints on port 8008 of each Patroni container.

### 8.1 Cluster Topology

The `/cluster` endpoint returns all members, their roles, lag, and timeline in one call.

```bash
docker exec $(docker ps -qf "name=patroni1") \
  sh -c 'wget -qO- --timeout=5 http://patroni1:8008/cluster 2>/dev/null'
```

Alert conditions:

- No member has `"role":"leader"` — no primary.
- Any member `"lag"` exceeds your RPO.
- A known member is absent from the response — unreachable node.

Largest lag value for scripting:

```bash
docker exec $(docker ps -qf "name=patroni1") \
  sh -c 'wget -qO- --timeout=5 http://patroni1:8008/cluster 2>/dev/null \
    | grep -o "\"lag\":[0-9]*" | sort -t: -k2 -n | tail -1'
```

### 8.2 Per-Node Health and Leader Check

```bash
# Health — returns 200 for any running member
docker exec $(docker ps -qf "name=patroni1") \
  sh -c 'wget -qO- --timeout=5 http://patroni1:8008/health 2>/dev/null'

# Leader — returns 200 only from the primary, 503 from standbys
docker exec $(docker ps -qf "name=patroni1") \
  sh -c 'wget -S --timeout=5 http://patroni1:8008/leader 2>&1 | grep "HTTP/" | awk "{print \$2}"'
```

Confirm exactly one node returns 200 from `/leader`:

```bash
LEADER_COUNT=0
for i in 1 2 3 4; do
  CODE=$(docker exec $(docker ps -qf "name=patroni${i}") \
    sh -c "wget -S --timeout=5 http://patroni${i}:8008/leader 2>&1 \
      | grep 'HTTP/' | awk '{print \$2}'" 2>/dev/null)
  [ "$CODE" = "200" ] && LEADER_COUNT=$((LEADER_COUNT + 1))
done
echo "leader_count=$LEADER_COUNT"
# Alert if != 1
```

### 8.3 DCS Connectivity

```bash
# /config returns 200 only when Patroni can reach etcd
docker exec $(docker ps -qf "name=patroni1") \
  sh -c 'wget -S --timeout=5 http://patroni1:8008/config 2>&1 | grep "HTTP/" | awk "{print \$2}"'
# Alert on anything other than 200
```

### 8.4 Timeline Divergence

A timeline mismatch across nodes indicates an incomplete failover or split-brain risk.

```bash
for i in 1 2 3 4; do
  TL=$(docker exec $(docker ps -qf "name=patroni${i}") \
    sh -c "wget -qO- --timeout=5 http://patroni${i}:8008/health 2>/dev/null \
      | grep -o '\"timeline\":[0-9]*' | cut -d: -f2")
  echo "patroni${i} timeline=${TL:-UNREACHABLE}"
done
# Alert if values differ across nodes
```

---

## 9. Sentinel Internal Monitors

Sentinel is healthy when all expected nodes are up, they agree on the same master, and that master is reachable.

### 9.1 Master Reachability

```bash
docker exec $(docker ps -qf "name=sentinel1") \
  sh -c 'redis-cli -p 26379 SENTINEL masters 2>/dev/null'
```

Key fields (returned as a flat key-value list):

| Field                 | Alert condition                                                             |
|------------------------|----------------------------------------------------------------------------|
| `status`              | Alert if not `ok`                                                           |
| `flags`               | Alert if contains `disconnected`, `o_down`, or `s_down`                     |
| `num-slaves`          | Alert if below expected replica count                                       |
| `num-other-sentinels` | Alert if below (expected sentinels − 1)                                     |
| `quorum`              | Alert if `num-other-sentinels + 1` < quorum value — failover cannot proceed |

Extract master flags for scripting:

```bash
docker exec $(docker ps -qf "name=sentinel1") \
  sh -c 'redis-cli -p 26379 SENTINEL masters 2>/dev/null \
    | awk "/^flags$/{getline; print}"'
# Expected: "master" — anything else (s_down, o_down, disconnected) is an alert
```

### 9.2 Quorum Check

```bash
# Run from every sentinel — if any returns NOQUORUM, failover is not possible from that node
docker exec $(docker ps -qf "name=sentinel1") \
  sh -c 'redis-cli -p 26379 SENTINEL ckquorum redis-master 2>/dev/null'
# Expected: OK N usable Sentinels. Quorum and failover authorization can be reached
# Alert on: NOQUORUM or NOAUTH
```

### 9.3 Master Address Consistency

All sentinels must agree on the same master address. Divergence means a split has occurred.

```bash
for i in 1 2 3 4 5; do
  MASTER=$(docker exec $(docker ps -qf "name=sentinel${i}") \
    sh -c 'redis-cli -p 26379 SENTINEL get-master-addr-by-name redis-master 2>/dev/null \
      | tr "\n" ":"' 2>/dev/null)
  echo "sentinel${i} master=${MASTER:-UNREACHABLE}"
done
# Alert if any two sentinels report different addresses
```

### 9.4 Recent Failover Detection

A recent failover warrants investigation even if the cluster is now healthy.

```bash
docker exec $(docker ps -qf "name=sentinel1") \
  sh -c 'redis-cli -p 26379 SENTINEL masters 2>/dev/null \
    | awk "/^last-ok-ping-reply/{getline; print \"last_ok_ping_ms=\"\$0}"'

docker exec $(docker ps -qf "name=sentinel1") \
  sh -c 'redis-cli -p 26379 SENTINEL masters 2>/dev/null \
    | awk "/^last-failover-attempt$/{getline; print \"last_failover_epoch=\"\$0}"'
# Non-zero epoch means a failover was attempted — cross-check with cluster logs
```

---

## 10. Composite Shell Script

Wraps port and health endpoint checks into a single script. Wire the output into your collection mechanism. The
internal monitors in sections 6–9 are best run as separate scheduled jobs given their heavier queries.

```bash
#!/usr/bin/env bash
# stack_health_check.sh
# Adjust container names, hostnames, ports, and service names for your stack.
# Output: one line per check — OK or FAIL prefixed with a label.

probe_tcp() {
  local label=$1 container=$2 host=$3 port=$4
  local result
  result=$(docker exec "$container" sh -c "nc -z -w 3 $host $port && echo OK || echo FAIL" 2>/dev/null)
  echo "$label: $(echo "$result" | grep -q '^OK' && echo OK || echo FAIL)"
}

probe_http() {
  local label=$1 container=$2 url=$3
  local result
  result=$(docker exec "$container" sh -c "wget -qO- --timeout=5 $url 2>/dev/null && echo OK || echo FAIL" 2>/dev/null)
  echo "$label: $(echo "$result" | grep -q 'OK' && echo OK || echo FAIL)"
}

probe_process() {
  local label=$1 container=$2 pattern=$3
  local result
  result=$(docker exec "$container" sh -c "pgrep -f '$pattern' > /dev/null && echo OK || echo FAIL" 2>/dev/null)
  echo "$label: $(echo "$result" | grep -q '^OK' && echo OK || echo FAIL)"
}

# --- etcd ---
for i in 1 2 3 4 5; do
  probe_tcp "etcd${i}_client" "$(docker ps -qf "name=etcd${i}")" "etcd${i}" 2379
  probe_tcp "etcd${i}_peer"   "$(docker ps -qf "name=etcd${i}")" "etcd${i}" 2380
done

# --- HAProxy ---
probe_tcp "haproxy_pg_primary" "$(docker ps -qf "name=haproxy")" haproxy 5432
probe_tcp "haproxy_pg_replica" "$(docker ps -qf "name=haproxy")" haproxy 5433
probe_tcp "haproxy_stats"      "$(docker ps -qf "name=haproxy")" haproxy 7000

# --- Patroni ---
for i in 1 2 3 4; do
  probe_http "patroni${i}_health" "$(docker ps -qf "name=patroni${i}")" "http://patroni${i}:8008/health"
done

# --- Redis ---
probe_tcp "redis_master"   "$(docker ps -qf "name=redis-master")"   redis-master   6379
probe_tcp "redis_replica1" "$(docker ps -qf "name=redis-replica1")" redis-replica1 6379
probe_tcp "redis_replica2" "$(docker ps -qf "name=redis-replica2")" redis-replica2 6379
probe_tcp "redis_replica3" "$(docker ps -qf "name=redis-replica3")" redis-replica3 6379

# --- Sentinels ---
for i in 1 2 3 4 5; do
  probe_tcp "sentinel${i}" "$(docker ps -qf "name=sentinel${i}")" "sentinel${i}" 26379
done

# --- Application HTTP health endpoints ---
for CONTAINER in $(docker ps -qf "name=<ui-service>"); do
  probe_http "ui_ready_${CONTAINER:0:12}" "$CONTAINER" "http://localhost:<port>/api/ready"
done

for CONTAINER in $(docker ps -qf "name=<api-service>"); do
  probe_http "api_ready_${CONTAINER:0:12}" "$CONTAINER" "http://localhost:<port>/api/ready"
done

# --- Background worker (no HTTP endpoint) ---
probe_process "jobs_process" "$(docker ps -qf "name=<jobs-service>")" "<process-pattern>"

# --- HAProxy frontend byte snapshot ---
echo ""
echo "HAProxy frontend traffic:"
docker exec "$(docker ps -qf "name=haproxy")" \
  sh -c 'wget -qO- http://localhost:7000/stats;csv 2>/dev/null \
    | awk -F, "$2==\"FRONTEND\" {
        printf \"  %-30s  bin=%-12s  bout=%-12s  status=%s\n\", $1, $9, $10, $18
      }"'
```

Cron schedule (30-second approximate interval):

```cron
* * * * * /etc/scripts/stack_health_check.sh >> /var/log/stack_health.log 2>&1
* * * * * sleep 30 && /etc/scripts/stack_health_check.sh >> /var/log/stack_health.log 2>&1
```

---

## 11. Jube Application Monitors

Five SQL queries run directly against the Jube application database. Poll on a short interval (30–60 seconds). Run from
inside any Patroni container or any host with `psql` access to the primary via HAProxy port 5432.

The queries read from Jube's own instrumentation tables — no schema changes or agents required.

### 11.1 Archive Queue Saturation

Measures the backlog of transaction records waiting to be written to the archive. A rising value means the asynchronous
writer is falling behind, typically due to database I/O pressure or a crashed worker.

```bash
docker exec $(docker ps -qf "name=patroni1") \
  psql -U postgres -Atc "
    SELECT \"Archive\"
    FROM \"EntityAnalysisModelAsynchronousQueueBalance\"
    ORDER BY 1 DESC
    LIMIT 1;
  "
```

Alert if value > 1000.

### 11.2 Case Creation Queue Saturation

Measures the backlog of cases waiting to be created. Saturation here means compliance workflows are being delayed —
alerts and cases are not reaching analysts in near-real-time.

```bash
docker exec $(docker ps -qf "name=patroni1") \
  psql -U postgres -Atc "
    SELECT \"CaseCreation\"
    FROM \"EntityAnalysisAsynchronousQueueBalance\"
    ORDER BY 1 DESC
    LIMIT 1;
  "
```

Alert if value > 100.

### 11.3 HTTP Processing Counter (Engine Liveness)

Measures the volume of HTTP requests being processed by the engine. A value at or near zero means the engine is dead or
not receiving traffic — the application-layer equivalent of the HAProxy frontend zero-byte check, and will typically
fire together with it.

```bash
docker exec $(docker ps -qf "name=patroni1") \
  psql -U postgres -Atc "
    SELECT \"Model\"
    FROM \"HttpProcessingCounter\"
    ORDER BY 1 DESC
    LIMIT 1;
  "
```

Alert if value is zero or not advancing between polls.

### 11.4 Model Response Time

Average response time in milliseconds per model invocation. Degradation indicates infrastructure pressure (database
latency, Redis latency, CPU saturation) or a model that has grown too complex for its invocation volume.

```bash
docker exec $(docker ps -qf "name=patroni1") \
  psql -U postgres -Atc "
    SELECT \"ModelTotalResponseTime\" / NULLIF(\"ModelInvoke\", 0)
    FROM \"EntityAnalysisModelProcessingCounter\"
    ORDER BY 1 DESC
    LIMIT 1;
  "
```

Alert if value > 5000 (5 milliseconds).

### 11.5 Response Elevation Rate

Average response elevation as a proportion of total invocations. A high elevation rate indicates the model is
triggering alerts on an abnormally large fraction of transactions — usually a sign of a misconfigured or degraded model
rather than a genuine fraud spike.

```bash
docker exec $(docker ps -qf "name=patroni1") \
  psql -U postgres -Atc "
    SELECT \"ResponseElevation\" / NULLIF(\"ModelInvoke\", 0)
    FROM \"EntityAnalysisModelProcessingCounter\"
    ORDER BY 1 DESC
    LIMIT 1;
  "
```

Alert if value > 0.20 (20%).

---

## 12. Container Log Monitoring

All containers write to STDOUT. Log4net (used by Jube) additionally writes errors and warnings to STDERR. Monitoring
splits accordingly: STDERR for Jube containers, STDOUT patterns for infrastructure.

Pass `--since` to bound the scan to your collection interval and avoid re-reading the full log history on every run.

### 12.1 STDERR — Jube Containers

Under normal operation STDERR should be silent. Any output warrants inspection.

```bash
# Count of STDERR lines per Jube container in the last 60 seconds
for CONTAINER in $(docker ps -qf "name=jube"); do
  COUNT=$(docker logs --since 60s "$CONTAINER" 2>&1 1>/dev/null | wc -l)
  echo "$CONTAINER stderr_lines=$COUNT"
done
```

For a higher-signal check, filter by severity keyword:

```bash
for CONTAINER in $(docker ps -qf "name=jube"); do
  ERRORS=$(docker logs --since 60s "$CONTAINER" 2>&1 1>/dev/null \
    | grep -ciE "ERROR|FATAL|WARN" || true)
  echo "$CONTAINER stderr_errors=$ERRORS"
done
```

Alert if `stderr_errors` > 0.

### 12.2 STDOUT — Infrastructure Containers

Infrastructure containers log cluster-state transitions to STDOUT. The signal here is state changes and crash markers,
not a raw error count.

```bash
# etcd — leader elections and member failures
for CONTAINER in $(docker ps -qf "name=etcd"); do
  docker logs --since 60s "$CONTAINER" 2>/dev/null \
    | grep -iE "elected|lost|failed|compaction error" \
    | sed "s/^/$CONTAINER: /"
done

# Patroni — failovers, demotions, DCS errors
for CONTAINER in $(docker ps -qf "name=patroni"); do
  docker logs --since 60s "$CONTAINER" 2>/dev/null \
    | grep -iE "demoted|promoted|failover|etcd|cannot|timeout" \
    | sed "s/^/$CONTAINER: /"
done

# Redis — replica disconnections, AOF/RDB errors
for CONTAINER in $(docker ps -qf "name=redis"); do
  docker logs --since 60s "$CONTAINER" 2>/dev/null \
    | grep -iE "replica|replication|error|warning|lost connection" \
    | sed "s/^/$CONTAINER: /"
done

# HAProxy — backend server state changes
docker logs --since 60s $(docker ps -qf "name=haproxy") 2>/dev/null \
  | grep -iE "DOWN|UP|no server|backend|timeout"
```

### 12.3 Restart Detection

A container that has restarted recently has likely crashed. Check restart counts across all containers:

```bash
docker ps --format "table {{.Names}}\t{{.Status}}" | grep -i "restarting\|unhealthy"
```

For a numeric restart count per container:

```bash
docker inspect --format '{{.Name}} restarts={{.RestartCount}} status={{.State.Status}}' \
  $(docker ps -q) 2>/dev/null | sed 's|^/||'
```

Alert if `restarts` has increased since the last poll, or if `status` is anything other than `running`.

---

## 13. Quick Reference

| Component             | Container pattern  | Port(s)                | Probe                                 | Internal monitor                                     |
|------------------------|----------------------|---------------------------|------------------------------------------|-----------------------------------------------------------|
| etcd (×N)             | `etcd<N>`          | 2379 client, 2380 peer | TCP + `etcdctl endpoint health`       | —                                                    |
| PostgreSQL primary    | `haproxy`          | 5432                   | TCP                                   | `pg_stat_activity`, `pg_stat_replication`            |
| PostgreSQL replica    | `haproxy`          | 5433                   | TCP                                   | replication lag query                                |
| HAProxy               | `haproxy`          | 7000                   | TCP + CSV byte counters               | frontend status, delta byte check                    |
| Patroni (×N)          | `patroni<N>`       | 8008                   | HTTP `/health`, `/leader`, `/cluster` | `/config` for DCS, timeline check                    |
| Redis master          | `redis-master`     | 6379                   | TCP + `PING`                          | `INFO memory`, `INFO stats`                          |
| Redis replicas (×N)   | `redis-replica<N>` | 6379                   | TCP                                   | `INFO replication`                                   |
| Redis Sentinel (×N)   | `sentinel<N>`      | 26379                  | TCP + `SENTINEL ckquorum`             | `SENTINEL masters` flags, address check              |
| Application (HTTP)    | per service        | varies                 | `wget /api/ready`                     | —                                                    |
| Application (no HTTP) | per service        | —                      | `pgrep`                               | —                                                    |
| Jube archive queue    | `patroni<N>`       | 5432 (via HAProxy)     | SQL                                   | `EntityAnalysisModelAsynchronousQueueBalance` > 1000 |
| Jube case queue       | `patroni<N>`       | 5432 (via HAProxy)     | SQL                                   | `EntityAnalysisAsynchronousQueueBalance` > 100       |
| Jube engine liveness  | `patroni<N>`       | 5432 (via HAProxy)     | SQL                                   | `HttpProcessingCounter` at zero                      |
| Jube response time    | `patroni<N>`       | 5432 (via HAProxy)     | SQL                                   | avg ms per invocation > 5000                         |
| Jube elevation rate   | `patroni<N>`       | 5432 (via HAProxy)     | SQL                                   | avg elevation ratio > 20%                            |
| Jube containers       | `jube*`            | —                      | STDERR line count                     | Log4net ERROR / FATAL / WARN                         |
| All containers        | —                  | —                      | Restart count delta                   | `docker inspect` RestartCount                        |
