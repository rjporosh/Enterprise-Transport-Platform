cat > update-all.sh << 'EOF'
#!/bin/bash

SERVICES=("auth" "booking" "bus" "notification" "payment" "route")

echo "========================================"
echo "  DATABASE UPDATE - ALL 6 SERVICES"
echo "========================================"

for svc in "${SERVICES[@]}"; do
    SERVICE_NAME="${svc}-service"
    PROJECT_NAME="$(echo ${svc} | sed 's/.*/\u&/')Service"
    
    INFRA="services/${SERVICE_NAME}/src/${PROJECT_NAME}.Infrastructure/${PROJECT_NAME}.Infrastructure.csproj"
    API="services/${SERVICE_NAME}/src/${PROJECT_NAME}.Api/${PROJECT_NAME}.Api.csproj"
    
    echo ""
    echo ">>> Updating: ${SERVICE_NAME}"
    echo "----------------------------------------"
    
    dotnet ef database update \
        --project "$INFRA" \
        --startup-project "$API"
    
    if [ $? -eq 0 ]; then
        echo "✅ ${SERVICE_NAME} - SUCCESS"
    else
        echo "❌ ${SERVICE_NAME} - FAILED"
    fi
done

echo ""
echo "========================================"
echo "  ALL DONE!"
echo "========================================"
EOF

chmod +x update-all.sh