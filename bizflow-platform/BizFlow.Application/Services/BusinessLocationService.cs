using BizFlow.Application.DTOs.Location;
using BizFlow.Application.Common.Constants;
using BizFlow.Application.Common.Exceptions;
using BizFlow.Application.Interfaces.Repositories;
using BizFlow.Application.Interfaces.Services;
using BizFlow.Application.Mappers;
using BizFlow.Domain.Entities;

namespace BizFlow.Application.Services
{
    public class BusinessLocationService : IBusinessLocationService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IHireService _hireService;

        public BusinessLocationService(IUnitOfWork unitOfWork, IHireService hireService)
        {
            _unitOfWork = unitOfWork;
            _hireService = hireService;
        }

        public async Task<IEnumerable<BusinessLocationDto>> GetOwnedLocationsAsync(Guid userId)
        {
            var locations = await _unitOfWork.BusinessLocations.GetOwnedByUserIdAsync(userId);
            
            var result = new List<BusinessLocationDto>();
            foreach (var loc in locations)
            {
                var (_, ownerName) = await _unitOfWork.BusinessLocations.GetByIdWithOwnerAsync(loc.BusinessLocationId);
                result.Add(BusinessLocationMapper.ToDto(loc, ownerName));
            }
            
            return result;
        }

        public async Task<IEnumerable<BusinessLocationDto>> GetWorkLocationsAsync(Guid userId)
        {
            var locations = await _unitOfWork.BusinessLocations.GetWorkLocationsByUserIdAsync(userId);
            
            var result = new List<BusinessLocationDto>();
            foreach (var loc in locations)
            {
                var (_, ownerName) = await _unitOfWork.BusinessLocations.GetByIdWithOwnerAsync(loc.BusinessLocationId);
                result.Add(BusinessLocationMapper.ToDto(loc, ownerName));
            }
            
            return result;
        }

        public async Task<BusinessLocationDto> CreateLocationAsync(Guid userId, CreateLocationRequest request)
        {
            return await _unitOfWork.ExecuteResilientAsync(async _ =>
            {
                // Create the location using mapper
                var location = BusinessLocationMapper.ToEntity(request);

                var createdLocation = await _unitOfWork.BusinessLocations.AddAsync(location);
                await _unitOfWork.SaveChangesAsync();

                // Assign current user as owner
                var ownerAssignment = new UserLocationAssignment
                {
                    UserId = userId,
                    BusinessLocationId = createdLocation.BusinessLocationId,
                    IsOwner = true,
                    IsActive = true
                };
                await _unitOfWork.BusinessLocations.AddUserLocationAssignmentAsync(ownerAssignment);

                // Optional: Assign hired employees if provided
                if (request.EmployeeIds != null && request.EmployeeIds.Any())
                {
                    // Validate employees using HireService
                    var validationResult = await _hireService.ValidateEmployeesForAssignmentAsync(userId, request.EmployeeIds);

                    // If any employees are not hired, throw BadRequestException
                    if (!validationResult.AllValid)
                    {
                        throw new BadRequestException(
                            MessageKeys.EmployeesNotHired, 
                            new { invalidEmployeeIds = validationResult.InvalidEmployeeIds }
                        );
                    }

                    // All employees are valid - batch create assignments
                    foreach (var employeeId in validationResult.ValidEmployeeIds)
                    {
                        var employeeAssignment = new UserLocationAssignment
                        {
                            UserId = employeeId,
                            BusinessLocationId = createdLocation.BusinessLocationId,
                            IsOwner = false,
                            IsActive = true
                        };
                        await _unitOfWork.BusinessLocations.AddUserLocationAssignmentAsync(employeeAssignment);
                    }
                }

                await _unitOfWork.SaveChangesAsync();

                return BusinessLocationMapper.ToDto(createdLocation);
            });
        }

        public async Task<bool> UpdateLocationStatusAsync(Guid userId, int locationId, bool isActive)
        {
            // Check ownership
            var isOwner = await _unitOfWork.BusinessLocations.IsOwnerOfLocationAsync(userId, locationId);
            if (!isOwner)
                return false;

            var location = await _unitOfWork.BusinessLocations.GetByIdAsync(locationId);
            if (location == null)
                return false;

            location.IsActive = isActive;
            _unitOfWork.BusinessLocations.Update(location);
            await _unitOfWork.SaveChangesAsync();

            return true;
        }

        public async Task<bool> UpdateLocationAsync(Guid userId, int locationId, UpdateLocationRequest request)
        {
            // Check ownership
            var isOwner = await _unitOfWork.BusinessLocations.IsOwnerOfLocationAsync(userId, locationId);
            if (!isOwner)
                return false;

            var location = await _unitOfWork.BusinessLocations.GetByIdAsync(locationId);
            if (location == null)
                return false;

            // Update using mapper
            BusinessLocationMapper.UpdateEntity(location, request);

            _unitOfWork.BusinessLocations.Update(location);
            await _unitOfWork.SaveChangesAsync();

            return true;
        }
    }
}
