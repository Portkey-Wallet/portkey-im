using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using IM.ChannelContact;
using IM.ChannelContact.Dto;
using IM.Common;
using IM.Commons;
using IM.Dapper.Repository;
using IM.Options;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using Volo.Abp.DependencyInjection;

namespace IM.ChannelContactService.Provider;

public interface IChannelProvider
{
    Task<List<ContactDto>> GetContactsAsync(Guid userId);
    Task<MemberInfo> GetMemberInfoAsync(string channelId, string relationId);
    Task<List<MemberInfo>> GetMembersAsync(string channelUuid, List<string> relationIds);
    Task<MemberInfo> GetMemberAsync(string channelUuid, string relationId);

    Task<(IEnumerable<MemberQueryDto> data, int totalCount)> GetChannelMembersAsync(
        ChannelMembersRequestDto requestDto);

    Task<ChannelDetailResponseDto> GetChannelDetailInfoAsync(string relationId, string channelUuid);
}

public class ChannelProvider : IChannelProvider, ISingletonDependency
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IHttpClientProvider _httpClientProvider;
    private readonly CAServerOptions _caServerOptions;
    private readonly IProxyChannelContactAppService _proxyChannelContactAppService;
    private readonly IImRepository _imRepository;

    public ChannelProvider(IHttpContextAccessor httpContextAccessor, IHttpClientProvider httpClientProvider,
        IOptionsSnapshot<CAServerOptions> caServerOptions, IProxyChannelContactAppService proxyChannelContactAppService,
        IImRepository imRepository)
    {
        _httpContextAccessor = httpContextAccessor;
        _httpClientProvider = httpClientProvider;
        _proxyChannelContactAppService = proxyChannelContactAppService;
        _imRepository = imRepository;
        _caServerOptions = caServerOptions.Value;
    }

    public Task<MemberInfo> GetMemberInfoAsync(string channelId, string relationId)
    {
        throw new System.NotImplementedException();
    }

    public async Task<(IEnumerable<MemberQueryDto> data, int totalCount)> GetChannelMembersAsync(
        ChannelMembersRequestDto requestDto)
    {
        var sql =
            $"select relation_id as RelationId,is_admin as IsAdmin,`index` from im_channel_member where channel_uuid='{requestDto.ChannelUuid}' and status=0 order by `index` limit {requestDto.SkipCount}, {requestDto.MaxResultCount}; select count(*) from im_channel_member where channel_uuid='{requestDto.ChannelUuid}' and status=0;";

        return await _imRepository.QueryPageAsync<MemberQueryDto>(sql);
    }

    public async Task<ChannelDetailResponseDto> GetChannelDetailInfoAsync(string relationId, string channelUuid)
    {
        var sql =
            "select channel.uuid,channel.name,channel.icon,channel.announcement,channel.pin_announcement as PinAnnouncement,channel.open_access as OpenAccess,channel.type,im_user.mute,im_user.pin from im_channel channel join " +
            $"im_user_channel im_user on channel.uuid=im_user.channel_uuid where im_user.relation_id = '{relationId}' and channel.uuid = '{channelUuid}' and channel.status=0;";

        return await _imRepository.QueryFirstOrDefaultAsync<ChannelDetailResponseDto>(sql);
    }

    public async Task<List<MemberInfo>> GetMembersAsync(string channelUuid, List<string> relationIds)
    {
        var builder = new StringBuilder("(");
        foreach (var relationId in relationIds)
        {
            builder.Append($"'{relationId}',");
        }

        var inStr = builder.ToString();
        inStr = inStr.TrimEnd(',');
        inStr += ")";

        var sql =
            $"select relation_id as RelationId,is_admin as IsAdmin from im_channel_member where channel_uuid='{channelUuid}' and status=0 and relation_id in {inStr};";
        var imUserInfo = await _imRepository.QueryAsync<MemberInfo>(sql);
        var userList = imUserInfo?.ToList();
        if (userList.IsNullOrEmpty()) return new List<MemberInfo>();

        await _proxyChannelContactAppService.BuildUserNameAsync(userList, null);

        return userList;
    }

    public async Task<MemberInfo> GetMemberAsync(string channelUuid, string relationId)
    {
        var sql =
            $"select relation_id as RelationId,is_admin as IsAdmin from im_channel_member where channel_uuid='{channelUuid}' and status=0 and relation_id='{relationId}' limit 1;";
        var imUserInfo = await _imRepository.QueryFirstOrDefaultAsync<MemberInfo>(sql);
        await _proxyChannelContactAppService.BuildUserNameAsync(new List<MemberInfo>() { imUserInfo }, null);

        return imUserInfo;
    }

    public async Task<List<ContactDto>> GetContactsAsync(Guid userId)
    {
        var hasAuthToken = _httpContextAccessor.HttpContext.Request.Headers.TryGetValue(CommonConstant.AuthHeader,
            out var authToken);

        var header = new Dictionary<string, string>();
        if (hasAuthToken)
        {
            header.Add(CommonConstant.AuthHeader, authToken);
        }

        var url = $"{_caServerOptions.BaseUrl}{CAServerConstant.GetContactsByUserId}?userId={userId.ToString()}";
        return await _httpClientProvider.GetAsync<List<ContactDto>>(url, header);
    }
}