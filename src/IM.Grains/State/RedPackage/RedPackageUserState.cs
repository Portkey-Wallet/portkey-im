using IM.RedPackage;

namespace IM.Grains.State.RedPackage;

public class RedPackageUserState
{
    public UserViewStatus ViewStatus { get; set; } = UserViewStatus.Init;
}