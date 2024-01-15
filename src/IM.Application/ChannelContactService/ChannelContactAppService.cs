using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using IM.ChannelContact;
using IM.ChannelContact.Dto;
using IM.ChannelContactService.Provider;
using IM.Commons;
using Microsoft.AspNetCore.Http;
using IM.Message.Provider;
using Volo.Abp;
using Volo.Abp.Auditing;


namespace IM.ChannelContactService;

[RemoteService(false), DisableAuditing]
public class ChannelContactAppService : ImAppService, IChannelContactAppService
{
    private readonly IProxyChannelContactAppService _proxyChannelContactAppService;
    private readonly IGroupProvider _groupProvider;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IUnreadMessageUpdateProvider _unreadMessageUpdateProvider;

    public ChannelContactAppService(IProxyChannelContactAppService proxyChannelContactAppService,
        IGroupProvider groupProvider, IHttpContextAccessor httpContextAccessor,
        IUnreadMessageUpdateProvider unreadMessageUpdateProvider)
    {
        _proxyChannelContactAppService = proxyChannelContactAppService;
        _groupProvider = groupProvider;
        _httpContextAccessor = httpContextAccessor;
        _unreadMessageUpdateProvider = unreadMessageUpdateProvider;
    }


    public async Task<CreateChannelResponseDto> CreateChannelAsync(CreateChannelRequestDto requestDto)
    {
        var responseDto = await _proxyChannelContactAppService.CreateChannelAsync(requestDto);
        var authToken = GetAuthFromHeader();
        
        // add group information asynchronously
        _ = _groupProvider.AddGroupAsync(responseDto?.ChannelUuid, authToken);
        return responseDto;
    }

    public async Task<ChannelDetailInfoResponseDto> GetChannelDetailInfoAsync(ChannelDetailInfoRequestDto requestDto)
    {
        return await _proxyChannelContactAppService.GetChannelDetailInfoAsync(requestDto);
    }

    public async Task<List<MemberInfo>> GetChannelMembersAsync(ChannelMembersRequestDto requestDto)
    {
        return await _proxyChannelContactAppService.GetChannelMembersAsync(requestDto);
    }

    public async Task<string> JoinChannelAsync(JoinChannelRequestDto joinChannelRequestDto)
    {
        var response = await _proxyChannelContactAppService.JoinChannelAsync(joinChannelRequestDto);
        var authToken = GetAuthFromHeader();
        
        // update group information asynchronously
        _ = _groupProvider.UpdateGroupAsync(joinChannelRequestDto?.ChannelUuid, authToken);
        return response;
    }

    public async Task<string> RemoveFromChannelAsync(RemoveMemberRequestDto removeMemberRequestDto)
    {
        var response = await _proxyChannelContactAppService.RemoveFromChannelAsync(removeMemberRequestDto);
        var authToken = GetAuthFromHeader();
        
        // update group information asynchronously
        _ = _groupProvider.UpdateGroupAsync(removeMemberRequestDto?.ChannelUuid, authToken);
        return response;
    }

    public async Task<string> LeaveChannelAsync(LeaveChannelRequestDto leaveChannelRequestDto)
    {
        var response = await _proxyChannelContactAppService.LeaveChannelAsync(leaveChannelRequestDto);
        var authToken = GetAuthFromHeader();
        
        // update group information asynchronously
        _ = _groupProvider.UpdateGroupAsync(leaveChannelRequestDto?.ChannelUuid, authToken);
        // update unread message count asynchronously
        _ = _unreadMessageUpdateProvider.UpdateUnReadMessageCountAsync(leaveChannelRequestDto?.ChannelUuid, authToken);
        return response;
    }

    public async Task<string> DisbandChannelAsync(DisbandChannelRequestDto disbandChannelRequestDto)
    {
        var response = await _proxyChannelContactAppService.DisbandChannelAsync(disbandChannelRequestDto);
        
        // delete group asynchronously
        _ = _groupProvider.DeleteGroupAsync(disbandChannelRequestDto?.ChannelUuid);
        return response;
    }

    public async Task<string> ChannelOwnerTransferAsync(OwnerTransferRequestDto ownerTransferRequestDto)
    {
        return await _proxyChannelContactAppService.ChannelOwnerTransferAsync(ownerTransferRequestDto);
    }

    public async Task<bool> IsAdminAsync()
    {
        return await _proxyChannelContactAppService.IsAdminAsync();
    }

    public async Task<AnnouncementResponseDto> ChannelAnnouncementAsync(ChannelAnnouncementRequestDto requestDto)
    {
        return await _proxyChannelContactAppService.ChannelAnnouncementAsync(requestDto);
    }

    public async Task<string> SetChannelAnnouncementAsync(ChannelSetAnnouncementRequestDto requestDto)
    {
        return await _proxyChannelContactAppService.SetChannelAnnouncementAsync(requestDto);
    }

    public async Task<string> SetChannelNameAsync(SetChannelNameRequestDto requestDto)
    {
        var response = await _proxyChannelContactAppService.SetChannelNameAsync(requestDto);
        var authToken = GetAuthFromHeader();
        
        // add group information asynchronously
        _ = _groupProvider.UpdateGroupAsync(requestDto?.ChannelUuid, authToken);
        return response;
    }

    public async Task<Object> AddChannelMemberAsync(ChannelAddMemeberRequestDto requestDto)
    {
        var response = await _proxyChannelContactAppService.AddChannelMemberAsync(requestDto);
        var authToken = GetAuthFromHeader();
        
        // add group information asynchronously
        _ = _groupProvider.UpdateGroupAsync(requestDto?.ChannelUuid, authToken);
        return response;
    }

    private string GetAuthFromHeader()
    {
        return _httpContextAccessor.HttpContext?.Request.Headers.GetOrDefault(RelationOneConstant.AuthHeader);
    }
}