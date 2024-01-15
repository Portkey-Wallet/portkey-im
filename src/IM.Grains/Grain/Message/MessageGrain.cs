using IM.Grains.State.MessageState;
using Orleans;

namespace IM.Grains.Grain.Message;

public class MessageGrain : Grain<MessageState>, IMessageGrain
{
    public async Task<GrainResultDto<bool>> AddMessageAsync(string contextEncrypt, int keyVersion, string id)
    {
        State.ContextEncrypt = contextEncrypt;
        State.KeyVersion = keyVersion;
        State.Id = id;
        await WriteStateAsync();
        return new GrainResultDto<bool>
        {
            Data = true
        };
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