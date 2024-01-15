using System;
using System.Collections.Generic;
using System.Text;
using IM.Localization;
using Volo.Abp.Application.Services;

namespace IM;

/* Inherit your application services from this class.
 */
public abstract class ImAppService : ApplicationService
{
    protected ImAppService()
    {
        LocalizationResource = typeof(ImResource);
    }
}
