using IM.Commons;
using IM.Grains.State.Mute;
using Orleans;
using Volo.Abp.ObjectMapping;

namespace IM.Grains.Grain.Mute;

public class MuteGrain : Grain<MuteState>, IMuteGrain
{
    private readonly IObjectMapper _objectMapper;

    public MuteGrain(IObjectMapper objectMapper)
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

    public async Task<GrainResultDto<MuteGrainDto>> ModifyMute(MuteGrainDto mute)
    {
        var result = new GrainResultDto<MuteGrainDto>();
        State = _objectMapper.Map<MuteGrainDto, MuteState>(mute);
        State.Id = this.GetPrimaryKeyString();
        State.LastModificationTime = DateTime.UtcNow;

        await WriteStateAsync();
        result.Data = _objectMapper.Map<MuteState, MuteGrainDto>(State);
        return result;
    }

    public Task<GrainResultDto<MuteGrainDto>> GetMute()
    {
        var result = new GrainResultDto<MuteGrainDto>();
        if (State.Id.IsNullOrWhiteSpace())
        {
            result.Code = CommonResult.MuteNotExistCode;
            return Task.FromResult(result);
        }

        result.Data = _objectMapper.Map<MuteState, MuteGrainDto>(State);
        return Task.FromResult(result);
    }
}