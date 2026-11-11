# ── Build stage ───────────────────────────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copy solution and restore (layer cache)
COPY SchoolAdmission.sln .
COPY src/SchoolAdmission.Domain/SchoolAdmission.Domain.csproj             src/SchoolAdmission.Domain/
COPY src/SchoolAdmission.Application/SchoolAdmission.Application.csproj   src/SchoolAdmission.Application/
COPY src/SchoolAdmission.Infrastructure/SchoolAdmission.Infrastructure.csproj src/SchoolAdmission.Infrastructure/
COPY src/SchoolAdmission.API/SchoolAdmission.API.csproj                   src/SchoolAdmission.API/
COPY tests/SchoolAdmission.UnitTests/SchoolAdmission.UnitTests.csproj     tests/SchoolAdmission.UnitTests/
COPY tests/SchoolAdmission.IntegrationTests/SchoolAdmission.IntegrationTests.csproj tests/SchoolAdmission.IntegrationTests/

RUN dotnet restore

# Copy everything and build
COPY . .
RUN dotnet publish src/SchoolAdmission.API/SchoolAdmission.API.csproj \
    -c Release \
    -o /app/publish \
    --no-restore

# ── Runtime stage ─────────────────────────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime
WORKDIR /app

# curl is required by the docker-compose healthcheck (the aspnet image does not ship it)
RUN apt-get update \
    && apt-get install -y --no-install-recommends curl \
    && rm -rf /var/lib/apt/lists/*

# Non-root user for security
RUN addgroup --system --gid 1001 appgroup && \
    adduser --system --uid 1001 --ingroup appgroup appuser

COPY --from=build --chown=appuser:appgroup /app/publish .

USER appuser

EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production

ENTRYPOINT ["dotnet", "SchoolAdmission.API.dll"]
