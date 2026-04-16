using BizFlow.Application.Common.Constants;
using BizFlow.Application.Common.Exceptions;
using BizFlow.Application.DTOs.Employee;
using BizFlow.Application.Interfaces.Repositories;
using BizFlow.Application.Interfaces.Services;
using BizFlow.Domain.Entities;
using Microsoft.Extensions.Logging;

namespace BizFlow.Application.Services
{
    public class EmployeeService : IEmployeeService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly INotificationService _notificationService;
        private readonly ILogger<EmployeeService> _logger;

        public EmployeeService(
            IUnitOfWork unitOfWork,
            INotificationService notificationService,
            ILogger<EmployeeService> logger)
        {
            _unitOfWork = unitOfWork;
            _notificationService = notificationService;
            _logger = logger;
        }

        public async Task<IEnumerable<UserSearchResultDto>> SearchUserByContactAsync(Guid ownerId, string query)
        {
            if (string.IsNullOrWhiteSpace(query))
            {
                return Enumerable.Empty<UserSearchResultDto>();
            }

            return await _unitOfWork.Employees.SearchByContactAsync(ownerId, query.Trim());
        }

        public async Task<HireDto> InviteEmployeeAsync(Guid ownerId, Guid employeeId)
        {
            if (ownerId == employeeId)
            {
                throw new BadRequestException(MessageKeys.BadRequest);
            }

            var profileExists = await _unitOfWork.Employees.ProfileExistsAsync(employeeId);
            if (!profileExists)
            {
                throw new NotFoundException(MessageKeys.UserNotFound);
            }

            var existingHire = await _unitOfWork.Employees.GetOpenHireAsync(ownerId, employeeId);
            if (existingHire != null)
            {
                if (existingHire.Status == "pending" || existingHire.Status == "accepted")
                {
                    throw new ConflictException(MessageKeys.EmployeeAlreadyHired);
                }
            }

            var hire = new Hire
            {
                OwnerId = ownerId,
                EmployeeId = employeeId,
                IsActive = false,
                Status = "pending",
                InvitedAt = DateTime.UtcNow,
                StartAt = null,
                EndAt = null
            };

            var created = await _unitOfWork.Employees.CreateHireAsync(hire);

            await _notificationService.SendEmployeeInviteAsync(employeeId, string.Empty);

            return new HireDto
            {
                HireId = created.HireId,
                OwnerId = created.OwnerId,
                EmployeeId = created.EmployeeId,
                CreatedAt = created.InvitedAt,
                IsActive = created.IsActive == true
            };
        }

        public async Task RemoveEmployeeAsync(Guid ownerId, Guid employeeId)
        {
            var hire = await _unitOfWork.Employees.GetActiveHireAsync(ownerId, employeeId);
            if (hire == null)
            {
                throw new NotFoundException(MessageKeys.UserNotFound);
            }

            var hasActiveAssignments = await _unitOfWork.Employees.HasActiveAssignmentsAsync(employeeId);
            if (hasActiveAssignments)
            {
                throw new BadRequestException(MessageKeys.EmployeeHasActiveAssignments);
            }

            hire.IsActive = false;
            hire.Status = "inactive";
            hire.EndAt = DateTime.UtcNow;

            await _unitOfWork.SaveChangesAsync();

            // Deletion state must be persisted even if downstream notification dispatch fails.
            try
            {
                await _notificationService.NotifyEmployeeRemovedAsync(employeeId, string.Empty);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(
                    ex,
                    "NotifyEmployeeRemovedAsync failed after employee removal. ownerId={OwnerId}, employeeId={EmployeeId}",
                    ownerId,
                    employeeId);
            }
        }

        public async Task<IEnumerable<EmployeeInvitationDto>> GetPendingInvitationsAsync(Guid employeeId)
        {
            return await _unitOfWork.Employees.GetPendingInvitationsAsync(employeeId);
        }

        public async Task AcceptInvitationAsync(Guid employeeId, int hireId)
        {
            var invitation = await _unitOfWork.Employees.GetPendingInvitationByIdAsync(employeeId, hireId);
            if (invitation == null)
            {
                throw new NotFoundException(MessageKeys.NotFound);
            }

            invitation.IsActive = true;
            invitation.Status = "accepted";
            invitation.StartAt = DateTime.UtcNow;
            invitation.EndAt = null;
            await _unitOfWork.SaveChangesAsync();
        }

        public async Task RejectInvitationAsync(Guid employeeId, int hireId)
        {
            var invitation = await _unitOfWork.Employees.GetPendingInvitationByIdAsync(employeeId, hireId);
            if (invitation == null)
            {
                throw new NotFoundException(MessageKeys.NotFound);
            }

            invitation.Status = "rejected";
            invitation.IsActive = false;
            await _unitOfWork.SaveChangesAsync();
        }
    }
}
