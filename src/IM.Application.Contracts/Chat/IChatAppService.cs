using System.Threading.Tasks;
using IM.Chat;
using IM.Dtos;
using IM.Message.Dtos;

namespace IM;

public interface IChatAppService
{
    Task<ChatMetaDto> GetChatAsync(string id);
    Task<ChatMetaDto> AddOrUpdateChatAsync(ChatMetaDto chat);

    Task<ChatMetaDto> GetChatInfoAsync(string chatId, EventMessageRequestDto reqDto, ChatType type);

    Task<bool> ChatMetaSetRunningAsync(string id);

    Task<bool> ChatMetaSetIdleAsync(string id);

    Task UpdateMetaAsync(string chatId, long upperTime, long lowerTime, string upperId, string lowerId, long pos);
}