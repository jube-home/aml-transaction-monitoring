#!/bin/bash
set -e

read -rp "This will recreate all Docker secrets.  THIS IS DESTRUCTIVE. Are you sure? [y/N] " confirm
[[ "$confirm" =~ ^[Yy]$ ]] || { echo "Aborted."; exit 1; }

genkey() {
    local length="${1:-32}"
    local charset="${2:-A-Za-z0-9_-}"

    if ! [[ "$length" =~ ^[0-9]+$ ]] || (( length < 1 )); then
        echo "Usage: genkey [length] [charset]" >&2
        return 1
    fi

    LC_ALL=C tr -dc "$charset" </dev/urandom | head -c "$length"
    echo
}

create_secret() {
    local name="$1" value="$2"
    docker secret rm "$name" &>/dev/null || true
    printf "%s" "$value" | docker secret create "$name" - > /dev/null
}

# Generate RSA key pair for asymmetric password encryption
echo "Generating RSA 4096-bit key pair..."
TMPKEY=$(mktemp)
trap "rm -f $TMPKEY" EXIT

openssl genrsa 4096 2>/dev/null > "$TMPKEY"
PRIVATE_KEY_B64=$(base64 -w 0 < "$TMPKEY")
PUBLIC_KEY_B64=$(openssl rsa -pubout -in "$TMPKEY" 2>/dev/null | base64 -w 0)

# Verify keys
openssl rsa -check -in "$TMPKEY" -noout 2>/dev/null && echo "Private key: OK"
echo "$PUBLIC_KEY_B64" | base64 -d | openssl rsa -pubin -noout 2>/dev/null && echo "Public key: OK"

rm -f "$TMPKEY"
trap - EXIT

PATRONI_SUPERUSER_PASSWORD=$(genkey 16)
PATRONI_REPLICATION_PASSWORD=$(genkey 16)
PATRONI_ADMIN_PASSWORD=$(genkey 16)
JUBE_APP_PASSWORD=$(genkey 16)
JUBE_REPORTING_PASSWORD=$(genkey 16)
JUBE_MIGRATION_PASSWORD=$(genkey 16)
REDIS_PASSWORD=$(genkey 16)
API_HMAC_KEY=$(genkey 32)
JWT_KEY=$(genkey 32)
PASSWORD_HASHING_KEY=$(genkey 32)
ENCRYPTION_KEY=$(genkey 32)
HAPROXY_COOKIE_SECRET=$(genkey 32)

create_secret PATRONI_SUPERUSER_PASSWORD                 "$PATRONI_SUPERUSER_PASSWORD"
create_secret PATRONI_REPLICATION_PASSWORD               "$PATRONI_REPLICATION_PASSWORD"
create_secret PATRONI_ADMIN_PASSWORD                     "$PATRONI_ADMIN_PASSWORD"
create_secret JUBE_APP_PASSWORD                          "$JUBE_APP_PASSWORD"
create_secret JUBE_REPORTING_PASSWORD                    "$JUBE_REPORTING_PASSWORD"
create_secret JUBE_MIGRATION_PASSWORD                    "$JUBE_MIGRATION_PASSWORD"
create_secret REDIS_PASSWORD                             "$REDIS_PASSWORD"
create_secret API_HMAC_KEY                               "$API_HMAC_KEY"
create_secret JWT_KEY                                    "$JWT_KEY"
create_secret PASSWORD_HASHING_KEY                       "$PASSWORD_HASHING_KEY"
create_secret PASSWORD_ASYMMETRIC_ENCRYPTION_PRIVATE_KEY "$PRIVATE_KEY_B64"
create_secret ENCRYPTION_KEY "$ENCRYPTION_KEY"
create_secret HAPROXY_COOKIE_SECRET "$HAPROXY_COOKIE_SECRET"

echo "PATRONI_SUPERUSER_PASSWORD=$PATRONI_SUPERUSER_PASSWORD
PATRONI_REPLICATION_PASSWORD=$PATRONI_REPLICATION_PASSWORD
PATRONI_ADMIN_PASSWORD=$PATRONI_ADMIN_PASSWORD
JUBE_APP_PASSWORD=$JUBE_APP_PASSWORD
JUBE_REPORTING_PASSWORD=$JUBE_REPORTING_PASSWORD
JUBE_MIGRATION_PASSWORD=$JUBE_MIGRATION_PASSWORD
REDIS_PASSWORD=$REDIS_PASSWORD
API_HMAC_KEY=$API_HMAC_KEY
JWT_KEY=$JWT_KEY
PASSWORD_HASHING_KEY=$PASSWORD_HASHING_KEY
PASSWORD_ASYMMETRIC_ENCRYPTION_PRIVATE_KEY=$PRIVATE_KEY_B64
PASSWORD_ASYMMETRIC_ENCRYPTION_PUBLIC_KEY=$PUBLIC_KEY_B64
ENCRYPTION_KEY=$ENCRYPTION_KEY
HAPROXY_COOKIE_SECRET=$HAPROXY_COOKIE_SECRET" > secrets.txt

echo ""
echo "Public key (PASSWORD_ASYMMETRIC_ENCRYPTION_PUBLIC_KEY - base64):"
echo "$PUBLIC_KEY_B64"
echo ""
echo "Secrets file created (secrets.txt) which contains all infrastructure passwords and keys. IMPORTANT: Please be certain to remove this file once it has been stored securely."