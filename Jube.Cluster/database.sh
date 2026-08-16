#!/bin/bash
# psql-leader.sh — finds the Patroni leader and runs SQL

ENV_FILE="$(dirname "$0")/secrets.txt"

if [ ! -f "$ENV_FILE" ]; then
    echo "ERROR: .env file not found at $ENV_FILE — aborting."
    exit 1
fi

set -a
source "$ENV_FILE"
set +a

# Find the leader node name from patronictl
LEADER=$(docker exec $(docker ps -q -f name=patroni1) \
    patronictl -c /etc/patroni.yml list 2>/dev/null \
    | awk '/Leader/ {print $2}')

if [ -z "$LEADER" ]; then
    echo "ERROR: Could not determine Patroni leader — aborting."
    exit 1
fi

echo "Leader is: $LEADER"
echo "Running SQL..."

docker exec -i \
    -e PGPASSWORD="$PATRONI_SUPERUSER_PASSWORD" \
    $(docker ps -q -f name=$LEADER) \
    psql -h haproxy -p 5432 -U postgres \
    -v app_user="$APP_USER" \
    -v app_db="$APP_DB" \
    -v app_password="$JUBE_APP_PASSWORD" \
    -v reporting_password="$JUBE_REPORTING_PASSWORD" \
    -v migration_password="$JUBE_MIGRATION_PASSWORD" \
    << EOF
CREATE USER jube_app WITH PASSWORD '$JUBE_APP_PASSWORD';
CREATE USER jube_reporting WITH PASSWORD '$JUBE_REPORTING_PASSWORD';
CREATE USER jube_migration WITH PASSWORD '$JUBE_MIGRATION_PASSWORD';
ALTER USER jube_app SET search_path = public;
ALTER USER jube_reporting SET search_path = public;
ALTER USER jube_migration SET search_path = public;
GRANT USAGE, CREATE ON SCHEMA public TO jube_migration;
GRANT SELECT, INSERT, UPDATE, DELETE ON ALL TABLES IN SCHEMA public TO jube_migration;
GRANT ALL ON ALL SEQUENCES IN SCHEMA public TO jube_migration;
ALTER USER jube_migration NOCREATEDB NOCREATEROLE NOSUPERUSER;
GRANT USAGE ON SCHEMA public TO jube_app;
GRANT SELECT, INSERT, UPDATE ON ALL TABLES IN SCHEMA public TO jube_app;
GRANT USAGE, SELECT ON ALL SEQUENCES IN SCHEMA public TO jube_app;
ALTER DEFAULT PRIVILEGES FOR ROLE jube_migration IN SCHEMA public GRANT SELECT, INSERT, UPDATE ON TABLES TO jube_app;
ALTER DEFAULT PRIVILEGES FOR ROLE jube_migration IN SCHEMA public GRANT USAGE, SELECT ON SEQUENCES TO jube_app;
GRANT USAGE ON SCHEMA public TO jube_reporting;
GRANT SELECT ON ALL TABLES IN SCHEMA public TO jube_reporting;
ALTER DEFAULT PRIVILEGES FOR ROLE jube_migration IN SCHEMA public GRANT SELECT ON TABLES TO jube_reporting;
EOF

echo "Done."