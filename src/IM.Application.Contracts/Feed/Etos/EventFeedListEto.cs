using System;
using Volo.Abp.EventBus;

namespace IM.Feed.Etos;

[EventName("EventFeedListEto")]
public class EventFeedListEto
{
    public string RelationId { get; set; }
    public DateTimeOffset CreationTime { get; set; }
    public string Token { get; set; }
    public string CaToken { get; set; }
}