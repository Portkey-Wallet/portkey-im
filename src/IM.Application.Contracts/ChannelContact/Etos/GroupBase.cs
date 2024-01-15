using System.Collections.Generic;
using IM.ChannelContact.Dto;

namespace IM.ChannelContact.Etos;

public class GroupBase
{
    public string Id { get; set; }
    public List<GroupMember> Members { get; set; } = new();
}