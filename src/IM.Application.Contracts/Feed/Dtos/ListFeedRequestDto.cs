namespace IM.Feed.Dtos;

public class ListFeedRequestDto
{
    public string Keyword { get; set; }
    public string Cursor { get; set; }
    public int SkipCount { get; set; }
    public string ChannelUuid { get; set; }
    public int MaxResultCount { get; set; }
}