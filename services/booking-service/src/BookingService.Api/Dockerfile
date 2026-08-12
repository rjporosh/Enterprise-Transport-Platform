# syntax=docker/dockerfile:1

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY BookingService.sln .
COPY src/BookingService.Domain/BookingService.Domain.csproj src/BookingService.Domain/
COPY src/BookingService.Application/BookingService.Application.csproj src/BookingService.Application/
COPY src/BookingService.Infrastructure/BookingService.Infrastructure.csproj src/BookingService.Infrastructure/
COPY src/BookingService.Api/BookingService.Api.csproj src/BookingService.Api/
COPY tests/BookingService.UnitTests/BookingService.UnitTests.csproj tests/BookingService.UnitTests/
COPY tests/BookingService.IntegrationTests/BookingService.IntegrationTests.csproj tests/BookingService.IntegrationTests/
RUN dotnet restore BookingService.sln

COPY . .
RUN dotnet publish src/BookingService.Api/BookingService.Api.csproj -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

RUN addgroup --gid 1000 appgroup && adduser --uid 1000 --gid 1000 --disabled-password --gecos "" appuser
USER appuser

COPY --from=build /app/publish .

ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

HEALTHCHECK --interval=30s --timeout=5s --start-period=15s --retries=3 \
    CMD wget -qO- http://localhost:8080/health || exit 1

ENTRYPOINT ["dotnet", "BookingService.Api.dll"]
