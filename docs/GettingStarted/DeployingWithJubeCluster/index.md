---
layout: default
title: Deploying with Jube Cluster
nav_order: 2
parent: Getting Started
---

🚀 Get to pre-production in weeks, not months, with private [training](https://www.jube.io/jube-training) direct from Jube's developer — real sovereignty, zero vendor lock-in.

# Deploying with Jube Cluster

[Deploying with Docker](../DeployingWithDocker/index.html) covers a single-node deployment, which is the right
starting point for evaluation, proof of concept, and smaller production workloads. This page covers a load-balanced,
highly available, clustered deployment on Docker Swarm - multiple Postgres nodes under Patroni, multiple Redis nodes
under Sentinel, and multiple Jube application instances, all fronted by HAProxy - suitable for production workloads
that need to keep running through the loss of a node.

This is a real, working runbook, not a conceptual overview - the commands here are what an operator actually runs,
in order, to stand up and operate the cluster. Placeholders like `<node1-ip>`, `<hostname1>` and `<deploy-user>`
stand in for values specific to your environment.

## Architecture Overview

Five-node Docker Swarm (four plus a tiebreaker) cluster, quorum-based throughout so the cluster keeps making
progress after losing any single node.

| Layer         | Components                                                         |
|---------------|--------------------------------------------------------------------|
| Consensus     | etcd1, etcd2, etcd3, etcd4, etcd5                                  |
| Database      | patroni1, patroni2, patroni3, patroni4 (PostgreSQL, under Patroni) |
| WAL Archive   | pgBackRest, writing to a NAS-backed named Docker volume            |
| Cache         | redis-master, redis-replica1/2/3, sentinel1/2/3/4/5                |
| Load Balancer | haproxy                                                            |
| Application   | jube-jobs (singleton), jube-api (scaled), jube-ui (scaled)         |

**Postgres traffic flows through HAProxy only** - application containers connect to `haproxy:5432` (read/write) or
`haproxy:5433` (read-only, routed to a replica), never directly to a `patroniN` node. HAProxy makes that
primary/replica routing decision by polling each Patroni node's own REST API (`GET /primary` / `GET /replica` on
port 8008) rather than guessing from a raw TCP check - this is Patroni's own recommended integration pattern, since
only Patroni itself reliably knows which node currently holds the lease.

**Redis traffic bypasses HAProxy entirely.** Jube's `RedisConnectionString` points directly at the five Sentinels
(`sentinel1:26379,...,serviceName=redis-master,...`), using StackExchange.Redis's native Sentinel-aware connection
mode: the client asks the Sentinel quorum who the current master is, connects there, and re-asks automatically on a
failover. This is the standard client-side pattern for Redis Sentinel high availability, and is arguably a better
fit than routing Redis through a TCP load balancer would be, since the client library's own failover detection is
faster and more precise than a proxy re-running a periodic health check. The Redis nodes themselves also defer to
Sentinel on their own startup, for the same reason - see [Data Layer Configuration](#data-layer-configuration) below.

Both the etcd (5) and Sentinel (5) counts are deliberately odd - both are quorum/majority-vote systems, and an odd
node count avoids the tied vote a same-sized even count can produce during a partition. Patroni's replica count (4)
has no such constraint, since Postgres replication itself isn't a voting system - Patroni's leader election is
delegated entirely to etcd.

## Data Layer Configuration

The Patroni, pgBackRest and Redis configuration in this cluster isn't left at defaults - most of it is a deliberate
tradeoff worth understanding rather than copying blind.

### Postgres / Patroni

`synchronous_mode: true` with `synchronous_mode_strict: false`, alongside Postgres's own `synchronous_commit: off`,
is a specific and intentional combination, not a contradiction. `synchronous_mode` controls **failover safety**:
Patroni always designates a synchronous standby, and will only promote a standby that Patroni knows had fully
received what the old primary had committed - so automatic failover cannot silently lose a transaction. It does
not, by itself, mean every commit *waits* for that standby. That's controlled separately by
`synchronous_commit: off`, which lets a client's `COMMIT` return as soon as the local WAL write completes, without
waiting for the standby's acknowledgement over the network. Together: failover promotion is safe, but everyday
commit latency doesn't pay a synchronous-replication network round trip. The `strict: false` half of that first
setting means the primary keeps accepting writes even if the synchronous standby is temporarily unreachable, rather
than the whole cluster refusing writes because one designated standby is briefly down.

`use_pg_rewind: true` lets a demoted former primary rejoin the cluster as a standby by rewinding just the diverged
WAL, rather than requiring a full new basebackup - faster recovery after a failover. `use_slots: true` with
`max_slot_wal_keep_size: 4GB` uses replication slots so a standby that briefly disconnects (a restart, a network
blip) doesn't need a full resync - the primary retains the WAL it needs - while the cap stops a *permanently* dead
standby's slot from filling the disk indefinitely. `maximum_lag_on_failover: 33554432` (32MB) excludes a standby
that's fallen too far behind from being an automatic-failover candidate at all, favouring "wait" or "pick a better
candidate" over promoting something meaningfully behind.

`archive_mode`/`archive_command` continuously ship WAL to pgBackRest for point-in-time recovery, bounded by
`archive_timeout: 60s` - the maximum window of committed-but-not-yet-archived data if archiving somehow stalled.
`checkpoint_timeout: 15min` and `checkpoint_completion_target: 0.9` spread checkpoint I/O smoothly across most of
that interval rather than bursting it; `wal_compression: lz4` trades a small amount of CPU for meaningfully less
WAL volume to write and ship. `commit_delay: 1000` (microseconds) with `commit_siblings: 10` briefly batches
concurrent commits so their WAL flushes can be amortised together under load - a classic high-throughput OLTP
tuning, appropriate for a continuous stream of transaction-monitoring writes rather than occasional writes.
Autovacuum is tuned meaningfully more aggressive than Postgres's defaults (`autovacuum_vacuum_scale_factor: 0.01`,
`autovacuum_analyze_scale_factor: 0.005`, versus defaults an order of magnitude larger) for the same reason - a
high-insert-rate workload accumulates dead tuples and stale statistics faster than the defaults assume, and letting
either lag invites both bloat and poor query plans.

