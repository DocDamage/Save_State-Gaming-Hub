# Docker Setup Guide

**Status**: 📋 Planned (V2.0 Deployment Option)
**Last Updated**: January 2, 2026
**Layer**: DevOps / Deployment
**Related**: [ENGINEERING_RULES](../ENGINEERING_RULES.md)

---

> **Note**: Docker deployment is a planned feature for production deployments. The current SaveState Reborn application runs as a native desktop application using Avalonia UI. This guide is for future web API or headless deployments.

This guide explains how to run SaveStateReborn using Docker and Docker Compose.

## Prerequisites

- Docker Engine 20.10+
- Docker Compose 2.0+

## Quick Start

### Development

```bash
# Start development environment with hot reload
docker-compose -f docker-compose.dev.yml up --build

# Or use the default development override
docker-compose up --build
```

### Production

```bash
# Start production environment
docker-compose -f docker-compose.prod.yml up --build -d

# Include monitoring stack
docker-compose -f docker-compose.prod.yml --profile monitoring up --build -d

# Include reverse proxy
docker-compose -f docker-compose.prod.yml --profile nginx up --build -d
```

### CI/CD

```bash
# Run test suite in containers
docker-compose -f docker-compose.ci.yml up --build

# Run security scanning
docker-compose -f docker-compose.ci.yml --profile security up --build
```

## Configuration

### Environment Variables

| Variable | Description | Default |
|----------|-------------|---------|
| `ASPNETCORE_ENVIRONMENT` | Environment (Development/Production) | Production |
| `ASPNETCORE_URLS` | URLs to listen on | http://+:8080 |
| `ConnectionStrings__DefaultConnection` | Database connection string | SQLite in container |
| `DOTNET_RUNNING_IN_CONTAINER` | Container detection flag | true |

### Volumes

- `savestate-data`: Persistent data storage for SQLite database and application data

## Development Workflow

### Hot Reload

The development setup mounts source code and uses `dotnet watch` for automatic recompilation:

```bash
docker-compose up --build
# Application will restart automatically when code changes
```

### Debugging

Attach debugger to port 9229 (configured in development compose file):

```bash
# In VS Code, add this to launch.json
{
    "name": "Docker Debug",
    "type": "coreclr",
    "request": "attach",
    "processId": "${command:pickProcess}",
    "pipeTransport": {
        "pipeProgram": "docker",
        "pipeArgs": ["exec", "-i", "savestate-dev"],
        "debuggerPath": "/vsdbg/vsdbg",
        "pipeCwd": "${workspaceRoot}"
    }
}
```

## Production Deployment

### Basic Production Setup

```bash
# Build and start
docker-compose -f docker-compose.prod.yml up --build -d

# View logs
docker-compose -f docker-compose.prod.yml logs -f savestate

# Check health
curl http://localhost/health
```

### With Reverse Proxy

```bash
# Start with Nginx reverse proxy
docker-compose -f docker-compose.prod.yml --profile nginx up --build -d

# Application available at http://localhost
```

### With Monitoring

```bash
# Start with Prometheus monitoring
docker-compose -f docker-compose.prod.yml --profile monitoring up --build -d

# Prometheus UI at http://localhost:9090
```

## Database Management

### SQLite (Default)

Data is stored in the `savestate-data` volume as SQLite database.

### PostgreSQL (Alternative)

For production workloads, use PostgreSQL:

```yaml
# Add to docker-compose.prod.yml
services:
  postgres:
    image: postgres:15-alpine
    environment:
      POSTGRES_DB: savestate
      POSTGRES_USER: savestate
      POSTGRES_PASSWORD: ${POSTGRES_PASSWORD}
    volumes:
      - postgres-data:/var/lib/postgresql/data

  savestate:
    depends_on:
      - postgres
    environment:
      ConnectionStrings__DefaultConnection: Host=postgres;Database=savestate;Username=savestate;Password=${POSTGRES_PASSWORD}
```

## Troubleshooting

### Common Issues

1. **Port already in use**

   ```bash
   # Find process using port
   lsof -i :8080
   # Change port in docker-compose.yml
   ```

2. **Permission denied on Linux**

   ```bash
   # Ensure user has Docker permissions
   sudo usermod -aG docker $USER
   ```

3. **Database file locked**

   ```bash
   # Reset database volume
   docker-compose down -v
   docker-compose up --build
   ```

4. **GUI applications in containers**

   ```bash
   # For Avalonia GUI apps, enable X11 forwarding
   xhost +local:docker
   docker run --rm -e DISPLAY=$DISPLAY -v /tmp/.X11-unix:/tmp/.X11-unix app-image
   ```

### Logs

```bash
# View all logs
docker-compose logs -f

# View specific service logs
docker-compose logs -f savestate

# View last 100 lines
docker-compose logs --tail=100 savestate
```

### Cleanup

```bash
# Stop and remove containers
docker-compose down

# Remove volumes (WARNING: deletes data)
docker-compose down -v

# Remove images
docker-compose down --rmi all
```

## Security Considerations

### Production Checklist

- [ ] Change default ports if needed
- [ ] Use environment variables for secrets
- [ ] Enable HTTPS with SSL certificates
- [ ] Configure proper firewall rules
- [ ] Regular security updates of base images
- [ ] Implement proper logging and monitoring

### SSL/TLS Setup

```yaml
# Add to docker-compose.prod.yml nginx service
volumes:
  - ./ssl:/etc/ssl/certs:ro
environment:
  - CERT_PATH=/etc/ssl/certs/cert.pem
  - KEY_PATH=/etc/ssl/certs/key.pem
```

## Performance Tuning

### Resource Limits

```yaml
services:
  savestate:
    deploy:
      resources:
        limits:
          memory: 1G
          cpus: '1.0'
        reservations:
          memory: 512M
          cpus: '0.5'
```

### Database Optimization

For high-load scenarios:

```yaml
services:
  savestate:
    environment:
      - ConnectionStrings__DefaultConnection=Host=postgres;Pooling=true;MinPoolSize=5;MaxPoolSize=100
```

## Contributing

When adding new features:

1. Update Docker configurations if needed
2. Test with `docker-compose.ci.yml`
3. Update this documentation
4. Ensure all environments work (dev, prod, ci)

---

**Related Documentation**:

- [Engineering Rules](../ENGINEERING_RULES.md) - Code standards
- [Threat Model](../architecture/THREAT_MODEL.md) - Security considerations
