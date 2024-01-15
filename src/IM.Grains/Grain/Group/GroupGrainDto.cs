using IM.ChannelContact.Dto;

namespace IM.Grains.Grain.Group;

public class GroupGrainDto
{
    public string Id { get; set; }
    public string Name { get; set; }
    public string Type { get; set; }
    public string Icon { get; set; }
    public List<GroupMember> Members { get; set; } = new();
}