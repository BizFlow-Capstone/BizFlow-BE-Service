using Autofac;
using BizFlow.Application.Interfaces.Repositories;
using BizFlow.Infrastructure.Data;
using BizFlow.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
               .Where(t => t.Name.EndsWith("Service"))
               .AsImplementedInterfaces()
               .InstancePerLifetimeScope();

            builder.RegisterAssemblyTypes(ThisAssembly)
               .Where(t => t.Name.EndsWith("Strategy"))
               .AsImplementedInterfaces()
               .InstancePerLifetimeScope();
        }
    }
}
