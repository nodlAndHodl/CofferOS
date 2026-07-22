# CofferOS backend (ASP.NET Core, .NET 10) — build context is the repo root.
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy solution-wide build configuration first for better layer caching.
COPY global.json Directory.Build.props Directory.Packages.props ./

# Copy the backend sources (csproj + code).
COPY src/backend ./src/backend

RUN dotnet restore src/backend/CofferOS.Api/CofferOS.Api.csproj
RUN dotnet publish src/backend/CofferOS.Api/CofferOS.Api.csproj \
    -c Release -o /app --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app ./

# Watch-only service. SQLite lives on a mounted volume so data persists locally.
ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production
ENV ConnectionStrings__Default="Data Source=/data/cofferos.db"

EXPOSE 8080
VOLUME ["/data"]

ENTRYPOINT ["dotnet", "CofferOS.Api.dll"]
