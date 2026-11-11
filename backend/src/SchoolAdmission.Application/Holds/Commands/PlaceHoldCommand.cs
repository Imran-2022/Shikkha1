using FluentValidation;
using MediatR;
using Microsoft.Extensions.Configuration;
using SchoolAdmission.Application.Common.Exceptions;
using SchoolAdmission.Application.Holds.DTOs;
using SchoolAdmission.Domain.Interfaces;

namespace SchoolAdmission.Application.Holds.Commands;

// ── Command ───────────────────────────────────────────────────────────────────

public sealed record PlaceHoldCommand(string ClassId, string StudentId)
    : IRequest<PlaceHoldResponse>;

// ── Validator ─────────────────────────────────────────────────────────────────

public sealed class PlaceHoldCommandValidator : AbstractValidator<PlaceHoldCommand>
{
    public PlaceHoldCommandValidator()
    {
        RuleFor(x => x.ClassId)
            .NotEmpty().WithMessage("Class ID is required.");

        RuleFor(x => x.StudentId)
            .NotEmpty().WithMessage("Student ID (X-Student-Id header) is required.")
            .Length(17).WithMessage("Student ID must be a 17-digit birth registration number.")
            .Matches(@"^\d{17}$").WithMessage("Student ID must contain exactly 17 digits.");
    }
}

// ── Handler ───────────────────────────────────────────────────────────────────

public sealed class PlaceHoldCommandHandler
    : IRequestHandler<PlaceHoldCommand, PlaceHoldResponse>
{
    private readonly ISchoolClassRepository _repository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IConfiguration _configuration;

    public PlaceHoldCommandHandler(
        ISchoolClassRepository repository,
        IUnitOfWork unitOfWork,
        IConfiguration configuration)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
        _configuration = configuration;
    }

    public async Task<PlaceHoldResponse> Handle(
        PlaceHoldCommand request,
        CancellationToken cancellationToken)
    {
        var holdDurationSeconds = _configuration.GetValue<int>("HOLD_DURATION_SECONDS", 600);
        var holdDuration = TimeSpan.FromSeconds(holdDurationSeconds);

        // One transaction wraps: lock class row -> load aggregate -> check rules -> insert hold -> commit.
        // Concurrent requests for the same class queue on the row lock, so each one sees the
        // committed result of the previous one and seat counts can never be exceeded.
        return await _unitOfWork.ExecuteInTransactionAsync(async ct =>
        {
            var schoolClass = await _repository.GetByIdWithHoldsForUpdateAsync(request.ClassId, ct)
                ?? throw new NotFoundException($"Class '{request.ClassId}' was not found.");

            // Throws domain exceptions on business rule violations (-> rollback)
            var hold = schoolClass.PlaceHold(request.StudentId, holdDuration);

            await _unitOfWork.SaveChangesAsync(ct);

            return new PlaceHoldResponse(
                hold.Id,
                hold.ClassId,
                hold.StudentId,
                hold.Status.ToString().ToUpperInvariant(),
                hold.ExpiresAt);
        }, cancellationToken);
    }
}
