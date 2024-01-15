using System.Collections.Generic;

namespace IM.ChannelContact.Dto;

public class ChannelAddMemeberRequestDto
{
    public string ChannelUuid { get; set; }
    public List<string> Members { get; set; }
}