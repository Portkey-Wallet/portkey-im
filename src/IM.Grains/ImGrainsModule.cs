using IM.Signature;
using Microsoft.Extensions.DependencyInjection;
using Volo.Abp.AutoMapper;
using Volo.Abp.Modularity;

namespace IM.Grains;

[DependsOn(typeof(ImApplicationContractsModule),
    typeof(AbpAutoMapperModule),
    typeof(ImSignatureModule))]
public class ImGrainsModule : AbpModule
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        Configure<AbpAutoMapperOptions>(options => { options.AddMaps<ImGrainsModule>(); });

        var configuration = context.Services.GetConfiguration();
        var connStr = configuration["GraphQL:Configuration"];
    }
}