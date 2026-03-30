using BizFlow.Application.Common.Models;
using BizFlow.Application.Interfaces.Services;
using BizFlow.Domain.Entities;
using Google.Cloud.Firestore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Linq;
using System.IO;

namespace BizFlow.Infrastructure.Services
{
    public class FirestoreService : IFirestoreService
    {
        private const string UsageTrackingCollection = "usage_tracking";
        private const string SubscriptionAccessCollection = "subscription_access";
        private const string SystemConfigCollection = "system_config";
        private const string FirestoreGateDocId = "firestore_gate";

        private readonly ILogger<FirestoreService> _logger;
        private readonly FirestoreDb? _db;
        private readonly string _projectId = string.Empty;
        private readonly string? _serviceAccountPath;
        private readonly bool _serviceAccountFileExists;

        public FirestoreService(IConfiguration configuration, ILogger<FirestoreService> logger, IOptions<FirebaseAuthConfig> firebaseConfig)
        {
            _logger = logger;

            _projectId = firebaseConfig.Value.ProjectId?.Trim() ?? string.Empty;
            _serviceAccountPath = string.IsNullOrWhiteSpace(firebaseConfig.Value.ServiceAccountPath)
                ? null
                : firebaseConfig.Value.ServiceAccountPath.Trim();
            _serviceAccountFileExists = !string.IsNullOrWhiteSpace(_serviceAccountPath) && File.Exists(_serviceAccountPath);

            if (string.IsNullOrWhiteSpace(_projectId) || _projectId == "your_firebase_project_id")
            {
                _logger.LogWarning("Firestore is disabled because FirebaseAuth:ProjectId is not configured.");
                return;
            }

            try
            {
                if (_serviceAccountFileExists)
                {
                    // Prefer explicit service-account path to avoid relying on ambient ADC.
                    Environment.SetEnvironmentVariable("GOOGLE_APPLICATION_CREDENTIALS", _serviceAccountPath);
                    _db = FirestoreDb.Create(_projectId);
                }
                else
                {
                    _db = FirestoreDb.Create(_projectId);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to initialize FirestoreDb. Firestore operations will be skipped.");
            }
        }

        public async Task SetUsageTrackingAsync(Guid ownerProfileId, Subscription sub, IReadOnlyCollection<PlanFeature> features, IReadOnlyDictionary<string, int>? existingUsed = null)
        {
            if (_db == null)
            {
                return;
            }

            var doc = BuildUsageDoc(ownerProfileId, sub, features, existingUsed);
            var docRef = _db.Collection(UsageTrackingCollection).Document(BuildActiveDocId(ownerProfileId));
            await docRef.SetAsync(doc);
        }

        public async Task IncrementFeatureUsageAsync(Guid ownerProfileId, string featureCode)
        {
            if (_db == null)
            {
                return;
            }

            var docRef = _db.Collection(UsageTrackingCollection).Document(BuildActiveDocId(ownerProfileId));
            await docRef.UpdateAsync(new Dictionary<string, object>
            {
                [$"features.{featureCode}.used"] = FieldValue.Increment(1),
                ["updatedAt"] = Timestamp.FromDateTime(DateTime.UtcNow)
            });
        }

        /// <summary>
        /// Used for reconcile/sync when we need to force Firestore to match the correct number from SQL.
        /// </summary>
        /// <param name="docId">The ID of the usage document</param>
        /// <param name="featureCode">The code of the feature</param>
        /// <param name="count">The count to set</param>
        /// <returns></returns>
        public async Task SetFeatureUsedCountAsync(string docId, string featureCode, int count)
        {
            if (_db == null)
            {
                return;
            }

            var docRef = _db.Collection(UsageTrackingCollection).Document(docId);
            await docRef.UpdateAsync(new Dictionary<string, object>
            {
                [$"features.{featureCode}.used"] = count,
                ["updatedAt"] = Timestamp.FromDateTime(DateTime.UtcNow)
            });
        }

        /// <summary>
        /// Mark the usage tracking as expired, typically when a subscription expires. 
        /// This will set status to "expired" and also set all feature limits to 0 to prevent further usage until a new subscription is active.
        /// </summary>
        public async Task MarkUsageTrackingExpiredAsync(Guid ownerProfileId)
        {
            if (_db == null)
            {
                return;
            }

            var docRef = _db.Collection(UsageTrackingCollection).Document(BuildActiveDocId(ownerProfileId));
            var snapshot = await docRef.GetSnapshotAsync();
            if (!snapshot.Exists)
            {
                return;
            }

            var updates = new Dictionary<string, object>
            {
                ["status"] = "expired",
                ["updatedAt"] = Timestamp.FromDateTime(DateTime.UtcNow)
            };

            if (snapshot.TryGetValue<Dictionary<string, object>>("features", out var features))
            {
                foreach (var key in features.Keys)
                {
                    updates[$"features.{key}.limit"] = 0;
                }
            }

            await docRef.UpdateAsync(updates);
        }

        
        /// <summary>
        /// Gets the usage document by its ID.
        /// </summary>
        /// <param name="docId">The ID of the usage document</param>
        /// <returns>The UsageTrackingDoc document, or null if not found or if Firestore is not initialized (e.g. due to missing configuration)</returns>
        public async Task<UsageTrackingDoc?> GetUsageDocAsync(string docId)
        {
            if (_db == null)
            {
                return null;
            }

            try
            {
                var snapshot = await _db.Collection(UsageTrackingCollection).Document(docId).GetSnapshotAsync();
                if (!snapshot.Exists)
                {
                    return null;
                }

                return ParseUsageDoc(snapshot);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to read Firestore usage document {DocId}.", docId);
                return null;
            }
        }

        /// <summary>
        /// Gets a quick snapshot of used/limit for a specific feature of the owner's active usage doc. 
        /// </summary>
        /// <param name="ownerProfileId">The ID of the owner's profile</param>
        /// <param name="featureCode">The code of the feature</param>
        /// <returns>FeatureUsageSnapshot or null if no active doc or feature not found.</returns>
        public async Task<FeatureUsageSnapshot?> GetUsageSnapshotAsync(Guid ownerProfileId, string featureCode)
        {
            var usageDoc = await GetUsageDocAsync(BuildActiveDocId(ownerProfileId));
            if (usageDoc == null || !usageDoc.Features.TryGetValue(featureCode, out var feature))
            {
                return null;
            }

            return new FeatureUsageSnapshot
            {
                Used = feature.Used,
                Limit = feature.Limit
            };
        }

        public async Task<FirestoreHealthStatus> GetHealthStatusAsync()
        {
            var status = new FirestoreHealthStatus
            {
                IsConnected = false,
                ProjectId = _projectId,
                ServiceAccountPath = _serviceAccountPath,
                ServiceAccountFileExists = _serviceAccountFileExists
            };

            if (_db == null)
            {
                status.Reason = string.IsNullOrWhiteSpace(_projectId)
                    ? "FirebaseAuth:ProjectId is missing."
                    : _serviceAccountPath != null && !_serviceAccountFileExists
                        ? $"Service account file not found: {_serviceAccountPath}"
                        : "FirestoreDb is not initialized.";
                return status;
            }

            try
            {
                // Lightweight read to verify Firestore connectivity and credentials.
                _ = await _db.Collection(UsageTrackingCollection).Limit(1).GetSnapshotAsync();
                status.IsConnected = true;
                return status;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Firestore health check failed.");
                status.Reason = ex.Message;
                return status;
            }
        }

        public async Task<bool> CheckHealthAsync()
        {
            var status = await GetHealthStatusAsync();
            return status.IsConnected;
        }

        public async Task<FirestoreGateDebugStatus> GetFirestoreGateDebugAsync()
        {
            var result = new FirestoreGateDebugStatus
            {
                ProjectId = _projectId
            };

            if (_db == null)
            {
                result.Reason = "FirestoreDb is not initialized.";
                return result;
            }

            try
            {
                var snap = await _db.Collection(SystemConfigCollection).Document(FirestoreGateDocId).GetSnapshotAsync();
                result.GateDocExists = snap.Exists;
                if (!snap.Exists)
                {
                    result.Reason =
                        "Doc system_config/firestore_gate chua ton tai. Rules se fail-closed (khong cho client doc usage_tracking).";
                    return result;
                }

                var data = snap.ToDictionary();
                result.AllowClientRead = TryGetBool(data, "allowClientRead");
                result.AllowClientReadNoAuth = TryGetBool(data, "allowClientReadNoAuth");

                result.IsClientReadEnabled =
                    result.AllowClientRead == true || result.AllowClientReadNoAuth == true;

                if (!result.IsClientReadEnabled)
                {
                    result.Reason =
                        "Gate ton tai nhung allowClientRead va allowClientReadNoAuth deu khong phai true (boolean). Rules se chan doc.";
                }

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to read Firestore gate doc for debug.");
                result.Reason = ex.Message;
                return result;
            }
        }

        private static bool? TryGetBool(IReadOnlyDictionary<string, object> data, string key)
        {
            if (!data.TryGetValue(key, out var v) || v == null)
            {
                return null;
            }

            return v switch
            {
                bool b => b,
                _ => null
            };
        }

        /// <summary>
        /// Creates or updates a subscription access grant for a member profile under the owner's subscription. 
        /// </summary>
        /// <param name="ownerProfileId">The ID of the owner's profile</param>
        /// <param name="memberProfileId">The ID of the member's profile</param>
        /// <param name="canReadUsage">Indicates if the member can read usage information</param>
        /// <param name="isActive">Indicates if the access grant is active</param>
        public async Task UpsertSubscriptionAccessGrantAsync(Guid ownerProfileId, Guid memberProfileId, bool canReadUsage, bool isActive)
        {
            if (_db == null)
            {
                return;
            }

            var memberRef = _db
                .Collection(SubscriptionAccessCollection)
                .Document(ownerProfileId.ToString())
                .Collection("members")
                .Document(memberProfileId.ToString());

            var now = Timestamp.FromDateTime(DateTime.UtcNow);
            await memberRef.SetAsync(new Dictionary<string, object>
            {
                ["ownerProfileId"] = ownerProfileId.ToString(),
                ["memberProfileId"] = memberProfileId.ToString(),
                ["canReadUsage"] = canReadUsage,
                ["isActive"] = isActive,
                ["updatedAt"] = now
            }, SetOptions.MergeAll);
        }

        /// <summary>
        /// Revokes a subscription access grant for a member profile by deleting the corresponding document in Firestore.
        /// </summary>
        /// <param name="ownerProfileId">The ID of the owner's profile</param>
        /// <param name="memberProfileId">The ID of the member's profile</param>
        public async Task RevokeSubscriptionAccessGrantAsync(Guid ownerProfileId, Guid memberProfileId)
        {
            if (_db == null)
            {
                return;
            }

            var memberRef = _db
                .Collection(SubscriptionAccessCollection)
                .Document(ownerProfileId.ToString())
                .Collection("members")
                .Document(memberProfileId.ToString());

            await memberRef.DeleteAsync();
        }

        private static string BuildActiveDocId(Guid ownerProfileId) => $"{ownerProfileId}_active";

        private static Dictionary<string, object> BuildUsageDoc(Guid ownerProfileId, Subscription sub, IReadOnlyCollection<PlanFeature> features, IReadOnlyDictionary<string, int>? existingUsed)
        {
            // Use accumulated allocated limits stored in SQL (FeatureUsages.AllocatedLimit) so that
            // when users buy the same plan multiple times, Firestore reflects the increased limits.
            var allocatedByFeatureId = sub.FeatureUsages?
                .ToDictionary(u => u.FeatureId, u => u.AllocatedLimit) ?? new Dictionary<int, int>();

            var featureMap = new Dictionary<string, object>();
            foreach (var feature in features)
            {
                var used = 0;
                if (existingUsed != null && existingUsed.TryGetValue(feature.Feature.FeatureCode, out var existingCount))
                {
                    used = existingCount;
                }

                var allocatedLimit = allocatedByFeatureId.TryGetValue(feature.FeatureId, out var l)
                    ? l
                    : feature.UsageLimit;

                featureMap[feature.Feature.FeatureCode] = new Dictionary<string, object>
                {
                    ["used"] = used,
                    ["limit"] = allocatedLimit
                };
            }

            return new Dictionary<string, object>
            {
                ["ownerProfileId"] = ownerProfileId.ToString(),
                ["planName"] = sub.SubscriptionPlan.Name,
                ["subscriptionId"] = sub.SubscriptionId.ToString(),
                ["startDate"] = Timestamp.FromDateTime(sub.StartDate.ToUniversalTime()),
                ["endDate"] = Timestamp.FromDateTime(sub.EndDate.ToUniversalTime()),
                ["status"] = "active",
                ["features"] = featureMap,
                ["updatedAt"] = Timestamp.FromDateTime(DateTime.UtcNow)
            };
        }

        private static UsageTrackingDoc ParseUsageDoc(DocumentSnapshot snapshot)
        {
            var data = snapshot.ToDictionary();
            var result = new UsageTrackingDoc
            {
                OwnerProfileId = data.TryGetValue("ownerProfileId", out var ownerObj) ? ownerObj?.ToString() ?? string.Empty : string.Empty,
                PlanName = data.TryGetValue("planName", out var planObj) ? planObj?.ToString() ?? string.Empty : string.Empty,
                SubscriptionId = data.TryGetValue("subscriptionId", out var subObj) ? subObj?.ToString() ?? string.Empty : string.Empty,
                Status = data.TryGetValue("status", out var statusObj) ? statusObj?.ToString() ?? "active" : "active",
                StartDate = data.TryGetValue("startDate", out var startObj) && startObj is Timestamp startTs ? startTs.ToDateTime() : DateTime.MinValue,
                EndDate = data.TryGetValue("endDate", out var endObj) && endObj is Timestamp endTs ? endTs.ToDateTime() : DateTime.MinValue,
                UpdatedAt = data.TryGetValue("updatedAt", out var updatedObj) && updatedObj is Timestamp updatedTs ? updatedTs.ToDateTime() : DateTime.UtcNow,
                Features = new Dictionary<string, UsageFeatureData>()
            };

            if (data.TryGetValue("features", out var featuresObj) && featuresObj is Dictionary<string, object> featureMap)
            {
                foreach (var entry in featureMap)
                {
                    if (entry.Value is Dictionary<string, object> values)
                    {
                        result.Features[entry.Key] = new UsageFeatureData
                        {
                            Used = values.TryGetValue("used", out var usedObj) ? Convert.ToInt32(usedObj) : 0,
                            Limit = values.TryGetValue("limit", out var limitObj) ? Convert.ToInt32(limitObj) : 0
                        };
                    }
                }
            }

            return result;
        }
    }
}
