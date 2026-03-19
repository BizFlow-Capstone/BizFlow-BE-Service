using BizFlow.Application.DTOs.Employee;

namespace BizFlow.Application.Interfaces.Services
{
    public interface IEmployeeService
    {
        Task<IEnumerable<UserSearchResultDto>> SearchUserByContactAsync(Guid ownerId, string query);
        Task<HireDto> InviteEmployeeAsync(Guid ownerId, Guid employeeId);
        Task RemoveEmployeeAsync(Guid ownerId, Guid employeeId);
        Task<IEnumerable<EmployeeInvitationDto>> GetPendingInvitationsAsync(Guid employeeId);
        Task AcceptInvitationAsync(Guid employeeId, int hireId);
        Task RejectInvitationAsync(Guid employeeId, int hireId);
    }
}
