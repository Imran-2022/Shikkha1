using MediatR;
using SchoolAdmission.Application.Classes.DTOs;
using SchoolAdmission.Domain.Interfaces;

namespace SchoolAdmission.Application.Classes.Queries;

public sealed record GetAllClassesQuery : IRequest<IReadOnlyList<ClassResponse>>;

public sealed class GetAllClassesQueryHandler
    : IRequestHandler<GetAllClassesQuery, IReadOnlyList<ClassResponse>>
{
    private readonly ISchoolClassRepository _repository;

    public GetAllClassesQueryHandler(ISchoolClassRepository repository)
        => _repository = repository;

    public async Task<IReadOnlyList<ClassResponse>> Handle(
        GetAllClassesQuery request,
        CancellationToken cancellationToken)
    {
        // Seat counts are aggregated by the database; holds are never loaded into memory.
        var summaries = await _repository.GetAllSeatSummariesAsync(cancellationToken);

        return summaries.Select(c => new ClassResponse(
            c.Id,
            c.Name,
            c.TotalSeats,
            c.HeldSeats,
            c.ConfirmedSeats,
            c.AvailableSeats)).ToList();
    }
}
