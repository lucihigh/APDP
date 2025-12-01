# Multi-stage build for SIMS (.NET 9) with repo root as build context
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Restore using only the project file for better caching
COPY SIMS/SIMS.csproj SIMS/
RUN dotnet restore SIMS/SIMS.csproj

# Copy the remaining source
COPY SIMS/. SIMS/
WORKDIR /src/SIMS

# Publish
RUN dotnet publish -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final
WORKDIR /app
COPY --from=build /app/publish .

# Render injects PORT; default to 8080 locally
ENV ASPNETCORE_URLS=http://0.0.0.0:${PORT:-8080}
ENTRYPOINT ["dotnet", "SIMS.dll"]
