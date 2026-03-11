# =============================================================================
# BizFlow API — Multi-stage Dockerfile (.NET 8)
# Context: ./bizflow-platform (the solution folder)
# =============================================================================

# ---- Stage 1: Build ----
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy csproj files first to leverage Docker layer caching
# (dependencies are only re-downloaded when .csproj files change)
COPY ["BizFlow.Api/BizFlow.Api.csproj",                   "BizFlow.Api/"]
COPY ["BizFlow.Application/BizFlow.Application.csproj",   "BizFlow.Application/"]
COPY ["BizFlow.Domain/BizFlow.Domain.csproj",             "BizFlow.Domain/"]
COPY ["BizFlow.Infrastructure/BizFlow.Infrastructure.csproj", "BizFlow.Infrastructure/"]

RUN dotnet restore "BizFlow.Api/BizFlow.Api.csproj"

# Copy all source code
COPY . .

# Publish release build
WORKDIR "/src/BizFlow.Api"
RUN dotnet publish "BizFlow.Api.csproj" \
    -c Release \
    -o /app/publish \
    /p:UseAppHost=false

# ---- Stage 2: Runtime ----
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app

# Security: run as non-root user
RUN adduser --disabled-password --gecos "" appuser

COPY --from=build /app/publish .

USER appuser

EXPOSE 8080

ENTRYPOINT ["dotnet", "BizFlow.Api.dll"]
