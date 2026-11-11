using SchoolAdmission.Domain.Entities;

namespace SchoolAdmission.Domain.Interfaces;

/// <summary>Read model: seat counts computed by the database, without loading holds.</summary>
public sealed record ClassSeatSummary(
    string Id,
    string Name,
    int TotalSeats,
    int HeldSeats,
    int ConfirmedSeats)
{
    public int AvailableSeats => TotalSeats - HeldSeats - ConfirmedSeats;
}

public interface ISchoolClassRepository
{
    Task<SchoolClass?> GetByIdAsync(string id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Locks the class row (SELECT … FOR UPDATE) and then loads the aggregate with its holds.
    /// MUST be called inside <see cref="IUnitOfWork.ExecuteInTransactionAsync{T}"/>; the lock is
    /// released when that transaction commits or rolls back. The returned aggregate is tracked,
    /// so changes are persisted by <see cref="IUnitOfWork.SaveChangesAsync"/> (no Update call needed).
    /// </summary>
    Task<SchoolClass?> GetByIdWithHoldsForUpdateAsync(string id, CancellationToken cancellationToken = default);

    /// <summary>Seat counts for all classes, computed in SQL (expired holds are not counted).</summary>
    Task<IReadOnlyList<ClassSeatSummary>> GetAllSeatSummariesAsync(CancellationToken cancellationToken = default);

    /// <summary>Seat counts for one class, computed in SQL.</summary>
    Task<ClassSeatSummary?> GetSeatSummaryAsync(string id, CancellationToken cancellationToken = default);

    void Add(SchoolClass schoolClass);
}
