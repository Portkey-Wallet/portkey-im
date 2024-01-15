using System;
using System.Collections;

namespace IM.ChannelContact.Dto;

public class RemoveMemberRequestDto
{
    public string ChannelUuid { get; set; }
    public ArrayList Members { get; set; }
    
}