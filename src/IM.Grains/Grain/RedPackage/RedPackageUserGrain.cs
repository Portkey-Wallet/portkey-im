using IM.Grains.State.RedPackage;
using IM.RedPackage;

namespace IM.Grains.Grain.RedPackage;

public class RedPackageUserGrain : Orleans.Grain<RedPackageUserState>, IRedPackageUserGrain
{
    public override async Task OnActivateAsync()
    {
        await ReadStateAsync();
        await base.OnActivateAsync();
    }

    public override async Task OnDeactivateAsync()
    {
        await WriteStateAsync();
        await base.OnDeactivateAsync();
    }
    
    public Task<GrainResultDto<UserViewStatus>> GetUserViewStatus()
    {
        var result = new GrainResultDto<UserViewStatus>();
        result.Success();
        result.Data = State.ViewStatus;
        return Task.FromResult(result);
    }
    
    public async Task SetUserViewStatus(UserViewStatus status)
    {
        if (State.ViewStatus == UserViewStatus.Init || State.ViewStatus == 0)
        {
            State.ViewStatus = status;
        }
        await WriteStateAsync();
    }
}