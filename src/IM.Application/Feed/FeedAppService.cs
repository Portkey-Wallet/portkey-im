using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using AElf.Indexing.Elasticsearch;
using IM.ChannelContact;
using IM.ChannelContact.Dto;
using IM.Commons;
using IM.Feed.Dtos;
using IM.Feed.Etos;
using IM.Grains.Grain.Feed;
using IM.Grains.Grain.RedPackage;
using IM.RedPackage;
using IM.Grains.Grain.Mute;
using IM.Message.Provider;
using IM.User.Provider;
using JetBrains.Annotations;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Net.Http.Headers;
using Nest;
using Newtonsoft.Json;
using Orleans;
using Orleans.Runtime;
using Volo.Abp;
using Volo.Abp.Auditing;
using Volo.Abp.EventBus.Distributed;
using Volo.Abp.Users;

namespace IM.Feed;

[RemoteService(false)]
[DisableAuditing]
public class FeedAppService : ImAppService, IFeedAppService
{
    private readonly IClusterClient _clusterClient;
    private readonly IDistributedEventBus _distributedEventBus;
    private readonly INESTRepository<FeedInfoIndex, string> _feedInfoIndex;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ILogger<FeedAppService> _logger;
    private readonly IProxyChannelContactAppService _proxyChannelContactAppService;
    private readonly IProxyFeedAppService _proxyFeedAppService;
    private readonly IRedPackageAppService _redPackageAppService;
    private readonly IUnreadMessageUpdateProvider _unreadMessageUpdateProvider;
    private readonly IUserProvider _userProvider;

    public FeedAppService(IClusterClient clusterClient, IDistributedEventBus distributedEventBus,
        IHttpContextAccessor httpContextAccessor, IProxyFeedAppService proxyFeedAppService,
        IProxyChannelContactAppService proxyChannelContactAppService, ILogger<FeedAppService> logger,
        IRedPackageAppService redPackageAppService,
        INESTRepository<FeedInfoIndex, string> feedInfoIndex, IUnreadMessageUpdateProvider unreadMessageUpdateProvider,
        IUserProvider userProvider)
    {
        _clusterClient = clusterClient;
        _distributedEventBus = distributedEventBus;
        _httpContextAccessor = httpContextAccessor;
        _proxyFeedAppService = proxyFeedAppService;
        _proxyChannelContactAppService = proxyChannelContactAppService;
        _logger = logger;
        _feedInfoIndex = feedInfoIndex;
        _redPackageAppService = redPackageAppService;
        _unreadMessageUpdateProvider = unreadMessageUpdateProvider;
        _userProvider = userProvider;
    }

    public async Task PinFeedAsync(PinFeedRequestDto input)
    {
        await _proxyFeedAppService.PinFeedAsync(input);
    }

    public async Task MuteFeedAsync(MuteFeedRequestDto input)
    {
        await _proxyFeedAppService.MuteFeedAsync(input);
        //await ModifyMuteAsync(input);
    }

    public async Task HideFeedAsync(HideFeedRequestDto input)
    {
        /*await _distributedEventBus.PublishAsync(new EventFeedHideEto
        {
            ChannelUuid = input.ChannelUuid
        });*/
        await _proxyFeedAppService.HideFeedAsync(input);
        await DeleteByChannelIdAsync(input.ChannelUuid);
    }

    public async Task<FeedMetaDto> GetFeedMetaAsync(string id)
    {
        var feedMetaGrain = _clusterClient.GetGrain<IFeedMetaGrain>(id);
        var feedMetaDto = await feedMetaGrain.GetAsync();
        if (feedMetaDto.IsEmpty())
        {
            return null;
        }

        return feedMetaDto;
    }

    public async Task<bool> FeedMetaSetRunningAsync(string id)
    {
        var feedMetaGrain = _clusterClient.GetGrain<IFeedMetaGrain>(id);
        if (feedMetaGrain == null)
        {
            return false;
        }

        var result = await feedMetaGrain.SetRunningAsync();
        return result.Data;
    }

    public async Task<bool> FeedMetaSetIdleAsync(string id)
    {
        var feedMetaGrain = _clusterClient.GetGrain<IFeedMetaGrain>(id);
        if (feedMetaGrain == null)
        {
            return false;
        }

        var result = await feedMetaGrain.SetIdleAsync();
        return result.Data;
    }

