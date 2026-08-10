# syntax=docker/dockerfile:1

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY ["src/BusService.Api/BusService.Api.csproj", "src/BusService.Api/"]
COPY ["src/BusService.Application/BusService.Application.csproj", "src/BusService.Application/"]
COPY ["src/BusService.Infrastructure/BusService.Infrastructure.csproj", "src/BusService.Infrastructure/"]
COPY ["src/BusService.Domain/BusService.Domain.csproj", "src/BusService.Domain/"]
RUN dotnet restore "src/BusService.Api/BusService.Api.csproj"

COPY . .
WORKDIR /src/src/BusService.Api
RUN dotnet publish -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app

RUN addgroup --system --gid 1000 appgroup \
    && adduser --system --uid 1000 --ingroup appgroup appuser
USER appuser

COPY --from=build /app/publish .

ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

HEALTHCHECK --interval=30s --timeout=5s --start-period=20s --retries=3 \
    CMD wget --no-verbose --tries=1 --spider http://localhost:8080/health || exit 1

ENTRYPOINT ["dotnet", "BusService.Api.dll"]
