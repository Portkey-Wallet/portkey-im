using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AElf.Indexing.Elasticsearch;
using Dapper;
using IM.ChannelContact;
using IM.Commons;
using IM.Dapper.Repository;
using IM.Entities.Es;
using IM.Enum.PinMessage;
using IM.Message.Dtos;
using IM.Options;
using IM.PinMessage;
using IM.PinMessage.Dtos;
using IM.Repository;
using IM.User.Provider;
using Microsoft.Extensions.Options;
using Nest;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using Volo.Abp;
using Volo.Abp.DependencyInjection;

namespace IM.Message.Provider;

public class MessageAppProvider : ImAppService, IMessageAppProvider, ISingletonDependency
{
    private readonly IChannelContactAppService _channelContactAppService;
    private readonly IImRepository _imRepository;
    private readonly IRefreshRepository<PinMessageIndex, string> _pinMessageRepository;
    private readonly PinMessageOptions _pinMessageOptions;
    private readonly INESTRepository<UserIndex, Guid> _userRepository;
    private readonly IMessageAppService _messageAppService;
    private readonly IUserProvider _userProvider;

    public MessageAppProvider(IChannelContactAppService channelContactAppService, IImRepository imRepository,
        IRefreshRepository<PinMessageIndex, string> pinMessageRepository,
        IOptionsSnapshot<PinMessageOptions> pinMessageOptions, INESTRepository<UserIndex, Guid> userRepository, IMessageAppService messageAppService, IUserProvider userProvider)
    {
        _channelContactAppService = channelContactAppService;
        _imRepository = imRepository;
        _pinMessageRepository = pinMessageRepository;
        _userRepository = userRepository;
        _messageAppService = messageAppService;
        _userProvider = userProvider;
        _pinMessageOptions = pinMessageOptions.Value;
    }

    public async Task HideMessageByLeaderAsync(HideMessageByLeaderRequestDto input)
    {
        var isAdmin = await _channelContactAppService.IsAdminAsync(input.ChannelUuId);
        if (!isAdmin)
        {
            throw new UserFriendlyException(CommonConstant.NoPermission);
        }

        var messageInfo = await GetMessageByIdAsync(input.ChannelUuId, input.MessageId);
        if (messageInfo == null)
        {
            throw new UserFriendlyException(CommonConstant.MessageNotExist);
        }

        var pinMessageIndex = await _pinMessageRepository.GetAsync(input.MessageId);
        var pinMessageIndexQuote = await GetListByParamAsync(input.MessageId, null);

        if (pinMessageIndex == null && pinMessageIndexQuote.Count == 0)
        {
            if (messageInfo.Status == 1)
            {
                return;
            }
            await DeleteMessageManuallyAsync(input.MessageId);
        }

        if (null != pinMessageIndex)
        {
            var portkeyId = CurrentUser.Id;
            if (portkeyId == null)
            {
                throw new UserFriendlyException(CommonConstant.UserNotExist);
            }

            var userIndex = await _userRepository.GetAsync((Guid)portkeyId);
            if (userIndex == null)
            {
                throw new UserFriendlyException(CommonConstant.UserNotExist);
            }

            await _pinMessageRepository.DeleteIndexAsync(pinMessageIndex.Id, true);
            var pinMessageType = System.Enum.TryParse<MessageType>(pinMessageIndex.Type, out var messageType)
                ? messageType
                : MessageType.TEXT;
            var pinMessageSysInfo = new PinMessageSysInfo
            {
                UserInfo = new UserInfo
                {
                    PortkeyId = portkeyId.ToString(),
                    RelationId = userIndex.RelationId,
                    Name = userIndex.Name,
                    Avatar = userIndex.Avatar
                },
                PinType = PinMessageOperationType.UnPin,
                MessageType = pinMessageType.ToString(),
                MessageId = input.MessageId,
                SendUuid = pinMessageIndex.SendUuid,
                Content = pinMessageIndex.Content
            };

            var sendUuid = userIndex.RelationId + "-" + pinMessageIndex.ChannelUuid + "-" + DateTime.UtcNow.Second +
                           "-" +
                           Guid.NewGuid();
            var settings = new JsonSerializerSettings
            {
                ContractResolver = new CamelCasePropertyNamesContractResolver()
            };
            var messageRequest = new SendMessageRequestDto
            {
                ChannelUuid = pinMessageIndex.ChannelUuid,
                Type = CommonConstant.PinSysName,
                SendUuid = sendUuid,
                Content = JsonConvert.SerializeObject(pinMessageSysInfo, settings)
            };

            await _messageAppService.SendMessageAsync(messageRequest);
        }
        await DeleteMessageManuallyAsync(input.MessageId);
        if (pinMessageIndexQuote.Count > 0)
        {
            foreach (var pinMessage in pinMessageIndexQuote)
            {
                pinMessage.Type = MessageType.TEXT.ToString();
                pinMessage.Quote.ChannelUuid = null;
                pinMessage.Quote.From = null;
                pinMessage.Quote.FromAvatar = null;
                pinMessage.Quote.FromName = null;
                pinMessage.Quote.Id = "0";
                pinMessage.Quote.SendUuid = null;
                pinMessage.Quote.Content = CommonConstant.MessageHasBeenDeleted;
                await _pinMessageRepository.AddOrUpdateIndexAsync(pinMessage, true);
            }
        }
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
        var messageInfo = await GetMessageByIdAsync(channelUuid,messageId);
        return messageInfo is { Status: 0 };
    }

