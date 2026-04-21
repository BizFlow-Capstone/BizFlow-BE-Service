using BizFlow.Application.DTOs.Hire;
using BizFlow.Application.DTOs.Location;
using BizFlow.Application.Common.Constants;
using BizFlow.Application.Common.Exceptions;
using BizFlow.Application.Common.Interfaces;
using BizFlow.Application.Interfaces.Repositories;
using BizFlow.Application.Interfaces.Services;
using AutoMapper;
using BizFlow.Domain.Entities;

namespace BizFlow.Application.Services
{
    public class BusinessLocationService : IBusinessLocationService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IHireService _hireService;
        private readonly IMapper _mapper;
        private readonly IReferenceLabelService _labels;

        public BusinessLocationService(IUnitOfWork unitOfWork, IHireService hireService, IMapper mapper, IReferenceLabelService labels)
        {
            _unitOfWork = unitOfWork;
            _hireService = hireService;
            _mapper = mapper;
            _labels = labels;
        }

        #region Query Methods

        public async Task<IEnumerable<BusinessLocationDto>> GetOwnedLocationsAsync(Guid userId)
        {
            return await _unitOfWork.BusinessLocations.GetLocationsByUserAsync(userId, isOwner: true);
        }

        public async Task<IEnumerable<BusinessLocationDto>> GetWorkLocationsAsync(Guid userId)
        {
            return await _unitOfWork.BusinessLocations.GetLocationsByUserAsync(userId, isOwner: false);
        }

        public async Task<BusinessLocationDetailDto> GetLocationDetailAsync(Guid userId, int locationId)
        {
            // RULE-LOC-05 + RULE-LOC-07: check access first, then fetch detail.
            await ValidateLocationAccessAsync(userId, locationId);

            var detail = await _unitOfWork.BusinessLocations.GetLocationDetailByIdAsync(locationId);
            if (detail == null)
                throw new NotFoundException(MessageKeys.NotFound);

            var acceptedOption = _labels.ToOption(ReferenceCategory.HireStatus, "accepted");
            foreach (var employee in detail.Employees)
            {
                employee.Status = acceptedOption;
            }

            return detail;
        }

        public async Task<EmployeeSummaryListDto> GetEmployeesByLocationAsync(Guid userId, int locationId)
        {
            await GetLocationAsOwnerOrThrowAsync(userId, locationId);

            var employees = await _unitOfWork.BusinessLocations.GetEmployeesByLocationIdAsync(locationId);
            var dtos = _mapper.Map<List<EmployeeSummaryDto>>(employees);
            var acceptedOption = _labels.ToOption(ReferenceCategory.HireStatus, "accepted");
            foreach (var dto in dtos)
            {
                dto.Status = acceptedOption;
            }
            return new EmployeeSummaryListDto
            {
                Employees = dtos
            };
        }

        #endregion

        #region Command Methods

        public async Task<BusinessLocationDto> CreateLocationAsync(Guid userId, CreateLocationRequest request)
        {
            var createdId = await _unitOfWork.ExecuteResilientAsync(async _ =>
            {
                await EnsureLocationNameUniqueAsync(userId, request.Name);

                var location = _mapper.Map<BusinessLocation>(request);
                var created = await _unitOfWork.BusinessLocations.AddAsync(location);
                await _unitOfWork.SaveChangesAsync(); // flush to get BusinessLocationId

                var ownerAssignment = new UserLocationAssignment
                {
                    UserId = userId,
                    BusinessLocationId = created.BusinessLocationId,
                    IsOwner = true,
                    IsActive = true,
                    AssignedAt = DateTime.UtcNow,
                    UnassignedAt = null
                };
                await _unitOfWork.BusinessLocations.AddUserLocationAssignmentAsync(ownerAssignment);

                if (request.EmployeeIds is { Count: > 0 })
                    await AssignEmployeesInternalAsync(userId, created.BusinessLocationId, request.EmployeeIds);

                return created.BusinessLocationId;
                // ExecuteResilientAsync saves assignments + commits here
            });

            return await _unitOfWork.BusinessLocations.GetLocationDtoByUserAndIdAsync(userId, createdId)
                ?? throw new NotFoundException(MessageKeys.NotFound);
        }

        public async Task UpdateLocationAsync(Guid userId, int locationId, UpdateLocationRequest request)
        {
            var location = await GetLocationAsOwnerOrThrowAsync(userId, locationId);

            if (request.Name != location.LocationName)
                await EnsureLocationNameUniqueAsync(userId, request.Name);

            _mapper.Map(request, location);
            _unitOfWork.BusinessLocations.Update(location);
            await _unitOfWork.SaveChangesAsync();
        }

        public async Task UpdateLocationStatusAsync(Guid userId, int locationId, bool isActive)
        {
            var location = await GetLocationAsOwnerOrThrowAsync(userId, locationId);

            location.IsActive = isActive;
            _unitOfWork.BusinessLocations.Update(location);
            await _unitOfWork.SaveChangesAsync();
        }

        public async Task AddEmployeesToLocationAsync(Guid ownerId, int locationId, List<Guid> employeeIds)
        {
            await EnsureOwnershipAsync(ownerId, locationId);

            await _unitOfWork.ExecuteResilientAsync(async _ =>
            {
                await AssignEmployeesInternalAsync(ownerId, locationId, employeeIds);
            });
        }