    public async Task<ListFeedResponseDto> ListFeedAsync(ListFeedRequestDto input,
        [CanBeNull] IDictionary<string, string> headers)
    {
        /*
        var relationIdFromToken = JwtHelper.ExtractRelationIdFromToken(
            _httpContextAccessor.HttpContext?.Request?.Headers[RelationOneConstant.AuthHeader]);

        if (!string.IsNullOrEmpty(input.Keyword))
        {
            if (string.IsNullOrEmpty(relationIdFromToken))
            {
                throw new UserFriendlyException("permission denied", HttpStatusCode.NotFound.ToString());
            }

            await SendFeedListEventAsync(relationIdFromToken, FeedConsts.FeedPullGapSearchInSec);

            return await SearchChannel(input.Keyword, input.Cursor, input.MaxResultCount, relationIdFromToken);
        }

        await SendFeedListEventAsync(relationIdFromToken, FeedConsts.FeedPullGapInSec);
        */
        var result = await FetchFeedListAsync(input, headers);
        foreach (var listFeedResponseItemDto in result.List)
        {
            var content = listFeedResponseItemDto.LastMessageContent;

            if (listFeedResponseItemDto.LastMessageType == RedPackageConstant.RedPackageCardType)
            {
                await BuildRedPackageLastMessageAsync(listFeedResponseItemDto);
            }
        }

        return result;
    }

    public async Task ProcessFeedAsync(EventFeedListEto eventData, string id)
    {
        var feedMetaGrain = _clusterClient.GetGrain<IFeedMetaGrain>(id);
        var cursor = "";
        var index = 0;
        var feedInfoList = await GetByRelationIdListAsync(id);
        var feedRelationOneList = new List<ListFeedResponseItemDto>();

        while (true)
        {
            var listDto = await FetchFeedListAsync(new ListFeedRequestDto
            {
                Cursor = cursor,
                MaxResultCount = FeedConsts.MaxFeedCount
            }, new Dictionary<string, string>
            {
                { HeaderNames.Authorization, eventData.Token }
            }, eventData.CaToken);

            //if list is empty, break
            if (listDto.List.Count == 0)
            {
                break;
            }

            feedRelationOneList.AddRange(listDto.List);

            if (listDto.List.Count < FeedConsts.MaxFeedCount)
            {
                break;
            }

            cursor = listDto.Cursor;
            index++;
        }

        var needToUpdate = feedInfoList.Join(
                feedRelationOneList,
                feedInfo => feedInfo.ChannelUuid,
                feedRelationOne => feedRelationOne.ChannelUuid,
                (feedInfo, feedRelationOne) => new
                {
                    FeedInfo = feedInfo,
                    FeedRelationOne = feedRelationOne
                }
            )
            .Where(item => item.FeedRelationOne.DisplayName != item.FeedInfo.DisplayName)
            .ToList();

        var needToDelete = feedInfoList
            .Where(feedInfo =>
                feedRelationOneList.All(feedRelationOne => feedRelationOne.ChannelUuid != feedInfo.ChannelUuid))
            .ToList();

        var needToAdd = feedRelationOneList
            .Where(feedRelationOne => feedInfoList.All(feedInfo => feedInfo.ChannelUuid != feedRelationOne.ChannelUuid))
            .ToList();

        foreach (var feedInfo in needToUpdate)
        {
            var feedRelationOne = feedInfo.FeedRelationOne;
            var feedInfoIndex = ObjectMapper.Map<ListFeedResponseItemDto, FeedInfoIndex>(feedRelationOne);
            feedInfoIndex.Id = feedInfo.FeedInfo.Id;
            await _feedInfoIndex.UpdateAsync(feedInfoIndex);
        }

        foreach (var listFeedResponseItemDto in needToAdd)
        {
            var feedInfo = ObjectMapper.Map<ListFeedResponseItemDto, FeedInfoIndex>(listFeedResponseItemDto);
            feedInfo.UserRelationId = id;
            feedInfo.Id = id + "_" + Guid.NewGuid();
            await AddFeedInfoIndexAsync(feedInfo);
        }

        foreach (var feedInfo in needToDelete)
        {
            await _feedInfoIndex.DeleteAsync(feedInfo.Id);
        }

        await feedMetaGrain.UpdateAsync(id, index);
    }

