# Project Management SaaS

A production-ready Project Management SaaS built with Vue 3, TypeScript, and ASP.NET Core 10.

## Tech Stack

### Frontend
- Vue 3 with Composition API
- TypeScript (strict mode)
- Vite
- Pinia (state management)
- Vue Router

### Backend
- ASP.NET Core 10 Web API
- Entity Framework Core
- SQLite
- Clean Architecture

## Project Structure

```
├── src/
│   ├── Backend/          # .NET Backend (Clean Architecture)
│   │   ├── src/
│   │   │   ├── ProjectManagement.Api/           # Presentation Layer
│   │   │   ├── ProjectManagement.Application/   # Application Layer
│   │   │   ├── ProjectManagement.Domain/        # Domain Layer
│   │   │   └── ProjectManagement.Infrastructure/ # Infrastructure Layer
│   │   └── tests/
│   │       └── ProjectManagement.UnitTests/
│   │
│   └── Frontend/         # Vue Frontend (Feature-Based)
│       ├── src/
│       │   ├── assets/
│       │   ├── common/
│       │   ├── core/
│       │   ├── features/
│       │   ├── layouts/
│       │   └── shared/
│       └── tests/
│
├── CLAUDE.md             # Architecture documentation
└── README.md
```

## Getting Started

### Prerequisites
- Node.js 20+
- .NET 10 SDK
- SQLite

### Backend Setup

```bash
cd src/Backend
dotnet restore
dotnet build
dotnet run --project src/ProjectManagement.Api
```

The API will be available at `http://localhost:5000`.

### Frontend Setup

```bash
cd src/Frontend
npm install
npm run dev
```

The frontend will be available at `http://localhost:5173`.

## Architecture

### Backend (Clean Architecture)

- **Domain Layer**: Entities, Value Objects, Domain Events (no external dependencies)
- **Application Layer**: Use Cases, Services, Validators, DTOs
- **Infrastructure Layer**: Database access, External services
- **API Layer**: Controllers, Middleware, Filters

### Frontend (Feature-Based)

Each feature module contains:
- `components/` - Feature-specific UI components
- `services/` - API service calls
- `stores/` - Pinia state management
- `types/` - Feature-specific TypeScript types
- `views/` - Route views/pages

## Features (Not Yet Implemented)

- [ ] User Authentication (JWT)
- [ ] Project Management (CRUD)
- [ ] Task Management (CRUD, assignments)
- [ ] User Management (teams, roles)
- [ ] Dashboard with analytics

## Development

### Running Tests

```bash
# Backend
cd src/Backend
dotnet test

# Frontend
cd src/Frontend
npm run test
```

### Building for Production

```bash
# Backend
dotnet publish -c Release

# Frontend
npm run build
```

## Docker Deployment

Quickly deploy the entire stack using Docker and Docker Compose.

### Prerequisites
- Docker Engine 24.0+
- Docker Compose v2.20+

### Development Mode (Recommended)

Use `docker-compose.dev.yml` for development with hot-reload:

```bash
# Start dev environment with hot reload
docker compose -f docker-compose.dev.yml up

# Frontend available at: http://localhost:5173
# Backend API at: http://localhost:5000
```

Changes to frontend code will auto-reload.

### Production Mode

```bash
# Build and start all services
docker compose up -d

# View logs
docker compose logs -f

# Stop services
docker compose down
```

The application will be available at:
- **Frontend**: http://localhost:8080
- **Backend API**: http://localhost:5000

### Production Deployment

1. **Generate a secure JWT secret**:
   ```bash
   openssl rand -base64 32
   ```

2. **Configure environment variables**:
   ```bash
   # Copy the example file
   cp config/backend.env.example config/backend.env

   # Edit and set your JWT_SECRET
   nano config/backend.env
   ```

3. **Deploy with production configuration**:
   ```bash
   docker-compose -f docker-compose.prod.yml up -d
   ```

### Environment Variables

| Variable | Description | Default |
|----------|-------------|---------|
| `JWT_SECRET` | JWT signing key (min 32 chars) | - |
| `API_URL` | Backend URL for frontend | http://backend:5000 |
| `ASPNETCORE_ENVIRONMENT` | .NET environment | Production |

### Docker Commands Reference

```bash
# Build images without starting
docker-compose build

# Rebuild specific service
docker-compose build backend
docker-compose build frontend

# View service status
docker-compose ps

# View logs for specific service
docker-compose logs backend
docker-compose logs -f frontend

# Restart services
docker-compose restart

# Remove containers and volumes (clears database)
docker-compose down -v

# Execute command in container
docker-compose exec backend dotnet --version
docker-compose exec frontend sh
```

### Production Checklist

- [ ] Set `JWT_SECRET` to a secure random value
- [ ] Configure CORS origins in `backend.env`
- [ ] Enable HTTPS with a reverse proxy (nginx, Traefik, Caddy)
- [ ] Set up database backups
- [ ] Configure log rotation

### Architecture

```
┌─────────────────────────────────────────────────────────┐
│                      Docker Engine                        │
├─────────────────────────────────────────────────────────┤
│                                                          │
│  ┌─────────────┐    ┌─────────────┐                     │
│  │   backend   │    │  frontend   │                     │
│  │   :5000     │    │    :80      │                     │
│  └──────┬──────┘    └──────┬──────┘                     │
│         │                   │                            │
│         └─────────┬─────────┘                            │
│                   │                                      │
│         ┌─────────▼─────────┐                            │
│         │   pm-network     │                            │
│         │   (bridge)       │                            │
│         └───────────────────┘                            │
│                   │                                      │
│         ┌─────────▼─────────┐                            │
│         │ backend-data      │                            │
│         │ (named volume)    │                            │
│         └───────────────────┘                            │
│                                                          │
└─────────────────────────────────────────────────────────┘
```

## License

MIT
