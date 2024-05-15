using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElf.Indexing.Elasticsearch;
using IM.ChannelContact;
using IM.Commons;
using IM.Contact;
using IM.Entities.Es;
using IM.Enum.PinMessage;
using IM.Message;
using IM.Message.Dtos;
using IM.Message.Provider;
using IM.Options;
using IM.PinMessage.Dtos;
using IM.Repository;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Nest;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using Volo.Abp;
using Volo.Abp.Auditing;
using Volo.Abp.ObjectMapping;

namespace IM.PinMessage;

[RemoteService(false), DisableAuditing]
public class PinMessageAppService : ImAppService, IPinMessageAppService
{
    private readonly IRefreshRepository<PinMessageIndex, string> _pinMessageRepository;
    private readonly IObjectMapper _objectMapper;
    private readonly PinMessageOptions _pinMessageOptions;
    private readonly IChannelContactAppService _channelContactAppService;
    private readonly INESTRepository<UserIndex, Guid> _userRepository;
    private readonly IMessageAppService _messageAppService;
    private readonly ILogger<PinMessageAppService> _logger;
    private readonly IContactAppService _contactAppService;
    private readonly IMessageAppProvider _messageAppProvider;

    public PinMessageAppService(
        IObjectMapper objectMapper,
        IOptionsSnapshot<PinMessageOptions> pinMessageOptions, IChannelContactAppService channelContactAppService,
        INESTRepository<UserIndex, Guid> userRepository, IMessageAppService messageAppService,
        ILogger<PinMessageAppService> logger, IContactAppService contactAppService,
        IRefreshRepository<PinMessageIndex, string> pinMessageRepository, IMessageAppProvider messageAppProvider)
    {
        _objectMapper = objectMapper;
        _channelContactAppService = channelContactAppService;
        _userRepository = userRepository;
        _messageAppService = messageAppService;
        _logger = logger;
        _contactAppService = contactAppService;
        _pinMessageRepository = pinMessageRepository;
        _messageAppProvider = messageAppProvider;

        _channelContactAppService = channelContactAppService;
        _userRepository = userRepository;
        _pinMessageOptions = pinMessageOptions.Value;
    }

    public async Task<PinMessageResponse> ListPinMessageAsync(PinMessageQueryParamDto pinMessageQueryParamDto)
    {
        var param = _objectMapper.Map<PinMessageQueryParamDto, ListPinMessageParam>(pinMessageQueryParamDto);
        param.SkipCount = 0;
        param.MaxResultCount = _pinMessageOptions.MaxPinMessageCount;
        var list = await ListPinMessageByParamAsync(param);
        var pinMessages = list
            .Select(pinMessageIndex => _objectMapper.Map<PinMessageIndex, Dtos.PinMessage>(pinMessageIndex)).ToList();
        return new PinMessageResponse
        {
            Data = pinMessages,
            TotalCount = list.Count
        };
    }


    public async Task<PinMessageResponseDto<bool>> PinMessageAsync(PinMessageParamDto paramDto)
    {
        var isAdmin = await _channelContactAppService.IsAdminAsync(paramDto.ChannelUuid);
        if (!isAdmin)
        {
            throw new UserFriendlyException(CommonConstant.NoPermission);
        }

        var isMessageExists = await _messageAppProvider.IsMessageInChannelAsync(paramDto.ChannelUuid,paramDto.Id);
        if (!isMessageExists)
        {
            throw new UserFriendlyException(CommonConstant.MessageNotExist);
        }

        var pinMessageIndex = await _pinMessageRepository.GetAsync(paramDto.Id);
        if (pinMessageIndex != null)
        {
            throw new UserFriendlyException(CommonConstant.MessagePinned);
        }

        var param = new ListPinMessageParam
        {
            ChannelUuid = paramDto.ChannelUuid
        };

        var count = await CountPinMessageByParamAsync(param);
        if (count >= _pinMessageOptions.MaxPinMessageCount)
        {
            throw new UserFriendlyException(
                CommonConstant.OverPinnedLimit);
        }

        var portkeyId = CurrentUser.Id;
        if (portkeyId == null)
        {
            throw new UserFriendlyException(CommonConstant.UserNotExist);
        }

        var userIndex = await _userRepository.GetAsync((Guid)portkeyId);
        var walletName = "";
        var avatar = "";
        if (userIndex == null)
        {
            throw new UserFriendlyException(CommonConstant.UserNotExist);
        }

        if (null != userIndex.Name)
        {
            walletName = userIndex.Name;
            avatar = userIndex.Avatar;
        }
        else
        {
            var holderInfo = await _contactAppService.GetHolderInfoAsync(portkeyId.ToString());
            if (holderInfo != null)
            {
                walletName = holderInfo.WalletName ?? portkeyId.ToString()[..8];
                avatar = holderInfo.Avatar;
            }
            else
            {
                walletName = portkeyId.ToString()[..8];
            }
        }
        

        var pinInfo = new PinInfo
        {
            Pinner = portkeyId.ToString(),
            PinnedAt = DateTimeOffset.Now.ToUnixTimeMilliseconds(),
            PinnerName = walletName
        };


        var index = _objectMapper.Map<PinMessageParamDto, PinMessageIndex>(paramDto);
        index.PinInfo = pinInfo;
        try
        {
            await _pinMessageRepository.AddOrUpdateIndexAsync(index, true);
            _logger.LogDebug("AddOrUpdate PinMessage:{pinMessage}", JsonConvert.SerializeObject(index));
        }
        catch (Exception e)
        {
            _logger.LogError("AddOrUpdate PinMessage error: {ex},PinMessage : {pinMessage}", e.Message,
                JsonConvert.SerializeObject(index));
        }

        var user = new UserInfo
        {
            PortkeyId = portkeyId.ToString(),
            RelationId = userIndex.RelationId,
            Name = walletName,
            Avatar = avatar
        };
        var builder = new BuildPinSysMsgDto
        {
            PortkeyId = (Guid)portkeyId,
            ChannelUuid = paramDto.ChannelUuid,
            Type = PinMessageOperationType.Pin,
            MessageId = paramDto.Id,
            SendUuid = paramDto.SendUuid,
            Content = paramDto.Content,
            MessageType = paramDto.Type.ToString(),
            UserInfo = user,
            RelationId = userIndex.RelationId
        };
        await SendPinSysMsgAsync(builder);
        return new PinMessageResponseDto<bool>
        {
            Code = int.Parse(CommonResult.SuccessCode),
            Data = true
        };
    }

