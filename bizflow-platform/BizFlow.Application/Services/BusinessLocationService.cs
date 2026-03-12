using BizFlow.Application.DTOs.Hire;
using BizFlow.Application.DTOs.Location;
using BizFlow.Application.Common.Constants;
using BizFlow.Application.Common.Exceptions;
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

        public BusinessLocationService(IUnitOfWork unitOfWork, IHireService hireService, IMapper mapper)
        {
            _unitOfWork = unitOfWork;
            _hireService = hireService;
            _mapper = mapper;
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
            // RULE-LOC-05: Owner or assigned Employee can view
            var hasAccess = await _unitOfWork.BusinessLocations.HasAccessToLocationAsync(userId, locationId);
            if (!hasAccess)
                throw new ForbiddenException(MessageKeys.Forbidden);

            var detail = await _unitOfWork.BusinessLocations.GetLocationDetailByIdAsync(locationId);
            if (detail == null)
                throw new NotFoundException(MessageKeys.NotFound);

            return detail;
        }

        public async Task<EmployeeSummaryListDto> GetEmployeesByLocationAsync(Guid userId, int locationId)
        {
            await EnsureOwnershipAsync(userId, locationId);

            var employees = await _unitOfWork.BusinessLocations.GetEmployeesByLocationIdAsync(locationId);
            return new EmployeeSummaryListDto
            {
                Employees = _mapper.Map<List<EmployeeSummaryDto>>(employees)
            };
        }

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

        #endregion

        #region Command Methods

        public async Task<BusinessLocationDto> CreateLocationAsync(Guid userId, CreateLocationRequest request)
        {
            return await _unitOfWork.ExecuteResilientAsync(async _ =>
            {
                await EnsureLocationNameUniqueAsync(userId, request.Name);

                var location = _mapper.Map<BusinessLocation>(request);
                var created = await _unitOfWork.BusinessLocations.AddAsync(location);
                await _unitOfWork.SaveChangesAsync();

                var ownerAssignment = new UserLocationAssignment
                {
                    UserId = userId,
                    BusinessLocationId = created.BusinessLocationId,
                    IsOwner = true,
                    IsActive = true
                };
                await _unitOfWork.BusinessLocations.AddUserLocationAssignmentAsync(ownerAssignment);

                if (request.EmployeeIds is { Count: > 0 })
                    await AssignEmployeesInternalAsync(userId, created.BusinessLocationId, request.EmployeeIds);

                await _unitOfWork.SaveChangesAsync();

                var locations = await _unitOfWork.BusinessLocations.GetLocationsByUserAsync(userId, isOwner: true);
                return locations.First(l => l.Id == created.BusinessLocationId);
            });
        }

        public async Task UpdateLocationAsync(Guid userId, int locationId, UpdateLocationRequest request)
        {
            var location = await GetLocationOrThrowAsync(locationId);
            await EnsureOwnershipAsync(userId, locationId);

            if (request.Name != location.LocationName)
                await EnsureLocationNameUniqueAsync(userId, request.Name);

            _mapper.Map(request, location);
            _unitOfWork.BusinessLocations.Update(location);
            await _unitOfWork.SaveChangesAsync();
        }

        public async Task UpdateLocationStatusAsync(Guid userId, int locationId, bool isActive)
        {
            var location = await GetLocationOrThrowAsync(locationId);
            await EnsureOwnershipAsync(userId, locationId);

            location.IsActive = isActive;
            _unitOfWork.BusinessLocations.Update(location);
            await _unitOfWork.SaveChangesAsync();
        }

        public async Task AddEmployeesToLocationAsync(Guid ownerId, int locationId, List<Guid> employeeIds)
        {
            await GetLocationOrThrowAsync(locationId);
            await EnsureOwnershipAsync(ownerId, locationId);

            await AssignEmployeesInternalAsync(ownerId, locationId, employeeIds);
            await _unitOfWork.SaveChangesAsync();
        }

        public async Task RemoveEmployeeFromLocationAsync(Guid ownerId, int locationId, Guid employeeId)
        {
            await GetLocationOrThrowAsync(locationId);
            await EnsureOwnershipAsync(ownerId, locationId);

            await _unitOfWork.BusinessLocations.RemoveEmployeeFromLocationAsync(locationId, employeeId);
            await _unitOfWork.SaveChangesAsync();
        }

        public async Task DeleteLocationAsync(Guid userId, int locationId)
        {
            var location = await GetLocationOrThrowAsync(locationId);
            await EnsureOwnershipAsync(userId, locationId);

            // TODO: RULE-LOC-06 — When Order module exists, check for existing orders:
             //var hasOrders = await _unitOfWork.Orders.HasOrdersByLocationAsync(locationId);
            // if (hasOrders) → return warning + require confirm (see business-location-flow.md Section 6)

            location.DeletedAt = DateTime.UtcNow;
            _unitOfWork.BusinessLocations.Update(location);
            await _unitOfWork.SaveChangesAsync();
        }

        #endregion

        #region Private Helpers

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
                    IsActive = true
                });
            }
        }

        #endregion
    }
}
