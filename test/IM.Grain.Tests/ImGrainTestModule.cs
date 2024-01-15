using IM.Grains;
using IM.Signature;
using Microsoft.Extensions.DependencyInjection;
using Orleans;
using Volo.Abp.AutoMapper;
using Volo.Abp.Caching;
using Volo.Abp.Modularity;
using Volo.Abp.ObjectMapping;

namespace IM.Grain.Tests;

[DependsOn(
    typeof(ImGrainsModule),
    typeof(ImDomainTestModule),
    typeof(IMDomainModule),
    typeof(AbpCachingModule),
    typeof(AbpAutoMapperModule),
    typeof(AbpObjectMappingModule),
    typeof(ImSignatureModule)
)]
public class ImGrainTestModule : AbpModule
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        context.Services.AddSingleton<IClusterClient>(sp => sp.GetService<ClusterFixture>().Cluster.Client);
        context.Services.AddHttpClient();
    }
}