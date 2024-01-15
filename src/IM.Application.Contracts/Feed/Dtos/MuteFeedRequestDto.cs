namespace IM.Feed.Dtos;

public class MuteFeedRequestDto
{
    public string ChannelUuid { get; set; }
    
    public bool Mute { get; set; }
}