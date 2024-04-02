using System.Threading.Tasks;
using IM.Message.Dtos;

namespace IM.Message.Provider;

public interface IMessageAppProvider
{
    Task HideMessageByLeaderAsync(HideMessageByLeaderRequestDto input);

    Task<bool> IsMessageInChannelAsync(string channelUuid, string messageId);
}

