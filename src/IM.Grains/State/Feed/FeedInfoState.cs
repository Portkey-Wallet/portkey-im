using IM.Feed.Dtos;

namespace IM.Grains.State.Feed;

public class FeedInfoState
{
    public string Id { get; set; }
    public DateTimeOffset LastUpdateTime { get; set; }
    public List<FeedInfoGrainDto> Data { get; set; }
}