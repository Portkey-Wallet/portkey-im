using System.Collections.Generic;

namespace IM.ChannelContact.Dto;

public class MembersInfoResponseDto
{
    public List<MemberInfo> Members { get; set; } = new();
    public int TotalCount { get; set; }
}