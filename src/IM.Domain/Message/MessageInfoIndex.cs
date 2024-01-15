using AElf.Indexing.Elasticsearch;
using Nest;

namespace IM.Message;

public class MessageInfoIndex : MessageBase<string>, IIndexBuild
{
    //[Keyword] public string Id { get; set; }

    public MessageType MsgType { get; set; }

    [Keyword] public string ChatId { get; set; }

    [Keyword] public string FromId { get; set; }

    [Keyword] public string ToId { get; set; }

    public string Content { get; set; }

    public MessageInfoIndex Quote { get; set; }

    public AtInfo AtList { get; set; }
}