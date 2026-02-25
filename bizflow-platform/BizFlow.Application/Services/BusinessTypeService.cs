using BizFlow.Application.DTOs.BusinessType;
using BizFlow.Application.Interfaces.Repositories;
using BizFlow.Application.Interfaces.Services;

namespace BizFlow.Application.Services
{
    public class BusinessTypeService : IBusinessTypeService
    {
        private readonly IUnitOfWork _unitOfWork;

        public BusinessTypeService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<IEnumerable<BusinessTypeDto>> GetAllAsync()
        {
            var businessTypes = await _unitOfWork.BusinessTypes.GetAllAsync();
            return businessTypes.Select(bt => new BusinessTypeDto
            {
                BusinessTypeId = bt.BusinessTypeId,
                Code = bt.Code,
                Name = bt.Name,
                Description = bt.Description,
                Status = bt.Status
            });
        }
    }
}
