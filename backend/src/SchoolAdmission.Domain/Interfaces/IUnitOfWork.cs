namespace SchoolAdmission.Domain.Interfaces;

public interface IUnitOfWork
{
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Runs <paramref name="action"/> inside a single explicit database transaction.
    /// Row locks taken by the repository (SELECT … FOR UPDATE) live exactly as long as
    /// this transaction, so the read-check-write sequence of a command is serialised.
    /// The action may be re-executed if the database reports a transient failure,
    /// so it must not have side effects outside the database.
    /// </summary>
    Task<T> ExecuteInTransactionAsync<T>(
        Func<CancellationToken, Task<T>> action,
        CancellationToken cancellationToken = default);
}
