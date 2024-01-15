using System;
using System.Threading.Tasks;
using IM.Feed;
using IM.Feed.Etos;
using Microsoft.Extensions.Logging;
using Volo.Abp.DependencyInjection;
using Volo.Abp.EventBus.Distributed;

namespace IM.EntityEventHandler.Core;

public class FeedHandler : IDistributedEventHandler<EventFeedListEto>, ITransientDependency
{
    private readonly IFeedAppService _feedAppService;
    private readonly ILogger<FeedHandler> _logger;

    public FeedHandler(
        ILogger<FeedHandler> logger,
        IFeedAppService feedAppService)
    {
        _logger = logger;
        _feedAppService = feedAppService;
    }

    public async Task HandleEventAsync(EventFeedListEto eventData)
    {
        try
        {
            var chatMetaDto = await _feedAppService.GetFeedMetaAsync(eventData.RelationId);
            if (chatMetaDto == null)
            {
                return;
            }

            if (await _feedAppService.FeedMetaSetRunningAsync(eventData.RelationId) == false)
            {
                if (DateTimeOffset.Now.Subtract(chatMetaDto.LastUpdateTime) >
                    TimeSpan.FromSeconds(FeedConsts.FeedPullGapMaxInSec))
                {
                    await _feedAppService.FeedMetaSetIdleAsync(eventData.RelationId);
                }

                return;
            }

            await _feedAppService.ProcessFeedAsync(eventData, eventData.RelationId);

            await _feedAppService.FeedMetaSetIdleAsync(eventData.RelationId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "handle feed event error");
            await _feedAppService.FeedMetaSetIdleAsync(eventData.RelationId);
        }
    }
}