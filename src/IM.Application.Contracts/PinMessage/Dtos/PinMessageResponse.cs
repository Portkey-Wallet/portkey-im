using System.Collections.Generic;

namespace IM.PinMessage.Dtos;


public class PinMessageResponse
{
    public List<PinMessage> Data { get; set; }
    
    public int TotalCount { get; set; }
    
}

public class PinMessage
{
    public string Id { get; set; }
    
    public string SendUuid { get; set; }
    
    public string ChannelUuid { get; set; }
    
    public long CreateAt { get; set; }
    
    public string Type { get; set; }
    
    public string From { get; set; }
    
    public string FromName { get; set; }
    
    public string FromAvatar { get; set; }
    
    public string Content { get; set; }
    
    public Quote Quote { get; set; }
    
    public PinInfo PinInfo { get; set; }
    
}

