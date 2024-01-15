namespace IM.Grains.Grain.Mute;

using Orleans;

public interface IMuteGrain : IGrainWithStringKey
{
    Task<GrainResultDto<MuteGrainDto>> ModifyMute(MuteGrainDto mute);
    Task<GrainResultDto<MuteGrainDto>> GetMute();
}