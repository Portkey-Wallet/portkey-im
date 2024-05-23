using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElf.Indexing.Elasticsearch;
using IM.ChannelContactService.Provider;
using IM.Chat;
using IM.Commons;
using IM.Dtos;
using IM.Entities.Es;
using IM.Enum.PinMessage;
using IM.Grains.Grain.Message;
using IM.Grains.Grain.RedPackage;
using IM.Message.Dtos;
using IM.Message.Etos;
using IM.Message.Provider;
using IM.Options;
using IM.PinMessage;
using IM.PinMessage.Dtos;
using IM.RedPackage;
using IM.Repository;
using IM.User;
using IM.User.Provider;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Net.Http.Headers;
using Nest;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using Orleans;
using Volo.Abp;
using Volo.Abp.Auditing;
using Volo.Abp.EventBus.Distributed;
using Volo.Abp.Security.Encryption;
using Volo.Abp.Users;

namespace IM.Message;

[RemoteService(false), DisableAuditing]
public class MessageAppService : ImAppService, IMessageAppService
{
    private readonly IChatAppService _chatAppService;
    private readonly IClusterClient _clusterClient;
    private readonly IDistributedEventBus _distributedEventBus;
    private readonly IStringEncryptionService _encryptionService;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ILogger<MessageAppService> _logger;
    private readonly INESTRepository<MessageInfoIndex, string> _messageInfoIndex;
    private readonly IRefreshRepository<PinMessageIndex, string> _pinMessageRepository;
    private readonly INESTRepository<UserIndex, Guid> _userRepository;
    private readonly IProxyMessageAppService _proxyMessageAppService;
    private readonly PinMessageOptions _pinMessageOptions;
    private readonly IGroupProvider _groupProvider;
    private readonly MessagePushOptions _messagePushOptions;
    private readonly IUserAppService _userAppService;
    private readonly IMessageAppProvider _messageAppProvider;
    private readonly IUserProvider _userProvider;


    public MessageAppService(IProxyMessageAppService proxyMessageAppService,
        IStringEncryptionService encryptionService,
        IChatAppService chatAppService,
        IClusterClient clusterClient,
        IDistributedEventBus distributedEventBus,
        ILogger<MessageAppService> logger,
        INESTRepository<MessageInfoIndex, string> messageInfoIndex,
        IHttpContextAccessor httpContextAccessor,
        IGroupProvider groupProvider,
        IOptionsSnapshot<MessagePushOptions> messagePushOptions,
        IOptionsSnapshot<PinMessageOptions> pinMessageOptions,
        INESTRepository<UserIndex, Guid> userRepository,
        IRefreshRepository<PinMessageIndex, string> pinMessageRepository,
        IUserAppService userAppService, IMessageAppProvider messageAppProvider, IUserProvider userProvider)
    {
        _proxyMessageAppService = proxyMessageAppService;
        _encryptionService = encryptionService;
        _chatAppService = chatAppService;
        _clusterClient = clusterClient;
        _distributedEventBus = distributedEventBus;
        _logger = logger;
        _messageInfoIndex = messageInfoIndex;
        _httpContextAccessor = httpContextAccessor;

        _userRepository = userRepository;
        _pinMessageRepository = pinMessageRepository;
        _pinMessageOptions = pinMessageOptions.Value;
        _groupProvider = groupProvider;
        _messagePushOptions = messagePushOptions.Value;
        _userAppService = userAppService;
        _messageAppProvider = messageAppProvider;
        _userProvider = userProvider;
    }

    public async Task<int> ReadMessageAsync(ReadMessageRequestDto input)
    {
        return await _proxyMessageAppService.ReadMessageAsync(input);
    }

    public async Task<SendMessageResponseDto> SendMessageAsync(SendMessageRequestDto input)
    {
        var responseDto = await _proxyMessageAppService.SendMessageAsync(input);
        if (responseDto == null || responseDto.ChannelUuid.IsNullOrEmpty())
        {
            return responseDto;
        }

        var authToken = GetAuthFromHeader();
        _ = PublishMessageAsync(input, CurrentUser.GetId(), authToken);
        return responseDto;
    }


