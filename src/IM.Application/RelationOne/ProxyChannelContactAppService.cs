using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using IM.ChannelContact;
using IM.ChannelContact.Dto;
using IM.Common;
using IM.Commons;
using IM.Entities.Es;
using IM.Options;
using IM.User.Dtos;
using IM.User.Etos;
using IM.User.Provider;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;
using Volo.Abp;
using Volo.Abp.Auditing;

namespace IM.RelationOne;

[RemoteService(false), DisableAuditing]
public class ProxyChannelContactAppService : ImAppService, IProxyChannelContactAppService
{
    private readonly IProxyRequestProvider _proxyRequestProvider;
    private readonly IUserProvider _userProvider;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IHttpClientProvider _httpClientProvider;
    private readonly CAServerOptions _caServerOptions;

    public ProxyChannelContactAppService(IProxyRequestProvider proxyRequestProvider, IUserProvider userProvider,
        IHttpContextAccessor httpContextAccessor, IHttpClientProvider httpClientProvider,
        IOptionsSnapshot<CAServerOptions> caServerOptions)
    {
        _proxyRequestProvider = proxyRequestProvider;
        _userProvider = userProvider;
        _httpContextAccessor = httpContextAccessor;
        _httpClientProvider = httpClientProvider;
        _caServerOptions = caServerOptions.Value;
    }


    public async Task<CreateChannelResponseDto> CreateChannelAsync(CreateChannelRequestDto requestDto)
    {
        var result =
            await _proxyRequestProvider.PostAsync<CreateChannelResponseDto>(ImUrlConstant.CreateChannel, requestDto);


        if (requestDto.ChannelIcon.IsNullOrWhiteSpace())
        {
            return result;
        }

        if (result == null || result.ChannelUuid.IsNullOrWhiteSpace())
        {
            return result;
        }

        var channelId = result.ChannelUuid;
        var channelInfo = await GetChannelDetailInfoAsync(new ChannelDetailInfoRequestDto()
        {
            ChannelUuid = channelId
        });

        if (channelInfo == null)
        {
            return result;
        }

        await SetChannelNameAsync(new SetChannelNameRequestDto()
        {
            ChannelIcon = requestDto.ChannelIcon,
            ChannelName = channelInfo.Name,
            ChannelUuid = channelId
        });

        return result;
    }

    public async Task<ChannelDetailInfoResponseDto> GetChannelDetailInfoAsync(ChannelDetailInfoRequestDto requestDto)
    {
        var url = ImUrlConstant.ChannelInfo + $"?channelUuid={requestDto.ChannelUuid}";
        var result = await _proxyRequestProvider.GetAsync<ChannelDetailInfoResponseDto>(url);
        var resultMembers = result.Members;
        if (resultMembers.Count == 0)
        {
            throw new UserFriendlyException("No members in the channel");
        }

        await BuildUserNameAsync(resultMembers);
        return result;
    }

    public async Task<List<MemberInfo>> GetChannelMembersAsync(ChannelMembersRequestDto requestDto)
    {
        var url = ImUrlConstant.ChannelMembers + $"?channelUuid={requestDto.ChannelUuid}";
        var result = await _proxyRequestProvider.GetAsync<List<MemberInfo>>(url);
        if (result.Count == 0)
        {
            throw new UserFriendlyException("No members in the channel");
        }

        await BuildUserNameAsync(result);
        return result;
    }

    public async Task<string> JoinChannelAsync(JoinChannelRequestDto joinChannelRequestDto)
    {
        var url = ImUrlConstant.JoinChannel;
        var result = await _proxyRequestProvider.PostAsync<string>(url, joinChannelRequestDto);
        return result;
    }

    public async Task<string> RemoveFromChannelAsync(RemoveMemberRequestDto removeMemberRequestDto)
    {
        var url = ImUrlConstant.MemberRemoveChannel;
        return await _proxyRequestProvider.PostAsync<string>(url, removeMemberRequestDto);
    }

    public async Task<string> LeaveChannelAsync(LeaveChannelRequestDto leaveChannelRequestDto)
    {
        var url = ImUrlConstant.MemberLeaveChannel;
        return await _proxyRequestProvider.PostAsync<string>(url, leaveChannelRequestDto);
    }

    public async Task<string> DisbandChannelAsync(DisbandChannelRequestDto disbandChannelRequestDto)
    {
        var url = ImUrlConstant.DisbandChannel;
        return await _proxyRequestProvider.PostAsync<string>(url, disbandChannelRequestDto);
    }

