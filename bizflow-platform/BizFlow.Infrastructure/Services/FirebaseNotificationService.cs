using BizFlow.Application.Common.Constants;
using BizFlow.Application.Common.Exceptions;
using BizFlow.Application.Common.Models;
using BizFlow.Application.DTOs.Notification;
using BizFlow.Application.Interfaces.Services;
using BizFlow.Domain.Entities;
using BizFlow.Infrastructure.DataContext;
using FirebaseAdmin;
using FirebaseAdmin.Messaging;
using Google.Apis.Auth.OAuth2;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace BizFlow.Infrastructure.Services
{
    public class FirebaseNotificationService : INotificationService
    {
        private static readonly HashSet<string> SupportedNavigationActionTypes = new(StringComparer.OrdinalIgnoreCase)
        {
            "NAVIGATE",
            "NAVIGATE_TO_SCREEN"
        };

        private static readonly Dictionary<string, string> SupportedTargetScreenAliases = new(StringComparer.OrdinalIgnoreCase)
        {
            ["EmployeeInvitationsPage"] = "EmployeeInvitationsPage",
            ["employeeinvitations"] = "EmployeeInvitationsPage",
            ["employeeinvitationspage"] = "EmployeeInvitationsPage",
            ["SubscriptionPlansPage"] = "SubscriptionPlansPage",
            ["subscriptionplans"] = "SubscriptionPlansPage",
            ["subscriptionplanspage"] = "SubscriptionPlansPage",
            ["premiumpaymentpage"] = "SubscriptionPlansPage",
            ["buypackagepage"] = "SubscriptionPlansPage"
        };

        private readonly BizFlowDbContext _context;
        private readonly ILogger<FirebaseNotificationService> _logger;
        private readonly IConfiguration _configuration;
        private readonly IPublishEndpoint? _publishEndpoint;
        private readonly INotificationRealtimePublisher? _realtimePublisher;

        public FirebaseNotificationService(
            BizFlowDbContext context,
            ILogger<FirebaseNotificationService> logger,
            IConfiguration configuration,
            IPublishEndpoint? publishEndpoint = null,
            INotificationRealtimePublisher? realtimePublisher = null)
        {
            _context = context;
            _logger = logger;
            _configuration = configuration;
            _publishEndpoint = publishEndpoint;
            _realtimePublisher = realtimePublisher;
        }

        public async Task RegisterDeviceTokenAsync(Guid userId, string token, string? deviceName, string platform)
        {
            if (string.IsNullOrWhiteSpace(token))
            {
                return;
            }

            var normalizedToken = token.Trim();
            if (normalizedToken.Length == 0)
            {
                return;
            }

            var now = DateTime.UtcNow;
            var normalizedPlatform = string.IsNullOrWhiteSpace(platform)
                ? "Unknown"
                : platform.Trim();
            var normalizedDeviceName = string.IsNullOrWhiteSpace(deviceName)
                ? null
                : deviceName.Trim();

            var strategy = _context.Database.CreateExecutionStrategy();
            await strategy.ExecuteAsync(async () =>
            {
                await using var transaction = await _context.Database.BeginTransactionAsync();

                await _context.DeviceTokens
                    .Where(deviceToken => deviceToken.Token == normalizedToken && deviceToken.IsActive == true)
                    .ExecuteUpdateAsync(setters => setters
                        .SetProperty(deviceToken => deviceToken.IsActive, false)
                        .SetProperty(deviceToken => deviceToken.LastUsedAt, now));

                var updatedRows = await _context.DeviceTokens
                    .Where(deviceToken => deviceToken.ProfileId == userId && deviceToken.Token == normalizedToken)
                    .ExecuteUpdateAsync(setters => setters
                        .SetProperty(deviceToken => deviceToken.IsActive, true)
                        .SetProperty(deviceToken => deviceToken.LastUsedAt, now)
                        .SetProperty(deviceToken => deviceToken.DeviceName, normalizedDeviceName)
                        .SetProperty(deviceToken => deviceToken.Platform, normalizedPlatform));

                if (updatedRows == 0)
                {
                    _context.DeviceTokens.Add(new DeviceToken
                    {
                        DeviceTokenId = Guid.NewGuid(),
                        ProfileId = userId,
                        Token = normalizedToken,
                        DeviceName = normalizedDeviceName,
                        Platform = normalizedPlatform,
                        RegisteredAt = now,
                        LastUsedAt = now,
                        IsActive = true
                    });

                    try
                    {
                        await _context.SaveChangesAsync();
                    }
                    catch (DbUpdateException)
                    {
                        await _context.DeviceTokens
                            .Where(deviceToken => deviceToken.ProfileId == userId && deviceToken.Token == normalizedToken)
                            .ExecuteUpdateAsync(setters => setters
                                .SetProperty(deviceToken => deviceToken.IsActive, true)
                                .SetProperty(deviceToken => deviceToken.LastUsedAt, now)
                                .SetProperty(deviceToken => deviceToken.DeviceName, normalizedDeviceName)
                                .SetProperty(deviceToken => deviceToken.Platform, normalizedPlatform));
                    }
                }

                await transaction.CommitAsync();
            });
        }

        public async Task UnregisterDeviceTokenAsync(Guid userId, string token)
        {
            if (string.IsNullOrWhiteSpace(token))
            {
                return;
            }

            var existing = await _context.DeviceTokens
                .FirstOrDefaultAsync(deviceToken => deviceToken.ProfileId == userId && deviceToken.Token == token.Trim());

            if (existing == null)
            {
                return;
            }

            existing.IsActive = false;
            existing.LastUsedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
        }

        public async Task SendToAllDevicesAsync(Guid userId, string title, string body)
        {
            var firebaseMessaging = GetFirebaseMessaging();
            if (firebaseMessaging == null)
            {
                _logger.LogWarning("Firebase messaging unavailable. Skip SendToAllDevicesAsync.");
                return;
            }

            var tokens = await GetActiveTokensByProfileIdAsync(userId);
            if (tokens.Count == 0)
            {
                return;
            }

            foreach (var tokenBatch in tokens.Chunk(500))
            {
                var message = new MulticastMessage
                {
                    Tokens = tokenBatch.ToList(),
                    Notification = new Notification
                    {
                        Title = title,
                        Body = body
                    }
                };

                try
                {
                    var result = await firebaseMessaging.SendEachForMulticastAsync(message);
                    _logger.LogInformation(
                        "SendToAllDevicesAsync userId={UserId}, success={Success}, failure={Failure}",
                        userId,
                        result.SuccessCount,
                        result.FailureCount);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex,
                        "SendToAllDevicesAsync failed for userId={UserId} and batchSize={BatchSize}",
                        userId,
                        tokenBatch.Length);
                }
            }
        }

        public async Task SendSilentNotificationAsync(Guid userId, Dictionary<string, string> data)
        {
            var firebaseMessaging = GetFirebaseMessaging();
            if (firebaseMessaging == null)
            {
                _logger.LogWarning("Firebase messaging unavailable. Skip SendSilentNotificationAsync.");
                return;
            }

            var tokens = await GetActiveTokensByProfileIdAsync(userId);
            if (tokens.Count == 0)
            {
                return;
            }

            foreach (var tokenBatch in tokens.Chunk(500))
            {
                var message = new MulticastMessage
                {
                    Tokens = tokenBatch.ToList(),
                    Data = data,
                    Android = new AndroidConfig
                    {
                        Priority = Priority.High
                    },
                    Apns = new ApnsConfig
                    {
                        Aps = new Aps
                        {
                            ContentAvailable = true
                        }
                    }
                };

                try
                {
                    var result = await firebaseMessaging.SendEachForMulticastAsync(message);
                    _logger.LogInformation(
                        "SendSilentNotificationAsync userId={UserId}, success={Success}, failure={Failure}",
                        userId,
                        result.SuccessCount,
                        result.FailureCount);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex,
                        "SendSilentNotificationAsync failed for userId={UserId} and batchSize={BatchSize}",
                        userId,
                        tokenBatch.Length);
                }
            }
        }

        public async Task SendEmployeeInviteAsync(Guid employeeId, string ownerName)
        {
            var employeeName = await _context.Profiles
                .AsNoTracking()
                .Where(profile => profile.ProfileId == employeeId)
                .Select(profile => profile.FullName)
                .FirstOrDefaultAsync();

            var normalizedOwnerName = string.IsNullOrWhiteSpace(ownerName) ? "BizFlow" : ownerName.Trim();
            var normalizedEmployeeName = string.IsNullOrWhiteSpace(employeeName) ? "bạn" : employeeName.Trim();

            var templateData = new Dictionary<string, string>();
            templateData["OwnerName"] = normalizedOwnerName;
            templateData["BusinessName"] = normalizedOwnerName;
            templateData["UserName"] = normalizedEmployeeName;

            await CreateDispatchAsync(Guid.Empty, new CreateNotificationDispatchRequest
            {
                EventCode = "EMPLOYEE_INVITE",
                NotificationType = "INVITE_EMPLOYEE",
                TemplateData = templateData,
                ActionType = "NAVIGATE",
                TargetScreen = "EmployeeInvitationsPage",
                SendToAllUsers = false,
                RecipientUserIds = new List<Guid> { employeeId }
            });
        }

        public async Task NotifyEmployeeRemovedAsync(Guid employeeId, string businessName)
        {
            var data = new Dictionary<string, string>
            {
                ["BusinessName"] = businessName
            };

            var hasActiveTemplate = await _context.NotificationTemplates
                .AsNoTracking()
                .AnyAsync(notificationTemplate =>
                    notificationTemplate.EventCode == "EMPLOYEE_REMOVED" && notificationTemplate.IsActive);

            await CreateDispatchAsync(Guid.Empty, new CreateNotificationDispatchRequest
            {
                EventCode = "EMPLOYEE_REMOVED",
                NotificationType = "EMPLOYEE_REMOVED",
                Title = hasActiveTemplate ? null : "Thông báo nhân sự",
                Content = hasActiveTemplate
                    ? null
                    : (string.IsNullOrWhiteSpace(businessName)
                        ? "Bạn không còn làm việc tại địa điểm kinh doanh này."
                        : $"Bạn không còn làm việc tại {businessName}."),
                TemplateData = data,
                SendToAllUsers = false,
                RecipientUserIds = new List<Guid> { employeeId }
            });
        }

        public Task<NotificationActionCatalogDto> GetActionCatalogAsync()
        {
            return Task.FromResult(new NotificationActionCatalogDto
            {
                ActionTypes = new List<NotificationActionTypeDto>
                {
                    new()
                    {
                        Code = "NONE",
                        DisplayName = "Không điều hướng",
                        Description = "Thông báo chỉ để đọc nội dung, không mở màn hình đích."
                    },
                    new()
                    {
                        Code = "NAVIGATE",
                        DisplayName = "Điều hướng",
                        Description = "Mở màn hình đích khi người dùng nhấn thông báo. Ưu tiên actionPayloadJson.route nếu có."
                    },
                    new()
                    {
                        Code = "NAVIGATE_TO_SCREEN",
                        DisplayName = "Điều hướng (legacy)",
                        Description = "Tương thích ngược với dữ liệu cũ, FE xử lý như NAVIGATE."
                    }
                },
                Targets = new List<NotificationTargetDto>
                {
                    new()
                    {
                        Code = "EMPLOYEE_INVITATIONS",
                        TargetScreen = "EmployeeInvitationsPage",
                        MobileRoute = "/employee-invitations",
                        WebRoute = "/employees/invitations",
                        Aliases = new List<string>
                        {
                            "employeeinvitations",
                            "employeeinvitationspage"
                        },
                        PayloadExampleJson = "{\"route\":\"/employee-invitations\"}"
                    },
                    new()
                    {
                        Code = "SUBSCRIPTION_PLANS",
                        TargetScreen = "SubscriptionPlansPage",
                        MobileRoute = "/subscription-plans",
                        WebRoute = "/billing/subscription-plans",
                        Aliases = new List<string>
                        {
                            "subscriptionplans",
                            "subscriptionplanspage",
                            "premiumpaymentpage",
                            "buypackagepage"
                        },
                        PayloadExampleJson = "{\"route\":\"/subscription-plans\"}"
                    }
                },
                Triggers = new List<NotificationTriggerDto>
                {
                    new()
                    {
                        EventCode = "EMPLOYEE_INVITE",
                        Feature = "Employee management",
                        TriggerDescription = "Tự động gửi khi chủ doanh nghiệp mời nhân viên mới."
                    },
                    new()
                    {
                        EventCode = "EMPLOYEE_REMOVED",
                        Feature = "Employee management",
                        TriggerDescription = "Tự động gửi khi chủ doanh nghiệp xoá/đuổi nhân viên khỏi địa điểm."
                    },
                    new()
                    {
                        EventCode = "SYSTEM_ANNOUNCEMENT",
                        Feature = "Admin broadcast",
                        TriggerDescription = "Admin gửi thông báo hệ thống thủ công từ màn Campaigns."
                    },
                    new()
                    {
                        EventCode = "PROMOTION",
                        Feature = "Marketing campaign",
                        TriggerDescription = "Admin gửi chiến dịch khuyến mãi thủ công từ màn Campaigns."
                    },
                    new()
                    {
                        EventCode = "SUBSCRIPTION_ACTIVATED",
                        Feature = "Subscription",
                        TriggerDescription = "Dùng cho luồng tự động khi gói dịch vụ được kích hoạt."
                    },
                    new()
                    {
                        EventCode = "SUBSCRIPTION_CANCELLED",
                        Feature = "Subscription",
                        TriggerDescription = "Dùng cho luồng tự động khi gói dịch vụ bị huỷ."
                    }
                }
            });
        }

        public async Task<IEnumerable<NotificationTemplateDto>> GetTemplatesAsync()
        {
            var templates = await _context.NotificationTemplates
                .AsNoTracking()
                .OrderBy(notificationTemplate => notificationTemplate.EventCode)
                .ToListAsync();

            return templates.Select(MapTemplate);
        }

        public async Task<NotificationTemplateDto?> GetTemplateByEventCodeAsync(string eventCode)
        {
            var normalizedEventCode = NormalizeEventCode(eventCode);

            var template = await _context.NotificationTemplates
                .AsNoTracking()
                .FirstOrDefaultAsync(notificationTemplate => notificationTemplate.EventCode == normalizedEventCode);

            return template == null ? null : MapTemplate(template);
        }

        public async Task<NotificationTemplateDto> UpsertTemplateAsync(string eventCode, UpsertNotificationTemplateRequest request)
        {
            var normalizedEventCode = NormalizeEventCode(eventCode);
            ValidateTemplateRequest(request);

            var template = await _context.NotificationTemplates
                .FirstOrDefaultAsync(notificationTemplate => notificationTemplate.EventCode == normalizedEventCode);

            if (template == null)
            {
                template = new NotificationTemplate
                {
                    NotificationTemplateId = Guid.NewGuid(),
                    EventCode = normalizedEventCode,
                    NotificationType = request.NotificationType.Trim(),
                    TitleTemplate = request.TitleTemplate.Trim(),
                    ContentTemplate = request.ContentTemplate.Trim(),
                    DefaultActionType = NormalizeNullable(request.DefaultActionType),
                    DefaultTargetScreen = NormalizeNullable(request.DefaultTargetScreen),
                    DefaultActionPayloadJson = NormalizeJsonOrNull(request.DefaultActionPayloadJson),
                    IsActive = request.IsActive,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                _context.NotificationTemplates.Add(template);
            }
            else
            {
                template.NotificationType = request.NotificationType.Trim();
                template.TitleTemplate = request.TitleTemplate.Trim();
                template.ContentTemplate = request.ContentTemplate.Trim();
                template.DefaultActionType = NormalizeNullable(request.DefaultActionType);
                template.DefaultTargetScreen = NormalizeNullable(request.DefaultTargetScreen);
                template.DefaultActionPayloadJson = NormalizeJsonOrNull(request.DefaultActionPayloadJson);
                template.IsActive = request.IsActive;
                template.UpdatedAt = DateTime.UtcNow;
            }

            await _context.SaveChangesAsync();
            return MapTemplate(template);
        }

        public async Task<NotificationTemplateDto> ToggleTemplateAsync(string eventCode, bool isActive)
        {
            var normalizedEventCode = NormalizeEventCode(eventCode);

            var template = await _context.NotificationTemplates
                .FirstOrDefaultAsync(notificationTemplate => notificationTemplate.EventCode == normalizedEventCode);

            if (template == null)
            {
                throw new NotFoundException(MessageKeys.NotFound, normalizedEventCode);
            }

            template.IsActive = isActive;
            template.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return MapTemplate(template);
        }

        public async Task<NotificationDispatchDto> CreateDispatchAsync(Guid createdByUserId, CreateNotificationDispatchRequest request)
        {
            if (request == null)
            {
                throw new BadRequestException(MessageKeys.BadRequest);
            }

            var now = DateTime.UtcNow;
            NotificationTemplate? template = null;
            Dictionary<string, string> templateData = request.TemplateData ?? new Dictionary<string, string>();

            if (!string.IsNullOrWhiteSpace(request.EventCode))
            {
                var normalizedEventCode = NormalizeEventCode(request.EventCode);
                template = await _context.NotificationTemplates
                    .FirstOrDefaultAsync(notificationTemplate => notificationTemplate.EventCode == normalizedEventCode && notificationTemplate.IsActive);
            }

            var notificationType = NormalizeNullable(request.NotificationType)
                ?? template?.NotificationType
                ?? throw new BadRequestException(MessageKeys.BadRequest, new { field = "notificationType" });

            var title = NormalizeNullable(request.Title)
                ?? (template != null ? RenderTemplate(template.TitleTemplate, templateData) : null)
                ?? throw new BadRequestException(MessageKeys.BadRequest, new { field = "title" });

            var content = NormalizeNullable(request.Content)
                ?? (template != null ? RenderTemplate(template.ContentTemplate, templateData) : null)
                ?? throw new BadRequestException(MessageKeys.BadRequest, new { field = "content" });

            var priority = NormalizePriority(request.Priority);

            var actionType = NormalizeNullable(request.ActionType) ?? template?.DefaultActionType;
            var targetScreen = NormalizeNullable(request.TargetScreen) ?? template?.DefaultTargetScreen;
            var actionPayloadJson = NormalizeJsonOrNull(request.ActionPayloadJson) ?? template?.DefaultActionPayloadJson;
            var dataJson = NormalizeJsonOrNull(request.DataJson);

            ValidateActionConfiguration(actionType, targetScreen, actionPayloadJson, "actionType", "targetScreen", "actionPayloadJson");
            targetScreen = CanonicalizeTargetScreen(targetScreen);

            var recipientUserIds = await ResolveRecipientUserIdsAsync(request.SendToAllUsers, request.RecipientUserIds);

            var dispatch = new NotificationDispatch
            {
                NotificationTemplateId = template?.NotificationTemplateId,
                NotificationType = notificationType,
                Priority = priority,
                Title = title,
                Content = content,
                DataJson = dataJson,
                ActionType = actionType,
                TargetScreen = targetScreen,
                ActionPayloadJson = actionPayloadJson,
                RecipientScope = request.SendToAllUsers ? "ALL_USERS" : "SPECIFIC_USERS",
                RecipientUserIdsJson = JsonSerializer.Serialize(recipientUserIds),
                ScheduledAt = request.ScheduledAt,
                Status = "PENDING",
                CreatedByUserId = createdByUserId == Guid.Empty ? null : createdByUserId,
                CreatedAt = now,
                UpdatedAt = now
            };

            _context.NotificationDispatches.Add(dispatch);
            await _context.SaveChangesAsync();

            if (!request.ScheduledAt.HasValue || request.ScheduledAt.Value <= now)
            {
                await EnqueueDispatchAsync(dispatch.NotificationDispatchId, CancellationToken.None);
            }

            return MapDispatch(dispatch);
        }

        public async Task ProcessDueDispatchesAsync(CancellationToken cancellationToken = default)
        {
            var now = DateTime.UtcNow;

            var pendingDispatches = await _context.NotificationDispatches
                .Where(dispatch => dispatch.Status == "PENDING" && dispatch.ScheduledAt != null && dispatch.ScheduledAt <= now)
                .OrderBy(dispatch => dispatch.ScheduledAt)
                .Take(100)
                .ToListAsync(cancellationToken);

            foreach (var dispatch in pendingDispatches)
            {
                await EnqueueDispatchAsync(dispatch.NotificationDispatchId, cancellationToken);
            }

            if (pendingDispatches.Count > 0)
            {
                _logger.LogInformation("Enqueued {Count} scheduled notification dispatches.", pendingDispatches.Count);
            }
        }

        public async Task ProcessDispatchAsync(long dispatchId, CancellationToken cancellationToken = default)
        {
            var dispatch = await _context.NotificationDispatches
                .FirstOrDefaultAsync(entity => entity.NotificationDispatchId == dispatchId, cancellationToken);

            if (dispatch == null)
            {
                return;
            }

            if (dispatch.Status == "COMPLETED")
            {
                return;
            }

            if (dispatch.ScheduledAt.HasValue && dispatch.ScheduledAt.Value > DateTime.UtcNow)
            {
                return;
            }

            var recipientUserIds = ParseRecipientUserIds(dispatch.RecipientUserIdsJson);
            await ExecuteDispatchAsync(dispatch, recipientUserIds, cancellationToken);

            await _context.SaveChangesAsync(cancellationToken);
        }

        public async Task ProcessNotificationOutboxAsync(CancellationToken cancellationToken = default)
        {
            var outboxRows = await _context.NotificationOutboxMessages
                .Where(message => message.Status == "PENDING" || (message.Status == "FAILED" && message.RetryCount < 5))
                .OrderBy(message => message.CreatedAt)
                .Take(100)
                .ToListAsync(cancellationToken);

            foreach (var message in outboxRows)
            {
                try
                {
                    var payload = JsonDocument.Parse(message.PayloadJson);
                    var dispatchId = payload.RootElement.GetProperty("dispatchId").GetInt64();

                    if (_publishEndpoint != null)
                    {
                        await _publishEndpoint.Publish(new NotificationDispatchRequestedEvent
                        {
                            DispatchId = dispatchId,
                            RequestedAt = DateTime.UtcNow
                        }, cancellationToken);
                    }
                    else
                    {
                        await ProcessDispatchAsync(dispatchId, cancellationToken);
                    }

                    message.Status = "PROCESSED";
                    message.ProcessedAt = DateTime.UtcNow;
                    message.LastAttemptAt = DateTime.UtcNow;
                    message.LastError = null;
                }
                catch (Exception ex)
                {
                    message.Status = "FAILED";
                    message.RetryCount += 1;
                    message.LastAttemptAt = DateTime.UtcNow;
                    message.LastError = ex.Message;
                }
            }

            if (outboxRows.Count > 0)
            {
                await _context.SaveChangesAsync(cancellationToken);
            }
        }

        public async Task ArchiveExpiredNotificationsAsync(CancellationToken cancellationToken = default)
        {
            await _context.Database.ExecuteSqlRawAsync(@"
INSERT INTO UserNotificationsArchive
(UserNotificationId, UserId, NotificationId, NotificationType, Priority, Title, Content, ActionType, TargetScreen, ActionPayloadJson, DeliveryStatus, CreatedAt, SentAt, ReadAt, ErrorMessage)
SELECT UserNotificationId, UserId, NotificationId, NotificationType, Priority, Title, Content, ActionType, TargetScreen, ActionPayloadJson, DeliveryStatus, CreatedAt, SentAt, ReadAt, ErrorMessage
FROM UserNotifications
WHERE CreatedAt < DATE_SUB(UTC_TIMESTAMP(), INTERVAL 180 DAY)
AND UserNotificationId NOT IN (SELECT UserNotificationId FROM UserNotificationsArchive);", cancellationToken);

            await _context.Database.ExecuteSqlRawAsync(@"
DELETE FROM UserNotifications
WHERE CreatedAt < DATE_SUB(UTC_TIMESTAMP(), INTERVAL 180 DAY);", cancellationToken);
        }

        public async Task<PaginatedResponse<NotificationDispatchDto>> GetDispatchesAsync(NotificationDispatchQueryParams query)
        {
            var pageNumber = query.PageNumber ?? 1;
            var pageSize = Math.Min(query.PageSize ?? 20, 100);

            var dispatchQuery = _context.NotificationDispatches.AsNoTracking().AsQueryable();

            if (!string.IsNullOrWhiteSpace(query.Status))
            {
                var normalizedStatus = query.Status.Trim().ToUpperInvariant();
                dispatchQuery = dispatchQuery.Where(dispatch => dispatch.Status == normalizedStatus);
            }

            if (query.FromDate.HasValue)
            {
                dispatchQuery = dispatchQuery.Where(dispatch => dispatch.CreatedAt >= query.FromDate.Value);
            }

            if (query.ToDate.HasValue)
            {
                dispatchQuery = dispatchQuery.Where(dispatch => dispatch.CreatedAt <= query.ToDate.Value);
            }

            var totalCount = await dispatchQuery.CountAsync();

            var items = await dispatchQuery
                .OrderByDescending(dispatch => dispatch.CreatedAt)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return new PaginatedResponse<NotificationDispatchDto>(items.Select(MapDispatch), totalCount, pageNumber, pageSize);
        }

        public async Task<PaginatedResponse<UserNotificationDto>> GetUserNotificationsAsync(Guid userId, NotificationQueryParams query)
        {
            var pageNumber = query.PageNumber ?? 1;
            var pageSize = Math.Min(query.PageSize ?? 20, 100);

            var notificationQuery = _context.UserNotifications
                .AsNoTracking()
                .Where(userNotification => userNotification.UserId == userId);

            if (query.UnreadOnly == true)
            {
                notificationQuery = notificationQuery.Where(userNotification => userNotification.ReadAt == null);
            }

            if (!string.IsNullOrWhiteSpace(query.NotificationType))
            {
                var normalizedType = query.NotificationType.Trim().ToUpperInvariant();
                notificationQuery = notificationQuery.Where(userNotification => userNotification.NotificationType == normalizedType);
            }

            if (query.FromDate.HasValue)
            {
                notificationQuery = notificationQuery.Where(userNotification => userNotification.CreatedAt >= query.FromDate.Value);
            }

            if (query.ToDate.HasValue)
            {
                notificationQuery = notificationQuery.Where(userNotification => userNotification.CreatedAt <= query.ToDate.Value);
            }

            var totalCount = await notificationQuery.CountAsync();

            var items = await notificationQuery
                .OrderByDescending(userNotification => userNotification.CreatedAt)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return new PaginatedResponse<UserNotificationDto>(items.Select(MapUserNotification), totalCount, pageNumber, pageSize);
        }

        public async Task<UserNotificationDto?> GetUserNotificationDetailAsync(Guid userId, long userNotificationId)
        {
            var userNotification = await _context.UserNotifications
                .AsNoTracking()
                .FirstOrDefaultAsync(notification => notification.UserId == userId && notification.UserNotificationId == userNotificationId);

            return userNotification == null ? null : MapUserNotification(userNotification);
        }

        public async Task<int> GetUnreadCountAsync(Guid userId)
        {
            return await _context.UserNotifications
                .AsNoTracking()
                .CountAsync(userNotification => userNotification.UserId == userId && userNotification.ReadAt == null);
        }

        public async Task<bool> MarkAsReadAsync(Guid userId, long userNotificationId)
        {
            var updatedRows = await _context.UserNotifications
                .Where(userNotification => userNotification.UserId == userId && userNotification.UserNotificationId == userNotificationId && userNotification.ReadAt == null)
                .ExecuteUpdateAsync(setters => setters.SetProperty(userNotification => userNotification.ReadAt, DateTime.UtcNow));

            return updatedRows > 0;
        }

        public async Task<int> MarkAllAsReadAsync(Guid userId)
        {
            return await _context.UserNotifications
                .Where(userNotification => userNotification.UserId == userId && userNotification.ReadAt == null)
                .ExecuteUpdateAsync(setters => setters.SetProperty(userNotification => userNotification.ReadAt, DateTime.UtcNow));
        }

        private async Task ExecuteDispatchAsync(NotificationDispatch dispatch, List<Guid> recipientUserIds, CancellationToken cancellationToken)
        {
            try
            {
                dispatch.Status = "PROCESSING";
                dispatch.UpdatedAt = DateTime.UtcNow;

                if (recipientUserIds.Count == 0)
                {
                    dispatch.Status = "FAILED";
                    dispatch.ErrorMessage = "No recipients";
                    dispatch.UpdatedAt = DateTime.UtcNow;
                    return;
                }

                var now = DateTime.UtcNow;
                var notificationRecord = new NotificationRecord
                {
                    NotificationId = Guid.NewGuid(),
                    NotificationType = dispatch.NotificationType,
                    Priority = dispatch.Priority,
                    Title = dispatch.Title,
                    Content = dispatch.Content,
                    ActionType = dispatch.ActionType,
                    TargetScreen = dispatch.TargetScreen,
                    ActionPayloadJson = dispatch.ActionPayloadJson,
                    DataJson = dispatch.DataJson,
                    CreatedAt = now
                };

                _context.Notifications.Add(notificationRecord);

                var notifications = recipientUserIds.Select(userId => new UserNotification
                {
                    UserId = userId,
                    NotificationId = notificationRecord.NotificationId,
                    NotificationType = dispatch.NotificationType,
                    Priority = dispatch.Priority,
                    Title = dispatch.Title,
                    Content = dispatch.Content,
                    ActionType = dispatch.ActionType,
                    TargetScreen = dispatch.TargetScreen,
                    ActionPayloadJson = dispatch.ActionPayloadJson,
                    DeliveryStatus = "SENT",
                    CreatedAt = now,
                    SentAt = now,
                    ReadAt = null
                }).ToList();

                _context.UserNotifications.AddRange(notifications);
                await _context.SaveChangesAsync(cancellationToken);

                foreach (var notification in notifications)
                {
                    var pushData = BuildPushData(dispatch, notification.UserNotificationId);
                    var pushError = await SendUserNotificationPushAsync(notification.UserId, dispatch.Title, dispatch.Content, dispatch.Priority, pushData, cancellationToken);

                    if (_realtimePublisher != null)
                    {
                        try
                        {
                            await _realtimePublisher.PublishUserNotificationAsync(notification.UserId, MapUserNotification(notification), cancellationToken);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogWarning(ex, "SignalR publish failed for UserNotificationId={UserNotificationId}", notification.UserNotificationId);
                        }
                    }

                    if (!string.IsNullOrWhiteSpace(pushError))
                    {
                        notification.ErrorMessage = pushError;
                    }
                }

                dispatch.Status = "COMPLETED";
                dispatch.SentAt = DateTime.UtcNow;
                dispatch.UpdatedAt = DateTime.UtcNow;
            }
            catch (Exception ex)
            {
                dispatch.Status = "FAILED";
                dispatch.ErrorMessage = ex.Message;
                dispatch.UpdatedAt = DateTime.UtcNow;
                _logger.LogError(ex, "ExecuteDispatchAsync failed for dispatchId={DispatchId}", dispatch.NotificationDispatchId);
            }
        }

        private async Task EnqueueDispatchAsync(long dispatchId, CancellationToken cancellationToken)
        {
            var markedRows = await _context.NotificationDispatches
                .Where(dispatch => dispatch.NotificationDispatchId == dispatchId && dispatch.Status == "PENDING")
                .ExecuteUpdateAsync(setters => setters
                    .SetProperty(dispatch => dispatch.Status, "PROCESSING")
                    .SetProperty(dispatch => dispatch.UpdatedAt, DateTime.UtcNow), cancellationToken);

            if (markedRows == 0)
            {
                return;
            }

            _context.NotificationOutboxMessages.Add(new NotificationOutboxMessage
            {
                EventType = nameof(NotificationDispatchRequestedEvent),
                PayloadJson = JsonSerializer.Serialize(new { dispatchId }),
                Status = "PENDING",
                RetryCount = 0,
                CreatedAt = DateTime.UtcNow
            });

            await _context.SaveChangesAsync(cancellationToken);
            await ProcessNotificationOutboxAsync(cancellationToken);
        }

        private async Task<string?> SendUserNotificationPushAsync(Guid userId, string title, string body, string priority, Dictionary<string, string> data, CancellationToken cancellationToken)
        {
            var firebaseMessaging = GetFirebaseMessaging();
            if (firebaseMessaging == null)
            {
                return "FIREBASE_UNAVAILABLE";
            }

            var tokens = await GetActiveTokensByProfileIdAsync(userId);
            if (tokens.Count == 0)
            {
                return "NO_ACTIVE_DEVICE_TOKEN";
            }

            var hasSuccess = false;
            var errors = new List<string>();

            foreach (var tokenBatch in tokens.Chunk(500))
            {
                var message = new MulticastMessage
                {
                    Tokens = tokenBatch.ToList(),
                    Notification = new Notification
                    {
                        Title = title,
                        Body = body
                    },
                    Data = data,
                    Android = new AndroidConfig
                    {
                        Priority = priority == "HIGH" ? Priority.High : Priority.Normal,
                        CollapseKey = $"notification_{data["notificationType"]}"
                    },
                    Apns = new ApnsConfig
                    {
                        Aps = new Aps
                        {
                            ContentAvailable = true
                        }
                    }
                };

                try
                {
                    var result = await firebaseMessaging.SendEachForMulticastAsync(message, cancellationToken);
                    hasSuccess = hasSuccess || result.SuccessCount > 0;
                    if (result.FailureCount > 0)
                    {
                        errors.Add($"failure:{result.FailureCount}");
                    }
                }
                catch (Exception ex)
                {
                    errors.Add(ex.Message);
                }
            }

            return hasSuccess ? (errors.Count == 0 ? null : string.Join(" | ", errors)) : (errors.Count == 0 ? "PUSH_SEND_FAILED" : string.Join(" | ", errors));
        }

        private static Dictionary<string, string> BuildPushData(NotificationDispatch dispatch, long userNotificationId)
        {
            var data = new Dictionary<string, string>
            {
                ["notificationType"] = dispatch.NotificationType,
                ["userNotificationId"] = userNotificationId.ToString()
            };

            if (!string.IsNullOrWhiteSpace(dispatch.ActionType))
            {
                data["actionType"] = dispatch.ActionType;
            }

            if (!string.IsNullOrWhiteSpace(dispatch.TargetScreen))
            {
                data["targetScreen"] = dispatch.TargetScreen;
            }

            if (!string.IsNullOrWhiteSpace(dispatch.ActionPayloadJson))
            {
                data["actionPayloadJson"] = dispatch.ActionPayloadJson;
            }

            if (!string.IsNullOrWhiteSpace(dispatch.DataJson))
            {
                data["dataJson"] = dispatch.DataJson;
            }

            return data;
        }

        private async Task<List<Guid>> ResolveRecipientUserIdsAsync(bool sendToAllUsers, List<Guid>? recipientUserIds)
        {
            if (sendToAllUsers)
            {
                return await _context.Profiles
                    .AsNoTracking()
                    .Select(profile => profile.ProfileId)
                    .ToListAsync();
            }

            var requestedIds = (recipientUserIds ?? new List<Guid>())
                .Where(userId => userId != Guid.Empty)
                .Distinct()
                .ToList();

            if (requestedIds.Count == 0)
            {
                throw new BadRequestException(MessageKeys.BadRequest, new { field = "recipientUserIds" });
            }

            var existingIds = await _context.Profiles
                .AsNoTracking()
                .Where(profile => requestedIds.Contains(profile.ProfileId))
                .Select(profile => profile.ProfileId)
                .ToListAsync();

            if (existingIds.Count == 0)
            {
                throw new NotFoundException(MessageKeys.UserNotFound);
            }

            return existingIds;
        }

        private static List<Guid> ParseRecipientUserIds(string? recipientUserIdsJson)
        {
            if (string.IsNullOrWhiteSpace(recipientUserIdsJson))
            {
                return new List<Guid>();
            }

            try
            {
                return JsonSerializer.Deserialize<List<Guid>>(recipientUserIdsJson) ?? new List<Guid>();
            }
            catch
            {
                return new List<Guid>();
            }
        }

        private static void ValidateTemplateRequest(UpsertNotificationTemplateRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.NotificationType) ||
                string.IsNullOrWhiteSpace(request.TitleTemplate) ||
                string.IsNullOrWhiteSpace(request.ContentTemplate))
            {
                throw new BadRequestException(MessageKeys.BadRequest);
            }

            var defaultActionType = NormalizeNullable(request.DefaultActionType);
            var defaultTargetScreen = NormalizeNullable(request.DefaultTargetScreen);
            var defaultActionPayloadJson = NormalizeJsonOrNull(request.DefaultActionPayloadJson);

            ValidateActionConfiguration(
                defaultActionType,
                defaultTargetScreen,
                defaultActionPayloadJson,
                "defaultActionType",
                "defaultTargetScreen",
                "defaultActionPayloadJson");

            request.DefaultActionType = defaultActionType;
            request.DefaultTargetScreen = CanonicalizeTargetScreen(defaultTargetScreen);
            request.DefaultActionPayloadJson = defaultActionPayloadJson;
        }

        private static void ValidateActionConfiguration(
            string? actionType,
            string? targetScreen,
            string? actionPayloadJson,
            string actionFieldName,
            string targetFieldName,
            string payloadFieldName)
        {
            if (string.IsNullOrWhiteSpace(actionType))
            {
                if (!string.IsNullOrWhiteSpace(targetScreen) || !string.IsNullOrWhiteSpace(actionPayloadJson))
                {
                    throw new BadRequestException(MessageKeys.BadRequest, new { field = actionFieldName });
                }

                return;
            }

            if (!SupportedNavigationActionTypes.Contains(actionType))
            {
                throw new BadRequestException(MessageKeys.BadRequest, new { field = actionFieldName });
            }

            if (string.IsNullOrWhiteSpace(targetScreen) && string.IsNullOrWhiteSpace(actionPayloadJson))
            {
                throw new BadRequestException(MessageKeys.BadRequest, new { field = targetFieldName });
            }

            if (!string.IsNullOrWhiteSpace(targetScreen) && !SupportedTargetScreenAliases.ContainsKey(targetScreen.Trim()))
            {
                throw new BadRequestException(MessageKeys.BadRequest, new { field = targetFieldName });
            }

            if (!string.IsNullOrWhiteSpace(actionPayloadJson))
            {
                try
                {
                    using var doc = JsonDocument.Parse(actionPayloadJson);
                    if (doc.RootElement.ValueKind != JsonValueKind.Object)
                    {
                        throw new BadRequestException(MessageKeys.BadRequest, new { field = payloadFieldName });
                    }
                }
                catch (BadRequestException)
                {
                    throw;
                }
                catch
                {
                    throw new BadRequestException(MessageKeys.BadRequest, new { field = payloadFieldName });
                }
            }
        }

        private static string? CanonicalizeTargetScreen(string? targetScreen)
        {
            if (string.IsNullOrWhiteSpace(targetScreen))
            {
                return null;
            }

            var normalized = targetScreen.Trim();
            return SupportedTargetScreenAliases.TryGetValue(normalized, out var canonical)
                ? canonical
                : normalized;
        }

        private static string NormalizeEventCode(string eventCode)
        {
            if (string.IsNullOrWhiteSpace(eventCode))
            {
                throw new BadRequestException(MessageKeys.BadRequest, new { field = "eventCode" });
            }

            return eventCode.Trim().ToUpperInvariant();
        }

        private static string? NormalizeNullable(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return null;
            }

            return value.Trim();
        }

        private static string? NormalizeJsonOrNull(string? json)
        {
            if (string.IsNullOrWhiteSpace(json))
            {
                return null;
            }

            try
            {
                using var doc = JsonDocument.Parse(json);
                return doc.RootElement.GetRawText();
            }
            catch
            {
                throw new BadRequestException(MessageKeys.BadRequest, new { field = "json" });
            }
        }

        private static string NormalizePriority(string? priority)
        {
            if (string.IsNullOrWhiteSpace(priority))
            {
                return "NORMAL";
            }

            var normalized = priority.Trim().ToUpperInvariant();
            return normalized == "HIGH" ? "HIGH" : "NORMAL";
        }

        private static string RenderTemplate(string template, Dictionary<string, string> data)
        {
            if (string.IsNullOrWhiteSpace(template))
            {
                return string.Empty;
            }

            return Regex.Replace(template, "\\{(\\w+)\\}", match =>
            {
                var key = match.Groups[1].Value;
                return data.TryGetValue(key, out var value) ? value : match.Value;
            });
        }

        private static NotificationTemplateDto MapTemplate(NotificationTemplate template)
        {
            return new NotificationTemplateDto
            {
                NotificationTemplateId = template.NotificationTemplateId,
                EventCode = template.EventCode,
                NotificationType = template.NotificationType,
                TitleTemplate = template.TitleTemplate,
                ContentTemplate = template.ContentTemplate,
                DefaultActionType = template.DefaultActionType,
                DefaultTargetScreen = template.DefaultTargetScreen,
                DefaultActionPayloadJson = template.DefaultActionPayloadJson,
                IsActive = template.IsActive,
                CreatedAt = template.CreatedAt,
                UpdatedAt = template.UpdatedAt
            };
        }

        private static NotificationDispatchDto MapDispatch(NotificationDispatch dispatch)
        {
            return new NotificationDispatchDto
            {
                NotificationDispatchId = dispatch.NotificationDispatchId,
                NotificationTemplateId = dispatch.NotificationTemplateId,
                NotificationType = dispatch.NotificationType,
                Priority = dispatch.Priority,
                Title = dispatch.Title,
                Content = dispatch.Content,
                RecipientScope = dispatch.RecipientScope,
                RecipientUserIdsJson = dispatch.RecipientUserIdsJson,
                ScheduledAt = dispatch.ScheduledAt,
                SentAt = dispatch.SentAt,
                Status = dispatch.Status,
                ErrorMessage = dispatch.ErrorMessage,
                CreatedAt = dispatch.CreatedAt,
                UpdatedAt = dispatch.UpdatedAt
            };
        }

        private static UserNotificationDto MapUserNotification(UserNotification userNotification)
        {
            return new UserNotificationDto
            {
                UserNotificationId = userNotification.UserNotificationId,
                NotificationType = userNotification.NotificationType,
                Priority = userNotification.Priority,
                Title = userNotification.Title,
                Content = userNotification.Content,
                ActionType = userNotification.ActionType,
                TargetScreen = userNotification.TargetScreen,
                ActionPayloadJson = userNotification.ActionPayloadJson,
                DeliveryStatus = userNotification.DeliveryStatus,
                CreatedAt = userNotification.CreatedAt,
                SentAt = userNotification.SentAt,
                ReadAt = userNotification.ReadAt
            };
        }

        private async Task<List<string>> GetActiveTokensByProfileIdAsync(Guid profileId)
        {
            return await _context.DeviceTokens
                .Where(deviceToken => deviceToken.ProfileId == profileId && deviceToken.IsActive == true)
                .Select(deviceToken => deviceToken.Token)
                .ToListAsync();
        }

        private async Task SendEmployeeInviteWithDataAsync(Guid userId, string title, string body)
        {
            var firebaseMessaging = GetFirebaseMessaging();
            if (firebaseMessaging == null)
            {
                _logger.LogWarning("Firebase messaging unavailable. Skip SendEmployeeInviteWithDataAsync.");
                return;
            }

            var tokens = await GetActiveTokensByProfileIdAsync(userId);
            if (tokens.Count == 0)
            {
                _logger.LogInformation("SendEmployeeInviteWithDataAsync skipped: no active tokens for userId={UserId}", userId);
                return;
            }

            foreach (var tokenBatch in tokens.Chunk(500))
            {
                var message = new MulticastMessage
                {
                    Tokens = tokenBatch.ToList(),
                    Notification = new Notification
                    {
                        Title = title,
                        Body = body
                    },
                    Data = new Dictionary<string, string>
                    {
                        ["type"] = "employee_invite"
                    }
                };

                try
                {
                    var result = await firebaseMessaging.SendEachForMulticastAsync(message);
                    _logger.LogInformation(
                        "SendEmployeeInviteWithDataAsync userId={UserId}, success={Success}, failure={Failure}",
                        userId,
                        result.SuccessCount,
                        result.FailureCount);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex,
                        "SendEmployeeInviteWithDataAsync failed for userId={UserId} and batchSize={BatchSize}",
                        userId,
                        tokenBatch.Length);
                }
            }
        }

        private FirebaseMessaging? GetFirebaseMessaging()
        {
            try
            {
                FirebaseApp? app = null;

                try
                {
                    app = FirebaseApp.DefaultInstance;
                }
                catch
                {
                    app = null;
                }

                if (app == null)
                {
                    var serviceAccountPath = Environment.GetEnvironmentVariable("GOOGLE_APPLICATION_CREDENTIALS");
                    if (string.IsNullOrWhiteSpace(serviceAccountPath))
                    {
                        serviceAccountPath = _configuration["Firebase:ServiceAccountPath"];
                    }

                    if (string.IsNullOrWhiteSpace(serviceAccountPath) || !File.Exists(serviceAccountPath))
                    {
                        _logger.LogWarning(
                            "Firebase service account file not found. GOOGLE_APPLICATION_CREDENTIALS={EnvPath}, Firebase:ServiceAccountPath={ConfigPath}",
                            Environment.GetEnvironmentVariable("GOOGLE_APPLICATION_CREDENTIALS"),
                            _configuration["Firebase:ServiceAccountPath"]);
                        return null;
                    }

                    try
                    {
                        app = FirebaseApp.Create(new AppOptions
                        {
                            Credential = GoogleCredential.FromFile(serviceAccountPath)
                        });
                    }
                    catch (Exception)
                    {
                        app = FirebaseApp.DefaultInstance;
                    }
                }

                if (app == null)
                {
                    return null;
                }

                return FirebaseMessaging.GetMessaging(app);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Cannot initialize Firebase messaging instance.");
                return null;
            }
        }
    }
}
