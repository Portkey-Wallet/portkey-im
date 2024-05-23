using System.Collections.Generic;
using System.Threading.Tasks;
using IM.Message.Dtos;

namespace IM.Message.Provider;

public interface IMessageAppProvider
{
    Task HideMessageByLeaderAsync(HideMessageByLeaderRequestDto input);

    Task<bool> IsMessageInChannelAsync(string channelUuid, string messageId);        
    Task<IMMessageInfoDto> GetMessageByIdAsync(string channelUuid, string messageId);

    Task InsertMessageAsync(SendMessageRequestDto input);
    Task<List<ListMessageResponseDto>> FilterHideMessage(List<ListMessageResponseDto> result);
}

