using IM.Enum.PinMessage;

namespace IM.PinMessage.Dtos;

public class PinMessageQueryParamDto
{
    public string ChannelUuid { get; set; }
    
    public PinMessageQueryType SortType { get; set; }
    
    public bool Ascending { get; set; }
    
    public int SkipCount { get; set; }
    
    public int MaxResultCount { get; set; }
    
}