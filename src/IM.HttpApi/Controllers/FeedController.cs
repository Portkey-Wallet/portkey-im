using System.Collections.Generic;
using System.Threading.Tasks;
using IM.Feed;
using IM.Feed.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
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
    private readonly ILogger<FeedController> _logger;

    public FeedController(IFeedAppService feedAppService, ILogger<FeedController> logger)
    {
        _feedAppService = feedAppService;
        _logger = logger;
    }

    [HttpGet("list")]
    public async Task<ListFeedResponseDto> ListFeedAsync(ListFeedRequestDto input)
    {
        var header = Request.Headers;
        string userAgent = header["user-aegnt"];
        _logger.LogDebug("Header agent is {agent}",userAgent);
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