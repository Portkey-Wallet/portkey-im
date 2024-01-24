using AElf.Indexing.Elasticsearch;
using Nest;

namespace IM.PinMessage;

public class PinMessageIndex : PinMessageBase<string>, IIndexBuild
{
    [Keyword] public override string Id { get; set; }
    [Keyword] public string SendUuid { get; set; }
    [Keyword] public string ChannelUuid { get; set; }
    [Keyword] public string Type { get; set; }
     public long CreateAt { get; set; }
    [Keyword] public string From { get; set; }
    [Keyword] public string FromName { get; set; }
    [Keyword] public string FromAvatar { get; set; }
    [Keyword] public string Content { get; set; }

    public Quote Quote { get; set; }
    public PinInfo PinInfo { get; set; }
}