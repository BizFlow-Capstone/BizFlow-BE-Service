using BizFlow.Application.DTOs.Employee;
using BizFlow.Domain.Entities;

namespace BizFlow.Application.Interfaces.Repositories
{
    public interface IEmployeeRepository
    {
        Task<List<UserSearchResultDto>> SearchByContactAsync(Guid ownerId, string query, int limit = 10);
        Task<bool> ProfileExistsAsync(Guid profileId);
        Task<Hire?> GetHireByOwnerEmployeeAsync(Guid ownerId, Guid employeeId);
        Task<Hire?> GetOpenHireAsync(Guid ownerId, Guid employeeId);
        Task<Hire?> GetActiveHireAsync(Guid ownerId, Guid employeeId);
        Task<Hire> CreateHireAsync(Hire hire);
        Task DeleteHireAsync(Hire hire);
        Task<bool> HasActiveAssignmentsAsync(Guid employeeId);
        Task<List<EmployeeInvitationDto>> GetPendingInvitationsAsync(Guid employeeId);
        Task<Hire?> GetPendingInvitationByIdAsync(Guid employeeId, int hireId);
        Task SaveChangesAsync();
    }
}
