using System.Linq.Expressions;
using JJDevHub.Content.Application.Abstractions;
using JJDevHub.Content.Core.Entities;
using JJDevHub.Shared.Kernel.BuildingBlocks;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace JJDevHub.Content.Persistence;

public class ContentDbContext : DbContext, IUnitOfWork
{
    private readonly IMediator _mediator;
    private readonly ICurrentUser _currentUser;

    public DbSet<WorkExperience> WorkExperiences => Set<WorkExperience>();

    public ContentDbContext(
        DbContextOptions<ContentDbContext> options,
        IMediator mediator,
        ICurrentUser currentUser)
        : base(options)
    {
        _mediator = mediator;
        _currentUser = currentUser;
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("content");
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ContentDbContext).Assembly);

        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            if (!typeof(AuditableEntity).IsAssignableFrom(entityType.ClrType))
                continue;

            modelBuilder.Entity(entityType.ClrType)
                .HasQueryFilter(BuildIsActiveFilter(entityType.ClrType));
        }
    }

    private static LambdaExpression BuildIsActiveFilter(Type entityType)
    {
        var parameter = Expression.Parameter(entityType, "e");
        var property = Expression.Property(parameter, nameof(AuditableEntity.IsActive));
        var body = Expression.Equal(property, Expression.Constant(true));
        return Expression.Lambda(body, parameter);
    }

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        ApplyAuditInfo();
        var domainEvents = CollectDomainEvents();

        var result = await base.SaveChangesAsync(cancellationToken);

        await DispatchDomainEventsAsync(domainEvents, cancellationToken);

        return result;
    }

    private void ApplyAuditInfo()
    {
        var utc = DateTime.UtcNow;
        var subject = _currentUser.Subject;

        foreach (var entry in ChangeTracker.Entries<AuditableEntity>())
        {
            switch (entry.State)
            {
                case EntityState.Added:
                    entry.Entity.ApplyPersistenceOnCreate(utc, subject);
                    break;
                case EntityState.Modified:
                    entry.Entity.ApplyPersistenceOnModify(utc, subject);
                    break;
            }
        }
    }

    private List<IDomainEvent> CollectDomainEvents()
    {
        var domainEntities = ChangeTracker
            .Entries<Entity>()
            .Where(e => e.Entity is IAggregateRoot agg && agg.DomainEvents.Count != 0)
            .Select(e => (IAggregateRoot)e.Entity)
            .ToList();

        var domainEvents = domainEntities
            .SelectMany(e => e.DomainEvents)
            .ToList();

        domainEntities.ForEach(e => e.ClearDomainEvents());

        return domainEvents;
    }

    private async Task DispatchDomainEventsAsync(
        List<IDomainEvent> domainEvents,
        CancellationToken cancellationToken)
    {
        foreach (var domainEvent in domainEvents)
        {
            await _mediator.Publish(domainEvent, cancellationToken);
        }
    }
}
