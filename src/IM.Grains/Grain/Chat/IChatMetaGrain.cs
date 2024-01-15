using Orleans;

namespace IM.Grains.Grain.Chat;

public interface IChatMetaGrain : IGrainWithStringKey
{
    Task<GrainResultDto<ChatMetaGrainDto>> GetByIdAsync(string id);

    Task<GrainResultDto<ChatMetaGrainDto>> AddOrUpdateAsync(ChatMetaGrainDto chatMeta);

    Task<GrainResultDto<bool>> SetRunningAsync();

    Task<GrainResultDto<bool>> SetIdleAsync();

    Task UpdateMetaAsync(long upperTime, long lowerTime, string upperId, string lowerId, long pos);
}