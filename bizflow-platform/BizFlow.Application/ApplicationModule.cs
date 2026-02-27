using Autofac;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BizFlow.Application
{
    public class ApplicationModule : Module
    {
        protected override void Load(ContainerBuilder builder)
        {
            builder.RegisterAssemblyTypes(ThisAssembly)
               .Where(t => t.Name.EndsWith("Service") && t.Name != "ImageService")
               .AsImplementedInterfaces()
               .InstancePerLifetimeScope();

            // ImageService is Singleton — stateless, only depends on Singleton services
            builder.RegisterType<BizFlow.Application.Services.ImageService>()
                   .AsImplementedInterfaces()
                   .SingleInstance();
        }
    }
}
