using FluentValidation;
using MediatR;
using SchoolAdmission.Application.Common.Exceptions;
using SchoolAdmission.Domain.Interfaces;

namespace SchoolAdmission.Application.Holds.Commands;

// ── Command ───────────────────────────────────────────────────────────────────

public sealed record CancelHoldCommand(string HoldId, string StudentId)
    : IRequest<Unit>;

// ── Validator ─────────────────────────────────────────────────────────────────

public sealed class CancelHoldCommandValidator : AbstractValidator<CancelHoldCommand>
{
    public CancelHoldCommandValidator()
    {
        RuleFor(x => x.HoldId).NotEmpty();
        RuleFor(x => x.StudentId).NotEmpty()
            .Matches(@"^\d{17}$").WithMessage("Student ID must be 17 digits.");
    }
}

// ── Handler ───────────────────────────────────────────────────────────────────

public sealed class CancelHoldCommandHandler : IRequestHandler<CancelHoldCommand, Unit>
{
    private readonly ISchoolClassRepository _classRepository;
    private readonly IAdmissionHoldRepository _holdRepository;
    private readonly IUnitOfWork _unitOfWork;

    public CancelHoldCommandHandler(
        ISchoolClassRepository classRepository,
        IAdmissionHoldRepository holdRepository,
        IUnitOfWork unitOfWork)
    {
        _classRepository = classRepository;
        _holdRepository = holdRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Unit> Handle(CancelHoldCommand request, CancellationToken cancellationToken)
    {
        var holdLookup = await _holdRepository.GetByIdAsync(request.HoldId, cancellationToken)
            ?? throw new NotFoundException($"Hold '{request.HoldId}' was not found.");

        return await _unitOfWork.ExecuteInTransactionAsync(async ct =>
        {
            var schoolClass = await _classRepository.GetByIdWithHoldsForUpdateAsync(holdLookup.ClassId, ct)
                ?? throw new NotFoundException($"Class '{holdLookup.ClassId}' was not found.");

            var hold = schoolClass.Holds.FirstOrDefault(h => h.Id == request.HoldId)
                ?? throw new NotFoundException($"Hold '{request.HoldId}' was not found.");

            hold.Cancel(request.StudentId);

            await _unitOfWork.SaveChangesAsync(ct);
            return Unit.Value;
        }, cancellationToken);
    }
}
