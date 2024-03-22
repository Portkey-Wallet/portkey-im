using System;
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
[Route("api/v2/channelContacts")]
[Authorize]
public class ChannelController : ImController
{
    private readonly IChannelContactV2AppService _channelContactAppService;

    public ChannelController(IChannelContactV2AppService channelContactAppService)
    {
        _channelContactAppService = channelContactAppService;
    }

    [HttpGet("members")]
    public async Task<MembersInfoResponseDto> GetChannelMembersAsync(ChannelMembersRequestDto requestDto)
    {
        return await _channelContactAppService.GetChannelMembersAsync(requestDto);
    }


    [HttpGet("channelDetailInfo")]
    public async Task<ChannelDetailResponseDto> GetChannelDetailInfoAsync(ChannelDetailInfoRequestDto requestDto)
    {
        return await _channelContactAppService.GetChannelDetailInfoAsync(requestDto);
    }

    [HttpGet, Route("/api/v1/channelContacts/searchMembers")]
    public async Task<MembersInfoResponseDto> SearchMembersAsync(SearchMembersRequestDto requestDto)
    {
        return await _channelContactAppService.SearchMembersAsync(requestDto);
    }
    
    [HttpGet, Route("/api/v1/channelContacts/contacts")]
    public async Task<List<ContactDto>> GetContactsAsync(string channelUuid)
    {
        return await _channelContactAppService.GetContactsAsync(channelUuid);
    }
}