    public async Task<string> ChannelOwnerTransferAsync(OwnerTransferRequestDto ownerTransferRequestDto)
    {
        var url = ImUrlConstant.ChannelOwnerTransfer;
        return await _proxyRequestProvider.PostAsync<String>(url, ownerTransferRequestDto);
    }

    public async Task<bool> IsAdminAsync(string id)
    {
        var url = ImUrlConstant.IsAdminChannel;
        return await _proxyRequestProvider.GetAsync<bool>(url + "?channelUuid=" + id);
    }

    public async Task<AnnouncementResponseDto> ChannelAnnouncementAsync(ChannelAnnouncementRequestDto requestDto)
    {
        var url = ImUrlConstant.ChannelAnnouncement + "?channelUuid=" + requestDto.ChannelUuid;
        return await _proxyRequestProvider.GetAsync<AnnouncementResponseDto>(url);
    }

    public async Task<string> SetChannelAnnouncementAsync(ChannelSetAnnouncementRequestDto requestDto)
    {
        var url = ImUrlConstant.ChannelSetAnnouncement;
        return await _proxyRequestProvider.PostAsync<string>(url, requestDto);
    }

    public async Task<string> SetChannelNameAsync(SetChannelNameRequestDto requestDto)
    {
        var url = ImUrlConstant.ChannelSetName;
        return await _proxyRequestProvider.PostAsync<string>(url, requestDto);
    }

    public async Task<Object> AddChannelMemberAsync(ChannelAddMemeberRequestDto requestDto)
    {
        var url = ImUrlConstant.AddChannelMembers;
        return await _proxyRequestProvider.PostAsync<Object>(url, requestDto);
    }

    private async Task<List<CAUserDto>> GetCaHolderAsync(List<Guid> userIds, string token = null)
    {
        var authToken = new StringValues();
        Debug.Assert((_httpContextAccessor.HttpContext != null || !string.IsNullOrEmpty(token)),
            "_httpContextAccessor.HttpContext != null");
        var hasAuthToken = _httpContextAccessor.HttpContext?.Request?.Headers.TryGetValue(CommonConstant.AuthHeader,
            out authToken);
        if (token != null)
        {
            authToken = token;
            hasAuthToken = true;
        }

        var header = new Dictionary<string, string>();
        if (hasAuthToken == true)
        {
            header.Add(CommonConstant.AuthHeader, authToken);
        }

        return await _httpClientProvider.PostAsync<List<CAUserDto>>(_caServerOptions.BaseUrl + "api/app/imUsers/names",
            userIds, header);
    }

    public async Task BuildUserNameAsync(List<MemberInfo> memberInfos, string caToken = null)
    {
        var userIds = new List<Guid>();
        var userIndices = await _userProvider.GetUserInfosByRelationIdsAsync(memberInfos.Select(t=>t.RelationId).ToList());
        foreach (var memberInfo in memberInfos)
        {
            var userIndex = userIndices.FirstOrDefault(t => t.RelationId == memberInfo.RelationId);
            if (userIndex == null)
            {
                continue;
            }

            var id = userIndex.Id;
            memberInfo.UserId = userIndex.Id;
            if (userIndex.CaAddresses.Count == CommonConstant.RegisterChainCount)
            {
                Logger.LogDebug("user has only one address, userId:{userId}, relationId:{relationId}, caHash:{caHash}",
                    userIndex.Id, userIndex.RelationId, userIndex.CaHash);

                var holder = await _userProvider.GetCaHolderInfoAsync(userIndex.CaHash);
                memberInfo.Addresses =
                    ObjectMapper.Map<List<GuardianDto>, List<CaAddressInfoDto>>(holder.CaHolderInfo);
            }
            else
            {
                memberInfo.Addresses =
                    ObjectMapper.Map<List<CaAddressInfo>, List<CaAddressInfoDto>>(userIndex.CaAddresses);
            }

            userIds.Add(id);
        }

        var caUserDtos = await GetCaHolderAsync(userIds, caToken);
        foreach (var memberInfo in memberInfos)
        {
            var caUserDto = caUserDtos.Find(x => x.PortkeyId == memberInfo.UserId.ToString());
            if (caUserDto == null || string.IsNullOrEmpty(caUserDto.Name))
            {
                continue;
            }

            memberInfo.Name = caUserDto.Name;
            memberInfo.Avatar = caUserDto.Avatar;
        }
    }
}