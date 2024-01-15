using AElf.Indexing.Elasticsearch;
using Nest;

namespace IM.Feed;

public class FeedInfoIndex : FeedBase<string>, IIndexBuild
{
    [Keyword] public string UserRelationId { get; set; }
    [Keyword] public int Status { get; set; }
    [Keyword] public string ChannelUuid { get; set; }
    [Text] public string DisplayName { get; set; }
    public string ChannelIcon { get; set; }
    public string ChannelType { get; set; }
    public int UnreadMessageCount { get; set; }
    public int MentionsCount { get; set; }
    public string LastMessageType { get; set; }
    public string LastMessageContent { get; set; }
    [Keyword] public string LastPostAt { get; set; }
    public string ToRelationId { get; set; }
    [Keyword] public bool Mute { get; set; }
    public bool Pin { get; set; }
}