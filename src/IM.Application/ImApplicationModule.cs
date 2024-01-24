using IM.Grains;
using IM.Options;
using IM.Repository;
using IM.Signature;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Volo.Abp.Account;
using Volo.Abp.AutoMapper;
using Volo.Abp.DistributedLocking;
using Volo.Abp.FeatureManagement;
using Volo.Abp.Identity;
using Volo.Abp.Modularity;
using Volo.Abp.PermissionManagement;
using Volo.Abp.SettingManagement;
using Volo.Abp.TenantManagement;

namespace IM;

[DependsOn(
    typeof(IMDomainModule),
    typeof(AbpAccountApplicationModule),
    typeof(ImApplicationContractsModule),
    typeof(AbpIdentityApplicationModule),
    typeof(AbpPermissionManagementApplicationModule),
    typeof(AbpTenantManagementApplicationModule),
    typeof(AbpFeatureManagementApplicationModule),
    typeof(AbpSettingManagementApplicationModule),
    typeof(ImGrainsModule),
    typeof(ImSignatureModule),
    typeof(AbpDistributedLockingModule)
)]
public class ImApplicationModule : AbpModule
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        Configure<AbpAutoMapperOptions>(options => { options.AddMaps<ImApplicationModule>(); });
        var configuration = context.Services.GetConfiguration();


        //Configure<ChainOptions>(configuration.GetSection("Chains"));
        Configure<RelationOneOptions>(configuration.GetSection("RelationOne"));
        context.Services.AddHttpClient();
        context.Services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();
        context.Services.AddSingleton(typeof(IRefreshRepository<,>), typeof(RefreshRepository<,>));
        
        Configure<AWSThumbnailOptions>(configuration.GetSection("AWSThumbnail"));
        Configure<CAServerOptions>(configuration.GetSection("CAServer"));
        Configure<VariablesOptions>(configuration.GetSection("Variables"));
        Configure<MessagePushOptions>(configuration.GetSection("MessagePush"));
        context.Services.AddHttpContextAccessor();
        Configure<PinMessageOptions>(configuration.GetSection("PinMessageOptions"));
        Configure<UserAddressOptions>(configuration.GetSection("UserAddress"));
        
    }
}