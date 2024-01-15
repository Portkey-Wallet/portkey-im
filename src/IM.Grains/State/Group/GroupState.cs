using IM.ChannelContact.Dto;
using IM.Chat;

namespace IM.Grains.State.Group;

public class GroupState
{
    public string Id { get; set; }
    public string Name { get; set; }
    public string Icon { get; set; }
    public string Type { get; set; }
    public List<GroupMember> Members { get; set; } = new();
    public bool IsDelete { get; set; }
}