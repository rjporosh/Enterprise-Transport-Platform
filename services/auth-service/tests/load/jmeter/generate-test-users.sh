#!/usr/bin/env bash
# Registers N users against a running Auth Service and writes test-users.csv
# (email,password) for the JMeter "Login Load" thread group's CSV Data Set
# config, which expects that exact filename in this directory.
#
# Usage: BASE_URL=http://localhost:8081 ./generate-test-users.sh 50
set -euo pipefail

BASE_URL="${BASE_URL:-http://localhost:8081}"
COUNT="${1:-50}"
PASSWORD="correct-horse-battery-staple"
OUT="$(dirname "$0")/test-users.csv"

echo "email,password" > "$OUT"
for i in $(seq 1 "$COUNT"); do
  EMAIL="jmeter-user-${i}-$(date +%s)@example.com"
  curl -s -o /dev/null -X POST "$BASE_URL/api/v1/auth/register" \
    -H "Content-Type: application/json" \
    -d "{\"email\":\"$EMAIL\",\"password\":\"$PASSWORD\",\"firstName\":\"JMeter\",\"lastName\":\"User$i\",\"phoneNumber\":null}"
  echo "$EMAIL,$PASSWORD" >> "$OUT"
done

echo "Wrote $COUNT users to $OUT"
