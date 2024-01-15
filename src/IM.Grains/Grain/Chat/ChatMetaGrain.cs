using IM.Chat;
using IM.Grains.State.Chat;
using Orleans;
using Volo.Abp.ObjectMapping;

namespace IM.Grains.Grain.Chat;

public class ChatMetaGrain : Grain<ChatMetaState>, IChatMetaGrain
{
    private readonly IObjectMapper _objectMapper;

    public ChatMetaGrain(IObjectMapper objectMapper)
    {
        _objectMapper = objectMapper;
    }

    public Task<GrainResultDto<ChatMetaGrainDto>> GetByIdAsync(string Id)
    {
        if (string.IsNullOrEmpty(Id))
        {
            return Task.FromResult(new GrainResultDto<ChatMetaGrainDto>
            {
                Data = new ChatMetaGrainDto()
            });
        }

        return Task.FromResult(
            new GrainResultDto<ChatMetaGrainDto>
            {
                Data = _objectMapper.Map<ChatMetaState, ChatMetaGrainDto>(State)
            }
        );
    }

    public async Task<GrainResultDto<ChatMetaGrainDto>> AddOrUpdateAsync(ChatMetaGrainDto chatMeta)
    {
        if (chatMeta.IsEmpty())
        {
            return null;
        }

        if (State.IsEmpty())
        {
            State = _objectMapper.Map<ChatMetaGrainDto, ChatMetaState>(chatMeta);
            State.ProcessStatus = ProcessStatus.Idle;
            State.UpperTime = 0;
            State.LowerTime = 0;
            State.UpperId = "";
            State.LowerId = "";
        }
        else
        {
            State = _objectMapper.Map<ChatMetaGrainDto, ChatMetaState>(chatMeta);
        }

        await WriteStateAsync();
        return new GrainResultDto<ChatMetaGrainDto>
        {
            Data = _objectMapper.Map<ChatMetaState, ChatMetaGrainDto>(State)
        };
    }

    public async Task<GrainResultDto<bool>> SetIdleAsync()
    {
        State.ProcessStatus = ProcessStatus.Idle;
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
            return new GrainResultDto<bool>
            {
                Data = false
            };
        }

        State.ProcessStatus = ProcessStatus.Processing;
        await WriteStateAsync();
        return new GrainResultDto<bool>
        {
            Data = true
        };
    }

    public async Task UpdateMetaAsync(long upperTime, long lowerTime, string upperId, string lowerId, long pos)
    {
        State.LastProcessTimeInMs = DateTimeOffset.Now;
        State.UpperTime = upperTime;
        State.LowerTime = lowerTime;
        State.UpperId = upperId;
        State.LowerId = lowerId;
        State.Pos = pos;
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