using FluentValidation;
using MediatR;
using SchoolAdmission.Application.Common.Exceptions;
using SchoolAdmission.Application.Holds.DTOs;
using SchoolAdmission.Domain.Interfaces;

namespace SchoolAdmission.Application.Holds.Queries;

// ── Get single hold ───────────────────────────────────────────────────────────

public sealed record GetHoldByIdQuery(string HoldId) : IRequest<HoldStatusResponse>;

public sealed class GetHoldByIdQueryHandler
    : IRequestHandler<GetHoldByIdQuery, HoldStatusResponse>
{
    private readonly IAdmissionHoldRepository _holdRepository;
    private readonly ISchoolClassRepository _classRepository;

    public GetHoldByIdQueryHandler(
        IAdmissionHoldRepository holdRepository,
        ISchoolClassRepository classRepository)
    {
        _holdRepository = holdRepository;
        _classRepository = classRepository;
    }

    public async Task<HoldStatusResponse> Handle(
        GetHoldByIdQuery request,
        CancellationToken cancellationToken)
    {
        var hold = await _holdRepository.GetByIdAsync(request.HoldId, cancellationToken)
            ?? throw new NotFoundException($"Hold '{request.HoldId}' was not found.");

        // Reflect live expiry status
        var status = hold.IsExpired ? "EXPIRED" : hold.Status.ToString().ToUpperInvariant();

        return new HoldStatusResponse(
            hold.Id,
            hold.ClassId,
            status,
            hold.ExpiresAt);
    }
}

// ── Get all holds for a student ───────────────────────────────────────────────

public sealed record GetStudentHoldsQuery(string StudentId)
    : IRequest<IReadOnlyList<StudentHoldResponse>>;

public sealed class GetStudentHoldsQueryValidator : AbstractValidator<GetStudentHoldsQuery>
{
    public GetStudentHoldsQueryValidator()
    {
        RuleFor(x => x.StudentId)
            .NotEmpty().WithMessage("Student ID (X-Student-Id header) is required.")
            .Matches(@"^\d{17}$").WithMessage("Student ID must contain exactly 17 digits.");
    }
}

public sealed class GetStudentHoldsQueryHandler
    : IRequestHandler<GetStudentHoldsQuery, IReadOnlyList<StudentHoldResponse>>
{
    private readonly IAdmissionHoldRepository _holdRepository;
    private readonly ISchoolClassRepository _classRepository;

    public GetStudentHoldsQueryHandler(
        IAdmissionHoldRepository holdRepository,
        ISchoolClassRepository classRepository)
    {
        _holdRepository = holdRepository;
        _classRepository = classRepository;
    }

    public async Task<IReadOnlyList<StudentHoldResponse>> Handle(
        GetStudentHoldsQuery request,
        CancellationToken cancellationToken)
    {
        var holds = await _holdRepository.GetByStudentIdAsync(request.StudentId, cancellationToken);

        return holds.Select(h =>
        {
            var status = h.IsExpired ? "EXPIRED" : h.Status.ToString().ToUpperInvariant();
            return new StudentHoldResponse(
                h.Id,
                h.ClassId,
                h.SchoolClass?.Name ?? h.ClassId,
                status,
                h.ExpiresAt);
        }).ToList();
    }
}
