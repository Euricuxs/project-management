# Project Management SaaS - Architecture Documentation

This document provides development guidance for AI coding assistants working on this codebase.

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
  "timestamp": "2026-06-25T10:30:00Z"
}

// Error Response
{
  "success": false,
  "data": null,
  "message": "Validation failed",
  "errors": [
    { "code": "VALIDATION_ERROR", "field": "email", "message": "Invalid email format" }
  ],
  "timestamp": "2026-06-25T10:30:00Z"
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
- **Error Store**: Centralized error state management (`core/store/errorStore.ts`)
- **Toast Notifications**: User-friendly error display

---

## Validation Strategy

### Backend
- **FluentValidation**: Rule-based validation in Application layer
- **Data Annotations**: Simple property validation on DTOs
- **Custom Validators**: Domain-specific validation rules

### Frontend
- **Zod**: Runtime type validation (form schemas, API responses)
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
- **Soft Deletes**: All entities support soft delete with global query filters
- **Auditing**: CreatedAt, UpdatedAt, CreatedBy, UpdatedBy
- **Concurrency**: Row versioning for optimistic concurrency

---

## Activity Tracking / Audit Log

### Overview
Comprehensive activity tracking for all CRUD operations with immutable history.

### Activity Types
```
Task Activities:
- TaskCreated, TaskUpdated, TaskDeleted, TaskMoved
- TaskAssigned, TaskUnassigned

Task Comment Activities:
- TaskCommentCreated, TaskCommentUpdated, TaskCommentDeleted

Label Activities:
- LabelAddedToTask, LabelRemovedFromTask

Project Activities:
- ProjectCreated, ProjectUpdated, ProjectDeleted
- ProjectArchived, ProjectRestored

Board Activities:
- BoardCreated, BoardUpdated, BoardDeleted

Column Activities:
- ColumnCreated, ColumnUpdated, ColumnDeleted

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
- Create/Update/Delete labels per project
- Color-coded labels with custom hex colors
- Assign/Remove labels to/from tasks
- Label picker component for task editing

### Database Schema
- Labels table (per project)
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

## Backend Architecture (Clean Architecture)

```
src/Backend/
├── src/
│   ├── ProjectManagement.Api/           # Presentation Layer
│   │   ├── Controllers/                 # API Controllers
│   │   ├── Middleware/                  # Custom middleware
│   │   ├── Extensions/                  # ServiceCollection extensions
│   │   ├── Filters/                     # Action/Exception filters
│   │   ├── Configuration/               # Settings classes
│   │   └── Program.cs                   # Application entry point
│   │
│   ├── ProjectManagement.Application/   # Application Layer
│   │   ├── Abstractions/                # Repository/Service interfaces
│   │   ├── Common/                      # DTOs, ViewModels
│   │   ├── Exceptions/                  # Application-level exceptions
│   │   └── Services/                    # Business logic services
│   │
│   ├── ProjectManagement.Domain/        # Domain Layer (Core)
│   │   ├── Entities/                    # Domain entities
│   │   ├── Enums/                      # Domain enumerations
│   │   ├── Events/                     # Domain events
│   │   ├── Interfaces/                 # Repository contracts
│   │   └── ValueObjects/               # Value objects
│   │
│   └── ProjectManagement.Infrastructure/ # Infrastructure Layer
│       ├── Data/                       # DbContext, configurations
│       ├── Repositories/               # Repository implementations
│       ├── Migrations/                 # EF Core migrations
│       └── Services/                   # Infrastructure services
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
src/Frontend/src/
├── core/                         # Core infrastructure
│   ├── api/                      # Axios instance, interceptors
│   ├── errors/                   # Error classes, handlers
│   ├── router/                   # Vue Router setup
│   └── store/                    # Pinia stores
│
├── common/                       # Shared utilities
│   ├── constants/               # App constants
│   ├── types/                   # Shared TypeScript types
│   └── utils/                   # Utility functions
│
├── features/                     # Feature modules
│   ├── auth/                    # Authentication
│   ├── dashboard/               # Dashboard
│   ├── workspace/               # Workspace management
│   ├── projects/                # Project management
│   ├── boards/                  # Kanban boards
│   ├── tasks/                   # Task management
│   ├── labels/                  # Label system
│   ├── users/                   # User management (placeholder)
│   └── settings/                # Settings (placeholder)
│
├── layouts/                      # App layouts
└── shared/                       # Reusable components, directives
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

## Domain Entities

| Entity | Description |
|--------|-------------|
| **User** | Application user with email, password hash, name, verification tokens |
| **Workspace** | Top-level organizational unit with owner and members |
| **WorkspaceMember** | Join table between User and Workspace with WorkspaceRole |
| **Project** | Belongs to workspace with name, key, status, dates, color |
| **Board** | Kanban board within a project |
| **Column** | Column on a board with position, optional WIP limit and status mapping |
| **TaskItem** | Work item with title, description, priority, status, assignee |
| **Label** | Colored label scoped to a project |
| **TaskLabel** | Many-to-many join between TaskItem and Label |
| **Activity** | Immutable audit log for tracking changes |
| **RefreshToken** | JWT refresh token with expiration and revocation |

---

## Enumerations

| Enum | Values |
|------|--------|
| **ProjectStatus** | Planning, Active, OnHold, Completed, Archived |
| **TaskStatus** | Todo, InProgress, InReview, Done, Cancelled |
| **TaskPriority** | Low, Medium, High, Critical |
| **WorkspaceRole** | Owner, Admin, Member, Guest |
| **BoardType** | Kanban, List, Timeline |

---

## Pending Features

These features are planned for future development:

1. **Task Comments & Attachments** - Comment threads and file attachments
2. **User Profiles** - Avatar uploads and team management
3. **Notifications** - Real-time and email notifications
4. **Advanced Search** - Full-text search with filter builder
5. **Reporting** - Burndown charts, velocity metrics, time tracking
