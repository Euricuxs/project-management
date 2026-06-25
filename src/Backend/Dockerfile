# Build stage
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS builder

WORKDIR /src

# Disable fallback packages to avoid Windows path issues
ENV NUGET_FALLBACK_PACKAGES=""
ENV DOTNET_CLI_TELEMETRY_OPTOUT=1

# Copy NuGet config to override any Windows settings
COPY nuget.config ./

# Copy solution and project files for better layer caching
COPY ProjectManagement.sln ./
COPY src/ProjectManagement.Domain/ProjectManagement.Domain.csproj src/ProjectManagement.Domain/
COPY src/ProjectManagement.Application/ProjectManagement.Application.csproj src/ProjectManagement.Application/
COPY src/ProjectManagement.Infrastructure/ProjectManagement.Infrastructure.csproj src/ProjectManagement.Infrastructure/
COPY src/ProjectManagement.Api/ProjectManagement.Api.csproj src/ProjectManagement.Api/

# Restore dependencies
RUN dotnet restore src/ProjectManagement.Api/ProjectManagement.Api.csproj

# Copy source code
COPY src/ src/

# Build and publish
WORKDIR /src/src/ProjectManagement.Api
RUN dotnet publish -c Release -o /app/publish

# Production stage - minimal runtime
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS production

# Install curl for health check
RUN apt-get update && apt-get install -y curl && rm -rf /var/lib/apt/lists/*

# Create non-root user for security
RUN useradd -m appuser

WORKDIR /app

# Copy published output
COPY --from=builder /app/publish .

# Set ownership
RUN chown -R appuser:appuser /app

# Switch to non-root user
USER appuser

# Expose port
EXPOSE 5000

# Set environment variables
ENV ASPNETCORE_URLS=http://+:5000
ENV ASPNETCORE_ENVIRONMENT=Production

# Health check
HEALTHCHECK --interval=30s --timeout=3s --start-period=5s --retries=3 \
    CMD curl -f http://localhost:5000/health || exit 1

# Entry point
ENTRYPOINT ["dotnet", "ProjectManagement.Api.dll"]
