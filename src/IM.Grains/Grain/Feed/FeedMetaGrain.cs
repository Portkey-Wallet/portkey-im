using IM.Chat;
using IM.Feed.Dtos;
using IM.Grains.State.Feed;
using Orleans;
using Volo.Abp.ObjectMapping;

namespace IM.Grains.Grain.Feed;

public class FeedMetaGrain : Grain<FeedMetaState>, IFeedMetaGrain
{
    private readonly IObjectMapper _objectMapper;

    public FeedMetaGrain(IObjectMapper objectMapper)
    {
        _objectMapper = objectMapper;
    }

    public async Task<FeedMetaDto> UpdateAsync(string id, int index)
    {
        State.Id = id;
        State.LastUpdateTime = DateTimeOffset.Now;
        State.EndIndex = index;
        if (State.MaxIndex < index)
        {
            State.MaxIndex = index;
        }

        await WriteStateAsync();

        return _objectMapper.Map<FeedMetaState, FeedMetaDto>(State);
    }

    public async Task<FeedMetaDto> AddAsync(string id, int index)
    {
        State.Id = id;
        State.LastUpdateTime = DateTimeOffset.MinValue;
        State.EndIndex = index;
        if (State.MaxIndex < index)
        {
            State.MaxIndex = index;
        }

        await WriteStateAsync();

        return _objectMapper.Map<FeedMetaState, FeedMetaDto>(State);
    }

    public Task<FeedMetaDto> GetAsync()
    {
        return Task.FromResult(_objectMapper.Map<FeedMetaState, FeedMetaDto>(State));
    }

    public async Task<GrainResultDto<bool>> SetIdleAsync()
    {
        State.ProcessStatus = ProcessStatus.Idle;
        State.LastUpdateTime = DateTimeOffset.Now;
        await WriteStateAsync();
        return new GrainResultDto<bool>
        {
            Data = true
        };
    }

    public async Task<GrainResultDto<bool>> SetRunningAsync()
    {
        if (State.ProcessStatus == ProcessStatus.Processing)
        {
            return
                new GrainResultDto<bool>
                {
                    Data = false
                };
        }

        State.ProcessStatus = ProcessStatus.Processing;
        State.LastUpdateTime = DateTimeOffset.Now;
        State.LastUpdateTime = DateTimeOffset.Now;
        await WriteStateAsync();
        return
            new GrainResultDto<bool>
            {
                Data = true
            }
            ;
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