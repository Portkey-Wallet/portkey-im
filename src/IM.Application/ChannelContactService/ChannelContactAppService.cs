using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using IM.ChannelContact;
using IM.ChannelContact.Dto;
using IM.ChannelContactService.Provider;
using IM.Commons;
using Microsoft.AspNetCore.Http;
using IM.Message.Provider;
using IM.Options;
using Microsoft.Extensions.Options;
using Volo.Abp;
using Volo.Abp.Auditing;
using Volo.Abp.Users;


namespace IM.ChannelContactService;

[RemoteService(false), DisableAuditing]
public class ChannelContactAppService : ImAppService, IChannelContactAppService
{
    private readonly IProxyChannelContactAppService _proxyChannelContactAppService;
    private readonly IGroupProvider _groupProvider;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IUnreadMessageUpdateProvider _unreadMessageUpdateProvider;
    private readonly ChatBotBasicInfoOptions _chatBotBasicInfoOptions;

    public ChannelContactAppService(IProxyChannelContactAppService proxyChannelContactAppService,
        IGroupProvider groupProvider, IHttpContextAccessor httpContextAccessor,
        IUnreadMessageUpdateProvider unreadMessageUpdateProvider,
        IOptionsSnapshot<ChatBotBasicInfoOptions> chatBotBasicInfoOptions)
    {
        _proxyChannelContactAppService = proxyChannelContactAppService;
        _groupProvider = groupProvider;
        _httpContextAccessor = httpContextAccessor;
        _unreadMessageUpdateProvider = unreadMessageUpdateProvider;
        _chatBotBasicInfoOptions = chatBotBasicInfoOptions.Value;
    }


    public async Task<CreateChannelResponseDto> CreateChannelAsync(CreateChannelRequestDto requestDto)
    {
        var responseDto = await _proxyChannelContactAppService.CreateChannelAsync(requestDto);
        var authToken = GetAuthFromHeader();

        // add group information asynchronously
        _ = _groupProvider.AddGroupAsync(responseDto?.ChannelUuid, authToken);
        var chatBot = requestDto.Members.Select(t=>t == _chatBotBasicInfoOptions.RelationId).FirstOrDefault();
        if (chatBot)
        {
            responseDto.BotChannel = true;
        }

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
        await _groupProvider.LeaveGroupAsync(leaveChannelRequestDto?.ChannelUuid, CurrentUser.GetId().ToString());
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
        if (ownerTransferRequestDto.RelationId == _chatBotBasicInfoOptions.RelationId)
        {
            return "";
        }

        return await _proxyChannelContactAppService.ChannelOwnerTransferAsync(ownerTransferRequestDto);
    }

    public async Task<bool> IsAdminAsync(string id)
    {
        return await _proxyChannelContactAppService.IsAdminAsync(id);
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