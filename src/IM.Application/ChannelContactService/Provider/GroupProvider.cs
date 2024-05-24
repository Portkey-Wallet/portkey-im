using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElf.Indexing.Elasticsearch;
using IM.ChannelContact;
using IM.ChannelContact.Dto;
using IM.ChannelContact.Etos;
using IM.Common;
using IM.Commons;
using IM.Entities.Es;
using IM.Grains.Grain.Group;
using IM.Options;
using IM.RelationOne.Dtos;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Nest;
using Orleans;
using Volo.Abp.DependencyInjection;
using Volo.Abp.EventBus.Distributed;
using Volo.Abp.ObjectMapping;

namespace IM.ChannelContactService.Provider;

public interface IGroupProvider
{
    Task AddGroupAsync(string groupId, string authToken);
    Task UpdateGroupAsync(string groupId, string authToken);
    Task DeleteGroupAsync(string groupId);
    Task<GroupIndex> GetGroupInfosAsync(string groupId);
    Task LeaveGroupAsync(string groupId, string userId);
}

public class GroupProvider : IGroupProvider, ISingletonDependency
{
    private readonly IProxyChannelContactAppService _proxyChannelContactAppService;
    private readonly IDistributedEventBus _distributedEventBus;
    private readonly IClusterClient _clusterClient;
    private readonly ILogger<GroupProvider> _logger;
    private readonly IObjectMapper _objectMapper;
    private readonly INESTRepository<UserIndex, Guid> _userRepository;
    private readonly INESTRepository<GroupIndex, string> _groupRepository;
    private readonly IHttpClientProvider _httpClientProvider;
    private readonly RelationOneOptions _relationOneOptions;

    public GroupProvider(IProxyChannelContactAppService proxyChannelContactAppService,
        IDistributedEventBus distributedEventBus, IClusterClient clusterClient, ILogger<GroupProvider> logger,
        IObjectMapper objectMapper, INESTRepository<UserIndex, Guid> userRepository,
        INESTRepository<GroupIndex, string> groupRepository, IHttpClientProvider httpClientProvider,
        IOptionsSnapshot<RelationOneOptions> relationOneOptions)
    {
        _proxyChannelContactAppService = proxyChannelContactAppService;
        _distributedEventBus = distributedEventBus;
        _clusterClient = clusterClient;
        _logger = logger;
        _objectMapper = objectMapper;
        _userRepository = userRepository;
        _groupRepository = groupRepository;
        _httpClientProvider = httpClientProvider;
        _relationOneOptions = relationOneOptions.Value;
    }

    public async Task AddGroupAsync(string groupId, string authToken)
    {
        if (groupId.IsNullOrEmpty()) return;

        var groupInfo = await GetChannelDetailInfoAsync(groupId, authToken);
        if (groupInfo == null)
        {
            _logger.LogError("get group detail is null, group id: {id}.", groupId);
            return;
        }

        var groupGrain = _clusterClient.GetGrain<IGroupGrain>(groupId);
        var addGroup = await groupGrain.AddGroup(groupInfo);
        if (!addGroup.Success())
        {
            _logger.LogError("add group fail, group id: {id}, error code: {code}", groupId, addGroup.Code);
            return;
        }

        await _distributedEventBus.PublishAsync(_objectMapper.Map<GroupGrainDto, GroupAddOrUpdateEto>(addGroup.Data),
            false, false);
        _logger.LogInformation("add group success, group id: {id}", groupId);
    }

    public async Task UpdateGroupAsync(string groupId, string authToken)
    {
        if (groupId.IsNullOrEmpty()) return;
        var groupGrain = _clusterClient.GetGrain<IGroupGrain>(groupId);
        var groupInfo = await GetChannelDetailInfoAsync(groupId, authToken);
        if (groupInfo == null)
        {
            _logger.LogError("get group detail is null, group id: {id}.", groupId);
            return;
        }

        var updateGroup = await groupGrain.UpdateGroup(groupInfo);
        if (!updateGroup.Success())
        {
            _logger.LogError("add group fail, group id: {id}, error code: {code}", groupId, updateGroup.Code);
        }

        await _distributedEventBus.PublishAsync(_objectMapper.Map<GroupGrainDto, GroupAddOrUpdateEto>(updateGroup.Data),
            false, false);
        _logger.LogInformation("update group success, group id: {id}", groupId);
    }

