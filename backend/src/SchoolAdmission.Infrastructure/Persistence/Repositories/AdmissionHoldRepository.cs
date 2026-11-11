using Microsoft.EntityFrameworkCore;
using SchoolAdmission.Domain.Entities;
using SchoolAdmission.Domain.Interfaces;

namespace SchoolAdmission.Infrastructure.Persistence.Repositories;

/// <summary>
/// Read-only lookups (untracked). Holds are always modified through the SchoolClass aggregate
/// loaded by <see cref="ISchoolClassRepository.GetByIdWithHoldsForUpdateAsync"/>.
/// </summary>
public sealed class AdmissionHoldRepository : IAdmissionHoldRepository
{
    private readonly AppDbContext _context;

    public AdmissionHoldRepository(AppDbContext context) => _context = context;

    public async Task<AdmissionHold?> GetByIdAsync(
        string id,
        CancellationToken cancellationToken = default)
        => await _context.Holds
            .AsNoTracking()
            .Include(h => h.SchoolClass)
            .FirstOrDefaultAsync(h => h.Id == id, cancellationToken);

    public async Task<IReadOnlyList<AdmissionHold>> GetByStudentIdAsync(
        string studentId,
        CancellationToken cancellationToken = default)
        => await _context.Holds
            .AsNoTracking()
            .Include(h => h.SchoolClass)
            .Where(h => h.StudentId == studentId)
            .OrderByDescending(h => h.CreatedAt)
            .ToListAsync(cancellationToken);
}
