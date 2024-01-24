using IM.Enum.PinMessage;

namespace IM.PinMessage.Dtos;

public class ListPinMessageParam
{
    public string Id { get; set; }
    
    public string ChannelUuid { get; set; }
    
    public string SendUuid { get; set; }
    
    public string Type { get; set; }
    
    public long CreateAt { get; set; }
    
    public string From { get; set; }
    
    public PinMessageQueryType SortType { get; set; }
    
    public bool Ascending { get; set; }
    
    public int SkipCount { get; set; }
    
    public int MaxResultCount { get; set; }
    
}