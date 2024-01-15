using System.Collections.Generic;
using System.Threading.Tasks;
using IM.Message.Dtos;

namespace IM.Message;

public interface IProxyMessageAppService : IBaseMessageAppService
{
    Task<List<ListMessageResponseDto>> ListMessageWithHeaderAsync(ListMessageRequestDto input,
        Dictionary<string, string> headers);
    
    Task<UnreadCountResponseDto> GetUnreadMessageCountWithTokenAsync(string autoToken);
}