using Volo.Abp.Modularity;

namespace IM.Mysql;

[DependsOn(
    typeof(IMDomainModule)
    )]
public class IMMysqlModule : AbpModule
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
    }
}