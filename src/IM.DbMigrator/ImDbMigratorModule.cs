using IM.MongoDB;
using Volo.Abp.Autofac;
using Volo.Abp.BackgroundJobs;
using Volo.Abp.Modularity;

namespace IM.DbMigrator;

[DependsOn(
    typeof(AbpAutofacModule),
    typeof(ImMongoDbModule),
    typeof(ImApplicationContractsModule)
    )]
public class ImDbMigratorModule : AbpModule
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        Configure<AbpBackgroundJobOptions>(options => options.IsJobExecutionEnabled = false);
    }
}
