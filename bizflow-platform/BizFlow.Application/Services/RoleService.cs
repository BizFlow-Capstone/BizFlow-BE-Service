using BizFlow.Application.DTOs;
using BizFlow.Application.Interfaces.Repositories;
using BizFlow.Application.Interfaces.Services;
using BizFlow.Domain.Entities;

namespace BizFlow.Application.Services
{
    public class RoleService : IRoleService
    {
        private readonly IUnitOfWork _unitOfWork;

        public RoleService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<IEnumerable<RoleDto>> GetAllRolesAsync()
        {
            var roles = await _unitOfWork.Roles.GetAllAsync();
            return roles.Select(MapToDto);
        }

        public async Task<RoleDto?> GetRoleByIdAsync(Guid id)
        {
            var role = await _unitOfWork.Roles.GetByIdAsync(id);
            return role == null ? null : MapToDto(role);
        }

        public async Task<RoleDto?> GetRoleByNameAsync(string name)
        {
            var role = await _unitOfWork.Roles.GetByNameAsync(name);
            return role == null ? null : MapToDto(role);
        }

        private static RoleDto MapToDto(Role role) => new()
        {
            Id = role.RoleId,
            Name = role.Name,
            Description = role.Description,
            CreatedAt = role.CreateAt,
            UpdatedAt = role.UpdateAt
        };
    }
}
