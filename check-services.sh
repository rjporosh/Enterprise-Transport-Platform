#!/bin/bash

echo "========================================"
echo "  SERVICE STATUS CHECK"
echo "========================================"

check() {
    local name=$1
    local port=$2
    if lsof -i :$port -sTCP:LISTEN > /dev/null 2>&1; then
        echo "✅ $name - Running on :$port"
    else
        echo "❌ $name - NOT running on :$port"
    fi
}

check "Auth Service" 5101
check "Notification Service" 5301
check "Route Service" 5401
check "Payment Service" 5003
check "Booking Service" 5601
check "Bus Service" 5201

echo ""
echo "========================================"
