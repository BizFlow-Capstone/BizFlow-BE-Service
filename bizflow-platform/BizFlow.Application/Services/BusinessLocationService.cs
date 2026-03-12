using BizFlow.Application.DTOs.Hire;
using BizFlow.Application.DTOs.Location;
using BizFlow.Application.Common.Constants;
using BizFlow.Application.Common.Exceptions;
using BizFlow.Application.Interfaces.Repositories;
using BizFlow.Application.Interfaces.Services;
using BizFlow.Application.Mappers;
using AutoMapper;
using BizFlow.Domain.Entities;

namespace BizFlow.Application.Services
{
    public class BusinessLocationService : IBusinessLocationService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IHireService _hireService;
        private readonly IMapper _mapper;

        public BusinessLocationService(IUnitOfWork unitOfWork, IHireService hireService, IMapper mapper)
        {
            _unitOfWork = unitOfWork;
            _hireService = hireService;
            _mapper = mapper;
        }

        #region Query Methods

        /// <summary>
        /// Gets all locations owned by a user
        /// </summary>
        public async Task<IEnumerable<BusinessLocationDto>> GetOwnedLocationsAsync(Guid userId)
        {
            var locations = await _unitOfWork.BusinessLocations.GetOwnedByUserIdAsync(userId);
            
            var result = new List<BusinessLocationDto>();
            foreach (var loc in locations)
            {
                // Retrieve OwnerName for each location to pass into Mapper context
                var (_, ownerName) = await _unitOfWork.BusinessLocations.GetByIdWithOwnerAsync(loc.BusinessLocationId);
                
                var dto = _mapper.Map<BusinessLocationDto>(loc);
                dto.OwnerName = ownerName;
                result.Add(dto);
            }
            
            return result;
        }

        /// <summary>
        /// Gets all locations where user works (not owned)
        /// </summary>
        public async Task<IEnumerable<BusinessLocationDto>> GetWorkLocationsAsync(Guid userId)
        {
            var locations = await _unitOfWork.BusinessLocations.GetWorkLocationsByUserIdAsync(userId);
            
            var result = new List<BusinessLocationDto>();
            foreach (var loc in locations)
            {
                var (_, ownerName) = await _unitOfWork.BusinessLocations.GetByIdWithOwnerAsync(loc.BusinessLocationId);
                var dto = _mapper.Map<BusinessLocationDto>(loc);
                dto.OwnerName = ownerName;
                result.Add(dto);
            }
            
            return result;
        }

        #endregion

        #region Command Methods