    public async Task<IMMessageInfoDto> GetMessageByIdAsync(string channelUuid, string messageId)
    {
        var parameters = new DynamicParameters();
        parameters.Add("@channelUuid", channelUuid);
        parameters.Add("@messageId", messageId);

        var sql =
            "select id as Id,send_uuid as SendUuid,channel_uuid as ChannelUuid,quote_id as QuoteId , status as status, mentioned_user as mentionedUser,block_relation_id AS BlockRelationId from im_message where channel_uuid=@channelUuid  and id=@messageId limit 1;";
        var imMessageInfoDto = await _imRepository.QueryFirstOrDefaultAsync<IMMessageInfoDto>(sql, parameters);
        return imMessageInfoDto;
    }

    public async Task InsertMessageAsync(SendMessageRequestDto input)
    {
        var userIndex = await _userProvider.GetUserInfoByIdAsync((Guid)CurrentUser.Id);
        var parameters = new DynamicParameters();
        var now = new DateTimeOffset(DateTime.UtcNow).ToUnixTimeSeconds();
        parameters.Add("@id", now);
        parameters.Add("@sendUuid", input.SendUuid);
        parameters.Add("@channelUuid", input.ChannelUuid);
        parameters.Add("@content", input.Content);
        parameters.Add("@type", input.Type);
        //parameters.Add("@mentionedUser", input.MentionedUser);
        parameters.Add("@blockRelationId", input.BlockRelationId);
        parameters.Add("@from",userIndex.RelationId);
        var sql =
            "INSERT INTO im_message (id,send_uuid, `from`,channel_uuid,content,type,block_relation_id) VALUES (@id,@sendUuid,@from ,@channelUuid,@content,@type,@blockRelationId);";
        await _imRepository.ExecuteAsync(sql, parameters);
    }

    public async Task<List<ListMessageResponseDto>> FilterHideMessage(List<ListMessageResponseDto> tempList)
    {
        var userIndex = await _userProvider.GetUserInfoByIdAsync((Guid)CurrentUser.Id);
        var result = new List<ListMessageResponseDto>();
        foreach (var dto in tempList)
        {
            var message = await GetMessageByIdAsync(dto.ChannelUuid, dto.Id);
            if (!string.IsNullOrEmpty(message.BlockRelationId) && message.BlockRelationId == userIndex.RelationId)
            {
                continue;
            }
            result.Add(dto);
        }
        return result;
    }


    private async Task<List<PinMessageIndex>> GetListByParamAsync(string quoteId, string channelUuid)
    {
        var mustQuery = new List<Func<QueryContainerDescriptor<PinMessageIndex>, QueryContainer>>();
        if (quoteId != null)
        {
            mustQuery.Add(q => q.Term(i => i.Field(f => f.Quote.Id).Value(quoteId)));
        }

        if (channelUuid != null)
        {
            mustQuery.Add(q => q.Term(i => i.Field(f => f.ChannelUuid).Value(channelUuid)));
        }

        QueryContainer Filter(QueryContainerDescriptor<PinMessageIndex> f)
        {
            return f.Bool(b => b.Must(mustQuery));
        }

        var pinMessageResult = await _pinMessageRepository.GetListAsync(Filter, skip: 0,
            limit: _pinMessageOptions.MaxPinMessageCount);

        return pinMessageResult.Item2;
    }
}