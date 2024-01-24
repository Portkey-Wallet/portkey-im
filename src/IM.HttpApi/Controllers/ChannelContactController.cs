using System.Collections.Generic;
using System.Threading.Tasks;
using IM.ChannelContact;
using IM.ChannelContact.Dto;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Volo.Abp;

namespace IM.Controllers;

[RemoteService]
[Area("app")]
[ControllerName("ChannelContact")]
[Route("api/v1/channelContacts")]
//[Authorize]
public class ChannelContactController : ImController
{
    private readonly IChannelContactAppService _channelContactAppAppService;


    public ChannelContactController(IChannelContactAppService channelContactAppAppService)
    {
        _channelContactAppAppService = channelContactAppAppService;
    }


    [HttpPost("createChannel"), Authorize]
    public async Task<CreateChannelResponseDto> CreateChannelAsync(CreateChannelRequestDto requestDto)
    {
        return await _channelContactAppAppService.CreateChannelAsync(requestDto);
    }

    [HttpPost("join"), Authorize]
    public async Task<string> JoinChannelAsync(JoinChannelRequestDto requestDto)
    {
        return await _channelContactAppAppService.JoinChannelAsync(requestDto);
    }

    [HttpPost("members/remove"), Authorize]
    public async Task<string> RemoveMemberAsync(RemoveMemberRequestDto requestDto)
    {
        return await _channelContactAppAppService.RemoveFromChannelAsync(requestDto);
    }

    [HttpPost("members/leave"), Authorize]
    public async Task<string> LeaveChannelAsync(LeaveChannelRequestDto requestDto)
    {
        return await _channelContactAppAppService.LeaveChannelAsync(requestDto);
    }

    [HttpPost("disband")]
    public async Task<string> DisbandChannelAsync(DisbandChannelRequestDto requestDto)
    {
        return await _channelContactAppAppService.DisbandChannelAsync(requestDto);
    }

    [HttpPost("ownerTransfer")]
    public async Task<string> OwnerTransferAsync(OwnerTransferRequestDto requestDto)
    {
        return await _channelContactAppAppService.ChannelOwnerTransferAsync(requestDto);
    }

    [HttpGet("isAdmin")]
    public async Task<bool> IsAdminAsync(string channelUuid)
    {
        return await _channelContactAppAppService.IsAdminAsync(channelUuid);
    }

    [HttpGet("announcement")]
    public async Task<AnnouncementResponseDto> AnnouncementAsync(ChannelAnnouncementRequestDto requestDto)
    {
        return await _channelContactAppAppService.ChannelAnnouncementAsync(requestDto);
    }

    [HttpPost("announcement/update")]
    public async Task<string> SetAnnouncementAsync(ChannelSetAnnouncementRequestDto requestDto)
    {
        return await _channelContactAppAppService.SetChannelAnnouncementAsync(requestDto);
    }

    [HttpGet("channelDetailInfo")]
    public async Task<ChannelDetailInfoResponseDto> GetChannelDetailInfoAsync(ChannelDetailInfoRequestDto requestDto)
    {
        return await _channelContactAppAppService.GetChannelDetailInfoAsync(requestDto);
    }

    [HttpGet("members")]
    public async Task<List<MemberInfo>> GetChannelMembersAsync(ChannelMembersRequestDto requestDto)
    {
        return await _channelContactAppAppService.GetChannelMembersAsync(requestDto);
    }

    [HttpPost("update"), Authorize]
    public async Task<string> SetChannelNameAsync(SetChannelNameRequestDto requestDto)
    {
        return await _channelContactAppAppService.SetChannelNameAsync(requestDto);
    }

    [HttpPost("members/add")]
    public async Task ChannelAddMembersAsync(ChannelAddMemeberRequestDto requestDto)
    {
        await _channelContactAppAppService.AddChannelMemberAsync(requestDto);
    }
}