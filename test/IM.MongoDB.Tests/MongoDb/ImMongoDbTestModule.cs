using System;
using Volo.Abp.Data;
using Volo.Abp.Modularity;

namespace IM.MongoDB;

[DependsOn(
    typeof(ImTestBaseModule),
    typeof(ImMongoDbModule)
    )]
public class ImMongoDbTestModule : AbpModule
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        // var stringArray = IMMongoDbFixture.ConnectionString.Split('?');
        // var connectionString = stringArray[0].EnsureEndsWith('/') +
        //                            "Db_" +
        //                        Guid.NewGuid().ToString("N") + "/?" + stringArray[1];
        //
        // Configure<AbpDbConnectionOptions>(options =>
        // {
        //     options.ConnectionStrings.Default = connectionString;
        // });
    }
}