    public async Task<UnpinMessageResponseDto<bool>> UnpinMessageAsync(CancelPinMessageParamDto paramDto)
    {
        var isAdmin = await _channelContactAppService.IsAdminAsync(paramDto.ChannelUuid);
        if (!isAdmin)
        {
            throw new UserFriendlyException(CommonConstant.NoPermission);
        }

        var pinMessage = await _pinMessageRepository.GetAsync(paramDto.Id);
        if (pinMessage == null)
        {
            throw new UserFriendlyException(CommonConstant.PinMessageNotExist);
        }

        try
        {
            await _pinMessageRepository.DeleteIndexAsync(paramDto.Id, true);
            _logger.LogDebug("Delete PinMessage:{id}", paramDto.Id);

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

            var user = new UserInfo
            {
                PortkeyId = portkeyId.ToString(),
                RelationId = userIndex.RelationId,
                Name = userIndex.Name,
                Avatar = userIndex.Avatar
            };

            var builder = new BuildPinSysMsgDto
            {
                PortkeyId = (Guid)portkeyId,
                ChannelUuid = paramDto.ChannelUuid,
                Type = PinMessageOperationType.UnPin,
                MessageId = paramDto.Id,
                SendUuid = pinMessage.SendUuid,
                Content = pinMessage.Content,
                MessageType = pinMessage.Type,
                RelationId = userIndex.RelationId,
                UserInfo = user
            };
            await SendPinSysMsgAsync(builder);
        }
        catch (Exception e)
        {
            _logger.LogError("Delete PinMessage error: {ex},PinMessageId : {pinMessageId}", e.Message,
                paramDto.Id);
        }

        return new UnpinMessageResponseDto<bool>
        {
            Data = true
        };
    }

    public async Task<UnpinMessageResponseDto<bool>> UnpinMessageAllAsync(CancelPinMessageAllParamDto paramDto)
    {
        var isAdmin = await _channelContactAppService.IsAdminAsync(paramDto.ChannelUuid);
        if (!isAdmin)
        {
            throw new UserFriendlyException(CommonConstant.NoPermission);
        }

        var param = new ListPinMessageParam
        {
            ChannelUuid = paramDto.ChannelUuid,
            MaxResultCount = _pinMessageOptions.MaxPinMessageCount,
            SkipCount = 0
        };
        var list = await ListPinMessageByParamAsync(param);
        if (list.Count == 0)
        {
            throw new UserFriendlyException(CommonConstant.PinMessageNotExist);
        }

        var ids = list.Select(t => t.Id).ToList();
        try
        {
            foreach (var id in ids)
            {
                await _pinMessageRepository.DeleteIndexAsync(id, true);
                _logger.LogDebug("Delete PinMessage:{id}", id);
            }

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

            var user = new UserInfo
            {
                PortkeyId = portkeyId.ToString(),
                RelationId = userIndex.RelationId,
                Name = userIndex.Name,
                Avatar = userIndex.Avatar
            };
            var builder = new BuildPinSysMsgDto
            {
                PortkeyId = (Guid)portkeyId,
                ChannelUuid = paramDto.ChannelUuid,
                Type = PinMessageOperationType.RemoveAll,
                MessageId = "",
                SendUuid = "",
                Content = "",
                MessageType = MessageType.TEXT.ToString(),
                UnpinTotalCount = ids.Count,
                RelationId = userIndex.RelationId,
                UserInfo = user
            };
            await SendPinSysMsgAsync(builder);
        }
        catch (Exception e)
        {
            _logger.LogError("delete PinMessage error: {ex},PinMessageIds : {pinMessageIds}", e.Message,
                ids);
        }

        return new UnpinMessageResponseDto<bool>
        {
            Data = true,
        };
    }

