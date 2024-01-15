using System;
using Volo.Abp.EventBus;

namespace IM.Message.Etos;

[EventName("EventMessageEto")]
public class EventMessageEto
{
    public string ChannelUuid { get; set; }
    public string ToRelationId { get; set; }
    public string FromRelationId { get; set; }
    public string ChatId { get; set; }
    public DateTimeOffset CreationTime { get; set; }
    public string Token { get; set; }
}