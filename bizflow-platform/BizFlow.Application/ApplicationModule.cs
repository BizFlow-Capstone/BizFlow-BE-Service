using Autofac;
using AutoMapper;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static System.Runtime.InteropServices.JavaScript.JSType;

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

            // Register all AutoMapper Profile classes
            builder.RegisterAssemblyTypes(ThisAssembly)
                   .AssignableTo<Profile>()
                   .As<Profile>();

            // Register the Mapper using the profiles found above
            builder.Register(context =>
            {
                var profiles = context.Resolve<IEnumerable<Profile>>();
                var config = new MapperConfiguration(cfg =>
                {
                    foreach (var profile in profiles)
                    {
                        cfg.AddProfile(profile);
                    }
                });
                return config.CreateMapper();
            }).As<IMapper>().InstancePerLifetimeScope();
            
            // ImageService is Singleton — stateless, only depends on Singleton services
            builder.RegisterType<BizFlow.Application.Services.ImageService>()
                   .AsImplementedInterfaces()
                   .SingleInstance();
        }
    }
}