    private async Task<List<PinMessageIndex>> ListPinMessageByParamAsync(ListPinMessageParam param)
    {
        var mustQuery = new List<Func<QueryContainerDescriptor<PinMessageIndex>, QueryContainer>>();
        if (param.Id != null)
        {
            mustQuery.Add(q => q.Term(i => i.Field(f => f.Id).Value(param.Id)));
        }

        if (param.ChannelUuid != null)
        {
            mustQuery.Add(q => q.Term(i => i.Field(f => f.ChannelUuid).Value(param.ChannelUuid)));
        }

        if (param.From != null)
        {
            mustQuery.Add(q => q.Term(i => i.Field(f => f.From).Value(param.From)));
        }

        if (param.Type != null)
        {
            mustQuery.Add(q => q.Term(i => i.Field(f => f.Type).Value(param.Type)));
        }

        if (param.SendUuid != null)
        {
            mustQuery.Add(q => q.Term(i => i.Field(f => f.SendUuid).Value(param.SendUuid)));
        }

        QueryContainer Filter(QueryContainerDescriptor<PinMessageIndex> f)
        {
            return f.Bool(b => b.Must(mustQuery));
        }

        var messageQueryTypeEnum = param.SortType;
        Func<SortDescriptor<PinMessageIndex>, IPromise<IList<ISort>>> sort;
        if (param.Ascending)
        {
            sort = messageQueryTypeEnum switch
            {
                PinMessageQueryType.MESSAGE => s => s.Ascending(a => a.CreateAt),
                PinMessageQueryType.PINMESSAGE => s => s.Ascending(a => a.PinInfo.PinnedAt),
                _ => s => s.Ascending(a => a.CreateAt)
            };
        }
        else
        {
            sort = messageQueryTypeEnum switch
            {
                PinMessageQueryType.MESSAGE => s => s.Descending(a => a.CreateAt),
                PinMessageQueryType.PINMESSAGE => s => s.Descending(a => a.PinInfo.PinnedAt),
                _ => s => s.Descending(a => a.CreateAt)
            };
        }


        var result = await _pinMessageRepository.GetSortListAsync(Filter, sortFunc: sort, skip: param.SkipCount,
            limit: param.MaxResultCount);

        return result.Item2;
    }

    private async Task<int> CountPinMessageByParamAsync(ListPinMessageParam listPinMessageParam)
    {
        var mustQuery = new List<Func<QueryContainerDescriptor<PinMessageIndex>, QueryContainer>>();
        if (listPinMessageParam.ChannelUuid != null)
        {
            mustQuery.Add(q => q.Term(i => i.Field(f => f.ChannelUuid).Value(listPinMessageParam.ChannelUuid)));
        }

        QueryContainer Filter(QueryContainerDescriptor<PinMessageIndex> f)
        {
            return f.Bool(b => b.Must(mustQuery));
        }

        var countResponse = await _pinMessageRepository.CountAsync(Filter);
        return (int)countResponse.Count;
    }

    private async Task SendPinSysMsgAsync(BuildPinSysMsgDto dto)
    {
        var pinMessageSysInfo = new PinMessageSysInfo
        {
            UserInfo = dto.UserInfo,
            PinType = dto.Type,
            MessageType = dto.MessageType,
            MessageId = dto.MessageId,
            SendUuid = dto.SendUuid,
            Content = dto.Content,
            UnpinnedCount = dto.UnpinTotalCount
        };

        var sendUuid = dto.RelationId + "-" + dto.ChannelUuid + "-" + DateTime.UtcNow.Second + "-" +
                       Guid.NewGuid();
        var settings = new JsonSerializerSettings
        {
            ContractResolver = new CamelCasePropertyNamesContractResolver()
        };
        var messageRequest = new SendMessageRequestDto
        {
            ChannelUuid = dto.ChannelUuid,
            Type = CommonConstant.PinSysName,
            SendUuid = sendUuid,
            Content = JsonConvert.SerializeObject(pinMessageSysInfo, settings)
        };

        await _messageAppService.SendMessageAsync(messageRequest);
    }
}

public class BuildPinSysMsgDto
{
    public Guid PortkeyId { get; set; }
    public string ChannelUuid { get; set; }
    public PinMessageOperationType Type { get; set; }
    public string MessageId { get; set; }
    public string SendUuid { get; set; }
    public string Content { get; set; }

    public string MessageType { get; set; }

    public int UnpinTotalCount { get; set; }

    public UserInfo UserInfo { get; set; }

    public string RelationId { get; set; }
}