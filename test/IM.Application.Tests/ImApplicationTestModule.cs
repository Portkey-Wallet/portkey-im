using IM.Grain.Tests;
using IM.Hub;
using Microsoft.Extensions.DependencyInjection;
using Volo.Abp.AutoMapper;
using Volo.Abp.EventBus;
using Volo.Abp.Modularity;

namespace IM;

[DependsOn(
    typeof(ImApplicationModule),
    typeof(AbpEventBusModule),
    typeof(ImGrainTestModule),
    typeof(ImDomainTestModule)
)]
public class ImApplicationTestModule : AbpModule
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        // context.Services.AddSingleton(sp => sp.GetService<ClusterFixture>().Cluster.Client);
        context.Services.AddSingleton<IConnectionProvider, ConnectionProvider>();
        Configure<AbpAutoMapperOptions>(options => { options.AddMaps<ImApplicationModule>(); });
        
        base.ConfigureServices(context);
    }
}