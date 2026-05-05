using BizFlow.Application.DTOs.BusinessType;

namespace BizFlow.Application.Interfaces.Services
{
    public interface IBusinessTypeService
    {
        Task<IEnumerable<BusinessTypeDto>> GetAllActiveAsync();
    }
}
