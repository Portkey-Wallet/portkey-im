using System.Collections.Generic;
using System.Threading.Tasks;
using IM.Message.Dtos;

namespace IM.Message;

public interface IBaseMessageAppService
{
    Task<object> HideMessageAsync(HideMessageRequestDto input);
    Task<int> ReadMessageAsync(ReadMessageRequestDto input);

    Task<List<ListMessageResponseDto>> ListMessageAsync(ListMessageRequestDto input);

    Task<SendMessageResponseDto> SendMessageAsync(SendMessageRequestDto input);
    Task<UnreadCountResponseDto> GetUnreadMessageCountAsync();
}