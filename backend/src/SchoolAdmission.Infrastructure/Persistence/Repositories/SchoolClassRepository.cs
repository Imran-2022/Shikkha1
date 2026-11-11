using Microsoft.EntityFrameworkCore;
using SchoolAdmission.Domain.Entities;
using SchoolAdmission.Domain.Enums;
using SchoolAdmission.Domain.Interfaces;

namespace SchoolAdmission.Infrastructure.Persistence.Repositories;

public sealed class SchoolClassRepository : ISchoolClassRepository
{
    private readonly AppDbContext _context;

    public SchoolClassRepository(AppDbContext context) => _context = context;

    public async Task<SchoolClass?> GetByIdAsync(
        string id,
        CancellationToken cancellationToken = default)
        => await _context.Classes
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == id, cancellationToken);

    public async Task<SchoolClass?> GetByIdWithHoldsForUpdateAsync(
        string id,
        CancellationToken cancellationToken = default)
    {
        if (_context.Database.CurrentTransaction is null)
        {
            throw new InvalidOperationException(
                "GetByIdWithHoldsForUpdateAsync must run inside IUnitOfWork.ExecuteInTransactionAsync; " +
                "otherwise the row lock is released immediately and protects nothing.");
        }

        // 1. Take the row lock on the same connection/transaction that will perform the write.
        //    Concurrent transactions for the same class block here until we commit or roll back.
        //    (Interpolated overload => {id} is a SQL parameter, and the CancellationToken is
        //    really treated as a token, not as a query parameter.)
        await _context.Database.ExecuteSqlInterpolatedAsync(
            $"SELECT 1 FROM classes WHERE id = {id} FOR UPDATE",
            cancellationToken);

        // 2. Only now read the aggregate. Under READ COMMITTED this statement gets a fresh snapshot,
        //    so it sees every hold committed by transactions that held the lock before us.
        return await _context.Classes
            .Include(c => c.Holds)
            .FirstOrDefaultAsync(c => c.Id == id, cancellationToken);
    }

    public async Task<IReadOnlyList<ClassSeatSummary>> GetAllSeatSummariesAsync(
        CancellationToken cancellationToken = default)
    {
        var now = DateTimeOffset.UtcNow;

        return await _context.Classes
            .AsNoTracking()
            .OrderBy(c => c.Name)
            .Select(c => new ClassSeatSummary(
                c.Id,
                c.Name,
                c.TotalSeats,
                c.Holds.Count(h => h.Status == HoldStatus.Held && h.ExpiresAt > now),
                c.Holds.Count(h => h.Status == HoldStatus.Confirmed)))
            .ToListAsync(cancellationToken);
    }

    public async Task<ClassSeatSummary?> GetSeatSummaryAsync(
        string id,
        CancellationToken cancellationToken = default)
    {
        var now = DateTimeOffset.UtcNow;

        return await _context.Classes
            .AsNoTracking()
            .Where(c => c.Id == id)
            .Select(c => new ClassSeatSummary(
                c.Id,
                c.Name,
                c.TotalSeats,
                c.Holds.Count(h => h.Status == HoldStatus.Held && h.ExpiresAt > now),
                c.Holds.Count(h => h.Status == HoldStatus.Confirmed)))
            .FirstOrDefaultAsync(cancellationToken);
    }

    public void Add(SchoolClass schoolClass)
        => _context.Classes.Add(schoolClass);
}
