FROM alpine:3.21 AS builder

RUN apk add --no-cache \
    postgresql17-dev \
    python3 \
    python3-dev \
    py3-virtualenv \
    gcc \
    musl-dev \
    linux-headers \
    libffi-dev \
    openssl-dev \
    pkgconfig \
    make \
    libc-dev

RUN python3 -m venv /opt/patroni \
 && /opt/patroni/bin/pip install --no-cache-dir --upgrade pip setuptools wheel \
 && /opt/patroni/bin/pip install --no-cache-dir "patroni[etcd3]==3.3.2" psycopg2==2.9.9


FROM alpine:3.21

RUN apk add --no-cache \
    postgresql17 \
    postgresql17-client \
    python3 \
    curl \
    jq \
    bash \
    su-exec \
    pgbackrest

COPY --from=builder /opt/patroni /opt/patroni

RUN mkdir -p /var/lib/postgresql/data \
             /var/lib/postgresql/tmp \
             /var/lib/postgresql/tmp/pgbackrest-lock \
             /var/run/postgresql \
             /etc/patroni \
             /home/postgres/.config \
             /etc/pgbackrest \
             /var/log/pgbackrest \
             /var/spool/pgbackrest \
 && chown -R postgres:postgres \
        /var/lib/postgresql \
        /var/run/postgresql \
        /etc/patroni \
        /home/postgres \
        /etc/pgbackrest \
        /var/log/pgbackrest \
        /var/spool/pgbackrest \
 && chmod 750 /var/lib/postgresql/data \
 && chmod 700 /var/lib/postgresql/tmp \
 && chmod 755 /var/run/postgresql /etc/patroni /home/postgres/.config

ENV TMPDIR=/var/lib/postgresql/tmp

RUN cat > /post_bootstrap.sh << 'EOF'
#!/bin/bash
PATRONI_ADMIN_PASSWORD=$(cat /run/secrets/PATRONI_ADMIN_PASSWORD)
psql -U postgres -c "CREATE USER admin WITH PASSWORD '${PATRONI_ADMIN_PASSWORD}' CREATEROLE CREATEDB;" 2>/dev/null || \
psql -U postgres -c "ALTER USER admin WITH PASSWORD '${PATRONI_ADMIN_PASSWORD}';"
EOF
RUN chmod +x /post_bootstrap.sh

RUN cat > /entrypoint.sh << 'EOF'
#!/bin/bash
set -e

load_secret() {
    local var_name=$1
    local file_path="/run/secrets/$var_name"
    if [ -f "$file_path" ]; then
        export "$var_name"=$(cat "$file_path")
    fi
}

if [ ! -f /var/lib/pgbackrest/repo/.initialized ]; then
    echo "Initialising pgBackRest repo directory ownership..."
    mkdir -p /var/lib/pgbackrest/repo
    chown -R postgres:postgres /var/lib/pgbackrest/repo
    chmod 750 /var/lib/pgbackrest/repo
    touch /var/lib/pgbackrest/repo/.initialized
fi

load_secret "PATRONI_SUPERUSER_PASSWORD"
load_secret "PATRONI_REPLICATION_PASSWORD"
load_secret "PATRONI_ADMIN_PASSWORD"

exec su-exec postgres patroni "$@"
EOF

RUN chmod +x /entrypoint.sh

ENV PATH="/opt/patroni/bin:$PATH"

HEALTHCHECK --interval=10s --timeout=5s --retries=3 \
    CMD curl -sf http://localhost:8008/health || exit 1

EXPOSE 8008 5432 8432

ENTRYPOINT ["/entrypoint.sh"]
CMD ["/etc/patroni.yml"]
