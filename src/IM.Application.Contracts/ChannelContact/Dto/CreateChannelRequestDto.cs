using System.Collections.Generic;

namespace IM.ChannelContact.Dto;

public class CreateChannelRequestDto
{
    
    public string Name { get; set; }
    
    public string Type { get; set; }
    public string ChannelIcon { get; set; }
    
    public List<string> Members { get; set; }
    
}