`pg_hba` scopes replication, superuser-over-network, and application traffic all to the `10.0.1.0/24` overlay
subnet specifically, rather than a broader range - defence in depth underneath "all traffic must flow through
HAProxy," so even a misconfiguration elsewhere can't accept a Postgres connection from outside the cluster's own
network. The one exception, `local all all trust`, only ever applies to the Unix socket inside a given container -
unreachable without already having exec access to that specific container - which is exactly what makes the
[Total Postgres Password Wipeout Recovery](#total-postgres-password-wipeout-recovery) procedure above both possible
and safe: the trust boundary it relies on is equivalent to already having host/container access, not a network-open
door.

### pgBackRest and why the backup repository is on NAS

`repo1-path` (the pgBackRest backup repository - WAL archive plus periodic full backups) is mounted from NAS
(`/mnt/pgbackrest`), not host-local disk. The reason is durability independence: if backups lived on the same
host's disk as the Patroni node producing them, losing that host would take out the live database *and* its own
backup history in the same event. NAS decouples the two, and also means any node - not just the one that produced
a given backup - can read the repository for a restore.

Within that, `archive-async=y` plus `spool-path` (mounted as `tmpfs` in the compose file, so it's RAM-backed and
non-persistent) decouples Postgres's `archive_command` from actual NAS write latency: Postgres hands a completed
WAL segment to a fast local spool and moves on immediately, while a background pgBackRest process drains that spool
to the NAS repository asynchronously. A slow or momentarily congested NAS write can never stall the primary's own
WAL archiving as a result - the spool being non-durable is fine precisely because the durable copy of record is
whatever has already landed in the NAS repository, not whatever's transiently sitting in the spool.
`repo1-retention-full=2` (keep the last two full backups, with WAL retained to cover both) is a deliberate rollback
safety margin - if the newest full backup turns out to be bad, there's still a second, older one to fall back to
rather than only ever having exactly one.

### Redis

`appendonly yes` with `appendfsync everysec` accepts the same kind of throughput/durability tradeoff as Postgres's
`synchronous_commit off` above: an fsync once per second, rather than per write, bounds the worst-case data loss on
a hard crash to roughly one second of writes, in exchange for not paying an fsync's latency on every single
command. `no-appendfsync-on-rewrite yes` avoids a further fsync stall while the AOF file is being compacted in the
background. `io-threads 4` with `io-threads-do-reads yes` parallelises Redis's normally single-threaded network I/O
across threads, worthwhile on the multi-core hosts this cluster runs on under concurrent load. `protected-mode no`
is safe specifically *because* `requirepass`/`masterauth` are always set (via the wrapper command, from the
`REDIS_PASSWORD` secret) and the network is already confined to the private overlay - protected mode exists to stop
an accidentally-unauthenticated instance being reachable from anywhere, which doesn't apply here since both of the
conditions it guards against are independently already covered.

**Every node defers to Sentinel on startup, rather than any one node hardcoding itself as master.** Each of
`redis-master`, `redis-replica1`, `redis-replica2` and `redis-replica3` runs the same shape of startup logic: query
each Sentinel in turn for who it currently considers the master (`SENTINEL get-master-addr-by-name redis-master`),
and if one answers with a master that isn't itself, start as `--replicaof` that node - only falling back to the
historical static topology (`redis-master` seeds as a plain master; the others target `redis-master` directly) if
no Sentinel can be reached or none has an opinion yet, which is only ever true on a genuinely fresh cluster.

This matters because Sentinel's failover promotion happens at the Redis protocol level, live, without restarting
any container - but a container restart (a crash, a redeploy, `docker service update --force`) always re-runs
whatever is written in `docker-compose.yml`. A static, unconditional `redis-master` command with no `--replicaof`
at all would mean: after Sentinel has genuinely promoted a different node to master following a real failure, the
*original* `redis-master` service could later restart and declare itself master all over again with its own old
data - a second, independent master directly contradicting whatever Sentinel and the rest of the cluster now agree
is authoritative. Querying Sentinel first closes that specific gap - every node's actual role after any restart
reflects what the cluster currently believes, not what the compose file assumed when it was first written.

**`redis-master`'s volume is NAS-backed (`/mnt/redis`) while the three replicas use local, Swarm-managed volumes**
(`redis-replicaN-data`) - and this is purely a backup decision, not a role/failover one now that startup role
resolution is Sentinel-driven for all four. Moving that one node's AOF file off the host and onto NAS gives a
further, independent layer of backup for the data beyond in-cluster replica redundancy itself - the same
durability-independence reasoning as pgBackRest's repository above, applied to Redis. The three replicas
deliberately don't need this: their entire purpose is live redundancy, and any one of them can always fully
re-sync from whichever node Sentinel currently considers master, so paying NAS latency for volumes that exist to be
disposable and rebuildable would be pure overhead with no corresponding benefit.

## Why background processing runs on one instance

`jube-jobs` is the only service with `EnableMigration`, `EnableReprocessing`, `EnableSanctionLoader`,
`CachePruneServer`, `EnableTtlCounter`, `EnableSearchKeyCache`, `EnableCasesAutomation` and `EnableCallback` all set
to `True`; `jube-api` and `jube-ui` have every one of those `False`. This isn't an oversight - several of these are
documented as safe to run on only one instance at a time (
see [Environment Variables](../../Concepts/EnvironmentVariables/index.html)),
and letting every scaled `jube-api`/`jube-ui` replica also run background processing would mean N copies of the same
job racing each other. `jube-jobs` is deployed as a singleton for exactly this reason; `jube-api`/`jube-ui` are the
stateless, horizontally-scaled request handlers.

## Building and Distributing Images

Build the images from source:

```bash
docker build --no-cache -t jube.patroni:<date> .
docker build --no-cache -f Jube.App/Dockerfile -t jube.app:<date> .
```

Save each image to a tar file, collected under an `images/` directory at the root of this repository:

```bash
docker save -o images/jube.patroni:<date>.tar jube.patroni:<date>
docker save -o images/jube.app:<date>.tar jube.app:<date>
docker save -o images/redis.tar redis:7-alpine
docker save -o images/etcd.tar quay.io/coreos/etcd:v3.5.3
docker save -o images/haproxy.tar haproxy:2.8
```

Zip the whole directory (after resetting permissions - see [File System](#file-system) below, and removing any
`.git`/IDE directories) with a date, name or version in the archive name, and distribute it to every node.

This tar-file distribution, rather than pushing to a private registry, is the standard approach where the cluster's
network cannot reach an external registry (an air-gapped or tightly firewalled deployment, which is common for
on-premises AML/fraud infrastructure at a regulated institution). The operational cost is real and worth
understanding before choosing it: every image update is a manual build-save-copy-load cycle across every node,
rather than a single `docker service update --image registry/jube.app:tag`, so version drift between nodes is a risk
this process has to actively guard against (see the `docker images` verification step below). Where the network
allows it, a private registry (Harbor, a cloud provider's container registry, or similar) removes this manual step
entirely and should be preferred.

## First Time Setup

### NAS Permissions

The mount needs to be accessible to containers under SELinux enforcing, which means the share needs `container_file_t`
context. For example:

| Share            | Mount path                 | Owner (uid:gid) | Mode   |
|------------------|----------------------------|-----------------|--------|
| pgBackRest repo  | `/var/lib/pgbackrest/repo` | `70:70`         | `0750` |
| pgBackRest spool | `/var/spool/pgbackrest`    | `70:70`         | `0750` |
| Redis data       | `/data`                    | `999:999`       | `0750` |

```
# pgBackRest
<share_path>  (rw,all_squash,anonuid=70,anongid=70)

# Redis
<share_path>  (rw,all_squash,anonuid=999,anongid=999)
```

The Patroni entrypoint handles initialisation of its own directories automatically:

```
container starts as root
  └─ /var/lib/pgbackrest/repo/.initialized absent?
       └─ mkdir -p, chown postgres:postgres, chmod 750, touch .initialized
  └─ /var/spool/pgbackrest/.initialized absent?
       └─ mkdir -p, chown postgres:postgres, chmod 750, touch .initialized
  └─ su-exec postgres patroni ...
```

Redis handles its own `/data` directory internally; no init script is required for it.

The bind-mounted directory still needs to be owned/writable by the right user before the first container start -
pre-create it on each host:

```bash
sudo mkdir -p /mnt/pgbackrest
sudo chown 70:70 /mnt/pgbackrest
sudo chcon -Rt container_file_t /mnt/pgbackrest

# Redis
sudo mkdir -p /mnt/redis
sudo chown 999:999 /mnt/redis
sudo chcon -Rt container_file_t /mnt/redis
```

### SSL Offloading

SSL certificates should be issued by your organisation's certificate authority and placed in:

```bash
etc/haproxy/ssl
```

referenced from the HAProxy config by a fixed naming convention:

* Frontend `jube_api` expects **jube-api.pem** in that directory.
* Frontend `jube_ui` expects **jube-ui.pem** in that directory.

Treat the HAProxy configuration files as production configuration under version control alongside the rest of this
deployment, rather than something edited ad hoc on a host.

### Creating Secrets

Every credential the cluster needs - Postgres passwords, the Redis password, `JWTKey`, `PasswordHashingKey`,
`ApiHmacKey`, `ElementSymmetricEncryptionKey`, the RSA keypair for `PasswordAsymmetricEncryption`, and
`HAPROXY_COOKIE_SECRET` - is generated locally and loaded into Docker Swarm's own secret store (`docker secret
create`), never written into `docker-compose.yml` or an Environment Variable directly. For the Jube application
services, Jube's own `[@Key@]` tokenisation (
see [Environment Variables](../../Concepts/EnvironmentVariables/index.html))
resolves each Environment Variable's `[@SECRET_NAME@]` placeholder against the mounted secret file at container
start, so the secret value itself is never visible in `docker service inspect`, container environment listings, or
this compose file. This list is just what this particular cluster deployment happens to need, not a fixed or
hardcoded set - Jube's tokenisation is entirely free-form (see [Environment
Variables](../../Concepts/EnvironmentVariables/index.html)), so wiring in an additional secret for a new Environment
Variable is just adding it to the `secrets:` block below, mounting it on the relevant service, and referencing
`[@ITS_NAME@]` from that variable's value - no code change involved.

`HAPROXY_COOKIE_SECRET` is the one exception to *how* that resolution happens, since it configures HAProxy rather
than Jube: HAProxy's `dynamic-cookie-key` directive (used for `jube_ui`'s session-affinity cookie, `SRV_ID`) only
accepts a literal string in the config file itself and has no built-in equivalent of Jube's `[@Key@]` resolution -
confirmed against the real `haproxy:2.8` image that it never opens a mounted secret file directly, regardless of
what's written after the directive. `haproxy.cfg` still uses the same `[@HAPROXY_COOKIE_SECRET@]` token convention
for consistency, but the `haproxy` service's `command:` in `docker-compose.yml` has to resolve it itself - a `sed`
substitution replaces `[@HAPROXY_COOKIE_SECRET@]` with the mounted secret file's contents into a writable copy of
the config before handing off to HAProxy. The secret is still never written to disk unencrypted outside of Swarm's
own secret store, but it does briefly exist in the rendered config passed to the HAProxy process.

Any secret created outside this process - for example, a client-provided key received over email or another channel
that isn't end-to-end secure - should still be rotated into `docker secret create` rather than left as a plain
Environment Variable, for the same reason: once it's a Swarm secret, it's no longer readable from the running
service's inspected configuration.

### On All Nodes

From the cluster directory, load images **on every host**:

```bash
docker load -i images/jube.patroni:<date>.tar
docker load -i images/jube.app:<date>.tar
docker load -i images/redis.tar
docker load -i images/etcd.tar
docker load -i images/haproxy.tar
```

Verify the images are present, and that no stale images/volumes remain from a previous version:

```bash
docker images
docker volume ls
docker volume rm <image>  # if a stale one exists
```

Open the firewall ports Swarm needs **on every host**, including its overlay-network encryption protocols:

```bash
sudo firewall-cmd --permanent --add-port=2377/tcp
sudo firewall-cmd --permanent --add-port=7946/tcp
sudo firewall-cmd --permanent --add-port=7946/udp
sudo firewall-cmd --permanent --add-port=4789/udp
sudo firewall-cmd --permanent --add-port=500/udp
sudo firewall-cmd --permanent --add-port=4500/udp
sudo firewall-cmd --reload
sudo systemctl restart docker
```

Run the permissions script **on every host**:

```bash
sudo chmod +x permissions-init.sh
./permissions-init.sh
```

### On Node 1 Only

Initialise the swarm:

```bash
docker swarm init --advertise-addr <node1-ip>
```

Get the manager join token:

```bash
docker swarm join-token manager
```

### On Nodes 2 and 3

Join as managers using the token from node 1:

```bash
docker swarm join --token <manager-join-token> <node1-ip>:2377
```

### Back on Node 1

Verify all nodes have joined:

```bash
docker node ls
```

Add zone labels - these are what the placement constraints below pin each Patroni/etcd instance to a distinct host:

```bash
docker node update --label-add zone=host1 <hostname1>
docker node update --label-add zone=host2 <hostname2>
docker node update --label-add zone=host3 <hostname3>
```

Validate the labels landed correctly:

```bash
docker node ls -q | xargs docker node inspect -f '{{ .ID }} [{{ .Description.Hostname }}]: {{ range $k, $v := .Spec.Labels }}{{ $k }}={{ $v }} {{end}}'
```

> If running on a single host for evaluation, comment out the placement constraints in the compose file's Jube
> services first - otherwise Swarm has nowhere to schedule the tiebreaker role and scaling will simply fail to find
> an eligible host.

```yaml
      # placement:
      # constraints:
      # - node.labels.zone != $CRITICAL_HOST_TIEBREAKER
```

Create the `.env` file the compose file reads image tags and zone assignments from:

```bash
cat <<EOF > .env
JUBE_IMAGE=jube.app:<date>
PATRONI_IMAGE=jube.patroni:<date>
CRITICAL_HOST_1=host1
CRITICAL_HOST_2=host1
CRITICAL_HOST_3=host1
CRITICAL_HOST_4=host1
CRITICAL_HOST_TIEBREAKER=host1
EOF
```

Generate and load all secrets:

```bash
sudo chmod +x secrets-init.sh
./secrets-init.sh
```

`secrets-init.sh` is destructive by design - it prompts for confirmation, then recreates every secret from scratch
(random passwords, a random `ENCRYPTION_KEY` for `ElementSymmetricEncryptionKey`, a random `HAPROXY_COOKIE_SECRET`
for HAProxy's session-affinity cookie, plus a fresh 4096-bit RSA keypair for
`PasswordAsymmetricEncryptionPrivateKey`/`PasswordAsymmetricEncryptionPublicKey`) and loads them into Docker Swarm.
It also writes everything it generated to `secrets.txt` as a one-time bootstrap record, since Swarm secrets
themselves can't be read back out once created.

**`secrets.txt` must be moved to secure storage and then deleted from the deployment directory immediately** - it is
the only place the generated credentials exist in the clear, and this workflow assumes it does not persist on disk
past the initial setup. The public half of the RSA keypair is printed to the console separately and also written to
`secrets.txt`, and needs to be copied into the `PasswordAsymmetricEncryptionPublicKey` Environment Variable for
`jube-api`/`jube-ui` in `docker-compose.yml` (the `CHANGE-ME-paste-public-key-printed-by-secrets-init.sh`
placeholder) before the first deploy - only the private key travels through Swarm secrets/tokenisation
automatically; the public key is not itself sensitive, but does need this one manual step. Without it,
`PasswordAsymmetricEncryption=True` (set by default in the compose file for `jube-api`/`jube-ui`) will fail to
start, since [Jube refuses to start with RSA password encryption enabled and either key
missing](../../Concepts/EnvironmentVariables/index.html). `ENCRYPTION_KEY` and `PASSWORD_ASYMMETRIC_ENCRYPTION_PRIVATE_KEY`
require no such manual step - both flow into `jube-api`/`jube-jobs`/`jube-ui` automatically via the `[@Key@]` Docker
Secrets tokenisation pattern (see [Environment Variables](../../Concepts/EnvironmentVariables/index.html)), the same
as the passwords and `JWT_KEY`/`API_HMAC_KEY` above. `HAPROXY_COOKIE_SECRET` also requires no manual step, but flows
in differently - see the note on it under [Creating Secrets](#creating-secrets) above, since HAProxy's config file
has no equivalent of Jube's `[@Key@]` tokenisation.

## Every Deploy (Fresh or After Full Reset)

Remove any existing stack first:

```bash
sudo chmod +x remove.sh
./remove.sh
```

Deploy the stack:

```bash
sudo chmod +x deploy.sh
./deploy.sh
```

Check stability:

```bash
docker service ls
```

Watch Patroni especially - wait until all four nodes show, a leader is elected, and lag is 0:

```bash
docker exec -it $(docker ps -q -f name=patroni1) patronictl -c /etc/patroni.yml list
```

Check logs if anything looks wrong:

```bash
docker service logs --follow --since 1m jube-cluster_patroni1
```

If a node doesn't come up on its own - common on a brand new cluster - force it:

```bash
docker service update --force jube-cluster_patroni1
```

Then create the pgBackRest stanza (once, on a fresh cluster) and verify it:

```bash
docker exec -u postgres $(docker ps -q -f name=patroni1) \
    pgbackrest --config=/etc/pgbackrest/pgbackrest.conf \
               --lock-path=/var/lib/postgresql/tmp/pgbackrest-lock \
               --stanza=postgres-cluster \
               stanza-create

docker exec -u postgres $(docker ps -q -f name=patroni1) \
    pgbackrest --config=/etc/pgbackrest/pgbackrest.conf \
               --stanza=postgres-cluster \
               check
```

If a Patroni node shows shutdown purely due to startup timing, force it individually - **never force two Patroni
nodes at once, or you risk losing quorum**:

```bash
docker service update --force jube-cluster_patroni2
```

Verify HAProxy shows exactly **one** green server in `postgres_primary` at `http://<host-ip>:7000`.

### Getting patroni1 into the leader role

The deployment expects `patroni1` specifically to hold the leader role. Check who currently holds it, and who the
current Sync Standby is - you can only promote the current Sync Standby, so getting `patroni1` into the lead
sometimes takes two switchovers rather than one:

```bash
docker exec -it $(docker ps -q -f name=patroni1) patronictl -c /etc/patroni.yml list
```

If `patroni1` is not the current Sync Standby, switch leadership to whichever node is (this rotates `patroni1` into
the Sync Standby slot):

```bash
docker exec -it $(docker ps -q -f name=patroni1) \
  patronictl -c /etc/patroni.yml switchover postgres-cluster \
  --leader <current-leader> --candidate <current-sync-standby> --force
```

Once `patroni1` shows as the Sync Standby, perform the final switchover to make it leader:

```bash
docker exec -it $(docker ps -q -f name=patroni1) \
  patronictl -c /etc/patroni.yml switchover postgres-cluster \
  --leader <current-leader> --candidate patroni1 --force
```

### Creating the database users

Run the least-privilege database user setup **before** scaling Jube up - `jube-api`/`jube-jobs`/`jube-ui` connect as
`jube_app`/`jube_reporting`/`jube_migration` from the moment they start, and those users don't exist until this
script creates them (mirroring the same `service`/`reporting` least-privilege split as the single-node
[`CreateUsers.sql`](../CreateUsers.sql) referenced from [Deploying with Docker](../DeployingWithDocker/index.html) -
`jube_migration` gets DDL rights, `jube_app` gets DML only, `jube_reporting` gets read-only):

```bash
sudo chmod +x database.sh
./database.sh
```

Scale up `jube-jobs` first, since it runs the database migration on startup:

```bash
docker service scale jube-cluster_jube-jobs=1
```

Watch its logs until migration completes - a welcome message with no .NET exceptions:

```bash
docker service logs -f jube-cluster_jube-jobs
```

Only then scale up the application tier:

```bash
docker service scale jube-cluster_jube-api=4 jube-cluster_jube-ui=4
```

Watch both come up cleanly the same way:

```bash
docker service logs -f jube-cluster_jube-api
docker service logs -f jube-cluster_jube-ui
```

Navigate to `http://<host-ip>:5001`.

> `jube-api` and `jube-ui` are both defined with `replicas: 0` in the compose file, and scaled up manually only
> after migration succeeds - deliberately, so a fresh deployment can never race the application tier against an
> unmigrated schema.

Run a full backup to validate the NAS mount is actually working end to end, not just mounted:

```bash
# Find the primary
docker ps | grep patroni1

# Full backup, skipping WAL verification (busy cluster, WAL is archived separately anyway)
docker exec -it <container-id> pgbackrest --stanza=postgres-cluster --archive-check=n --archive-timeout=60 --type=full backup

# Watch progress
docker exec -it <container-id> ps aux | grep pgbackrest
```

Browse the NAS separately to confirm Redis AOF/RDB files are landing there too.

Finally, clear the bootstrap secrets from the shell and disk:

```bash
unset PATRONI_SUPERUSER_PASSWORD PATRONI_REPLICATION_PASSWORD PATRONI_ADMIN_PASSWORD \
      JUBE_APP_PASSWORD JUBE_REPORTING_PASSWORD JUBE_MIGRATION_PASSWORD REDIS_PASSWORD \
      API_HMAC_KEY JWT_KEY PASSWORD_HASHING_KEY ENCRYPTION_KEY HAPROXY_COOKIE_SECRET

rm secrets.txt   # only after it's safely stored elsewhere
```

## Forcing a Compose Change to Apply

Swarm diffs the running service spec against the new compose file and only updates what changed - occasionally it
decides nothing has changed when something has, and silently skips the update.

Check what the running service actually has:

```bash
docker service inspect jube-cluster_<service> --pretty
```

Force Swarm to re-evaluate the whole compose spec:

```bash
docker stack deploy --with-registry-auth -c docker-compose.yml jube-cluster
```

Force a specific service, optionally busting the image cache:

```bash
docker service update --force jube-cluster_<service>
docker service update --force --image <image>:latest jube-cluster_<service>
```

## Full Reset of Corrupted Swarm State

```bash
docker stack rm jube-cluster
sleep 15
docker swarm leave --force
docker network prune -f
sudo rm -rf /var/lib/docker/swarm
sudo systemctl restart docker
docker swarm init --advertise-addr <node1-ip>
docker stack deploy -c docker-compose.yml jube-cluster
```

## Monitoring Dashboards

Two operational dashboards are deployed alongside the cluster - a container log viewer and an uptime monitor - both
defined in `docker-compose.yml`:

```yaml
  dozzle:
    image: amir20/dozzle:latest
    command:
      - --auth-provider
      - simple
    environment:
      - DOZZLE_MODE=swarm
    volumes:
      - /var/run/docker.sock:/var/run/docker.sock:ro
      - ./dozzle/users.yml:/data/users.yml:ro
    ports:
      - "8080:8080"
    networks:
      jube-cluster:
        aliases:
          - dozzle
    deploy:
      mode: global
      restart_policy:
        condition: on-failure
        delay: 30s
      placement:
        constraints:
          - node.role == manager

  uptime-kuma:
    image: louislam/uptime-kuma:1
    volumes:
      - kuma_data:/app/data
      - /var/run/docker.sock:/var/run/docker.sock:ro
    ports:
      - "3001:3001"
    networks:
      jube-cluster:
        aliases:
          - uptime-kuma
    deploy:
      restart_policy:
        condition: on-failure
        delay: 30s
      replicas: 1
      placement:
        constraints:
          - node.role == manager
```

A few choices here are worth understanding rather than copying blind:

* **`mode: global` with a manager-only constraint on Dozzle**, rather than a fixed replica count, deploys one Dozzle
  instance per manager node. Paired with `DOZZLE_MODE=swarm`, each instance discovers and aggregates logs from every
  task across the whole Swarm, not just containers local to its own node - this is what the internal-only agent port
  `7007` in the Port Map below is for (agent-to-UI communication between those instances), and is Dozzle's documented
  pattern for Swarm log aggregation, rather than something specific to this cluster.
* **Uptime Kuma is pinned to `replicas: 1`** rather than scaled or global, and uses a named external volume
  (`kuma_data`) rather than a bind mount. Uptime Kuma keeps its state in a local SQLite database, which cannot be
  shared across replicas - running more than one would mean two independent, diverging monitors, and an unpinned
  single replica risks Swarm rescheduling it onto a node without its data volume. Pinning to `manager` plus
  `replicas: 1` keeps it running in exactly one predictable place.
* **Both mount `/var/run/docker.sock` read-only.** Docker socket access is powerful regardless of the read-only
  flag on the bind mount (a process with socket access can still start/stop/inspect containers via the Docker API),
  so this is a real trust decision, not a fully contained one - it's what both tools need to introspect the cluster,
  and is the standard mechanism for this class of tool, but it's worth knowing these two containers are more
  privileged than the rest of the stack.
* **Dozzle is given its own login** (`--auth-provider simple`, backed by `./dozzle/users.yml`) on top of the
  network-layer restriction described below - defense in depth, since Dozzle's log viewer can surface anything an
  application logs, which may include sensitive data depending on log level. Generate real credentials before
  deploying:

  ```bash
  docker run --rm amir20/dozzle generate <username> --password <password> --name "<Display Name>"
  ```

  and replace the placeholder `dozzle/users.yml` with the output - the template committed in this directory is
  intentionally not a working credential file. Uptime Kuma has its own first-run admin account setup instead (no
  compose-level configuration needed for it).

## Port Map

### Host-Exposed Ports

| Port   | Service        | Reachable via              | Exposed?    | Reason                               |
|--------|----------------|----------------------------|-------------|--------------------------------------|
| `5432` | PostgreSQL R/W | HAProxy → patroni1/2/3/4   | ⚠️ Dev only | Remove in production                 |
| `5433` | PostgreSQL R/O | HAProxy → patroni1/2/3/4   | ⚠️ Dev only | Remove in production                 |
| `7000` | HAProxy Stats  | haproxy                    | ✅ Always    | Ops monitoring dashboard             |
| `8080` | Dozzle UI      | dozzle (manager only)      | ✅ Always    | Container log viewer                 |
| `3001` | Uptime Kuma    | uptime-kuma (manager only) | ✅ Always    | Uptime monitoring                    |
| `5001` | Jube UI        | jube-ui                    | ✅ Always    | Web interface                        |
| `5002` | Jube API       | jube-api                   | ✅ Always    | Public API (maps to 5001 internally) |

> In production, remove the two dev-only Postgres entries from the compose file's `ports:` block entirely.
> Application services on the Swarm network reach Postgres via `haproxy:5432`/`haproxy:5433` regardless.

Unlike Postgres, Redis has no host-published port and no HAProxy entry at all in this topology - clients (including
Jube itself) reach it exclusively via the Sentinel ports below, from inside the overlay network, matching the
Sentinel-aware connection pattern described in [Architecture Overview](#architecture-overview).

Ports `7000` (HAProxy stats) and `3001` (Uptime Kuma) are ops/diagnostic dashboards with no authentication layer of
their own beyond Uptime Kuma's first-run admin account. Port `8080` (Dozzle) has its own login (see
[Monitoring Dashboards](#monitoring-dashboards) above). For all three, this deployment assumes the ports are
reachable only from a private management network or VPN, never from the open internet - Dozzle's own auth is
defense in depth, not a substitute for that assumption. If it doesn't hold for your network, put a reverse proxy
with authentication in front of HAProxy stats and Uptime Kuma too, or firewall all three to known management IPs,
before exposing the cluster more broadly.

### Internal-Only Ports

| Port    | Service           | Reason                                                                                                |
|---------|-------------------|-------------------------------------------------------------------------------------------------------|
| `2379`  | etcd client API   | Cluster consensus - no external access needed                                                         |
| `2380`  | etcd peer traffic | etcd-to-etcd peering only                                                                             |
| `5432`  | Patroni direct    | Application traffic must flow through HAProxy                                                         |
| `8008`  | Patroni REST API  | HAProxy health checks only                                                                            |
| `6379`  | Redis direct      | Not used - see Sentinel ports below                                                                   |
| `26379` | Redis Sentinel    | Sentinel-to-Sentinel coordination, and the entry point Jube's Sentinel-aware Redis client connects to |
| `7007`  | Dozzle agent      | Agent-to-UI communication only                                                                        |

## Health Checks

**PostgreSQL (Patroni)** - HAProxy polls port 8008 every 3s:

- `GET /primary` → 200 OK → eligible for the `postgres_primary` backend.
- `GET /replica` → 200 OK → eligible for the `postgres_replicas` backend.
- Anything else → marked down after 3 consecutive failures; existing sessions on that server are terminated
  immediately on mark-down, rather than left to drain, since a demoted primary must stop taking writes at once.
- 2 consecutive successes → marked back up.

**Patroni container health** (Docker Swarm's own healthcheck) - polls `/liveness` on port 8008 every 10s, 10
retries. `/liveness` returns 200 as long as the Patroni process itself is alive, regardless of replication state -
it answers "is this container worth restarting", not "is this node the primary".

**Redis** - Sentinel monitors `redis-master` directly (not via HAProxy, per the architecture above):
`sentinel monitor redis-master redis-master 6379 3` (quorum of 3), `down-after-milliseconds 5000`,
`failover-timeout 60000`.

## Diagnostics — General

```bash
# All services and replica counts
docker service ls

# Why a service won't start
docker service ps --no-trunc jube-cluster_<service>

# Live logs for a service, or by task ID
docker service logs -f jube-cluster_<service>
docker service logs <task-id>

# Patroni cluster state
docker exec -it $(docker ps -q -f name=patroni1) patronictl -c /etc/patroni.yml list

# HAProxy stats
http://localhost:7000

# Shell into a container as root
docker exec -it -u root <container-id> /bin/sh

# Network tools on Alpine-based containers
apk add --no-cache busybox-extras
```

## Diagnostics — SELinux

```bash
# Recent denials, optionally filtered to a service
sudo ausearch -m AVC -ts recent
sudo ausearch -m AVC -ts recent | grep sentinel

# Check a file's SELinux label
ls -laZ <cluster-directory>/redis/sentinel1/sentinel.conf

# Temporarily disable, to confirm SELinux is the cause - then re-enable
sudo setenforce 0
sudo setenforce 1
```

> Always re-enable SELinux after testing. If disabling it fixes the issue, run the permissions script to apply the
> correct labels rather than leaving SELinux disabled - the permissions script exists specifically so this is never
> a live tradeoff.

## Diagnostics — Volumes

```bash
docker volume ls
docker volume ls -f dangling=true
docker volume inspect jube-cluster_etcd1_data
docker volume rm jube-cluster_etcd1_data
docker volume rm $(docker volume ls -f name=jube-cluster -q)
docker volume prune
```

## Diagnostics — Network

```bash
# Overlay networks
docker network ls --filter driver=overlay
docker network inspect jube-cluster_jube-cluster
docker network prune -f

# DNS resolution from inside a container
docker exec -it $(docker ps -q -f name=patroni1) nslookup etcd1
docker exec -it $(docker ps -q -f name=patroni1) cat /etc/resolv.conf

# Docker daemon errors, including network allocation failures
journalctl -u docker -n 50 --no-pager

# Firewall and host networking
sudo firewall-cmd --list-ports
ip addr show

# A container's internal overlay IP
docker inspect -f '{{range .NetworkSettings.Networks}}{{.IPAddress}}{{end}}' <container-id>
```

> Containers are reachable via the published host IP and port. Internal `10.x.x.x` overlay addresses are not
> routable from the host - always use the published host port, not the overlay IP, when connecting from outside a
> container.

## Diagnostics — Swarm Cluster Membership

```bash
docker node ls
docker node inspect <hostname> --pretty
docker swarm join-token manager
docker swarm join-token worker
docker node update --availability drain <hostname>   # before maintenance
docker node update --availability active <hostname>  # after maintenance
docker node promote <hostname>
```

## Diagnostics — Failover Testing

```bash
docker ps | grep patroni

# Simulate a hard failure
docker kill <container-id>

# Simulate a slow/unresponsive node, and recover from it
docker pause <container-id>
docker unpause <container-id>

# Manually trigger a Patroni failover
docker exec -it $(docker ps -q -f name=patroni1) \
    patronictl -c /etc/patroni.yml failover postgres-cluster

# Reinitialise a single bad node without cluster downtime
docker exec -it $(docker ps -q -f name=patroni1) \
    patronictl -c /etc/patroni.yml reinit postgres-cluster <member>
```

## File System

Reset the deployment directory back to sane, predictable permissions after a `chmod`/`chown` mishap:

```bash
sudo chown -R <deploy-user>:<deploy-user> <cluster-directory>
sudo find <cluster-directory> -type d -exec chmod 755 {} \;
sudo find <cluster-directory> -type f -exec chmod 644 {} \;
sudo find <cluster-directory> -name "*.sh" -exec chmod 755 {} \;
```

## Total Postgres Password Wipeout Recovery

If every Postgres credential is genuinely lost (not merely rotated), the local Unix socket accepts `trust`
authentication from the container's own `postgres` user - this is a last-resort local-access-only recovery path,
not a routine operation:

```bash
docker exec -it $(docker ps -q -f name=patroni1) su-exec postgres psql -U postgres
```

## Notes

- Never pre-create Docker networks manually - always let the stack manage them, since Swarm's own network lifecycle
  handling assumes it owns creation and teardown.
- Patroni and Redis should be pinned to specific hosts via placement constraints - unpinned, Swarm is free to
  reschedule a stateful service onto whichever host has capacity, which defeats the zone-per-node design this
  cluster relies on for fault isolation.
- Mount Postgres data to remote/redundant disks in production, not host-local storage.
- Remember the pgBackRest stanza step on a fresh cluster - nothing else prompts for it, and backups silently have
  nowhere to go without it.
- The Patroni entrypoint fixes pgBackRest volume ownership automatically on every container start - seeing the
  volume start with the wrong ownership (Fedora maps UID 999 to `avahi`) is expected and self-corrects.
- Sentinel conf files are rewritten by Redis Sentinel at runtime - reset them manually if they become corrupted.
- `jube-api` is mapped `5002:5001` externally - the application always listens on `5001` internally; `5002` is only
  the host-published port.
- `EnableMigration=True` is set only on `jube-jobs` - explicitly `False` on `jube-api` and `jube-ui`, so migration
  can never run from more than one place.
- On Fedora with SELinux, MCS category mismatches (`c###,c###` labels) are resolved with `chcon -Rl s0`, which
  strips the categories so any container can access the files.
- The `/run/docker.sock` SELinux label resets on every Docker restart - the permissions script re-applies it, so
  this is expected rather than a regression each time.
- `jube-api`'s `ReportConnectionString` points at the read replica pool (`haproxy:5433`), matching `jube-ui` and
  `jube-jobs` - all three route reporting queries away from the primary.
