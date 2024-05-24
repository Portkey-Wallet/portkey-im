using IM.Mysql.Config;
using Volo.Abp.AutoMapper;
using Volo.Abp.Modularity;

namespace IM.Mysql;

[DependsOn(
    typeof(IMDomainModule)
    )]
public class IMMysqlModule : AbpModule
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        Configure<AbpAutoMapperOptions>(options => { options.AddMaps<IMMysqlModule>(); });
        var configuration = context.Services.GetConfiguration();
        Configure<ImBaseDbOptions>(configuration.GetSection("ImDb"));
    }
}