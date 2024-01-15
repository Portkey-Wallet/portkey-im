using IM.Feed.Dtos;
using IM.Grains.State.Feed;
using Orleans;

namespace IM.Grains.Grain.Feed;

public class FeedInfoGrain : Grain<FeedInfoState>, IFeedInfoGrain
{
    public async Task AddInfoAsync(List<FeedInfoGrainDto> data, string id)
    {
        State.Id = id;
        State.LastUpdateTime = DateTimeOffset.Now;
        if (data.Count > 0)
        {
            State.Data = data;
        }

        await WriteStateAsync();
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
}