using System.Collections.Generic;

namespace IM.ChannelContact.Dto;

public class MembersInfoResponseDto
{
    public List<MemberInfo> Members { get; set; }
    public int TotalCount { get; set; }
}