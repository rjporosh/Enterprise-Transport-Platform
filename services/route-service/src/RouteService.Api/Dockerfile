FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY ["services/route-service/src/RouteService.Api/RouteService.Api.csproj", "services/route-service/src/RouteService.Api/"]
COPY ["services/route-service/src/RouteService.Application/RouteService.Application.csproj", "services/route-service/src/RouteService.Application/"]
COPY ["services/route-service/src/RouteService.Domain/RouteService.Domain.csproj", "services/route-service/src/RouteService.Domain/"]
COPY ["services/route-service/src/RouteService.Infrastructure/RouteService.Infrastructure.csproj", "services/route-service/src/RouteService.Infrastructure/"]
RUN dotnet restore "services/route-service/src/RouteService.Api/RouteService.Api.csproj"
COPY . .
WORKDIR "/src/services/route-service/src/RouteService.Api"
RUN dotnet build "RouteService.Api.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "RouteService.Api.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "RouteService.Api.dll"]
