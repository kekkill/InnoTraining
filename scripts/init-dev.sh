#!/usr/bin/env bash
set -euo pipefail
cd "$(dirname "$0")/.."
if [ -e .env ]; then echo '.env already exists; no changes made.' >&2; exit 1; fi
umask 077
{
  echo "jwt_key=$(openssl rand -base64 48 | tr -d '\n')"
  echo "service_key=$(openssl rand -base64 32 | tr -d '\n')"
  echo "users_db_password=$(openssl rand -base64 24 | tr -d '\n')"
  echo "products_db_password=$(openssl rand -base64 24 | tr -d '\n')"
  echo 'admin_email=admin@example.com'
  echo "admin_password=Aa1!$(openssl rand -base64 24 | tr -d '\n')"
  echo 'users_public_url=http://localhost:5081'
} > .env
echo '.env created. Local admin credentials are in this file; do not commit it.'
