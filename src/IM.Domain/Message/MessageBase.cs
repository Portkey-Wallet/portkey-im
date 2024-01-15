using IM.Entities.Es;
using Nest;

namespace IM.Message;

public class MessageBase<TKey> : ImEsEntity<TKey>
{
    [Keyword] public long MsgPosition { get; set; }

    [Keyword] public long CreateTimeInMs { get; set; }
}