using FluentValidation;
using MediatR;
using SchoolAdmission.Application.Classes.DTOs;
using SchoolAdmission.Domain.Entities;
using SchoolAdmission.Domain.Interfaces;

namespace SchoolAdmission.Application.Classes.Commands;

// ── Command ───────────────────────────────────────────────────────────────────

public sealed record CreateClassCommand(string Name, int TotalSeats)
    : IRequest<ClassCreatedResponse>;

// ── Validator ─────────────────────────────────────────────────────────────────

public sealed class CreateClassCommandValidator : AbstractValidator<CreateClassCommand>
{
    public CreateClassCommandValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Class name is required.")
            .MaximumLength(200).WithMessage("Class name must not exceed 200 characters.");

        RuleFor(x => x.TotalSeats)
            .GreaterThan(0).WithMessage("Total seats must be greater than zero.");
    }
}

// ── Handler ───────────────────────────────────────────────────────────────────

public sealed class CreateClassCommandHandler
    : IRequestHandler<CreateClassCommand, ClassCreatedResponse>
{
    private readonly ISchoolClassRepository _repository;
    private readonly IUnitOfWork _unitOfWork;

    public CreateClassCommandHandler(
        ISchoolClassRepository repository,
        IUnitOfWork unitOfWork)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
    }

    public async Task<ClassCreatedResponse> Handle(
        CreateClassCommand request,
        CancellationToken cancellationToken)
    {
        var schoolClass = SchoolClass.Create(request.Name, request.TotalSeats);
        _repository.Add(schoolClass);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new ClassCreatedResponse(
            schoolClass.Id,
            schoolClass.Name,
            schoolClass.TotalSeats);
    }
}
