using IM.Localization;
using Volo.Abp.Authorization.Permissions;
using Volo.Abp.Localization;

namespace IM.Permissions;

public class ImPermissionDefinitionProvider : PermissionDefinitionProvider
{
    public override void Define(IPermissionDefinitionContext context)
    {
        var myGroup = context.AddGroup(ImPermissions.GroupName);
        //Define your own permissions here. Example:
        //myGroup.AddPermission(IMPermissions.MyPermission1, L("Permission:MyPermission1"));
    }

    private static LocalizableString L(string name)
    {
        return LocalizableString.Create<ImResource>(name);
    }
}
