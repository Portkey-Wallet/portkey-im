using IM.Commons;
using IM.Grains.Grain.User;
using IM.Grains.State.User;
using Volo.Abp.ObjectMapping;
using Orleans;

namespace IM.Grains.Grain;

public class UserGrain : Grain<UserState>, IUserGrain
{
    private readonly IObjectMapper _objectMapper;

    public UserGrain(IObjectMapper objectMapper)
    {
        _objectMapper = objectMapper;
    }

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

    public async Task<GrainResultDto<UserGrainDto>> AddUser(UserGrainDto user)
    {
        var result = new GrainResultDto<UserGrainDto>();
        if (State.Id != Guid.Empty && !State.IsDeleted)
        {
            result.Code = CommonResult.UserExistCode;
            return result;
        }

        State = _objectMapper.Map<UserGrainDto, UserState>(user);
        State.Id = this.GetPrimaryKey();
        State.CreateTime = TimeHelper.GetTimeStampInMilliseconds();
        State.LastModifyTime = TimeHelper.GetTimeStampInMilliseconds();

        await WriteStateAsync();

        result.Data = _objectMapper.Map<UserState, UserGrainDto>(State);
        return result;
    }

    public async Task<GrainResultDto<UserGrainDto>> UpdateUser(UserGrainDto user)
    {
        var result = new GrainResultDto<UserGrainDto>();
        if (State.Id != Guid.Empty && State.IsDeleted)
        {
            result.Code = CommonResult.UserNotExistCode;
            return result;
        }

        State = _objectMapper.Map<UserGrainDto, UserState>(user);
        State.Id = this.GetPrimaryKey();
        State.LastModifyTime = TimeHelper.GetTimeStampInMilliseconds();

        await WriteStateAsync();

        result.Data = _objectMapper.Map<UserState, UserGrainDto>(State);
        return result;
    }

    public async Task<GrainResultDto<UserGrainDto>> DeleteUser()
    {
        var result = new GrainResultDto<UserGrainDto>();
        if (State.Id != Guid.Empty && !State.IsDeleted)
        {
            result.Code = CommonResult.UserNotExistCode;
            return result;
        }

        State.IsDeleted = true;
        State.LastModifyTime = TimeHelper.GetTimeStampInMilliseconds();
        await WriteStateAsync();

        result.Data = _objectMapper.Map<UserState, UserGrainDto>(State);
        return result;
    }

    public Task<bool> Exist() =>
        Task.FromResult(State.Id != Guid.Empty && !State.IsDeleted);

    public Task<bool> NeedUpdate()
    {
        if (State.Id != Guid.Empty && State.IsDeleted)
        {
            return Task.FromResult(false);
        }

        return Task.FromResult(State.CaAddresses == null || State.CaAddresses.Count < 2);
    }

    public Task<UserGrainDto> GetUser()
    {
        return Task.FromResult(_objectMapper.Map<UserState, UserGrainDto>(State));
    }
}