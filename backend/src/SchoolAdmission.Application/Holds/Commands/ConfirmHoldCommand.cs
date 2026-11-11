using FluentValidation;
using MediatR;
using SchoolAdmission.Application.Common.Exceptions;
using SchoolAdmission.Application.Holds.DTOs;
using SchoolAdmission.Domain.Interfaces;

namespace SchoolAdmission.Application.Holds.Commands;

// ── Command ───────────────────────────────────────────────────────────────────

public sealed record ConfirmHoldCommand(string HoldId, string StudentId)
    : IRequest<ConfirmHoldResponse>;

// ── Validator ─────────────────────────────────────────────────────────────────

public sealed class ConfirmHoldCommandValidator : AbstractValidator<ConfirmHoldCommand>
{
    public ConfirmHoldCommandValidator()
    {
        RuleFor(x => x.HoldId).NotEmpty();
        RuleFor(x => x.StudentId).NotEmpty()
            .Matches(@"^\d{17}$").WithMessage("Student ID must be 17 digits.");
    }
}

// ── Handler ───────────────────────────────────────────────────────────────────

public sealed class ConfirmHoldCommandHandler
    : IRequestHandler<ConfirmHoldCommand, ConfirmHoldResponse>
{
    private readonly ISchoolClassRepository _classRepository;
    private readonly IAdmissionHoldRepository _holdRepository;
    private readonly IUnitOfWork _unitOfWork;

    public ConfirmHoldCommandHandler(
        ISchoolClassRepository classRepository,
        IAdmissionHoldRepository holdRepository,
        IUnitOfWork unitOfWork)
    {
        _classRepository = classRepository;
        _holdRepository = holdRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<ConfirmHoldResponse> Handle(
        ConfirmHoldCommand request,
        CancellationToken cancellationToken)
    {
        // Untracked lookup just to learn which class (and therefore which row lock) is needed
        var holdLookup = await _holdRepository.GetByIdAsync(request.HoldId, cancellationToken)
            ?? throw new NotFoundException($"Hold '{request.HoldId}' was not found.");

        return await _unitOfWork.ExecuteInTransactionAsync(async ct =>
        {
            var schoolClass = await _classRepository.GetByIdWithHoldsForUpdateAsync(holdLookup.ClassId, ct)
                ?? throw new NotFoundException($"Class '{holdLookup.ClassId}' was not found.");

            var hold = schoolClass.Holds.FirstOrDefault(h => h.Id == request.HoldId)
                ?? throw new NotFoundException($"Hold '{request.HoldId}' was not found.");

            // Idempotent: a second confirm of an already-confirmed hold is a no-op
            hold.Confirm(request.StudentId);

            await _unitOfWork.SaveChangesAsync(ct);

            return new ConfirmHoldResponse(
                hold.Id,
                hold.Status.ToString().ToUpperInvariant());
        }, cancellationToken);
    }
}
