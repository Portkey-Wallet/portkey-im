using Microsoft.Extensions.DependencyInjection;
using Volo.Abp.AutoMapper;
using Volo.Abp.Modularity;

namespace IM.Signature;

public class ImSignatureModule : AbpModule
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        Configure<SignatureServerOptions>(context.Services.GetConfiguration().GetSection("SignatureServer"));
    }
}