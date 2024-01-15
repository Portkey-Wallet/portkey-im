using IM.RedPackage;
using Orleans;

namespace IM.Grains.Grain.RedPackage;

public interface IRedPackageUserGrain : IGrainWithStringKey
{
    Task<GrainResultDto<UserViewStatus>> GetUserViewStatus();
    Task SetUserViewStatus(UserViewStatus status);
}