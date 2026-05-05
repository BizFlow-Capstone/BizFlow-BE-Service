using BizFlow.Application.Common.Constants;
using BizFlow.Application.Common.Interfaces;
using BizFlow.Application.DTOs.BusinessType;
using BizFlow.Application.Interfaces.Repositories;
using BizFlow.Application.Interfaces.Services;

namespace BizFlow.Application.Services
{
    public class BusinessTypeService : IBusinessTypeService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IReferenceLabelService _labels;

        public BusinessTypeService(IUnitOfWork unitOfWork, IReferenceLabelService labels)
        {
            _unitOfWork = unitOfWork;
            _labels = labels;
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
                Status = _labels.ToOption(ReferenceCategory.BusinessTypeStatus, bt.Status),
            });
        }

        public async Task<BusinessTypeDto> GetActiveByIdAsync(Guid businessTypeId)
        {
            var bt = await _unitOfWork.BusinessTypes.GetActiveByIdAsync(businessTypeId);
            if (bt is null)
            {
                throw new KeyNotFoundException(MessageKeys.NotFound);
            }

            return new BusinessTypeDto
            {
                BusinessTypeId = bt.BusinessTypeId,
                Code = bt.Code,
                Name = bt.Name,
                Description = bt.Description,
                Status = _labels.ToOption(ReferenceCategory.BusinessTypeStatus, bt.Status),
            };
        }
    }
}
