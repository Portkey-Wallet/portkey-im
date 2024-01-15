using System.Threading.Tasks;
using IM.Feed;
using IM.Feed.Etos;
using Volo.Abp.DependencyInjection;
using Volo.Abp.EventBus.Distributed;

namespace IM.EntityEventHandler.Core;

public class FeedHideHandler : IDistributedEventHandler<EventFeedHideEto>, ITransientDependency
{
    private readonly IFeedAppService _feedAppService;
    
    public FeedHideHandler(
        IFeedAppService feedAppService)
    {
        _feedAppService = feedAppService;
    }
    
    public async Task HandleEventAsync(EventFeedHideEto eventData)
    {
        await _feedAppService.DeleteByChannelIdAsync(eventData.ChannelUuid);
    }
}