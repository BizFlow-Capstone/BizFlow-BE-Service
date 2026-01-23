using BizFlow.Application.DTOs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BizFlow.Application.Interfaces.Services
{
    public interface IRoleService
    {
        Task<IEnumerable<RoleDto>> GetAllRolesAsync();
        Task<RoleDto?> GetRoleByIdAsync(Guid id);
        Task<RoleDto?> GetRoleByNameAsync(string name);
    }
}
