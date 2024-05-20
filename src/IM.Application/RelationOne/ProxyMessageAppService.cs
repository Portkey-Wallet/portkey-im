using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using IM.ChannelContact;
using IM.ChannelContact.Dto;
using IM.ChannelContactService.Provider;
using IM.Common;
using IM.Commons;
using IM.Message;
using IM.Message.Dtos;
using IM.Options;
using IM.User.Provider;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using Volo.Abp;
using Volo.Abp.Auditing;
using Volo.Abp.Users;

namespace IM.RelationOne;

[RemoteService(false), DisableAuditing]
public class ProxyMessageAppService : ImAppService, IProxyMessageAppService
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IProxyRequestProvider _proxyRequestProvider;
    private readonly RelationOneOptions _relationOneOptions;
    private readonly IChannelProvider _channelProvider;
    private readonly IChannelContactV2AppService _channelContactAppService;
    private readonly IBlockUserProvider _blockUserProvider;
    private readonly IUserProvider _userProvider;

    public ProxyMessageAppService(IProxyRequestProvider proxyRequestProvider, IHttpContextAccessor httpContextAccessor,
        IOptionsSnapshot<RelationOneOptions> relationOneOptions, IChannelProvider channelProvider,
        IChannelContactV2AppService channelContactAppService, IBlockUserProvider blockUserProvider, IUserProvider userProvider)
    {
        _proxyRequestProvider = proxyRequestProvider;
        _httpContextAccessor = httpContextAccessor;
        _channelProvider = channelProvider;
        _channelContactAppService = channelContactAppService;
        _blockUserProvider = blockUserProvider;
        _userProvider = userProvider;
        _relationOneOptions = relationOneOptions.Value;
    }

    public async Task<int> ReadMessageAsync(ReadMessageRequestDto input)
    {
        var result =
            await _proxyRequestProvider.PostAsync<int>(
                "api/v1/message/read", input, null);

        return result;
    }

    public async Task HideMessageAsync(HideMessageRequestDto input)
    {
        var result =
            await _proxyRequestProvider.PostAsync<object>(
                "api/v1/message/hide", input, null);
    }

    public async Task<SendMessageResponseDto> SendMessageAsync(SendMessageRequestDto input)
    {
        var result =
            await _proxyRequestProvider.PostAsync<SendMessageResponseDto>(
                "api/v1/message/send", input, null);

        return result;
    }

    public async Task<List<ListMessageResponseDto>> ListMessageWithHeaderAsync(ListMessageRequestDto input,
        Dictionary<string, string> headers)
    {
        var baseUrl = "api/v1/message/list";
        var queryString = new StringBuilder();
        queryString.Append("?limit=").Append(input.Limit == 0 ? 10 : input.Limit);

        if (!string.IsNullOrEmpty(input.ChannelUuid))
        {
            queryString.Append("&channelUuid=").Append(input.ChannelUuid);
        }
        else if (!string.IsNullOrEmpty(input.ToRelationId))
        {
            queryString.Append("&toRelationId=").Append(input.ToRelationId);
        }

        queryString.Append("&maxCreateAt=").Append(input.MaxCreateAt);
        var result =
            await _proxyRequestProvider.GetAsync<List<ListMessageResponseDto>>(
                baseUrl + queryString, headers);

        return result;
    }

    public async Task<UnreadCountResponseDto> GetUnreadMessageCountAsync()
    {
        var result =
            await _proxyRequestProvider.GetAsync<UnreadCountResponseDto>(
                "api/v1/message/unreadCount");

        return result;
    }

    public async Task<UnreadCountResponseDto> GetUnreadMessageCountWithTokenAsync(string authToken)
    {
        var header = new Dictionary<string, string>()
        {
            [CommonConstant.AuthHeader] = authToken,
        };

        var result =
            await _proxyRequestProvider.GetAsync<UnreadCountResponseDto>(
                "api/v1/message/unreadCount", header);

        return result;
    }

    public async Task<List<ListMessageResponseDto>> ListMessageAsync(
        ListMessageRequestDto input)
    {
        var flag = false;
        var userInfo = await _userProvider.GetUserInfoByIdAsync((Guid)CurrentUser.Id);
        var channelInfo = await _channelProvider.GetChannelInfoByUUIDAsync(input.ChannelUuid);
        if (channelInfo.Type == "P")
        {
            var param = new ChannelMembersRequestDto
            {
                ChannelUuid = input.ChannelUuid
            };
            var members = await _channelContactAppService.GetChannelMembersAsync(param);
            var memberInfos = members.Members.Where(t => t.RelationId.ToString() != userInfo.RelationId).ToList();
            var blockUserId = memberInfos.FirstOrDefault()!.RelationId;
            var blockUserInfo = await _blockUserProvider.GetBlockUserInfoAsync(userInfo.RelationId, blockUserId);
            if (blockUserInfo != null)
            {
                flag = true;
            }
        }

        var baseUrl = "api/v1/message/list";
        var queryString = new StringBuilder();
        queryString.Append("?limit=").Append(input.Limit == 0 ? 10 : input.Limit);

        if (!string.IsNullOrEmpty(input.ChannelUuid))
        {
            queryString.Append("&channelUuid=").Append(input.ChannelUuid);
        }
        else if (!string.IsNullOrEmpty(input.ToRelationId))
        {
            queryString.Append("&toRelationId=").Append(input.ToRelationId);
        }

        if (flag)
        {
            queryString.Append("&isBlock=").Append(1);
        }

        queryString.Append("&maxCreateAt=").Append(input.MaxCreateAt);
        var result =
            await _proxyRequestProvider.GetAsync<List<ListMessageResponseDto>>(
                baseUrl + queryString);

        return result;
    }
}