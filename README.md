# Project Management SaaS

<div align="center">

![Vue.js](https://img.shields.io/badge/Vue.js-3.x-4FC08D?logo=vue.js)
![TypeScript](https://img.shields.io/badge/TypeScript-5.x-3178C6?logo=typescript)
![ASP.NET Core](https://img.shields.io/badge/ASP.NET%20Core-10-512BD4?logo=.net)
![Entity Framework](https://img.shields.io/badge/EF%20Core-10.x-512BD4?logo=Microsoft)
![SQLite](https://img.shields.io/badge/SQLite-3.x-003B57?logo=sqlite)
![Docker](https://img.shields.io/badge/Docker-Ready-2496ED?logo=docker)
![License](https://img.shields.io/badge/License-MIT-green)

**A production-ready project management platform with Kanban boards, workspaces, and full audit logging.**

[Features](#key-features) • [Architecture](#architecture) • [Quick Start](#quick-start) • [API](#api-overview) • [Documentation](#documentation)

</div>

---

## Project Overview

A full-stack project management SaaS built with Vue 3 and ASP.NET Core 10, featuring workspaces, projects, Kanban boards, tasks, and comprehensive activity tracking.

**Key Highlights:**
- Clean Architecture on the backend with proper layer separation
- Feature-based modular frontend architecture
- JWT authentication with refresh token rotation
- Soft delete pattern with audit logging
- Drag-and-drop Kanban boards

---

## Key Features

| Feature | Description |
|---------|-------------|
| **Authentication** | JWT-based auth with secure refresh token rotation and multi-device logout |
| **Workspaces** | Organizational units with role-based member management (Owner, Admin, Member, Guest) |
| **Projects** | Full CRUD with archive/restore functionality and workspace-scoped access |
| **Kanban Boards** | Drag-and-drop boards with automatic task key generation (e.g., `PROJ-1`) |
| **Tasks** | Create, edit, move, and reorder with priority, status, and assignee support |
| **Labels** | Color-coded labels per project with task assignment |
| **Activity Tracking** | Immutable audit log capturing all changes with old/new value snapshots |
| **Dashboard** | Real-time statistics: active projects, completed tasks, team size, recent activity |

---

## Screenshots



## Demo Video




## Technology Stack

### Frontend

| Technology | Purpose |
|------------|---------|
| [Vue 3](https://vuejs.org/) | Progressive JavaScript framework with Composition API |
| [TypeScript](https://www.typescriptlang.org/) | Strict type safety throughout the codebase |
| [Vite](https://vitejs.dev/) | Next-generation build tool with HMR |
| [Pinia](https://pinia.vuejs.org/) | State management (Vuex successor) |
| [Vue Router](https://router.vuejs.org/) | Client-side routing with guards |

### Backend

| Technology | Purpose |
|------------|---------|
| [ASP.NET Core 10](https://learn.microsoft.com/en-us/aspnet/core/) | Modern .NET Web API |
| [Entity Framework Core](https://learn.microsoft.com/en-us/ef/core/) | ORM with SQLite provider |
| [FluentValidation](https://fluentvalidation.net/) | Request validation |
| [Serilog](https://serilog.net/) | Structured logging |
| [JWT Bearer](https://learn.microsoft.com/en-us/aspnet/core/security/authentication/jwt-authn/) | Authentication |

### Architecture

| Pattern | Implementation |
|---------|---------------|
| Backend | Clean Architecture (Domain → Application → Infrastructure → API) |
| Frontend | Feature-based module structure |
| Database | SQLite with code-first migrations |
| Containerization | Docker & Docker Compose |

---

## Architecture

### Backend: Clean Architecture

```
┌─────────────────────────────────────────────────────────────┐
│                    ProjectManagement.Api                     │
│              Controllers  │  Middleware  │  Filters         │
└─────────────────────────────┬───────────────────────────────┘
                              │
┌─────────────────────────────▼───────────────────────────────┐
│                ProjectManagement.Application                   │
│           Services  │  DTOs  │  Validators  │  Abstractions   │
└─────────────────────────────┬───────────────────────────────┘
                              │
┌─────────────────────────────▼───────────────────────────────┐
│                  ProjectManagement.Domain                      │
│              Entities  │  Enums  │  Value Objects             │
└─────────────────────────────────────────────────────────────┘
                              ▲
┌─────────────────────────────▼───────────────────────────────┐
│               ProjectManagement.Infrastructure                │
│             DbContext  │  Repositories  │  Services           │
└─────────────────────────────────────────────────────────────┘
```

### Frontend: Feature-Based Architecture

```
src/Frontend/src/
├── core/              # API client, router, store initialization
├── common/            # Shared types, utilities, constants
├── features/          # Feature modules
│   ├── auth/         # Authentication (login, register)
│   ├── dashboard/    # Dashboard view
│   ├── workspace/    # Workspace CRUD
│   ├── projects/     # Project management
│   ├── boards/       # Kanban boards
│   ├── tasks/        # Task management
│   └── labels/       # Label system
└── shared/           # Reusable components, directives
```

---

## Quick Start

### Prerequisites

- **Node.js** 20+
- **.NET 10 SDK**
- **Docker** (optional, for containerized setup)

### Docker (Recommended)

```bash
# Start development environment with hot-reload
docker compose -f docker-compose.dev.yml up

# Services will be available at:
#   Frontend: http://localhost:5173
#   Backend:  http://localhost:5000
```

### Local Development

**Backend:**

```bash
cd src/Backend
dotnet restore
dotnet build
dotnet run --project src/ProjectManagement.Api

# API available at http://localhost:5000
```

**Frontend:**

```bash
cd src/Frontend
npm install
npm run dev

# App available at http://localhost:5173
```

---

## API Overview

### Authentication

| Method | Endpoint | Description |
|--------|----------|-------------|
| POST | `/api/auth/login` | Authenticate with email/password |
| POST | `/api/auth/register` | Create new account |
| POST | `/api/auth/refresh` | Refresh access token |
| POST | `/api/auth/logout` | Revoke refresh token |
| GET | `/api/auth/me` | Get current user |

### Workspaces

| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/api/workspaces` | List user's workspaces |
| POST | `/api/workspaces` | Create workspace |
| PUT | `/api/workspaces/{id}` | Update workspace |
| DELETE | `/api/workspaces/{id}` | Delete workspace (soft) |

### Projects

| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/api/projects/workspace/{id}` | List workspace projects |
| POST | `/api/projects` | Create project |
| PUT | `/api/projects/{id}` | Update project |
| POST | `/api/projects/{id}/archive` | Archive project |
| POST | `/api/projects/{id}/restore` | Restore project |
| DELETE | `/api/projects/{id}` | Delete project |

### Boards

| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/api/boards/project/{id}` | List project boards |
| GET | `/api/boards/{id}` | Get board with columns/tasks |
| POST | `/api/boards` | Create board |
| PUT | `/api/boards/{id}` | Update board |
| DELETE | `/api/boards/{id}` | Delete board |

### Tasks

| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/api/tasks/board/{id}` | List board tasks |
| POST | `/api/tasks` | Create task |
| PUT | `/api/tasks/{id}` | Update task |
| POST | `/api/tasks/{id}/move` | Move to column/position |
| DELETE | `/api/tasks/{id}` | Delete task |

### Labels

| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/api/projects/{id}/labels` | List project labels |
| POST | `/api/projects/{id}/labels` | Create label |
| PUT | `/api/projects/{id}/labels/{lid}` | Update label |
| DELETE | `/api/projects/{id}/labels/{lid}` | Delete label |
| POST | `/api/tasks/{id}/labels` | Assign labels to task |
| DELETE | `/api/tasks/{id}/labels/{lid}` | Remove label |

### Dashboard & Activities

| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/api/dashboard` | Get statistics & overview |
| GET | `/api/activities/project/{id}` | Project activity log |
| GET | `/api/activities/recent` | Recent user activity |

---

## Project Highlights

### Backend Architecture
- **Clean Architecture**: Domain layer has zero external dependencies
- **FluentValidation**: All requests validated before reaching business logic
- **Soft Delete**: All entities support soft delete with EF Core global query filters
- **Concurrency Control**: Row versioning for optimistic concurrency
- **Structured Logging**: Serilog with request correlation IDs

### Authentication & Security
- **JWT with Refresh Tokens**: Secure token-based authentication
- **Refresh Token Rotation**: Automatic invalidation of used tokens
- **Multi-Device Logout**: Revoke all tokens from the current session
- **Inactivity Timeout**: Automatic logout after 30 minutes of inactivity

### Data Management
- **Activity Audit Logging**: Immutable history of all CRUD operations
- **State Snapshots**: Old and new values stored as JSON for change tracking
- **Task Key Generation**: Automatic unique keys (e.g., `SPRINT-42`) with collision handling
- **WIP Limits**: Optional column work-in-progress limits

### Frontend Patterns
- **Feature Modules**: Each feature is self-contained with its own components, services, stores, and types
- **Service Factory Pattern**: API clients created via factory functions for testability
- **Centralized Error Handling**: Global error store with toast notifications
- **Type-Safe API Client**: Axios with typed responses and interceptors

---

## Documentation

For detailed architecture documentation, see [CLAUDE.md](CLAUDE.md).

---

## Future Improvements

- [ ] Task comments and attachment support
- [ ] User profiles with avatar uploads
- [ ] Real-time notifications
- [ ] Full-text search with filter builder
- [ ] Reporting: burndown charts, velocity metrics
- [ ] Time tracking integration

---

## License

MIT License - See [LICENSE](LICENSE) for details.
