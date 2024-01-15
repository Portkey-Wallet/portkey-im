using Volo.Abp.EventBus;

namespace IM.Feed.Etos;

[EventName("EventFeedHideEto")]
public class EventFeedHideEto
{
    public string ChannelUuid { get; set; }
}