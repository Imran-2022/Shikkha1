using MediatR;
using SchoolAdmission.Application.Classes.DTOs;
using SchoolAdmission.Application.Common.Exceptions;
using SchoolAdmission.Domain.Interfaces;

namespace SchoolAdmission.Application.Classes.Queries;

public sealed record GetClassByIdQuery(string ClassId) : IRequest<ClassResponse>;

public sealed class GetClassByIdQueryHandler
    : IRequestHandler<GetClassByIdQuery, ClassResponse>
{
    private readonly ISchoolClassRepository _repository;

    public GetClassByIdQueryHandler(ISchoolClassRepository repository)
        => _repository = repository;

    public async Task<ClassResponse> Handle(
        GetClassByIdQuery request,
        CancellationToken cancellationToken)
    {
        var c = await _repository.GetSeatSummaryAsync(request.ClassId, cancellationToken)
            ?? throw new NotFoundException($"Class '{request.ClassId}' was not found.");

        return new ClassResponse(
            c.Id,
            c.Name,
            c.TotalSeats,
            c.HeldSeats,
            c.ConfirmedSeats,
            c.AvailableSeats);
    }
}
