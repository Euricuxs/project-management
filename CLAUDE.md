# Project Management SaaS - Architecture Documentation

## Overview

A production-ready Project Management SaaS built with Vue 3, TypeScript, and ASP.NET Core 10.
Features include workspaces, projects, Kanban boards, tasks, labels, and activity tracking.

## Tech Stack

### Frontend
- **Vue 3** - Progressive JavaScript framework with Composition API
- **TypeScript** - Strict type safety throughout
- **Vite** - Next-generation build tool
- **Pinia** - State management (Vuex successor)
- **Vue Router** - Client-side routing

### Backend
- **ASP.NET Core 10 Web API** - Modern .NET backend
- **Entity Framework Core** - ORM with SQLite provider
- **Clean Architecture** - Domain-driven design principles

---

## Backend Architecture (Clean Architecture)

```
src/Backend/
├── src/
│   └── ProjectManagement.Api/           # Presentation Layer
│       ├── Controllers/                 # API Controllers
│       ├── Middleware/                  # Custom middleware
│       ├── Extensions/                  # ServiceCollection extensions
│       ├── Filters/                     # Action/Exception filters
│       ├── Configuration/               # Settings classes
│       └── Program.cs                   # Application entry point
│
│   └── ProjectManagement.Application/   # Application Layer
│       ├── Abstractions/
│       │   ├── Repositories/           # Repository interfaces
│       │   ├── Services/               # Service interfaces
│       │   └── Validators/             # Validator interfaces
│       ├── Common/
│       │   ├── Interfaces/            # Cross-cutting interfaces
│       │   └── Models/                # Shared DTOs/ViewModels
│       └── Exceptions/                 # Application-level exceptions
│
│   └── ProjectManagement.Domain/        # Domain Layer (Core)
│       ├── Entities/                    # Domain entities
│       │   ├── User, Workspace, WorkspaceMember
│       │   ├── Project, Board, Column
│       │   ├── TaskItem, Label, TaskLabel
│       │   └── Activity (audit log)
│       ├── Enums/                      # Domain enumerations
│       ├── Events/                     # Domain events
│       ├── Interfaces/                 # Repository contracts
│       └── ValueObjects/               # Value objects
│
│   └── ProjectManagement.Infrastructure/ # Infrastructure Layer
│       ├── Data/                       # DbContext, configurations
│       ├── Repositories/               # Repository implementations
│       ├── Migrations/                 # EF Core migrations
│       └── Services/                    # Infrastructure services
│
└── tests/
    └── ProjectManagement.UnitTests/    # Unit tests
```

### Layer Dependencies

```
┌─────────────────────────────────────────────────────────┐
│                    ProjectManagement.Api                 │
│              (Controllers, Middleware, DI)              │
└────────────────────────────┬────────────────────────────┘
                             │ depends on
                             ▼
┌─────────────────────────────────────────────────────────┐
│               ProjectManagement.Application              │
│           (Use Cases, Services, Validators)             │
└────────────────────────────┬────────────────────────────┘
                             │ depends on
                             ▼
┌─────────────────────────────────────────────────────────┐
│                 ProjectManagement.Domain                 │
│            (Entities, Value Objects, Events)            │
└─────────────────────────────────────────────────────────┘
                             ▲
                             │ referenced by
┌─────────────────────────────────────────────────────────┐
│              ProjectManagement.Infrastructure            │
│         (Data Access, External Services)                │
└─────────────────────────────────────────────────────────┘
```

### Key Principles

1. **Domain Layer Independence**: Contains zero external dependencies
2. **Application Layer**: Contains business logic, depends only on Domain
3. **Infrastructure Layer**: Implements interfaces defined in Application
4. **API Layer**: Orchestrates HTTP requests, depends on Application

---

## Frontend Architecture (Feature-Based)

