using System.Collections.Generic;
using System.Threading.Tasks;
using IM.Feed;
using IM.Feed.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Volo.Abp;

namespace IM.Controllers;

[RemoteService]
[Area("app")]
[ControllerName("ImFeed")]
[Route("api/v1/feed")]
[Authorize]
public class FeedController : ImController
{
    private readonly IFeedAppService _feedAppService;

    public FeedController(IFeedAppService feedAppService)
    {
        _feedAppService = feedAppService;
    }

    [HttpGet("list")]
    public async Task<ListFeedResponseDto> ListFeedAsync(ListFeedRequestDto input)
    {
        return await _feedAppService.ListFeedAsync(input, new Dictionary<string, string>());
    }

    [HttpPost("pin")]
    public async Task PinFeedAsync(PinFeedRequestDto input)
    {
        await _feedAppService.PinFeedAsync(input);
    }

    [HttpPost("mute")]
    public async Task MuteFeedAsync(MuteFeedRequestDto input)
    {
        await _feedAppService.MuteFeedAsync(input);
    }

    [HttpPost("hide")]
    public async Task HideFeedAsync(HideFeedRequestDto input)
    {
        await _feedAppService.HideFeedAsync(input);
    }
}