using System.Data;
using MediatR;
using Microsoft.EntityFrameworkCore;
using SchoolAdmission.Domain.Entities;
using SchoolAdmission.Domain.Interfaces;

namespace SchoolAdmission.Infrastructure.Persistence;

public sealed class AppDbContext : DbContext, IUnitOfWork
{
    private readonly IMediator _mediator;

    public AppDbContext(DbContextOptions<AppDbContext> options, IMediator mediator)
        : base(options)
    {
        _mediator = mediator;
    }

    public DbSet<SchoolClass> Classes => Set<SchoolClass>();
    public DbSet<AdmissionHold> Holds => Set<AdmissionHold>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        // Dispatch domain events before persisting
        var entitiesWithEvents = ChangeTracker
            .Entries<Domain.Entities.BaseEntity>()
            .Where(e => e.Entity.DomainEvents.Count != 0)
            .Select(e => e.Entity)
            .ToList();

        var domainEvents = entitiesWithEvents
            .SelectMany(e => e.DomainEvents)
            .ToList();

        entitiesWithEvents.ForEach(e => e.ClearDomainEvents());

        var result = await base.SaveChangesAsync(cancellationToken);

        foreach (var domainEvent in domainEvents)
            await _mediator.Publish(domainEvent, cancellationToken);

        return result;
    }

    public Task<T> ExecuteInTransactionAsync<T>(
        Func<CancellationToken, Task<T>> action,
        CancellationToken cancellationToken = default)
    {
        // Already inside a transaction (nested call): just join it.
        if (Database.CurrentTransaction is not null)
            return action(cancellationToken);

        // With EnableRetryOnFailure, user-initiated transactions must run inside the execution
        // strategy so the whole unit (begin -> work -> commit) is retried together.
        var strategy = Database.CreateExecutionStrategy();

        return strategy.ExecuteAsync(async () =>
        {
            // A retry must not see entities tracked by the failed attempt.
            ChangeTracker.Clear();

            await using var transaction = await Database.BeginTransactionAsync(
                IsolationLevel.ReadCommitted, cancellationToken);

            var result = await action(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return result;
            // Leaving without commit (exception) disposes the transaction => ROLLBACK.
        });
    }
}
