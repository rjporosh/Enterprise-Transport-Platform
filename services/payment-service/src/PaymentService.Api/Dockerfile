FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY ["services/payment-service/src/PaymentService.Api/PaymentService.Api.csproj", "services/payment-service/src/PaymentService.Api/"]
COPY ["services/payment-service/src/PaymentService.Application/PaymentService.Application.csproj", "services/payment-service/src/PaymentService.Application/"]
COPY ["services/payment-service/src/PaymentService.Domain/PaymentService.Domain.csproj", "services/payment-service/src/PaymentService.Domain/"]
COPY ["services/payment-service/src/PaymentService.Infrastructure/PaymentService.Infrastructure.csproj", "services/payment-service/src/PaymentService.Infrastructure/"]
RUN dotnet restore "services/payment-service/src/PaymentService.Api/PaymentService.Api.csproj"
COPY . .
WORKDIR "/src/services/payment-service"
RUN dotnet build --no-restore -c Release

FROM build AS publish
RUN dotnet publish --no-build -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
RUN useradd -m -u 1000 appuser && chown -R appuser:appuser /app
USER appuser

COPY --from=publish /app/publish .
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

HEALTHCHECK --interval=30s --timeout=5s --start-period=10s --retries=3 \
  CMD wget -qO- http://localhost:8080/health/live || exit 1

ENTRYPOINT ["dotnet", "PaymentService.Api.dll"]
