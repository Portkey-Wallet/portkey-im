using IM.Feed.Dtos;
using Orleans;

namespace IM.Grains.Grain.Feed;

public interface IFeedInfoGrain : IGrainWithStringKey
{
    Task AddInfoAsync(List<FeedInfoGrainDto> data, string id);
}