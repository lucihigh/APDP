# Multi-stage build for SIMS (.NET 9) targeting Render Docker runtime
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Restore
COPY SIMS.csproj ./
RUN dotnet restore

# Build & publish
COPY . ./
RUN dotnet publish SIMS.csproj -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final
WORKDIR /app
COPY --from=build /app/publish .

# Render passes PORT; fall back to 8080 for local
ENV ASPNETCORE_URLS=http://0.0.0.0:${PORT:-8080}
ENTRYPOINT ["dotnet", "SIMS.dll"]