```
src/Frontend/
├── src/
│   ├── assets/                        # Static assets
│   │   ├── styles/                   # Global styles
│   │   ├── images/                   # Images
│   │   └── fonts/                    # Custom fonts
│   │
│   ├── common/                       # Shared utilities
│   │   ├── constants/               # App constants
│   │   ├── enums/                   # TypeScript enums
│   │   ├── utils/                   # Utility functions
│   │   ├── types/                   # Shared TypeScript types
│   │   └── hooks/                   # Shared composables
│   │
│   ├── core/                         # Core infrastructure
│   │   ├── api/                     # Axios instance, interceptors
│   │   ├── errors/                  # Error classes, handlers
│   │   ├── router/                  # Vue Router setup
│   │   └── store/                   # Root store, types
│   │
│   ├── features/                     # Feature modules
│   │   ├── auth/
│   │   │   ├── components/
│   │   │   ├── services/
│   │   │   ├── stores/
│   │   │   ├── types/
│   │   │   └── views/
│   │   ├── dashboard/
│   │   ├── workspace/
│   │   ├── projects/
│   │   ├── boards/                   # Kanban board with drag-drop
│   │   ├── tasks/
│   │   ├── labels/                  # Color-coded labels
│   │   ├── users/
│   │   └── settings/
│   │
│   ├── layouts/                     # App layouts
│   │
│   └── shared/                      # Cross-feature components
│       ├── components/             # Reusable UI components
│       ├── composables/            # Shared composition functions
│       ├── directives/             # Custom directives
│       └── plugins/                # Vue plugins
│
├── public/                          # Public static files
└── tests/                           # Test files
    ├── unit/
    ├── integration/
    └── e2e/
```

### Feature Module Structure

Each feature follows a consistent pattern:

```
features/<feature>/
├── components/          # Feature-specific UI components
├── services/            # API service calls (typed)
├── stores/              # Pinia store(s) for state
├── types/               # Feature-specific TypeScript types
└── views/               # Route views/pages
```

---

## API Response Structure

All API responses follow a consistent envelope:

```typescript
// Success Response
{
  "success": true,
  "data": { ... },
  "message": "Operation completed successfully",
  "errors": null,
  "timestamp": "2026-06-13T10:30:00Z"
}

// Error Response
{
  "success": false,
  "data": null,
  "message": "Validation failed",
  "errors": [
    { "code": "VALIDATION_ERROR", "field": "email", "message": "Invalid email format" }
  ],
  "timestamp": "2026-06-13T10:30:00Z"
}
```

---

## Error Handling Strategy

### Backend
- **Global Exception Filter**: Catches all unhandled exceptions
- **Validation Exception Filter**: Handles FluentValidation failures
- **Domain Exceptions**: Custom exceptions for business rule violations
- **Problem Details**: RFC 7807 compliant error responses

### Frontend
- **Axios Interceptors**: Global request/response handling
- **Error Store**: Centralized error state management
- **Toast Notifications**: User-friendly error display

---

## Validation Strategy

### Backend
- **FluentValidation**: Rule-based validation in Application layer
- **Data Annotations**: Simple property validation on DTOs
- **Custom Validators**: Domain-specific validation rules

### Frontend
- **Zod**: Runtime type validation
- **VeeValidate**: Form validation with Yup/Zod schemas
- **HTML5 Validation**: Native browser validation

---

## Logging Strategy

### Backend
- **Serilog**: Structured logging with sinks
- **Correlation IDs**: Request tracing across services
- **Log Levels**: Debug, Information, Warning, Error, Critical
- **Sensitive Data**: Automatic masking of PII

### Frontend
- **Pinia Plugin**: State change logging (dev only)
- **Axios Interceptors**: Request/response logging
- **Error Boundary**: Component-level error logging

---

## Dependency Injection

### Backend Service Registration

```csharp
// Extension method pattern for clean DI registration
services.AddApplicationServices();
services.AddInfrastructureServices(configuration);
services.AddApiServices();
```

### Frontend Service Registration

```typescript
// Factory pattern for services
const apiClient = createApiClient(baseUrl, authStore);
const projectService = createProjectService(apiClient);
```

---

## Repository Pattern Usage

Repository pattern is used **only where justified**:
- Complex query logic that benefits from abstraction
- Multiple data sources (future-proofing)
- Unit testing requiring data mocking

**Not used for:**
- Simple CRUD operations (direct DbContext is fine)
- When the overhead outweighs the benefit

---

## Security Considerations

- **JWT Bearer authentication**: Implemented with access/refresh tokens
- **Password hashing**: BCrypt with salt
- **Token validation**: Signature and expiration verification
- **CORS configuration**: Configurable allowed origins
- **Input sanitization**: All inputs validated
- **SQL injection prevention**: EF Core parameterized queries
- **XSS prevention**: Vue's built-in escaping
- **Rate limiting**: Configurable per-endpoint

---

## Database Strategy

