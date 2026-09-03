# Build context = repo root (see infrastructure/docker/docker-compose.yml).
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY ["services/ticketing-service/src/TicketingService.Api/TicketingService.Api.csproj", "services/ticketing-service/src/TicketingService.Api/"]
COPY ["services/ticketing-service/src/TicketingService.Application/TicketingService.Application.csproj", "services/ticketing-service/src/TicketingService.Application/"]
COPY ["services/ticketing-service/src/TicketingService.Domain/TicketingService.Domain.csproj", "services/ticketing-service/src/TicketingService.Domain/"]
COPY ["services/ticketing-service/src/TicketingService.Infrastructure/TicketingService.Infrastructure.csproj", "services/ticketing-service/src/TicketingService.Infrastructure/"]
COPY ["shared/contracts/Platform.Contracts.csproj", "shared/contracts/"]
COPY ["shared/shared-kernel/Platform.SharedKernel.csproj", "shared/shared-kernel/"]
RUN dotnet restore "services/ticketing-service/src/TicketingService.Api/TicketingService.Api.csproj"
COPY . .
RUN dotnet publish "services/ticketing-service/src/TicketingService.Api/TicketingService.Api.csproj" --no-restore -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
# libfontconfig1 + libfreetype6 are QuestPDF's only native runtime deps (text shaping).
RUN apt-get update && apt-get install -y --no-install-recommends libfontconfig1 libfreetype6 wget \
    && rm -rf /var/lib/apt/lists/* \
    && addgroup --gid 1654 app && adduser --uid 1654 --gid 1654 --disabled-password --gecos "" app
USER app
COPY --from=build /app/publish .
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080
HEALTHCHECK --interval=30s --timeout=5s --start-period=15s --retries=3 \
    CMD wget -qO- http://localhost:8080/health || exit 1
ENTRYPOINT ["dotnet", "TicketingService.Api.dll"]
