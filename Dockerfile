# Build stage
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copy solution and project files
COPY *.sln .
COPY src/SaveState.Core/*.csproj ./src/SaveState.Core/
COPY src/SaveState.Application/*.csproj ./src/SaveState.Application/
COPY src/SaveState.Infrastructure/*.csproj ./src/SaveState.Infrastructure/
COPY src/SaveState.Presentation/*.csproj ./src/SaveState.Presentation/

# Restore dependencies
RUN dotnet restore

# Copy source code
COPY src/ ./src/

# Build application
RUN dotnet build src/SaveState.Presentation/SaveState.Presentation.csproj -c Release --no-restore

# Publish
RUN dotnet publish src/SaveState.Presentation/SaveState.Presentation.csproj -c Release -o /app/publish --no-build

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime
WORKDIR /app

# Install system dependencies for Avalonia (if needed for GUI apps)
RUN apt-get update && apt-get install -y \
    libgtk-3-0 \
    libx11-xcb1 \
    libxrandr2 \
    libasound2 \
    libpangocairo-1.0-0 \
    libatk1.0-0 \
    libcairo-gobject2 \
    libgtk-3-0 \
    libgdk-pixbuf2.0-0 \
    && rm -rf /var/lib/apt/lists/*

# Copy published app
COPY --from=build /app/publish .

# Set environment
ENV ASPNETCORE_ENVIRONMENT=Production
ENV DOTNET_RUNNING_IN_CONTAINER=true

# Create data directory
RUN mkdir -p /app/data

# Health check (if we add web endpoints later)
# HEALTHCHECK --interval=30s --timeout=3s --start-period=5s --retries=3 \
#   CMD curl -f http://localhost:8080/health || exit 1

EXPOSE 8080

# Note: For GUI applications, you may need to run with X11 forwarding or use Xvfb
# For now, this sets up the container for potential web API endpoints
ENTRYPOINT ["dotnet", "SaveState.Presentation.dll"]
