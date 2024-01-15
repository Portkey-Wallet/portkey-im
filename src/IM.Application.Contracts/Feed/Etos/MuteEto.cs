using System;
using Volo.Abp.EventBus;

namespace IM.Feed.Etos;

[EventName("MuteEto")]
public class MuteEto
{
    public string Id { get; set; }
    public Guid UserId { get; set; }
    public string GroupId { get; set; }
    public bool Mute { get; set; }
    public DateTime LastModificationTime { get; set; }
}