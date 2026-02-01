using BizFlow.Domain.Entities;

namespace BizFlow.Application.Interfaces.Repositories
{
    public interface IHireRepository
    {
        /// <summary>
        /// Check if an employee is hired by a specific owner
        /// </summary>
        Task<bool> IsEmployeeHiredByOwnerAsync(Guid ownerId, Guid employeeId);

        /// <summary>
        /// Get all active employees hired by an owner
        /// </summary>
        Task<IEnumerable<Guid>> GetHiredEmployeeIdsAsync(Guid ownerId);

        /// <summary>
        /// Get hire record by owner and employee
        /// </summary>
        Task<Hire?> GetHireAsync(Guid ownerId, Guid employeeId);

        /// <summary>
        /// Add new hire record
        /// </summary>
        Task<Hire> AddAsync(Hire hire);

        /// <summary>
        /// Update hire record
        /// </summary>
        void Update(Hire hire);

        /// <summary>
        /// Get all hired employees with user details for an owner
        /// </summary>
        Task<IEnumerable<(Hire hire, string fullName, string email, string? phone)>> GetHiredEmployeesWithDetailsAsync(Guid ownerId);
    }
}