    public async Task DeleteGroupAsync(string groupId)
    {
        if (groupId.IsNullOrEmpty()) return;
        var groupGrain = _clusterClient.GetGrain<IGroupGrain>(groupId);

        var deleteResult = await groupGrain.DeleteGroup();
        if (!deleteResult.Success())
        {
            _logger.LogError("delete group fail, group id: {id}, error code: {code}", groupId, deleteResult.Code);
        }

        await _distributedEventBus.PublishAsync(new GroupDeleteEto()
            {
                Id = groupId
            },
            false, false);
        _logger.LogInformation("delete group success, group id: {id}", groupId);
    }

    public async Task<GroupIndex> GetGroupInfosAsync(string groupId)
    {
        var mustQuery = new List<Func<QueryContainerDescriptor<GroupIndex>, QueryContainer>>()
        {
            descriptor => descriptor.Term(i => i.Field(f => f.Id).Value(groupId))
        };

        QueryContainer Filter(QueryContainerDescriptor<GroupIndex> f) => f.Bool(b => b.Must(mustQuery));
        return await _groupRepository.GetAsync(Filter);
    }

    public async Task LeaveGroupAsync(string groupId, string userId)
    {
        if (groupId.IsNullOrEmpty()) return;
        var groupGrain = _clusterClient.GetGrain<IGroupGrain>(groupId);
        var resultDto = await groupGrain.LeaveGroup(userId);
        if (!resultDto.Success())
        {
            _logger.LogError("leave group fail, group id: {id}, userId:{userId}, error code: {code}", groupId, userId,
                resultDto.Code);
        }

        await _distributedEventBus.PublishAsync(_objectMapper.Map<GroupGrainDto, GroupAddOrUpdateEto>(resultDto.Data),
            false, false);
        _logger.LogInformation("leave group success, group id: {id}", groupId);
    }

    private async Task<List<GroupMember>> GetGroupMembersAsync(List<MemberInfo> memberInfos)
    {
        if (memberInfos.IsNullOrEmpty())
        {
            return new List<GroupMember>();
        }

        var members = _objectMapper.Map<List<MemberInfo>, List<GroupMember>>(memberInfos);
        var relationIds = members.Select(t => t.RelationId).ToList();
        var userInfos = await GetUserInfosAsync(relationIds);

        foreach (var member in members)
        {
            var userInfo = userInfos.FirstOrDefault(t => t.RelationId == member.RelationId);
            member.PortKeyId = userInfo?.Id.ToString();
        }

        return members;
    }

    private async Task<List<UserIndex>> GetUserInfosAsync(List<string> relationIds)
    {
        var mustQuery = new List<Func<QueryContainerDescriptor<UserIndex>, QueryContainer>>()
        {
            descriptor => descriptor.Terms(i => i.Field(f => f.RelationId).Terms(relationIds))
        };

        QueryContainer Filter(QueryContainerDescriptor<UserIndex> f) => f.Bool(b => b.Must(mustQuery));
        var result = await _userRepository.GetListAsync(Filter);

        return result.Item2;
    }

    private async Task<GroupGrainDto> GetChannelDetailInfoAsync(string channelUuid, string authToken)
    {
        if (channelUuid.IsNullOrWhiteSpace())
        {
            _logger.LogWarning("get channel detail fail, channelUuid is empty, groupId:{groupId}", channelUuid);
        }

        if (authToken.IsNullOrWhiteSpace())
        {
            _logger.LogWarning("get channel detail fail, auth token is empty, groupId:{groupId}", channelUuid);
        }

        var url = ImUrlConstant.ChannelInfo + $"?channelUuid={channelUuid}";
        var header = new Dictionary<string, string>()
        {
            [CommonConstant.AuthHeader] = authToken,
            [RelationOneConstant.KeyName] = _relationOneOptions.ApiKey
        };
        var result =
            await _httpClientProvider.GetAsync<RelationOneResponseDto<ChannelDetailInfoResponseDto>>(GetUrl(url),
                header);

        if (result?.Data == null)
        {
            _logger.LogWarning("get group info fail, groupId:{groupId}", channelUuid);
            return null;
        }

        var grainDto = _objectMapper.Map<ChannelDetailInfoResponseDto, GroupGrainDto>(result.Data);
        grainDto.Members = await GetGroupMembersAsync(result.Data.Members);
        return grainDto;
    }

    private string GetUrl(string url)
    {
        return $"{_relationOneOptions.BaseUrl.TrimEnd('/')}/{url}";
    }
}