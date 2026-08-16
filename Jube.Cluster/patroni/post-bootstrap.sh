#!/bin/bash
psql -U postgres -c "ALTER USER admin WITH PASSWORD '${PATRONI_ADMIN_PASSWORD}';"