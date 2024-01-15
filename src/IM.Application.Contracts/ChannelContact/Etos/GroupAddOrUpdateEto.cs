using Volo.Abp.EventBus;

namespace IM.ChannelContact.Etos;

[EventName("GroupCreateEto")]
public class GroupAddOrUpdateEto : GroupBase
{
    public string Type { get; set; }
    public string Name { get; set; }
    public string Icon { get; set; }
}