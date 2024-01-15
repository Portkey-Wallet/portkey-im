using IM.Feed.Dtos;
using Orleans;

namespace IM.Grains.Grain.Feed;

public interface IFeedMetaGrain : IGrainWithStringKey
{
    Task<FeedMetaDto> AddAsync(string id, int index);

    Task<FeedMetaDto> UpdateAsync(string id, int index);

    Task<FeedMetaDto> GetAsync();

    Task<GrainResultDto<bool>> SetRunningAsync();

    Task<GrainResultDto<bool>> SetIdleAsync();
}