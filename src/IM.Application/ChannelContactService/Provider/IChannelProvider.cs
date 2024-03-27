using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Dapper;
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

    public async Task<(IEnumerable<MemberQueryDto> data, int totalCount)> GetChannelMembersAsync(
        ChannelMembersRequestDto requestDto)
    {
        var parameters = new DynamicParameters();
        parameters.Add("@channelUuid", requestDto.ChannelUuid);
        parameters.Add("@skipCount", requestDto.SkipCount);
        parameters.Add("@maxResultCount", requestDto.MaxResultCount);

        var sql =
            "select relation_id as RelationId,is_admin as IsAdmin,`index` from im_channel_member where channel_uuid=@channelUuid and status=0 order by `index` limit @skipCount, @maxResultCount; select count(*) from im_channel_member where channel_uuid=@channelUuid and status=0;";

        return await _imRepository.QueryPageAsync<MemberQueryDto>(sql, parameters);
    }

    public async Task<ChannelDetailResponseDto> GetChannelDetailInfoAsync(string relationId, string channelUuid)
    {
        var parameters = new DynamicParameters();
        parameters.Add("@relationId", relationId);
        parameters.Add("@channelUuid", channelUuid);

        var sql =
            "select channel.uuid,channel.name,channel.icon,channel.announcement,channel.pin_announcement as PinAnnouncement,channel.open_access as OpenAccess,channel.type,im_user.mute,im_user.pin from im_channel channel join " +
            "im_user_channel im_user on channel.uuid=im_user.channel_uuid where im_user.relation_id = @relationId and channel.uuid = @channelUuid and channel.status=0;";

        return await _imRepository.QueryFirstOrDefaultAsync<ChannelDetailResponseDto>(sql,
            parameters);
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

        var parameters = new DynamicParameters();
        parameters.Add("@channelUuid", channelUuid);

        var sql =
            $"select relation_id as RelationId,is_admin as IsAdmin from im_channel_member where channel_uuid=@channelUuid and status=0 and relation_id in {inStr};";
        var imUserInfo = await _imRepository.QueryAsync<MemberInfo>(sql, parameters);
        var userList = imUserInfo?.ToList();
        if (userList.IsNullOrEmpty()) return new List<MemberInfo>();

        await _proxyChannelContactAppService.BuildUserNameAsync(userList, null);
        return userList;
    }

    public async Task<MemberInfo> GetMemberAsync(string channelUuid, string relationId)
    {
        var parameters = new DynamicParameters();
        parameters.Add("@relationId", relationId);
        parameters.Add("@channelUuid", channelUuid);

        var sql =
            "select relation_id as RelationId,is_admin as IsAdmin from im_channel_member where channel_uuid=@channelUuid and status=0 and relation_id=@relationId limit 1;";
        var imUserInfo = await _imRepository.QueryFirstOrDefaultAsync<MemberInfo>(sql, parameters);
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