using SchoolAdmission.Domain.Events;
using SchoolAdmission.Domain.Exceptions;

namespace SchoolAdmission.Domain.Entities;

/// <summary>
/// Aggregate root for a school class (e.g. "Class 6 – Morning Shift").
/// Each shift is modelled as a separate SchoolClass with its own seat count.
/// </summary>
public sealed class SchoolClass : BaseEntity
{
    private readonly List<AdmissionHold> _holds = [];

    private SchoolClass() { } // EF Core

    public string Id { get; private set; } = default!;
    public string Name { get; private set; } = default!;
    public int TotalSeats { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }

    public IReadOnlyCollection<AdmissionHold> Holds => _holds.AsReadOnly();

    // ── Computed seat counts ──────────────────────────────────────────────

    public int HeldSeats =>
        _holds.Count(h => h.Status == Enums.HoldStatus.Held && !h.IsExpired);

    public int ConfirmedSeats =>
        _holds.Count(h => h.Status == Enums.HoldStatus.Confirmed);

    public int AvailableSeats =>
        TotalSeats - HeldSeats - ConfirmedSeats;

    // ── Factory ───────────────────────────────────────────────────────────

    public static SchoolClass Create(string name, int totalSeats)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentOutOfRangeException.ThrowIfLessThan(totalSeats, 1);

        var id = $"c_{Guid.NewGuid():N}";

        var schoolClass = new SchoolClass
        {
            Id = id,
            Name = name.Trim(),
            TotalSeats = totalSeats,
            CreatedAt = DateTimeOffset.UtcNow
        };

        schoolClass.AddDomainEvent(new ClassCreatedEvent(schoolClass.Id, schoolClass.Name));
        return schoolClass;
    }

    // ── Business operations ───────────────────────────────────────────────

    /// <summary>
    /// Places a seat hold for the given student.
    /// Uses pessimistic logic: re-evaluates live counts from the hold list.
    /// </summary>
    public AdmissionHold PlaceHold(string studentId, TimeSpan holdDuration)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(studentId);

        // Guard: student must not already have an active hold / confirmed seat
        bool hasActive = _holds.Any(h =>
            h.StudentId == studentId &&
            (h.Status == Enums.HoldStatus.Confirmed ||
             (h.Status == Enums.HoldStatus.Held && !h.IsExpired)));

        if (hasActive)
            throw new DuplicateActiveHoldException(studentId, Name);

        // Guard: seats must be available (computed live)
        if (AvailableSeats <= 0)
            throw new NoSeatsAvailableException(Name);

        var hold = AdmissionHold.Create(studentId, Id, holdDuration);
        _holds.Add(hold);

        AddDomainEvent(new HoldPlacedEvent(hold.Id, Id, studentId, hold.ExpiresAt));
        return hold;
    }

    /// <summary>Marks an expired hold so it no longer occupies a seat.</summary>
    public void ExpireHold(string holdId)
    {
        var hold = _holds.FirstOrDefault(h => h.Id == holdId)
            ?? throw new HoldNotFoundException(holdId);

        hold.MarkExpired();
    }
}
