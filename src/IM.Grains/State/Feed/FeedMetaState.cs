using IM.Chat;

namespace IM.Grains.State.Feed;

public class FeedMetaState
{
    public string Id { get; set; }
    public ProcessStatus ProcessStatus { get; set; }

    public int MaxIndex { get; set; }
    public int EndIndex { get; set; }
    public DateTimeOffset LastUpdateTime { get; set; }
}