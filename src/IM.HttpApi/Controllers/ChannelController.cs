using System;
using System.Linq;
using System.Threading.Tasks;
using IM.ChannelContact;
using IM.ChannelContact.Dto;
using IM.Options;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
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
    private readonly ChatBotBasicInfoOptions _chatBotBasicInfoOptions;
    private readonly ILogger<ChannelController> _logger;

    public ChannelController(IChannelContactV2AppService channelContactAppService,
        IOptionsSnapshot<ChatBotBasicInfoOptions> chatBotBasicInfoOptions, ILogger<ChannelController> logger)
    {
        _channelContactAppService = channelContactAppService;
        _logger = logger;
        _chatBotBasicInfoOptions = chatBotBasicInfoOptions.Value;
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
    public async Task<ContactResultDto> GetContactsAsync(ContactRequestDto requestDto)
    {
        var result = await _channelContactAppService.GetContactsAsync(requestDto);
        var finalResult = result.Contacts.Where(t => t.ImInfo.RelationId != _chatBotBasicInfoOptions.RelationId)
            .ToList();
        result.Contacts = finalResult;
        result.TotalCount = finalResult.Count;
        return result;
    }
}