        public async Task RemoveEmployeeFromLocationAsync(Guid ownerId, int locationId, Guid employeeId)
        {
            await EnsureOwnershipAsync(ownerId, locationId);
            await _unitOfWork.BusinessLocations.RemoveEmployeeFromLocationAsync(locationId, employeeId);
            await _unitOfWork.SaveChangesAsync();
        }

        public async Task DeleteLocationAsync(Guid userId, int locationId)
        {
            var location = await GetLocationAsOwnerOrThrowAsync(userId, locationId);

            var hasData = await _unitOfWork.BusinessLocations.HasRelatedDataAsync(locationId);

            if (hasData)
            {
                // Soft delete — location has products, imports, or employees
                location.DeletedAt = DateTime.UtcNow;
                _unitOfWork.BusinessLocations.Update(location);
            }
            else
            {
                // Hard delete — location is empty, no related data
                _unitOfWork.BusinessLocations.Delete(location);
            }

            await _unitOfWork.SaveChangesAsync();
        }

        #endregion

        #region Private Helpers

        /// <summary>
        /// RULE-LOC-07: Validates user access to location.
        /// Owner → full access. Employee → blocked when IsActive=false.
        /// Reusable for Product/Import services.
        /// </summary>
        public async Task ValidateLocationAccessAsync(Guid userId, int locationId)
        {
            var location = await GetLocationOrThrowAsync(locationId);

            var hasAccess = await _unitOfWork.BusinessLocations.HasAccessToLocationAsync(userId, locationId);
            if (!hasAccess)
                throw new ForbiddenException(MessageKeys.Forbidden);

            var isOwner = await _unitOfWork.BusinessLocations.IsOwnerOfLocationAsync(userId, locationId);
            if (!isOwner && location.IsActive == false)
                throw new ForbiddenException(MessageKeys.LocationInactive);
        }

        public async Task ValidateOwnerAsync(Guid userId, int locationId)
        {
            await GetLocationOrThrowAsync(locationId);
            await EnsureOwnershipAsync(userId, locationId);
        }

        /// <summary>
        /// Throws NotFoundException if location doesn't exist (or is soft-deleted).
        /// Reusable guard — call before any mutation.
        /// </summary>
        private async Task<BusinessLocation> GetLocationOrThrowAsync(int locationId)
        {
            var location = await _unitOfWork.BusinessLocations.GetByIdAsync(locationId);
            if (location == null)
                throw new NotFoundException(MessageKeys.NotFound);
            return location;
        }

        /// <summary>
        /// Throws ForbiddenException if user is not owner.
        /// Reusable guard — call before any owner-only operation.
        /// </summary>
        private async Task EnsureOwnershipAsync(Guid userId, int locationId)
        {
            var isOwner = await _unitOfWork.BusinessLocations.IsOwnerOfLocationAsync(userId, locationId);
            if (!isOwner)
                throw new ForbiddenException(MessageKeys.Forbidden);
        }

        /// <summary>
        /// Fetches location and verifies ownership in one call site.
        /// Consolidates the GetLocationOrThrowAsync + EnsureOwnershipAsync pattern.
        /// </summary>
        private async Task<BusinessLocation> GetLocationAsOwnerOrThrowAsync(Guid userId, int locationId)
        {
            var location = await GetLocationOrThrowAsync(locationId);
            await EnsureOwnershipAsync(userId, locationId);
            return location;
        }

        /// <summary>
        /// Throws ConflictException if location name already exists for this owner.
        /// </summary>
        private async Task EnsureLocationNameUniqueAsync(Guid userId, string name)
        {
            var exists = await _unitOfWork.BusinessLocations.IsExistedByNameAsync(userId, name);
            if (exists)
                throw new ConflictException(MessageKeys.LocationAlreadyExists);
        }

        /// <summary>
        /// Validates and assigns employees to a location.
        /// Throws BadRequestException if employees are not hired or already assigned.
        /// </summary>
        private async Task AssignEmployeesInternalAsync(Guid ownerId, int locationId, IEnumerable<Guid> employeeIds)
        {
            var validationResult = await _hireService.ValidateEmployeesForAssignmentAsync(ownerId, employeeIds);
            if (!validationResult.AllValid)
                throw new BadRequestException(
                    MessageKeys.EmployeesNotHired,
                    new { invalidEmployeeIds = validationResult.InvalidEmployeeIds });

            var assignedIds = (await _unitOfWork.BusinessLocations.GetAssignedEmployeeIdsAsync(locationId)).ToHashSet();
            var alreadyAssigned = validationResult.ValidEmployeeIds.Where(id => assignedIds.Contains(id)).ToList();
            if (alreadyAssigned.Any())
                throw new BadRequestException(
                    MessageKeys.EmployeesAlreadyAssigned,
                    new { alreadyAssignedEmployeeIds = alreadyAssigned });

            foreach (var employeeId in validationResult.ValidEmployeeIds)
            {
                await _unitOfWork.BusinessLocations.AddUserLocationAssignmentAsync(new UserLocationAssignment
                {
                    UserId = employeeId,
                    BusinessLocationId = locationId,
                    IsOwner = false,
                    IsActive = true,
                    AssignedAt = DateTime.UtcNow,
                    UnassignedAt = null
                });
            }
        }

        #endregion
    }
}
