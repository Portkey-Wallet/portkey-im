using System.Threading.Tasks;
using IM.Dtos;
using IM.Grains.Grain.Chat;
using IM.Message.Dtos;
using Orleans;
using Volo.Abp;
using Volo.Abp.Auditing;

namespace IM.Chat;

[RemoteService(false), DisableAuditing]
public class ChatAppService : ImAppService, IChatAppService
{
    private readonly IClusterClient _clusterClient;

    public ChatAppService(IClusterClient clusterClient)
    {
        _clusterClient = clusterClient;
    }

    public async Task<ChatMetaDto> GetChatAsync(string id)
    {
        var chatMetaGrain = _clusterClient.GetGrain<IChatMetaGrain>(id);
        var chatMetaGrainDto = await chatMetaGrain.GetByIdAsync(id);
        if (chatMetaGrainDto.Data.IsEmpty())
        {
            return null;
        }

        return ObjectMapper.Map<ChatMetaGrainDto, ChatMetaDto>(chatMetaGrainDto.Data);
    }

    public async Task<ChatMetaDto> AddOrUpdateChatAsync(ChatMetaDto chat)
    {
        var chatMetaGrain = _clusterClient.GetGrain<IChatMetaGrain>(chat.Id);
        var chatMetaGrainDto =
            await chatMetaGrain.AddOrUpdateAsync(ObjectMapper.Map<ChatMetaDto, ChatMetaGrainDto>(chat));
        return ObjectMapper.Map<ChatMetaGrainDto, ChatMetaDto>(chatMetaGrainDto.Data);
    }

    public async Task<ChatMetaDto> GetChatInfoAsync(string chatId, EventMessageRequestDto reqDto, ChatType type)
    {
        if (type == ChatType.P2P)
        {
            return new ChatMetaDto
            {
                Id = chatId,
                ChatType = ChatType.P2P
            };
        }

        return new ChatMetaDto
        {
            Id = chatId,
            ChatType = ChatType.GROUP
        };

        return null;
    }

    public async Task<bool> ChatMetaSetRunningAsync(string id)
    {
        var chatMetaGrain = _clusterClient.GetGrain<IChatMetaGrain>(id);
        if (chatMetaGrain == null)
        {
            return false;
        }

        var result = await chatMetaGrain.SetRunningAsync();
        return result.Data;
    }

    public async Task<bool> ChatMetaSetIdleAsync(string id)
    {
        var chatMetaGrain = _clusterClient.GetGrain<IChatMetaGrain>(id);
        if (chatMetaGrain == null)
        {
            return false;
        }

        var result = await chatMetaGrain.SetIdleAsync();
        return result.Data;
    }

    public async Task UpdateMetaAsync(string chatId, long upperTime, long lowerTime, string upperId, string lowerId,
        long pos)
    {
        var chatMetaGrain = _clusterClient.GetGrain<IChatMetaGrain>(chatId);
        await chatMetaGrain.UpdateMetaAsync(upperTime, lowerTime, upperId, lowerId, pos);
    }
}