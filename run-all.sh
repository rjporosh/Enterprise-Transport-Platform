#!/bin/bash

echo "========================================"
echo "  STARTING ALL 6 SERVICES"
echo "========================================"

run_svc() {
    local svc=$1
    local project=$2
    local api="services/${svc}-service/src/${project}.Api/${project}.Api.csproj"
    echo ">>> Starting: ${svc}-service..."
    dotnet run --project "$api" --no-restore &
}

run_svc "auth" "AuthService"
run_svc "booking" "BookingService"
run_svc "bus" "BusService"
run_svc "notification" "NotificationService"
run_svc "payment" "PaymentService"
run_svc "route" "RouteService"
run_svc "ticketing" "TicketingService"

echo ""
echo "========================================"
echo "  All 7 services starting..."
echo "  Press Ctrl+C to stop all"
echo "========================================"

wait
