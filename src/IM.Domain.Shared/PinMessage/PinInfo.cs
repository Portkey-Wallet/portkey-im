namespace IM.PinMessage;

public class PinInfo
{
    public long PinnedAt { get; set; }
    
    public string Pinner { get; set; }
    
    public string PinnerName { get; set; }
    
}

public class Quote
{
    public string Id { get; set; }
    
    public string ChannelUuid { get; set; }
    
    public string SendUuid { get; set; }
    
    public string Type { get; set; }
    
    public long CreateAt { get; set; }
    
    public string From { get; set; }
    
    public string FromName { get; set; }
    
    public string FromAvatar { get; set; }
    
    public string Content { get; set; }
    
}