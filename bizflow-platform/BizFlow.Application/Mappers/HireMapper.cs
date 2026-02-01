using BizFlow.Application.DTOs.Hire;
using BizFlow.Domain.Entities;

namespace BizFlow.Application.Mappers
{
    /// <summary>
    /// Mapper for Hire entities and DTOs
    /// </summary>
    public static class HireMapper
    {
        /// <summary>
        /// Map Hire entity with User details to HiredEmployeeDto
        /// </summary>
        public static HiredEmployeeDto ToDto(Hire hire, string fullName, string email, string? phone)
        {
            return new HiredEmployeeDto
            {
                EmployeeId = hire.EmployeeId,
                FullName = fullName,
                Email = email,
                Phone = phone,
                StartAt = hire.StartAt,
                EndAt = hire.EndAt
            };
        }

        /// <summary>
        /// Map Hire entity with User entity to HiredEmployeeDto
        /// </summary>
        public static HiredEmployeeDto ToDto(Hire hire, User user)
        {
            return new HiredEmployeeDto
            {
                EmployeeId = hire.EmployeeId,
                FullName = user.FullName,
                Email = user.Email,
                Phone = user.Phone,
                StartAt = hire.StartAt,
                EndAt = hire.EndAt
            };
        }
    }
}
