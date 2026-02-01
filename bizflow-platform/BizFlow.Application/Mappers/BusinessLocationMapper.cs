using BizFlow.Application.DTOs.Location;
using BizFlow.Domain.Entities;

namespace BizFlow.Application.Mappers
{
    /// <summary>
    /// Mapper for Business Location entities and DTOs
    /// </summary>
    public static class BusinessLocationMapper
    {
        /// <summary>
        /// Map BusinessLocation entity to DTO
        /// </summary>
        public static BusinessLocationDto ToDto(BusinessLocation location, string? ownerName = null)
        {
            return new BusinessLocationDto
            {
                Id = location.BusinessLocationId,
                Name = location.Name,
                Address = location.Address,
                District = location.District,
                City = location.City,
                Phone = location.Phone,
                IsActive = location.IsActive ?? true,
                OwnerName = ownerName
            };
        }

        /// <summary>
        /// Map CreateLocationRequest to BusinessLocation entity
        /// </summary>
        public static BusinessLocation ToEntity(CreateLocationRequest request)
        {
            return new BusinessLocation
            {
                Name = request.Name,
                Address = request.Address,
                District = request.District,
                City = request.City,
                Phone = request.Phone,
                TaxCode = request.TaxCode,
                IsActive = true
            };
        }

        /// <summary>
        /// Update BusinessLocation entity from UpdateLocationRequest
        /// </summary>
        public static void UpdateEntity(BusinessLocation location, UpdateLocationRequest request)
        {
            if (!string.IsNullOrEmpty(request.Name))
                location.Name = request.Name;
            if (!string.IsNullOrEmpty(request.Address))
                location.Address = request.Address;
            if (request.District != null)
                location.District = request.District;
            if (request.City != null)
                location.City = request.City;
            if (request.Phone != null)
                location.Phone = request.Phone;
            if (request.TaxCode != null)
                location.TaxCode = request.TaxCode;
        }
    }
}
