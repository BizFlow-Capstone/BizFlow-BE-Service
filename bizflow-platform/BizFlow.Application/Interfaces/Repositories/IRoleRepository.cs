using BizFlow.Domain;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BizFlow.Application.Interfaces.Repositories
{
    public interface IRoleRepository
    {
        Task<Role?> GetByIdAsync(Guid id);
        Task<IEnumerable<Role>> GetAllAsync();
        Task<Role?> GetByNameAsync(string name);
    }
}
