# Audit: Task 2.2 & 2.3 vs codebase

**Date:** 2025-03-17  
**Scope:** [task-2.2-ef-core-configuration.md](task-2.2-ef-core-configuration.md), [task-2.3-command-handlers.md](task-2.3-command-handlers.md)

## Task 2.2 — EF Core / PostgreSQL

| Criterion | Result | Evidence |
|-----------|--------|----------|
| `ContentDbContext` with domain event dispatch in `SaveChangesAsync` | **Pass** | Events collected after audit fields. Explicit transaction: aggregates flushed first (`base.SaveChangesAsync`), then handlers dispatched, then outbox entries flushed, then commit — so PG constraints are validated before handlers run. |
| Fluent API for `WorkExperience`, no data annotations on domain | **Pass** | [WorkExperienceConfiguration.cs](../../../src/Services/JJDevHub.Content/JJDevHub.Content.Persistence/Configurations/WorkExperienceConfiguration.cs) |
| `DateRange` as owned type | **Pass** | `OwnsOne` on `Period` → `start_date` / `end_date` |
| Global query filter (soft delete) | **Pass** | `AuditableEntity.IsActive` filter in `OnModelCreating` |
| Audit fields automatic | **Pass** | `ApplyAuditInfo` + `ApplyPersistenceOnCreate/Modify` |
| `WorkExperienceRepository` + `IWorkExperienceRepository` | **Pass** | [WorkExperienceRepository.cs](../../../src/Services/JJDevHub.Content/JJDevHub.Content.Persistence/Repositories/WorkExperienceRepository.cs), soft delete via `MarkAsDeleted()` |
| DI: Npgsql + `ContentDbContext` | **Pass** | [DependencyInjection.cs](../../../src/Services/JJDevHub.Content/JJDevHub.Content.Persistence/DependencyInjection.cs) |
| `IUnitOfWork` → `ContentDbContext` | **Pass** | `AddScoped<IUnitOfWork>(sp => sp.GetRequiredService<ContentDbContext>())` |

### Fixes applied during audit

1. **Domain event order:** Switched to explicit transaction: flush aggregates → dispatch handlers (outbox + Mongo) → flush outbox → commit. Aggregates validated before handlers run; outbox committed atomically with aggregates.
2. **`IUnitOfWork` registration:** Explicit scoped registration added (handlers already used `repository.UnitOfWork`).

---

## Task 2.3 — Commands / MediatR / validation / read sync

| Criterion | Result | Evidence |
|-----------|--------|----------|
| Add / Update / Delete command handlers | **Pass** | `Commands/*/` |
| FluentValidation + pipeline | **Pass** | `ValidationBehavior`, `AddValidatorsFromAssembly` |
| `AddWorkExperienceCommandValidator` rules | **Pass** | CompanyName, Position, dates per task |
| Update validator | **Pass** (extra) | [UpdateWorkExperienceCommandValidator.cs](../../../src/Services/JJDevHub.Content/JJDevHub.Content.Application/Commands/UpdateWorkExperience/UpdateWorkExperienceCommandValidator.cs) |
| Query handlers → MongoDB | **Pass** | `GetWorkExperiencesQuery`, `GetWorkExperienceByIdQuery` + `IWorkExperienceReadStore` |
| Domain event handlers → read store + Kafka | **Pass** | `WorkExperience*DomainEventHandler` |
| Integration events | **Pass** | Created / Updated / Deleted |
| DI: MediatR, validators, behaviors | **Pass** | [Application/DependencyInjection.cs](../../../src/Services/JJDevHub.Content/JJDevHub.Content.Application/DependencyInjection.cs) |

### Fixes applied during audit

1. **Delete command:** Handler calls `experience.MarkAsDeleted()` (raises `WorkExperienceDeletedDomainEvent` + soft-delete) then `_repository.Delete(experience)` (removes from EF tracking).

### Residual notes

- **Mongo + Kafka vs PG:** With pre-commit dispatch, failure in handlers prevents PG commit; if PG commit fails *after* successful Mongo/Kafka, read model can diverge until reconciled — see [transactional-outbox-kafka.md](../transactional-outbox-kafka.md).
- **Task 2.3 doc** lists only `AddWorkExperienceCommandValidator`; codebase also validates Update (acceptable extension).
