using Volo.Abp.Settings;

namespace IM.Settings;

public class ImSettingDefinitionProvider : SettingDefinitionProvider
{
    public override void Define(ISettingDefinitionContext context)
    {
        //Define your own settings here. Example:
        //context.Add(new SettingDefinition(IMSettings.MySetting1));
    }
}