- **SQLite**: Development/lightweight production
- **EF Core Migrations**: Code-first database versioning
- **Soft Deletes**: All entities support soft delete
- **Auditing**: CreatedAt, UpdatedAt, CreatedBy, UpdatedBy

---

## Activity Tracking / Audit Log

### Overview
Comprehensive activity tracking for all CRUD operations with immutable history.

### Activity Types
```
Task Activities:
- TaskCreated, TaskUpdated, TaskDeleted, TaskMoved
- TaskAssigned, TaskUnassigned

Project Activities:
- ProjectCreated, ProjectUpdated, ProjectDeleted
- ProjectArchived, ProjectRestored

Board Activities:
- BoardCreated, BoardUpdated, BoardDeleted

Workspace Activities:
- WorkspaceCreated, WorkspaceUpdated, WorkspaceDeleted
- MemberAdded, MemberRemoved, MemberRoleChanged
```

### Features
- **Immutable History**: Activities cannot be modified or deleted
- **State Snapshots**: OldValues/NewValues stored as JSON
- **Efficient Storage**: Indexed by ProjectId, EntityType, UserId, CreatedAt
- **Bulk Operations**: Batch logging with individual fallback
- **Graceful Degradation**: Activity failures don't break main operations

### API Endpoints
- `GET /api/activities/project/{projectId}` - Paginated project activities
- `GET /api/activities/entity/{entityType}/{entityId}` - Entity history
- `GET /api/activities/recent` - Recent activities for current user

### Database Schema
```sql
Activities (
  Id, Type, UserId, UserName,
  EntityType, EntityId, EntityName,
  ProjectId, OldValues, NewValues,
  Description, IpAddress, UserAgent,
  CreatedAt
)
```

### Indexes
- (ProjectId, CreatedAt)
- (EntityType, EntityId, CreatedAt)
- UserId, Type, CreatedAt

---

## Labels System

### Features
- Create/Update/Delete labels per workspace
- Color-coded labels with custom hex colors
- Assign/Remove labels to/from tasks
- Label picker component for task editing

### Database Schema
- Labels table (per workspace)
- TaskLabels junction table (many-to-many)

---

## TypeScript Configuration

```json
{
  "compilerOptions": {
    "strict": true,
    "noImplicitAny": true,
    "strictNullChecks": true,
    "strictFunctionTypes": true,
    "noUnusedLocals": true,
    "noUnusedParameters": true,
    "noImplicitReturns": true,
    "noFallthroughCasesInSwitch": true,
    "forceConsistentCasingInFileNames": true
  }
}
```

---

## Build & Development

### Docker (Recommended for Development)

```bash
# Development mode with hot-reload
docker compose -f docker-compose.dev.yml up

# Frontend: http://localhost:5173
# Backend:  http://localhost:5000
```

### Native Development

#### Backend
```bash
cd src/Backend
dotnet restore
dotnet build
dotnet run --project src/ProjectManagement.Api
```

#### Frontend
```bash
cd src/Frontend
npm install
npm run dev      # Development with hot-reload
npm run build    # Production build
```

**Note:** Changes to Vue/TS files in native mode will hot-reload automatically. For Docker dev mode, source files are mounted as volumes.

---

## Implemented Features

### Backend APIs
- **Authentication**: Login, Register, Refresh Token, Logout
- **Workspaces**: CRUD with member management
- **Projects**: CRUD with archival/restore
- **Boards**: CRUD with Kanban columns
- **Tasks**: CRUD with move/reorder functionality
- **Labels**: CRUD with task assignment
- **Activities**: Audit log for all operations
- **Dashboard**: Statistics and overview widgets

### Frontend Features
- **Auth**: Login/Register forms with validation
- **Dashboard**: Overview with stats and quick actions
- **Workspace**: Workspace switcher and management
- **Projects**: Project listing with filtering
- **Kanban Board**: Drag-drop columns and tasks
- **Labels**: Color-coded label system
- **Activity Feed**: Recent activity display

---

## Pending Features

1. **Task Comments & Attachments**
   - Comment threads on tasks
   - File attachments

2. **User Management**
   - User profiles
   - Avatar uploads
   - Team management

3. **Notifications**
   - Real-time notifications
   - Email notifications

4. **Advanced Search**
   - Full-text search
   - Filter builder

5. **Reporting**
   - Burndown charts
   - Velocity metrics
   - Time tracking
