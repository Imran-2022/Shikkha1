using SchoolAdmission.Domain.Entities;

namespace SchoolAdmission.Domain.Interfaces;

public interface IAdmissionHoldRepository
{
    Task<AdmissionHold?> GetByIdAsync(string id, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<AdmissionHold>> GetByStudentIdAsync(
        string studentId,
        CancellationToken cancellationToken = default);
}
