cat > run-all.sh << 'EOF'
#!/bin/bash

SERVICES=("auth" "booking" "bus" "notification" "payment" "route")

echo "========================================"
echo "  STARTING ALL 6 SERVICES"
echo "========================================"

for svc in "${SERVICES[@]}"; do
    SERVICE_NAME="${svc}-service"
    PROJECT_NAME="$(echo ${svc} | sed 's/.*/\u&/')Service"
    API="services/${SERVICE_NAME}/src/${PROJECT_NAME}.Api/${PROJECT_NAME}.Api.csproj"
    
    echo ">>> Starting: ${SERVICE_NAME}..."
    dotnet run --project "$API" &
done

echo ""
echo "========================================"
echo "  All 6 services running!"
echo "  Press Ctrl+C to stop all"
echo "========================================"

wait
EOF

chmod +x run-all.sh