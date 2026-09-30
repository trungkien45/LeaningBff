using LearningBff.Localization;
using Volo.Abp.Authorization.Permissions;
using Volo.Abp.Localization;
using Volo.Abp.MultiTenancy;

namespace LearningBff.Permissions;

public class LearningBffPermissionDefinitionProvider : PermissionDefinitionProvider
{
    public override void Define(IPermissionDefinitionContext context)
    {
        var myGroup = context.AddGroup(LearningBffPermissions.GroupName);


        
        //Define your own permissions here. Example:
        //myGroup.AddPermission(LearningBffPermissions.MyPermission1, L("Permission:MyPermission1"));
    }

    private static LocalizableString L(string name)
    {
        return LocalizableString.Create<LearningBffResource>(name);
    }
}
