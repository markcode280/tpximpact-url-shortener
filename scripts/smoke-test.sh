#!/usr/bin/env bash
set -euo pipefail

base_url="${SNIP_BASE_URL:-http://localhost:3000}"
alias="smoke-${RANDOM}-${RANDOM}"
destination="https://example.com/smoke-test"

curl --fail --silent "$base_url/health" >/dev/null
create_status=$(curl --silent --output /dev/null --write-out '%{http_code}' \
  -H 'Content-Type: application/json' \
  --data "{\"fullUrl\":\"$destination\",\"customAlias\":\"$alias\"}" "$base_url/shorten")
test "$create_status" = 201
redirect_headers=$(curl --silent --dump-header - --output /dev/null "$base_url/$alias")
[[ "$redirect_headers" == *"302"* ]]
[[ "$redirect_headers" == *"$destination"* ]]
curl --fail --silent "$base_url/urls?page=1&pageSize=10" >/dev/null
delete_status=$(curl --silent --output /dev/null --write-out '%{http_code}' -X DELETE "$base_url/$alias")
test "$delete_status" = 204
missing_status=$(curl --silent --output /dev/null --write-out '%{http_code}' "$base_url/$alias")
test "$missing_status" = 404
printf '%s\n' 'Smoke test passed: health, create, redirect, list, persistent delete.'
