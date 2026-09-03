#!/bin/bash

echo "========================================"
echo "  DATABASE UPDATE - ALL 6 SERVICES"
echo "========================================"

update_db() {
    local svc=$1
    local project=$2
    local infra="services/${svc}-service/src/${project}.Infrastructure/${project}.Infrastructure.csproj"
    local api="services/${svc}-service/src/${project}.Api/${project}.Api.csproj"
    
    echo ""
    echo ">>> Restoring & Updating: ${svc}-service"
    echo "----------------------------------------"
    
    dotnet restore "$api" --verbosity quiet 2>/dev/null
    dotnet restore "$infra" --verbosity quiet 2>/dev/null
    dotnet ef database update --project "$infra" --startup-project "$api"
    
    if [ $? -eq 0 ]; then
        echo "✅ ${svc}-service - SUCCESS"
    else
        echo "❌ ${svc}-service - FAILED"
    fi
}

update_db "auth" "AuthService"
update_db "booking" "BookingService"
update_db "bus" "BusService"
update_db "notification" "NotificationService"
update_db "payment" "PaymentService"
update_db "route" "RouteService"
update_db "ticketing" "TicketingService"

echo ""
echo "========================================"
echo "  ALL DONE!"
echo "========================================"
