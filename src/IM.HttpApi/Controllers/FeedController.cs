using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using IM.ChatBot;
using IM.Feed;
using IM.Feed.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
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
        var result = await _feedAppService.ListFeedAsync(input, new Dictionary<string, string>());
        _logger.LogDebug("=====ListFeedAsync request:{0} response:{1}", JsonConvert.SerializeObject(input), JsonConvert.SerializeObject(result));
        result.List = result.List.Where(item => ChatConstant.ChatDisplayName.Equals(item.DisplayName)).ToList();
        return result;
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