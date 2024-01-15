using Orleans;

namespace IM.Grains.Grain.Message;

public interface IMessageGrain : IGrainWithStringKey
{
    Task<GrainResultDto<bool>> AddMessageAsync(string contextEncrypt, int keyVersion, string Id);
}