using System.Threading.Tasks;
using Dapper;
using IM.ChannelContact;
using IM.Commons;
using IM.Dapper.Repository;
using IM.Message.Dtos;
using Volo.Abp;
using Volo.Abp.DependencyInjection;

namespace IM.Message.Provider;

public class MessageAppProvider : IMessageAppProvider, ISingletonDependency
{
    private readonly IChannelContactAppService _channelContactAppService;
    private readonly IImRepository _imRepository;

    public MessageAppProvider(IChannelContactAppService channelContactAppService, IImRepository imRepository)
    {
        _channelContactAppService = channelContactAppService;
        _imRepository = imRepository;
    }

    public async Task HideMessageByLeaderAsync(HideMessageByLeaderRequestDto input)
    {
        var isAdmin = await _channelContactAppService.IsAdminAsync(input.ChannelUuId);
        if (!isAdmin)
        {
            throw new UserFriendlyException(CommonConstant.NoPermission);
        }

        var isMessageInChannel = await IsMessageInChannelAsync(input.ChannelUuId, input.MessageId);
        if (!isMessageInChannel)
        {
            throw new UserFriendlyException(CommonConstant.MessageNotExist);
        }

        await DeleteMessageManuallyAsync(input.MessageId);
    }

    private async Task DeleteMessageManuallyAsync(string messageId)
    {
        var parameters = new DynamicParameters();
        parameters.Add("@messageId", messageId);
        var sql =
            "update im_message set status = 1 where id = @messageId;";
        await _imRepository.ExecuteAsync(sql, parameters);
    }


    public async Task<bool> IsMessageInChannelAsync(string channelUuid, string messageId)
    {
        var parameters = new DynamicParameters();
        parameters.Add("@channelUuid", channelUuid);
        parameters.Add("@messageId", messageId);

        var sql =
            "select id as Id,send_uuid as SendUuid,channel_uuid as ChannelUuid,quote_id as QuoteId , mentioned_user as mentionedUser from im_message where channel_uuid=@channelUuid and status=0 and id=@messageId limit 1;";
        var messageInfo = await _imRepository.QueryFirstOrDefaultAsync<IMMessageInfoDto>(sql, parameters);
        return messageInfo != null;
    }
}