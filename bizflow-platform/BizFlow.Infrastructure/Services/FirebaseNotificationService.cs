using BizFlow.Application.Interfaces.Services;
using BizFlow.Domain.Entities;
using BizFlow.Infrastructure.DataContext;
using FirebaseAdmin;
using FirebaseAdmin.Messaging;
using Google.Apis.Auth.OAuth2;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace BizFlow.Infrastructure.Services
{
    public class FirebaseNotificationService : INotificationService
    {
        private readonly BizFlowDbContext _context;
        private readonly ILogger<FirebaseNotificationService> _logger;
        private readonly IConfiguration _configuration;

        public FirebaseNotificationService(
            BizFlowDbContext context,
            ILogger<FirebaseNotificationService> logger,
            IConfiguration configuration)
        {
            _context = context;
            _logger = logger;
            _configuration = configuration;
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
                    .Where(deviceToken => deviceToken.Token == normalizedToken && deviceToken.IsActive)
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

        public Task SendEmployeeInviteAsync(Guid employeeId, string ownerName)
        {
            var title = string.IsNullOrWhiteSpace(ownerName)
                ? "Bạn có lời mời làm nhân viên"
                : $"{ownerName} đã mời bạn làm nhân viên";

            return SendEmployeeInviteWithDataAsync(employeeId, title, "Mở ứng dụng để xem chi tiết lời mời.");
        }

        public Task NotifyEmployeeRemovedAsync(Guid employeeId, string businessName)
        {
            var data = new Dictionary<string, string>
            {
                ["type"] = "employee_removed",
                ["businessName"] = businessName
            };

            return SendSilentNotificationAsync(employeeId, data);
        }

        private async Task<List<string>> GetActiveTokensByProfileIdAsync(Guid profileId)
        {
            return await _context.DeviceTokens
                .Where(deviceToken => deviceToken.ProfileId == profileId && deviceToken.IsActive)
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
