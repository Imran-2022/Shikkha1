using MediatR;

namespace SchoolAdmission.Domain.Events;

public interface IDomainEvent : INotification
{
    Guid EventId { get; }
    DateTimeOffset OccurredAt { get; }
}
