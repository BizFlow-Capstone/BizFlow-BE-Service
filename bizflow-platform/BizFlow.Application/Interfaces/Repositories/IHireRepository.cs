using BizFlow.Domain.Entities;

namespace BizFlow.Application.Interfaces.Repositories
{
    public interface IHireRepository
    {
        /// <summary>
        /// Get all active employees hired by an owner
        /// </summary>
        Task<IEnumerable<Guid>> GetHiredEmployeeIdsAsync(Guid ownerId);

        /// <summary>
        /// Get all hired employees with user details for an owner
        /// </summary>
        Task<IEnumerable<(Hire hire, string fullName, string email, string? phone)>> GetHiredEmployeesWithDetailsAsync(Guid ownerId);
    }
}
