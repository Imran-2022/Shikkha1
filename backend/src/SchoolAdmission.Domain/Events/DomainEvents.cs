namespace SchoolAdmission.Domain.Events;

public sealed record ClassCreatedEvent(
    string ClassId,
    string ClassName,
    Guid EventId,
    DateTimeOffset OccurredAt) : IDomainEvent
{
    public ClassCreatedEvent(string classId, string className)
        : this(classId, className, Guid.NewGuid(), DateTimeOffset.UtcNow) { }
}

public sealed record HoldPlacedEvent(
    string HoldId,
    string ClassId,
    string StudentId,
    DateTimeOffset ExpiresAt,
    Guid EventId,
    DateTimeOffset OccurredAt) : IDomainEvent
{
    public HoldPlacedEvent(string holdId, string classId, string studentId, DateTimeOffset expiresAt)
        : this(holdId, classId, studentId, expiresAt, Guid.NewGuid(), DateTimeOffset.UtcNow) { }
}

public sealed record HoldConfirmedEvent(
    string HoldId,
    string ClassId,
    string StudentId,
    Guid EventId,
    DateTimeOffset OccurredAt) : IDomainEvent
{
    public HoldConfirmedEvent(string holdId, string classId, string studentId)
        : this(holdId, classId, studentId, Guid.NewGuid(), DateTimeOffset.UtcNow) { }
}

public sealed record HoldCancelledEvent(
    string HoldId,
    string ClassId,
    string StudentId,
    Guid EventId,
    DateTimeOffset OccurredAt) : IDomainEvent
{
    public HoldCancelledEvent(string holdId, string classId, string studentId)
        : this(holdId, classId, studentId, Guid.NewGuid(), DateTimeOffset.UtcNow) { }
}
