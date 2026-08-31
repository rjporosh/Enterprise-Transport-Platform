# syntax=docker/dockerfile:1
#
# Build context MUST be the repository root — the gateway references
# shared/common and shared/shared-kernel, which live outside infrastructure/gateway/.
#   docker build -f infrastructure/gateway/Dockerfile -t platform-gateway .

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Restore against just the csprojs first for layer caching.
COPY infrastructure/gateway/src/Platform.Gateway/Platform.Gateway.csproj infrastructure/gateway/src/Platform.Gateway/
COPY shared/common/Platform.Common.csproj shared/common/
COPY shared/shared-kernel/Platform.SharedKernel.csproj shared/shared-kernel/
RUN dotnet restore infrastructure/gateway/src/Platform.Gateway/Platform.Gateway.csproj

COPY infrastructure/gateway/src/ infrastructure/gateway/src/
COPY shared/ shared/
RUN dotnet publish infrastructure/gateway/src/Platform.Gateway/Platform.Gateway.csproj \
    -c Release -o /app/publish --no-restore /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

# The .NET 10 aspnet image ships a pre-created non-root user "app" (uid 1654).
USER app

COPY --from=build --chown=app:app /app/publish .

ENV ASPNETCORE_URLS=http://+:8080 \
    ASPNETCORE_ENVIRONMENT=Production \
    DOTNET_gcServer=1
EXPOSE 8080

HEALTHCHECK --interval=30s --timeout=5s --start-period=15s --retries=3 \
    CMD wget -qO- http://localhost:8080/health || exit 1

ENTRYPOINT ["dotnet", "Platform.Gateway.dll"]
