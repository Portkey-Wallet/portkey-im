using IM.Grains;
using Microsoft.Extensions.DependencyInjection;
using Volo.Abp.AspNetCore.Serilog;
using Volo.Abp.Autofac;
using Volo.Abp.Modularity;

namespace IM.Silo;

[DependsOn(typeof(AbpAutofacModule),
    typeof(AbpAspNetCoreSerilogModule),
    typeof(ImGrainsModule)
)]
public class ImOrleansSiloModule : AbpModule
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        context.Services.AddHostedService<ImHostedService>();
        var configuration = context.Services.GetConfiguration();
        // Configure<GrainOptions>(configuration.GetSection("Contract"));
    }
}