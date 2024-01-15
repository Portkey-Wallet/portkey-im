using System;

namespace IM.Chat;

public class ChatBase
{
    public string Id { get; set; }
    public ProcessStatus ProcessStatus { get; set; }
    public DateTimeOffset LastProcessTimeInMs { get; set; }
    public long LastProcessPosition { get; set; }
}