        /// <summary>
        /// Creates a new location and assigns owner + optional employees
        /// </summary>
        public async Task<BusinessLocationDto> CreateLocationAsync(Guid userId, CreateLocationRequest request)
        {
            return await _unitOfWork.ExecuteResilientAsync(async _ =>
            {
                // Verify location name doesn't already exist for this owner
                var isExisted = await _unitOfWork.BusinessLocations.IsExistedByNameAsync(userId, request.Name);
                if (isExisted)
                {
                    throw new ConflictException(MessageKeys.LocationAlreadyExists);
                }

                // Create the location using mapper
                var location = _mapper.Map<BusinessLocation>(request);

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
                    await AssignEmployeesToLocationAsync(userId, createdLocation.BusinessLocationId, request.EmployeeIds);
                }

                await _unitOfWork.SaveChangesAsync();
                
                var (_, ownerName) = await _unitOfWork.BusinessLocations.GetByIdWithOwnerAsync(createdLocation.BusinessLocationId);
                   
                var dto = _mapper.Map<BusinessLocationDto>(createdLocation);
                dto.OwnerName = ownerName;
                
                return dto;
            });
        }

        /// <summary>
        /// Updates location status (active/inactive) - owner only
        /// </summary>
        public async Task<bool> UpdateLocationStatusAsync(Guid userId, int locationId, bool isActive)
        {
            var location = await ValidateOwnershipAndGetLocationAsync(userId, locationId);
            if (location == null)
                return false;

            location.IsActive = isActive;
            _unitOfWork.BusinessLocations.Update(location);
            await _unitOfWork.SaveChangesAsync();

            return true;
        }

        /// <summary>
        /// Updates location details - owner only
        /// </summary>
        public async Task<bool> UpdateLocationAsync(Guid userId, int locationId, UpdateLocationRequest request)
        {
            var location = await _unitOfWork.BusinessLocations.GetByIdAsync(locationId);
            if (location == null)
            {
                throw new NotFoundException(MessageKeys.NotFound);
            }

            var isOwner = await _unitOfWork.BusinessLocations.IsOwnerOfLocationAsync(userId, locationId);
            if (!isOwner)
                return false;

            // Check if name is being changed and if new name already exists for this owner
            if (request.Name != location.LocationName)
            {
                var isExisted = await _unitOfWork.BusinessLocations.IsExistedByNameAsync(userId, request.Name);
                if (isExisted)
                {
                    throw new ConflictException(MessageKeys.LocationAlreadyExists);
                }
            }

            // Update entity using Mapper
            _mapper.Map(request, location); 
            
            _unitOfWork.BusinessLocations.Update(location);
            await _unitOfWork.SaveChangesAsync();

            return true;
        }

        /// <summary>
        /// Adds employees to a location (owner only)
        /// </summary>
        public async Task<bool> AddEmployeesToLocationAsync(Guid ownerId, int locationId, List<Guid> employeeIds)
        {
            // Verify ownership
            var isOwner = await _unitOfWork.BusinessLocations.IsOwnerOfLocationAsync(ownerId, locationId);
            if (!isOwner)
                return false;

            // Validate location exists
            var location = await _unitOfWork.BusinessLocations.GetByIdAsync(locationId);
            if (location == null)
            {
                throw new NotFoundException(MessageKeys.NotFound);
            }

            await AssignEmployeesToLocationAsync(ownerId, locationId, employeeIds);
            await _unitOfWork.SaveChangesAsync();

            return true;
        }

        /// <summary>
        /// Get employees assigned to a location (owner only)
        /// </summary>
        public async Task<EmployeeSummaryListDto> GetEmployeesByLocationAsync(Guid userId, int locationId)
        {
            // Verify ownership
            var isOwner = await _unitOfWork.BusinessLocations.IsOwnerOfLocationAsync(userId, locationId);
            if (!isOwner)
            {
                throw new ForbiddenException(MessageKeys.Forbidden);
            }

            var employees = await _unitOfWork.BusinessLocations.GetEmployeesByLocationIdAsync(locationId);

            return new EmployeeSummaryListDto
            {
                Employees = _mapper.Map<List<EmployeeSummaryDto>>(employees)
            };
        }

        /// <summary>
        /// Deletes a location (soft delete) - owner only
        /// </summary>
        public async Task<bool> DeleteLocationAsync(Guid userId, int locationId)
        {
            var location = await ValidateOwnershipAndGetLocationAsync(userId, locationId);
            if (location == null)
                return false;

            location.DeletedAt = DateTime.UtcNow;
            _unitOfWork.BusinessLocations.Update(location);
            await _unitOfWork.SaveChangesAsync();

            return true;
        }

        #endregion

        #region Private Helper Methods

        /// <summary>
        /// Validates ownership and retrieves location
        /// </summary>
        /// <returns>Location if user is owner, null otherwise</returns>
        private async Task<BusinessLocation?> ValidateOwnershipAndGetLocationAsync(Guid userId, int locationId)
        {
            var isOwner = await _unitOfWork.BusinessLocations.IsOwnerOfLocationAsync(userId, locationId);
            if (!isOwner)
                return null;

            return await _unitOfWork.BusinessLocations.GetByIdAsync(locationId);
        }

        /// <summary>
        /// Validates and assigns employees to a location
        /// </summary>
        private async Task AssignEmployeesToLocationAsync(Guid ownerId, int locationId, IEnumerable<Guid> employeeIds)
        {
            // Validate employees using HireService
            var validationResult = await _hireService.ValidateEmployeesForAssignmentAsync(ownerId, employeeIds);

            // If any employees are not hired, throw BadRequestException
            if (!validationResult.AllValid)
            {
                throw new BadRequestException(
                    MessageKeys.EmployeesNotHired, 
                    new { invalidEmployeeIds = validationResult.InvalidEmployeeIds }
                );
            }

            // Check if any employees are already assigned to this location
            var assignedEmployeeIds = await _unitOfWork.BusinessLocations.GetAssignedEmployeeIdsAsync(locationId);
            var assignedEmployeeIdsSet = assignedEmployeeIds.ToHashSet();

            var alreadyAssignedIds = validationResult.ValidEmployeeIds
                .Where(empId => assignedEmployeeIdsSet.Contains(empId))
                .ToList();

            if (alreadyAssignedIds.Any())
            {
                throw new BadRequestException(
                    MessageKeys.EmployeesAlreadyAssigned,
                    new { alreadyAssignedEmployeeIds = alreadyAssignedIds }
                );
            }

            // All employees are valid and not assigned - batch create assignments
            foreach (var employeeId in validationResult.ValidEmployeeIds)
            {
                var employeeAssignment = new UserLocationAssignment
                {
                    UserId = employeeId,
                    BusinessLocationId = locationId,
                    IsOwner = false,
                    IsActive = true
                };
                await _unitOfWork.BusinessLocations.AddUserLocationAssignmentAsync(employeeAssignment);
            }
        }

        #endregion
    }
}