    public async Task DeleteByChannelIdAsync(string channelId)
    {
        if (string.IsNullOrEmpty(channelId))
        {
            return;
        }

        var mustQuery = new List<Func<QueryContainerDescriptor<FeedInfoIndex>, QueryContainer>>();
        mustQuery.Add(q => q.Term(i => i.Field(f => f.ChannelUuid).Value(channelId)));

        QueryContainer Filter(QueryContainerDescriptor<FeedInfoIndex> f)
        {
            return f.Bool(b => b.Must(mustQuery));
        }

        var result = await _feedInfoIndex.GetListAsync(Filter);
        foreach (var feedInfoIndex in result.Item2)
        {
            await _feedInfoIndex.DeleteAsync(feedInfoIndex.Id);
        }
    }

    public async Task DeleteByRelationIdAsync(string relationId)
    {
        if (string.IsNullOrEmpty(relationId))
        {
            return;
        }

        var mustQuery = new List<Func<QueryContainerDescriptor<FeedInfoIndex>, QueryContainer>>();
        mustQuery.Add(q => q.Term(i => i.Field(f => f.UserRelationId).Value(relationId)));

        QueryContainer Filter(QueryContainerDescriptor<FeedInfoIndex> f)
        {
            return f.Bool(b => b.Must(mustQuery));
        }

        var result = await _feedInfoIndex.GetListAsync(Filter);
        foreach (var feedInfoIndex in result.Item2)
        {
            await _feedInfoIndex.DeleteAsync(feedInfoIndex.Id);
        }
    }

    public async Task<List<FeedInfoDto>> GetByRelationIdListAsync(string relationId)
    {
        if (string.IsNullOrEmpty(relationId))
        {
            return new List<FeedInfoDto>();
        }

        var mustQuery = new List<Func<QueryContainerDescriptor<FeedInfoIndex>, QueryContainer>>();
        mustQuery.Add(q => q.Term(i => i.Field(f => f.UserRelationId).Value(relationId)));

        QueryContainer Filter(QueryContainerDescriptor<FeedInfoIndex> f)
        {
            return f.Bool(b => b.Must(mustQuery));
        }

        var result = await _feedInfoIndex.GetListAsync(Filter);
        return ObjectMapper.Map<List<FeedInfoIndex>, List<FeedInfoDto>>(result.Item2.ToList());
    }

    public async Task<ListFeedResponseDto> SearchChannel(string key, string cursor, int maxCount, string id)
    {
        if (string.IsNullOrEmpty(id))
        {
            return new ListFeedResponseDto
            {
                Cursor = "",
                List = new List<ListFeedResponseItemDto>()
            };
        }

        var skip = 0;
        if (!string.IsNullOrEmpty(cursor))
        {
            int.TryParse(cursor, out skip);
        }

        var mustQuery = new List<Func<QueryContainerDescriptor<FeedInfoIndex>, QueryContainer>>();

        mustQuery.Add(q => q.Term(i => i.Field(f => f.UserRelationId).Value(id)));

        if (!string.IsNullOrEmpty(key))
        {
            mustQuery.Add(q => q.Wildcard(wc => wc
                .Field(f => f.DisplayName)
                .Value($"*{key}*")
                .CaseInsensitive()
            ));
        }

        QueryContainer Filter(QueryContainerDescriptor<FeedInfoIndex> f)
        {
            return f.Bool(b => b.Must(mustQuery));
        }

        var result = await _feedInfoIndex.GetListAsync(Filter,
            limit: maxCount, skip: skip);
        var feedList = result.Item2.ToList();

        return new ListFeedResponseDto
        {
            Cursor = (skip + 1).ToString(),
            List = ObjectMapper.Map<List<FeedInfoIndex>, List<ListFeedResponseItemDto>>(feedList)
        };
    }

    public async Task<ListFeedResponseDto> FetchFeedListAsync(ListFeedRequestDto input,
        [CanBeNull] IDictionary<string, string> headers, string caToken = null)
    {
        var feedList = await _proxyFeedAppService.ListFeedAsync(input, headers);
        var memberInfoList = feedList.List
            .Where(feed => !string.IsNullOrEmpty(feed.ToRelationId))
            .Select(feed => new MemberInfo
            {
                RelationId = feed.ToRelationId
            }).ToList();

        try
        {
            await _proxyChannelContactAppService.BuildUserNameAsync(memberInfoList, caToken);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "BuildUserNameAsync error");
        }

