using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElf.Indexing.Elasticsearch;
using IM.ChannelContactService.Provider;
using IM.Chat;
using IM.Commons;
using IM.Entities.Es;
using IM.Message;
using IM.Message.Dtos;
using IM.Message.Etos;
using IM.Message.Provider;
using IM.Options;
using IM.User.Provider;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Nest;
using Volo.Abp;
using Volo.Abp.DependencyInjection;
using Volo.Abp.EventBus.Distributed;
using Volo.Abp.ObjectMapping;

namespace IM.EntityEventHandler.Core;

public class MessageHandler : IDistributedEventHandler<EventMessageEto>, IDistributedEventHandler<MessageSendEto>,
    ITransientDependency
{
    private readonly IChatAppService _chatAppService;
    private readonly ILogger<MessageHandler> _logger;
    private readonly IMessageAppService _messageAppService;
    private readonly IObjectMapper _objectMapper;
    private readonly INESTRepository<GroupIndex, string> _groupRepository;
    private readonly IMessagePushProvider _messagePushProvider;
    private readonly IUserProvider _userProvider;
    private readonly MessagePushOptions _messagePushOptions;
    private readonly IChannelProvider _channelProvider;

    public MessageHandler(
        ILogger<MessageHandler> logger,
        IMessageAppService messageAppService,
        IChatAppService chatAppService,
        IObjectMapper objectMapper,
        INESTRepository<GroupIndex, string> groupRepository,
        IMessagePushProvider messagePushProvider,
        IUserProvider userProvider,
        IOptionsSnapshot<MessagePushOptions> messagePushOptions,
        IChannelProvider channelProvider)
    {
        _logger = logger;
        _messageAppService = messageAppService;
        _chatAppService = chatAppService;
        _objectMapper = objectMapper;
        _groupRepository = groupRepository;
        _messagePushProvider = messagePushProvider;
        _userProvider = userProvider;
        _channelProvider = channelProvider;
        _messagePushOptions = messagePushOptions.Value;
    }

    public async Task HandleEventAsync(EventMessageEto eventData)
    {
        try
        {
            if (await _chatAppService.ChatMetaSetRunningAsync(eventData.ChatId) == false)
            {
                _logger.LogInformation("chat {Id} has already running", eventData.ChatId);
                return;
            }

            var chatMetaDto = await _chatAppService.GetChatAsync(eventData.ChatId);

            /*
             * when chatMetaDto.Pos == ChatConsts.DonePosition
             * The retrieval of historical messages is complete, and you can now start pulling messages from the latest timestamp
             */
            await _messageAppService.ProcessMessageAsync(chatMetaDto.UpperTime,
                chatMetaDto.LowerTime == 0 || chatMetaDto.UpperTime == chatMetaDto.LowerTime
                    ? DateTimeOffset.Now.ToUnixTimeMilliseconds()
                    : chatMetaDto.LowerTime,
                chatMetaDto.UpperId, chatMetaDto.LowerId,
                chatMetaDto.Pos == ChatConsts.DonePosition || chatMetaDto.Pos == 0
                    ? DateTimeOffset.Now.ToUnixTimeMilliseconds()
                    : chatMetaDto.Pos, eventData, chatMetaDto);
            await _chatAppService.ChatMetaSetIdleAsync(eventData.ChatId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "handle message event error");
            await _chatAppService.ChatMetaSetIdleAsync(eventData.ChatId);
        }
    }

    public async Task HandleEventAsync(MessageSendEto eventData)
    {
        try
        {
            if (!_messagePushOptions.IsOpen)
            {
                return;
            }

            var user = await _userProvider.GetUserInfoByIdAsync(eventData.UserId);
            var userName = user?.Name;
            if (userName.IsNullOrWhiteSpace())
            {
                userName = CommonConstant.DefaultDisplayName;
            }

            var groupId = eventData.ChannelUuid;
            var groupInfo = await GetGroupInfosAsync(groupId);

            if (groupInfo == null)
            {
                throw new UserFriendlyException($"group not exist, groupId:{groupId}");
            }

            var muteUserIds = await _channelProvider.GetMuteMembersAsync(groupId);
            var toUserIds = groupInfo.Members
                .Where(f => f.PortKeyId != eventData.UserId.ToString() && !muteUserIds.Contains(f.RelationId))
                .Select(t => t.PortKeyId).Distinct().ToList();

            if (toUserIds.IsNullOrEmpty())
            {
                _logger.LogWarning("send to users is empty, userId:{userId}, groupId:{groupId}", eventData.UserId,
                    groupId);
                return;
            }

            var message = new ImMessagePushDto()
            {
                Content = MessageHelper.GetContent(eventData.Type, eventData.Content),
                SenderName = userName,
                ChannelId = eventData.ChannelUuid
            };

            if (groupInfo.Type.Equals(GroupType.G.ToString(), StringComparison.OrdinalIgnoreCase))
            {
                message.GroupName = groupInfo.Name;
                message.Icon = groupInfo.Icon;
                message.ChatType = ChatType.GROUP;
            }
            else
            {
                message.SenderName = userName;
                message.Icon = user?.Avatar;
                message.ChatType = ChatType.P2P;
            }

            message.ToUserIds = toUserIds;
            await _messagePushProvider.PushImMessageAsync(message);
            _logger.LogDebug(
                "send im message to message push, userId: {userId}, groupId: {groupId}, ToRelationId:{toRelationId}, content:{content}",
                eventData.UserId, eventData.ChannelUuid ?? string.Empty, eventData.ToRelationId ?? string.Empty,
                eventData.Content);
        }
        catch (Exception e)
        {
            _logger.LogError(e,
                "handle push message event error, userId: {userId}, groupId: {groupId}, ToRelationId:{toRelationId}, content:{content}",
                eventData.UserId, eventData.ChannelUuid ?? string.Empty, eventData.ToRelationId ?? string.Empty,
                eventData.Content);
        }
    }

    private async Task<GroupIndex> GetGroupInfosAsync(string groupId)
    {
        var mustQuery = new List<Func<QueryContainerDescriptor<GroupIndex>, QueryContainer>>()
        {
            descriptor => descriptor.Term(i => i.Field(f => f.Id).Value(groupId))
        };

        QueryContainer Filter(QueryContainerDescriptor<GroupIndex> f) => f.Bool(b => b.Must(mustQuery));
        return await _groupRepository.GetAsync(Filter);
    }
}