    public async Task HideMessageAsync(HideMessageRequestDto input)
    {
        var pinMessageIndex = await _pinMessageRepository.GetAsync(input.Id);
        var pinMessageIndexQuote = await GetListByParamAsync(input.Id, null);

        if (pinMessageIndex == null && pinMessageIndexQuote.Count == 0)
        {
            await _proxyMessageAppService.HideMessageAsync(input);
        }

        if (pinMessageIndex != null)
        {
            await _proxyMessageAppService.HideMessageAsync(input);
            await _pinMessageRepository.DeleteIndexAsync(pinMessageIndex.Id, true);
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
                MessageId = input.Id,
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

            await SendMessageAsync(messageRequest);
        }

        await _proxyMessageAppService.HideMessageAsync(input);
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

    public async Task<UnreadCountResponseDto> GetUnreadMessageCountAsync()
    {
        return await _proxyMessageAppService.GetUnreadMessageCountAsync();
    }

    public async Task EventProcessAsync(EventMessageRequestDto input)
    {
        //todo:fix this when need to save message
        //ChannelUuid always exist ,check relation id 1st
        var chatId = "";
        var type = ChatType.P2P;
        if (!string.IsNullOrEmpty(input.ChannelUuid))
        {
            chatId = input.ChannelUuid;
            type = ChatType.GROUP;
        }
        else if (!string.IsNullOrEmpty(input.ToRelationId) && !string.IsNullOrEmpty(input.FromRelationId))
        {
            var ids = new List<string> { input.ToRelationId, input.FromRelationId };
            ids.Sort();

            chatId = string.Concat(ids);
            type = ChatType.P2P;
        }

        if (string.IsNullOrEmpty(chatId))
        {
            return;
        }

        var chatDto = await _chatAppService.GetChatAsync(chatId);
        if (chatDto == null)
        {
            var chatInfoDto = await _chatAppService.GetChatInfoAsync(chatId, input, type);
            if (chatInfoDto == null)
            {
                return;
            }

            chatDto = await _chatAppService.AddOrUpdateChatAsync(chatInfoDto);
            if (chatDto == null)
            {
                return;
            }
        }

        //if 5min always process maybe a problem，need to reset
        if (DateTimeOffset.Now.Subtract(chatDto.LastProcessTimeInMs) >
            TimeSpan.FromMinutes(MessageConsts.MaxRunningTimeInMin) && chatDto.ProcessStatus ==
            ProcessStatus.Processing)
        {
            await _chatAppService.ChatMetaSetIdleAsync(chatId);
            chatDto.ProcessStatus = ProcessStatus.Idle;
        }

        if (chatDto.ProcessStatus == ProcessStatus.Processing)
        {
            return;
        }

        var auth = _httpContextAccessor?.HttpContext?.Request?.Headers[RelationOneConstant.AuthHeader]
            .FirstOrDefault();
        if (string.IsNullOrEmpty(auth))
        {
            _logger.LogError("cant not get token from header,chatID:{chatId}", chatId);
            return;
        }

        var eto = ObjectMapper.Map<EventMessageRequestDto, EventMessageEto>(input);
        eto.ChatId = chatId;
        eto.CreationTime = DateTimeOffset.Now;
        eto.Token = auth;
        await _distributedEventBus.PublishAsync(eto);
    }

    public async Task ProcessMessageAsync(long startTime, long endTime, string startId, string endId, long pos,
        EventMessageEto eventData, ChatMetaDto dto)
    {
        var lastPos = pos - 1;
        var maxCreateAt = endTime;
        while (true)
        {
            var req = new ListMessageRequestDto
            {
                MaxCreateAt = maxCreateAt,
                Limit = ChatConsts.MaxPullMessageCount
            };
            if (dto.ChatType == ChatType.P2P)
            {
                req.ToRelationId = eventData.ToRelationId;
            }
            else
            {
                req.ChannelUuid = eventData.ChannelUuid;
            }

            var result = await _proxyMessageAppService.ListMessageWithHeaderAsync(req,
                new Dictionary<string, string> { { HeaderNames.Authorization, eventData.Token } }
            );

            //The interface returns messages in ascending order of time, and they need to be reversed
            result.Reverse();
            _logger.LogInformation("start {maxCreateAt},count {count}", maxCreateAt, result.Count);

            var finish = false;
            foreach (var listMessageResponseDto in result)
            {
                //Determine if the upper boundary has been exceeded
                if (listMessageResponseDto.Id == startId || listMessageResponseDto.CreateAt < startTime)
                {
                    finish = true;
                    break;
                }

                _logger.LogInformation("store message {Id},content {content}", listMessageResponseDto.Id,
                    listMessageResponseDto.Content);
                await StoreMessageAsync(listMessageResponseDto, lastPos, eventData);
                lastPos--;
            }

            //The next retrieval time is the time of the last message, which is the smallest time among the returned messages.
            maxCreateAt = result.Last().CreateAt;
            //If the returned count is less than the limit or has exceeded the upper boundary, the process will be terminated
            if (result.Count < ChatConsts.MaxPullMessageCount || finish)
            {
                //If the last message is not the upper boundary, the last message is the upper boundary
                var msg = await GetLastMessageAsync(eventData.ChatId);
                if (msg == null)
                {
                    _logger.LogError("can not get last message");
                    await _chatAppService.UpdateMetaAsync(eventData.ChatId, endTime, endTime, endId, endId,
                        ChatConsts.DonePosition);
                    break;
                }

                await _chatAppService.UpdateMetaAsync(eventData.ChatId, msg.CreateTimeInMs, msg.CreateTimeInMs, msg.Id,
                    msg.Id,
                    ChatConsts.DonePosition);
                break;
            }

            //If the number of messages returned is equal to the limit, it means that there may be more messages, and the next retrieval time is the time of the last message
            await _chatAppService.UpdateMetaAsync(eventData.ChatId, startTime, maxCreateAt, startId,
                result.Last().Id, lastPos);
        }
    }

    public async Task<List<ListMessageResponseDto>> ListMessageAsync(
        ListMessageRequestDto input)
    {
        var result = await _proxyMessageAppService.ListMessageAsync(input);
        var transferMessages = new List<ListMessageResponseDto>();
        foreach (var listMessageResponseDto in result)
        {
            if (listMessageResponseDto.Type == RedPackageConstant.RedPackageCardType)
            {
                await BuildRedPackageMessageAsync(listMessageResponseDto);
            }
            else if (listMessageResponseDto.Type == RedPackageConstant.TransferCardType)
            {
                transferMessages.Add(listMessageResponseDto);
            }
        }

        if (!transferMessages.IsNullOrEmpty())
        {
            await BuildTransferMessagesAsync(transferMessages);
        }

        var pinMessages = await GetListByParamAsync(null, input.ChannelUuid);

        if (pinMessages.Count > 0)
        {
            foreach (var dto in result)
            {
                var pinMessage = pinMessages.FirstOrDefault(p => p.Id == dto.Id);
                if (pinMessage == null)
                {
                    continue;
                }

                dto.PinInfo = new PinMessageInfo
                {
                    Pinner = pinMessage.PinInfo.Pinner,
                    PinnerName = pinMessage.PinInfo.PinnerName,
                    PinnedAt = pinMessage.PinInfo.PinnedAt
                };
            }
        }

        foreach (var dto in result.Where(dto => dto.Quote != null)
                     .Where(dto =>
                         dto.Quote.Content.ToLower() == CommonConstant.MessageHasBeenHidden ||
                         dto.Quote.Content.ToLower() == CommonConstant.MessageHasBeenDeletedLower))
        {
            dto.Quote!.Content = CommonConstant.MessageHasBeenDeleted;
        }

        return result;
    }

    public async Task<MessageInfoDto> GetLastMessageAsync(string chatId)
    {
        var mustQuery = new List<Func<QueryContainerDescriptor<MessageInfoIndex>, QueryContainer>>();
        mustQuery.Add(q => q.Term(t => t.Field(f => f.ChatId).Value(chatId)));

        QueryContainer Filter(QueryContainerDescriptor<MessageInfoIndex> f)
        {
            return f.Bool(b => b.Must(mustQuery));
        }

        Func<SortDescriptor<MessageInfoIndex>, IPromise<IList<ISort>>> sortDescriptor;
        sortDescriptor = s => s.Descending(f => f.MsgPosition).Descending(f => f.CreateTimeInMs);
        await _messageInfoIndex.GetSortListAsync();
        var list = await _messageInfoIndex.GetSortListAsync(Filter,
            sortFunc: sortDescriptor,
            limit: 1,
            skip: 0);
        if (list.Item2.Count == 0)
        {
            return null;
        }

        return ObjectMapper.Map<MessageInfoIndex, MessageInfoDto>(list.Item2.First());
    }

    public async Task StoreMessageAsync(ListMessageResponseDto input, long pos, EventMessageEto eventData)
    {
        var messageMetaGrain = _clusterClient.GetGrain<IMessageGrain>(input.Id);

        var messageInfo = ObjectMapper.Map<ListMessageResponseDto, MessageInfoIndex>(input);
        messageInfo.MsgType =
            System.Enum.TryParse<MessageType>(input.Type, out var messageType) ? messageType : MessageType.TEXT;
        messageInfo.MsgPosition = pos;
        messageInfo.ToId = eventData.ChannelUuid + eventData.ToRelationId;

        await _messageInfoIndex.AddOrUpdateAsync(messageInfo);
        var encryptData = _encryptionService.Encrypt(input.Content);
        await messageMetaGrain.AddMessageAsync(encryptData, 1, input.Id);
    }

    private async Task PublishMessageAsync(SendMessageRequestDto input, Guid userId, string authToken)
    {
        try
        {
            if (!_messagePushOptions.IsOpen)
            {
                _logger.LogDebug("message push not open");
                return;
            }

            if (input.Type.Equals(CommonConstant.PinSysMessageName, StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            var messageEto = ObjectMapper.Map<SendMessageRequestDto, MessageSendEto>(input);
            messageEto.UserId = userId;

            await AddGroupIfNotExistAsync(input.ChannelUuid, authToken);
            await _distributedEventBus.PublishAsync(messageEto, false, false);
        }
        catch (Exception e)
        {
            Logger.LogError(e, "publish send message error, userId: {userId}, groupId: {groupId}", CurrentUser.GetId(),
                input.ChannelUuid);
        }
    }

    private async Task AddGroupIfNotExistAsync(string channelUuid, string authToken)
    {
        var groupInfo = await _groupProvider.GetGroupInfosAsync(channelUuid);
        if (groupInfo != null)
        {
            return;
        }

        await _groupProvider.AddGroupAsync(channelUuid, authToken);
    }

    private string GetAuthFromHeader()
    {
        return _httpContextAccessor.HttpContext?.Request.Headers.GetOrDefault(RelationOneConstant.AuthHeader);
    }

    private async Task BuildRedPackageMessageAsync(ListMessageResponseDto input)
    {
        try
        {
            input.RedPackage ??= new RedPackageMessage();
            var content =
                JsonConvert.DeserializeObject<CustomMessage<RedPackageCard>>(input.Content);
            if (content.Data.Id == Guid.Empty)
            {
                _logger.LogError("Parse RedPackageCard error,Content:{Content}", input.Content);
                return;
            }

            var grain = _clusterClient.GetGrain<IRedPackageUserGrain>(
                RedPackageHelper.BuildUserViewKey(CurrentUser.GetId(), content.Data.Id));

            input.RedPackage.ViewStatus = (await grain.GetUserViewStatus()).Data;
        }
        catch (Exception e)
        {
            _logger.LogError(e, "BuildRedPackageMessageAsync error,Content:{Content}", input.Content);
        }
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

    private async Task BuildTransferMessagesAsync(List<ListMessageResponseDto> messages)
    {
        try
        {
            var userIds = new List<Guid>();
            var customMessages = new Dictionary<string, TransferCustomMessage<TransferCard>>();

            foreach (var message in messages)
            {
                var transferMessage = BuildTransferMessage(message);
                if (transferMessage == null || transferMessage.Data == null) continue;

                customMessages.Add(message.Id, transferMessage);
                if (Guid.TryParse(transferMessage?.Data?.ToUserId, out var id))
                {
                    userIds.Add(id);
                }
            }

            var caUserDtos = await _userAppService.GetCaHolderAsync(userIds.Distinct().ToList(), string.Empty);
            foreach (var transferMessage in customMessages)
            {
                var caUserDto = caUserDtos.FirstOrDefault(x => x.PortkeyId == transferMessage.Value.Data.ToUserId);
                if (caUserDto == null || string.IsNullOrEmpty(caUserDto.Name))
                {
                    continue;
                }

                transferMessage.Value.Data.ToUserName = caUserDto.Name;
                var messageResponseDto = messages.FirstOrDefault(t => t.Id == transferMessage.Key);
                if (messageResponseDto == null) continue;

                messageResponseDto.Content = JsonConvert.SerializeObject(transferMessage.Value,
                    new JsonSerializerSettings()
                    {
                        ContractResolver = new CamelCasePropertyNamesContractResolver()
                    });
            }
        }
        catch (Exception e)
        {
            _logger.LogError(e, "BuildTransferMessagesAsync error, userId:{userId}", CurrentUser.GetId());
        }
    }

    private TransferCustomMessage<TransferCard> BuildTransferMessage(ListMessageResponseDto message)
    {
        try
        {
            var transfer = JsonConvert.DeserializeObject<TransferCustomMessage<TransferCard>>(message.Content);

            if (transfer == null)
            {
                _logger.LogError("Parse Transfer card message error,Content:{Content}", message.Content);
                return null;
            }

            transfer.Data.SenderName = message.FromName;
            return transfer;
        }
        catch (Exception e)
        {
            _logger.LogError(e, "BuildTransferMessage error,Content:{Content}", message.Content);
            return null;
        }
    }
}