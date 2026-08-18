---
layout: default
title: Software Inventory
nav_order: 2
parent: Deploying with Jube Cluster
grand_parent: Getting Started
---

🚀 Get to pre-production in weeks, not months, with private [training](https://www.jube.io/jube-training) direct from
Jube's developer — real sovereignty, zero vendor lock-in.

# Software Inventory

Every container image, package and runtime version that makes up the `Jube.Cluster` Docker Swarm stack, as defined in
`Jube.Cluster/docker-compose.yml` and the Dockerfiles it builds from. Useful for an upgrade, a security audit, or just
confirming what is actually running before raising a support ticket.

Two kinds of image make up the stack: pulled straight off the shelf (etcd, Redis, HAProxy, the two dashboards) and built
in-repo from a Dockerfile (Patroni, Jube itself). For the former, the version below is whatever tag
`docker-compose.yml` pins; for the latter, it's whatever the Dockerfile installs at build time. **Unpinned** means the
compose file or Dockerfile doesn't fix a version - it floats to whatever is current when the image is built or pulled,
and is worth locking down deliberately before a production build.

## Quick reference

| Component  | Version  |
|------------|----------|
| PostgreSQL | 17       |
| Patroni    | 3.3.2    |
| etcd       | v3.5.3   |
| Redis      | 7-alpine |
| HAProxy    | 2.8      |
| .NET       | 9.0      |
| Alpine     | 3.21     |

## Consensus & coordination

etcd is Patroni's leader-election store - five nodes, deliberately odd, so a network partition can't produce a tied vote
(see [Architecture Overview](../DeploymentRunbook/index.html#architecture-overview)).

| Service           | Image                 | Version  | Notes                                            |
|-------------------|-----------------------|----------|--------------------------------------------------|
| `etcd1` … `etcd5` | `quay.io/coreos/etcd` | `v3.5.3` | Pinned in `docker-compose.yml`. `ETCDCTL_API=3`. |

## Database & backup

Built from `Jube.Cluster/patroni/Dockerfile` - a two-stage Alpine build that compiles Patroni into a virtualenv, then
lays it over a slim Postgres/pgBackRest runtime image. Tagged and referenced as `${PATRONI_IMAGE}` in the compose file
(see [Building and Distributing Images](../DeploymentRunbook/index.html#building-and-distributing-images)).

| Component    | Version    | Notes                                                                                                                                     |
|--------------|------------|-------------------------------------------------------------------------------------------------------------------------------------------|
| Alpine Linux | 3.21       | Both build and runtime stages.                                                                                                            |
| PostgreSQL   | 17.x       | `postgresql17` / `postgresql17-client` / `postgresql17-dev` - Alpine 3.21's packaged 17 branch; patch version floats with the base image. |
| Patroni      | 3.3.2      | Pinned: `patroni[etcd3]==3.3.2`, installed via pip into a dedicated venv.                                                                 |
| psycopg2     | 2.9.9      | Pinned, Patroni's Postgres driver.                                                                                                        |
| pgBackRest   | *unpinned* | Alpine `pgbackrest` package - whatever the 3.21 repos currently carry.                                                                    |

> **Dev-only comparison** - the repo-root `docker-compose.yml` (single-node evaluation setup, not part of the Swarm
> cluster) runs plain `postgres:17` rather than the Patroni-wrapped image above.

## Cache & high availability

Redis master/replica under Sentinel - five Sentinels, for the same odd-quorum reasoning as etcd. All six roles run the
same off-the-shelf image.

| Service                                               | Image            | Version | Notes                                                                                                    |
|-------------------------------------------------------|------------------|---------|----------------------------------------------------------------------------------------------------------|
| `redis-master`, `redis-replica1`-`3`, `sentinel1`-`5` | `redis:7-alpine` | 7.x     | Major version pinned; patch floats with the alpine tag. Same image serves both Redis and Sentinel roles. |

> **Dev-only comparison** - the repo-root `docker-compose.yml` instead runs `redis/redis-stack:latest` - a different
> image family, fully unpinned.

## Edge & load balancing

| Service   | Image     | Version | Notes                                                                                                                          |
|-----------|-----------|---------|--------------------------------------------------------------------------------------------------------------------------------|
| `haproxy` | `haproxy` | `2.8`   | Pinned in `docker-compose.yml`. Routes Postgres primary/replica traffic (via Patroni's REST API) and both Jube HTTP frontends. |

## Application tier

Built from `Jube.App/Dockerfile`, tagged as `${JUBE_IMAGE}` and shared by `jube-ui`, `jube-api` and `jube-jobs` - same
image, three different Environment Variable profiles.

| Component                            | Version  | Notes                             |
|--------------------------------------|----------|-----------------------------------|
| `mcr.microsoft.com/dotnet/sdk`       | 9.0      | Build stage.                      |
| `mcr.microsoft.com/dotnet/aspnet`    | 9.0      | Runtime base for the final image. |
| Target framework (`Jube.App.csproj`) | `net9.0` |                                   |

> **Not part of the Swarm stack** - `Jube.LoadTest/Dockerfile` also targets `dotnet/sdk:9.0` and
> `dotnet/runtime:9.0`, but it's a standalone load-testing tool, not a service in `Jube.Cluster/docker-compose.yml`.

## Observability

| Service     | Image                  | Version               | Notes                                                     |
|-------------|------------------------|-----------------------|-----------------------------------------------------------|
| Dozzle      | `amir20/dozzle`        | *unpinned* (`latest`) | Swarm-wide log viewer, one instance per manager node.     |
| Uptime Kuma | `louislam/uptime-kuma` | 1.x                   | Major version pinned only; single replica (SQLite state). |

## Image tags resolved outside the repo

`${PATRONI_IMAGE}` and `${JUBE_IMAGE}` aren't hardcoded anywhere in `docker-compose.yml` - they come from a `.env`
file created at deploy time (not committed), following the build-tag-load workflow in the
[Deployment Runbook](../DeploymentRunbook/index.html#building-and-distributing-images):

```bash
docker build --no-cache -t jube.patroni:<date> .
docker build --no-cache -f Jube.App/Dockerfile -t jube.app:<date> .
```

`.env` then sets `PATRONI_IMAGE=jube.patroni:<date>` and `JUBE_IMAGE=jube.app:<date>`.

Images are distributed as tar files (`docker save` / `docker load`) rather than pulled from a registry - the standard
pattern for an air-gapped or tightly firewalled on-premises deployment, which this cluster is designed for. Whatever
date/tag was actually loaded on the running nodes is the ground truth, not this page - confirm with
`docker images` on each host.

## Sources

`Jube.Cluster/docker-compose.yml`, `Jube.Cluster/patroni/Dockerfile`, `Jube.App/Dockerfile`,
`Jube.LoadTest/Dockerfile`, and the repo-root `docker-compose.yml`. Package versions inside Alpine-based images (marked
*unpinned*) reflect whatever the Alpine 3.21 package repositories carried at build time, not a version fixed in source -
re-check after any rebuild.
