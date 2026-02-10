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

            // Register Jobs
            builder.RegisterType<ImageCleanupJob>().AsSelf().InstancePerDependency();
        }
    }
}
