namespace SchoolAdmission.Domain.Exceptions;

public abstract class DomainException : Exception
{
    protected DomainException(string message) : base(message) { }
}

public sealed class NoSeatsAvailableException : DomainException
{
    public NoSeatsAvailableException(string className)
        : base($"No seats available for class '{className}'.") { }
}

public sealed class DuplicateActiveHoldException : DomainException
{
    public DuplicateActiveHoldException(string studentId, string className)
        : base($"Student '{studentId}' already has an active hold or confirmed seat in class '{className}'.") { }
}

public sealed class HoldNotFoundException : DomainException
{
    public HoldNotFoundException(string holdId)
        : base($"Hold '{holdId}' was not found.") { }
}

public sealed class HoldExpiredException : DomainException
{
    public HoldExpiredException(string holdId)
        : base($"Hold '{holdId}' has expired and cannot be confirmed.") { }
}

public sealed class HoldAlreadyConfirmedException : DomainException
{
    public HoldAlreadyConfirmedException(string holdId)
        : base($"Hold '{holdId}' has already been confirmed.") { }
}

public sealed class UnauthorizedHoldActionException : DomainException
{
    public UnauthorizedHoldActionException(string holdId, string studentId)
        : base($"Student '{studentId}' is not authorized to act on hold '{holdId}'.") { }
}

public sealed class ClassNotFoundException : DomainException
{
    public ClassNotFoundException(string classId)
        : base($"Class '{classId}' was not found.") { }
}

public sealed class HoldNotCancellableException : DomainException
{
    public HoldNotCancellableException(string holdId, string status)
        : base($"Hold '{holdId}' in status '{status}' cannot be cancelled.") { }
}
