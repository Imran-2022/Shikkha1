using SchoolAdmission.Domain.Enums;
using SchoolAdmission.Domain.Events;
using SchoolAdmission.Domain.Exceptions;

namespace SchoolAdmission.Domain.Entities;

/// <summary>
/// Represents a seat hold placed by a student for a class.
/// Belongs to the SchoolClass aggregate.
/// </summary>
public sealed class AdmissionHold : BaseEntity
{
    private AdmissionHold() { } // EF Core

    public string Id { get; private set; } = default!;
    public string StudentId { get; private set; } = default!;
    public string ClassId { get; private set; } = default!;
    public HoldStatus Status { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset ExpiresAt { get; private set; }
    public DateTimeOffset? ConfirmedAt { get; private set; }
    public DateTimeOffset? CancelledAt { get; private set; }

    // Navigation property (not part of domain logic)
    public SchoolClass? SchoolClass { get; private set; }

    // ── Derived ───────────────────────────────────────────────────────────

    public bool IsExpired => Status == HoldStatus.Held && DateTimeOffset.UtcNow > ExpiresAt;

    // ── Factory ───────────────────────────────────────────────────────────

    internal static AdmissionHold Create(string studentId, string classId, TimeSpan holdDuration)
    {
        return new AdmissionHold
        {
            Id = $"h_{Guid.NewGuid():N}",
            StudentId = studentId,
            ClassId = classId,
            Status = HoldStatus.Held,
            CreatedAt = DateTimeOffset.UtcNow,
            ExpiresAt = DateTimeOffset.UtcNow.Add(holdDuration)
        };
    }

    // ── Business operations ───────────────────────────────────────────────

    /// <summary>Confirms the hold (simulates payment success). Idempotent.</summary>
    public void Confirm(string requestingStudentId)
    {
        if (StudentId != requestingStudentId)
            throw new UnauthorizedHoldActionException(Id, requestingStudentId);

        // Idempotent: already confirmed → return without side-effects
        if (Status == HoldStatus.Confirmed)
            return;

        if (IsExpired)
            throw new HoldExpiredException(Id);

        if (Status != HoldStatus.Held)
            throw new HoldExpiredException(Id); // Cancelled etc.

        Status = HoldStatus.Confirmed;
        ConfirmedAt = DateTimeOffset.UtcNow;

        AddDomainEvent(new HoldConfirmedEvent(Id, ClassId, StudentId));
    }

    /// <summary>Cancels the hold. Only possible when HELD.</summary>
    public void Cancel(string requestingStudentId)
    {
        if (StudentId != requestingStudentId)
            throw new UnauthorizedHoldActionException(Id, requestingStudentId);

        if (Status != HoldStatus.Held)
            throw new HoldNotCancellableException(Id, Status.ToString());

        Status = HoldStatus.Cancelled;
        CancelledAt = DateTimeOffset.UtcNow;

        AddDomainEvent(new HoldCancelledEvent(Id, ClassId, StudentId));
    }

    internal void MarkExpired()
    {
        if (Status == HoldStatus.Held)
            Status = HoldStatus.Expired;
    }
}
