using Microsoft.EntityFrameworkCore;
using SchoolAdmission.Domain.Enums;
using SchoolAdmission.Infrastructure.Persistence;

namespace SchoolAdmission.Infrastructure.Services;

/// <summary>
/// Writes expiry back to the database: every HELD hold whose ExpiresAt has passed becomes EXPIRED.
/// Seat counts and API responses are already correct without this (they evaluate ExpiresAt live);
/// the sweep keeps the stored status truthful for reporting, direct SQL and indexes.
/// </summary>
public sealed class HoldExpirySweeper
{
    private readonly AppDbContext _context;

    public HoldExpirySweeper(AppDbContext context) => _context = context;

    /// <returns>Number of holds marked as expired.</returns>
    public Task<int> SweepAsync(CancellationToken cancellationToken = default)
    {
        var now = DateTimeOffset.UtcNow;

        // Single set-based UPDATE, uses ix_holds_status_expires; never loads entities.
        return _context.Holds
            .Where(h => h.Status == HoldStatus.Held && h.ExpiresAt <= now)
            .ExecuteUpdateAsync(
                setters => setters.SetProperty(h => h.Status, HoldStatus.Expired),
                cancellationToken);
    }
}
