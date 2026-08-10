FROM mcr.microsoft.com/dotnet/aspnet:10.0-alpine AS base
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

FROM mcr.microsoft.com/dotnet/sdk:10.0-alpine AS build
WORKDIR /src
COPY ["services/auth-service/src/AuthService.Api/AuthService.Api.csproj", "services/auth-service/src/AuthService.Api/"]
COPY ["services/auth-service/src/AuthService.Application/AuthService.Application.csproj", "services/auth-service/src/AuthService.Application/"]
COPY ["services/auth-service/src/AuthService.Domain/AuthService.Domain.csproj", "services/auth-service/src/AuthService.Domain/"]
COPY ["services/auth-service/src/AuthService.Infrastructure/AuthService.Infrastructure.csproj", "services/auth-service/src/AuthService.Infrastructure/"]
RUN dotnet restore "services/auth-service/src/AuthService.Api/AuthService.Api.csproj"
COPY . .
WORKDIR "/src/services/auth-service/src/AuthService.Api"
RUN dotnet build "AuthService.Api.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "AuthService.Api.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "AuthService.Api.dll"]