        foreach (var feed in feedList.List)
        {
            if (!string.IsNullOrWhiteSpace(feed.ToRelationId))
            {
                var userInfo = await _userProvider.GetUserInfoAsync(feed.ToRelationId);
                _logger.Info("current userInfo is {info}", JsonConvert.SerializeObject(userInfo));
                feed.ToUserId = userInfo != null ? userInfo.Id.ToString() : "";
            }

            var memberInfo = memberInfoList.Find(x => x.RelationId == feed.ToRelationId);
            if (memberInfo == null)
            {
                continue;
            }

            if (memberInfo.Avatar.IsNullOrWhiteSpace())
            {
                continue;
            }

            _logger.LogInformation("add feed channel, id:{id} icon: {icon}", feed.ChannelUuid, memberInfo.Avatar);
            feed.ChannelIcon = memberInfo.Avatar;
        }

        return feedList;
    }

    public async Task<FeedMetaDto> AddFeedMetaAsync(string id)
    {
        var feedMetaGrain = _clusterClient.GetGrain<IFeedMetaGrain>(id);
        var feedMetaDto = await feedMetaGrain.AddAsync(id, 0);
        return feedMetaDto;
    }

    public async Task AddFeedInfoIndexAsync(FeedInfoIndex feedInfo)
    {
        await _feedInfoIndex.AddOrUpdateAsync(feedInfo);
    }

    private async Task BuildRedPackageLastMessageAsync(ListFeedResponseItemDto input)
    {
        try
        {
            CustomMessage<RedPackageCard> content =
                JsonConvert.DeserializeObject<CustomMessage<RedPackageCard>>(input.LastMessageContent);
            if (content.Data.Id == Guid.Empty)
            {
                _logger.LogError("Parse RedPackageCard error,Content:{Content}", input.LastMessageContent);
                return;
            }

            var grain = _clusterClient.GetGrain<IRedPackageUserGrain>(
                RedPackageHelper.BuildUserViewKey(CurrentUser.GetId(), content.Data.Id));

            input.RedPackage.ViewStatus = (await grain.GetUserViewStatus()).Data;
        }
        catch (Exception e)
        {
            _logger.LogError(e, "BuildRedPackageLastMessageAsync error,Content:{Content}", input.LastMessageContent);
        }
    }

    private async Task SendFeedListEventAsync(string relationIdFromToken, int timeoutInSec)
    {
        if (!string.IsNullOrEmpty(relationIdFromToken))
        {
            var dto = await GetFeedMetaAsync(relationIdFromToken);
            if (dto == null)
            {
                dto = await AddFeedMetaAsync(relationIdFromToken);
            }

            if (DateTimeOffset.Now.Subtract(dto.LastUpdateTime) > TimeSpan.FromSeconds(timeoutInSec))
            {
                await _distributedEventBus.PublishAsync(new EventFeedListEto
                {
                    RelationId = relationIdFromToken,
                    CreationTime = DateTimeOffset.Now,
                    Token = _httpContextAccessor.HttpContext?.Request?.Headers[RelationOneConstant.AuthHeader],
                    CaToken = _httpContextAccessor.HttpContext?.Request?.Headers[CommonConstant.AuthHeader]
                });
            }
        }
    }

    private async Task ModifyMuteAsync(MuteFeedRequestDto input)
    {
        var userId = CurrentUser.GetId();
        var grainId = $"{userId}-{input.ChannelUuid}";
        var muteGrain = _clusterClient.GetGrain<IMuteGrain>(grainId);
        var muteResult = await muteGrain.ModifyMute(new MuteGrainDto()
        {
            GroupId = input.ChannelUuid,
            Mute = input.Mute,
            UserId = userId
        });

        if (!muteResult.Success())
        {
            _logger.LogError("modify mute grain fail, groupId:{groupId}, userId{userId}", input.ChannelUuid, userId);
            return;
        }

        await _distributedEventBus.PublishAsync(ObjectMapper.Map<MuteGrainDto, MuteEto>(muteResult.Data), false, false);
    }
}