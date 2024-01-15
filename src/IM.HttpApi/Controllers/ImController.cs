using IM.Localization;
using Volo.Abp.AspNetCore.Mvc;

namespace IM.Controllers;

/* Inherit your controllers from this class.
 */
public abstract class ImController : AbpControllerBase
{
    protected ImController()
    {
        LocalizationResource = typeof(ImResource);
    }
}