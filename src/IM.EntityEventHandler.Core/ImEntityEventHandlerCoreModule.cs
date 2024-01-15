using System;
using Castle.Core.Configuration;
using IM.Commons;
using Microsoft.Extensions.DependencyInjection;
using Volo.Abp.AutoMapper;
using Volo.Abp.Modularity;

namespace IM.EntityEventHandler.Core
{
    [DependsOn(typeof(AbpAutoMapperModule), typeof(ImApplicationModule),
        typeof(ImApplicationContractsModule))]
    public class ImEntityEventHandlerCoreModule : AbpModule
    {
        public override void ConfigureServices(ServiceConfigurationContext context)
        {
            Configure<AbpAutoMapperOptions>(options =>
            {
                //Add all mappings defined in the assembly of the MyModule class
                options.AddMaps<ImEntityEventHandlerCoreModule>();
            });
        }
    }
}