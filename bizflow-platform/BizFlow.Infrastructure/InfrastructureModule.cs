using Autofac;
using BizFlow.Application.Interfaces.Repositories;
using BizFlow.Infrastructure.DataContext;
using BizFlow.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using BizFlow.Infrastructure.Jobs;
using BizFlow.Application.Common.Models;
using CloudinaryDotNet;
using BizFlow.Application.Interfaces.Services;
using BizFlow.Infrastructure.Services;
using FirebaseAdmin;
using Google.Apis.Auth.OAuth2;

namespace BizFlow.Infrastructure
{
    public class InfrastructureModule : Module
    {
        private readonly IConfiguration _configuration;

        public InfrastructureModule(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        protected override void Load(ContainerBuilder builder)
        {
            InitializeFirebaseApp();

            builder.RegisterType<UnitOfWork>()
                   .As<IUnitOfWork>()
                   .InstancePerLifetimeScope();

            // Register DbContext
            builder.Register(c =>
            {
                var optionsBuilder = new DbContextOptionsBuilder<BizFlowDbContext>();
                optionsBuilder.UseMySql(
                    _configuration.GetConnectionString("DefaultConnection"),
                    new MySqlServerVersion(new Version(8, 0, 45)),
                    options => options.EnableRetryOnFailure(
                        maxRetryCount: 5,
                        maxRetryDelay: TimeSpan.FromSeconds(30),
                        errorNumbersToAdd: null)
                );
                return new BizFlowDbContext(optionsBuilder.Options);
            }).InstancePerLifetimeScope();

            builder.RegisterAssemblyTypes(ThisAssembly)
               .Where(t => t.Name.EndsWith("Repository"))
               .AsImplementedInterfaces()
               .InstancePerLifetimeScope();

            builder.RegisterAssemblyTypes(ThisAssembly)
               .Where(t => t.Name.EndsWith("Service") && t.Name != "CloudinaryService")
               .AsImplementedInterfaces()
               .InstancePerLifetimeScope();

            builder.RegisterAssemblyTypes(ThisAssembly)
               .Where(t => t.Name.EndsWith("Strategy"))
               .AsImplementedInterfaces()
               .InstancePerLifetimeScope();

            // Register Cloudinary as Singleton
            builder.Register(c =>
            {
                var settings = _configuration.GetSection("CloudinarySettings").Get<CloudinarySettings>();
                if (settings == null) throw new InvalidOperationException("CloudinarySettings not configured");
                
                var account = new Account(settings.CloudName, settings.ApiKey, settings.ApiSecret);
                return new Cloudinary(account);
            }).SingleInstance();

            // Register CloudinaryService as Singleton (it uses the Singleton Cloudinary instance)
            builder.RegisterType<CloudinaryService>()
                   .As<ICloudinaryService>()
                   .SingleInstance();

                 // Firestore service should be singleton to reuse FirestoreDb instance
                 builder.RegisterType<FirestoreService>()
                     .As<IFirestoreService>()
                     .SingleInstance();

            // Hangfire enqueue abstraction (not picked up by *Service assembly scan)
            builder.RegisterType<HangfireBackgroundJobScheduler>()
                .As<IBackgroundJobScheduler>()
                .SingleInstance();

            // AI Service HTTP client (uses IHttpClientFactory from MS DI)
            builder.RegisterType<AiServiceHttpClient>()
                .As<IAiServiceClient>()
                .InstancePerLifetimeScope();

            // Register Jobs
            builder.RegisterType<ImageCleanupJob>().AsSelf().InstancePerDependency();
                 builder.RegisterType<SubscriptionExpiryCheckJob>().AsSelf().InstancePerDependency();
                 builder.RegisterType<SubscriptionReminderJob>().AsSelf().InstancePerDependency();
                 builder.RegisterType<UsageSnapshotJob>().AsSelf().InstancePerDependency();
                 builder.RegisterType<FirestoreSyncJob>().AsSelf().InstancePerDependency();
                 builder.RegisterType<StaleTransactionCleanupJob>().AsSelf().InstancePerDependency();
                 builder.RegisterType<StripePendingReconcileJob>().AsSelf().InstancePerDependency();
                 builder.RegisterType<StripeRefundReconcileJob>().AsSelf().InstancePerDependency();
                 builder.RegisterType<SubscriptionPlanStripeCatalogSyncJob>().AsSelf().InstancePerDependency();
            builder.RegisterType<ScheduledNotificationDispatchJob>().AsSelf().InstancePerDependency();
            builder.RegisterType<NotificationOutboxJob>().AsSelf().InstancePerDependency();
            builder.RegisterType<NotificationRetentionJob>().AsSelf().InstancePerDependency();
            builder.RegisterType<OtpCleanupJob>().AsSelf().InstancePerDependency();
            // AI nightly jobs
            builder.RegisterType<AiForecastJob>().AsSelf().InstancePerDependency();
            builder.RegisterType<AiAnomalyPatternJob>().AsSelf().InstancePerDependency();
            builder.RegisterType<AiReorderJob>().AsSelf().InstancePerDependency();
            builder.RegisterType<AiProductInsightsJob>().AsSelf().InstancePerDependency();
            builder.RegisterType<AiAnomalyCheckJob>().AsSelf().InstancePerDependency();
            builder.RegisterType<AiVectorStoreSyncJob>().AsSelf().InstancePerDependency();

            // Register Accounting Book engines
            builder.RegisterType<BizFlow.Infrastructure.Services.FormulaEngine.FormulaEngine>()
                   .As<IFormulaEngine>()
                   .InstancePerLifetimeScope();
            builder.RegisterType<BizFlow.Infrastructure.Services.BookRendering.BookRenderingService>()
                   .As<IBookRenderingService>()
                   .InstancePerLifetimeScope();
        }

        private void InitializeFirebaseApp()
        {
            if (IsFirebaseInitialized())
            {
                return;
            }

            var serviceAccountPath = Environment.GetEnvironmentVariable("GOOGLE_APPLICATION_CREDENTIALS");
            if (string.IsNullOrWhiteSpace(serviceAccountPath))
            {
                serviceAccountPath = _configuration["FirebaseAuth:ServiceAccountPath"];
            }

            // Backward compatibility for old key naming.
            if (string.IsNullOrWhiteSpace(serviceAccountPath))
            {
                serviceAccountPath = _configuration["Firebase:ServiceAccountPath"];
            }

            if (string.IsNullOrWhiteSpace(serviceAccountPath) || !File.Exists(serviceAccountPath))
            {
                return;
            }

            FirebaseApp.Create(new AppOptions
            {
                Credential = GoogleCredential.FromFile(serviceAccountPath)
            });
        }

        private static bool IsFirebaseInitialized()
        {
            try
            {
                _ = FirebaseApp.DefaultInstance;
                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}
