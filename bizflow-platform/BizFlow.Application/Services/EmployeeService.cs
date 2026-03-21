using BizFlow.Application.Common.Constants;
using BizFlow.Application.Common.Exceptions;
using BizFlow.Application.DTOs.Employee;
using BizFlow.Application.Interfaces.Repositories;
using BizFlow.Application.Interfaces.Services;
using BizFlow.Domain.Entities;

namespace BizFlow.Application.Services
{
    public class EmployeeService : IEmployeeService
    {
        private readonly IEmployeeRepository _employeeRepository;
        private readonly INotificationService _notificationService;

        public EmployeeService(
            IEmployeeRepository employeeRepository,
            INotificationService notificationService)
        {
            _employeeRepository = employeeRepository;
            _notificationService = notificationService;
        }

        public async Task<IEnumerable<UserSearchResultDto>> SearchUserByContactAsync(Guid ownerId, string query)
        {
            if (string.IsNullOrWhiteSpace(query))
            {
                return Enumerable.Empty<UserSearchResultDto>();
            }

            return await _employeeRepository.SearchByContactAsync(ownerId, query.Trim());
        }

        public async Task<HireDto> InviteEmployeeAsync(Guid ownerId, Guid employeeId)
        {
            if (ownerId == employeeId)
            {
                throw new BadRequestException(MessageKeys.BadRequest);
            }

            var profileExists = await _employeeRepository.ProfileExistsAsync(employeeId);
            if (!profileExists)
            {
                throw new NotFoundException(MessageKeys.UserNotFound);
            }

            var existingHire = await _employeeRepository.GetOpenHireAsync(ownerId, employeeId);
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

            var created = await _employeeRepository.CreateHireAsync(hire);

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
            var hire = await _employeeRepository.GetActiveHireAsync(ownerId, employeeId);
            if (hire == null)
            {
                throw new NotFoundException(MessageKeys.UserNotFound);
            }

            var hasActiveAssignments = await _employeeRepository.HasActiveAssignmentsAsync(employeeId);
            if (hasActiveAssignments)
            {
                throw new BadRequestException(MessageKeys.EmployeeHasActiveAssignments);
            }

            hire.IsActive = false;
            hire.Status = "inactive";
            hire.EndAt = DateTime.UtcNow;

            await _employeeRepository.SaveChangesAsync();
            await _notificationService.NotifyEmployeeRemovedAsync(employeeId, string.Empty);
        }

        public async Task<IEnumerable<EmployeeInvitationDto>> GetPendingInvitationsAsync(Guid employeeId)
        {
            return await _employeeRepository.GetPendingInvitationsAsync(employeeId);
        }

        public async Task AcceptInvitationAsync(Guid employeeId, int hireId)
        {
            var invitation = await _employeeRepository.GetPendingInvitationByIdAsync(employeeId, hireId);
            if (invitation == null)
            {
                throw new NotFoundException(MessageKeys.NotFound);
            }

            invitation.IsActive = true;
            invitation.Status = "accepted";
            invitation.StartAt = DateTime.UtcNow;
            invitation.EndAt = null;
            await _employeeRepository.SaveChangesAsync();
        }

        public async Task RejectInvitationAsync(Guid employeeId, int hireId)
        {
            var invitation = await _employeeRepository.GetPendingInvitationByIdAsync(employeeId, hireId);
            if (invitation == null)
            {
                throw new NotFoundException(MessageKeys.NotFound);
            }

            invitation.Status = "rejected";
            invitation.IsActive = false;
            await _employeeRepository.SaveChangesAsync();
        }
    }
}
