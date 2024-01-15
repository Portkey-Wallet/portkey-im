using System;
using IM.Chat;

namespace IM.Dtos;

public class ChatMetaDto
{
    public string Id { get; set; }
    public ChatType ChatType { get; set; }

    public ProcessStatus ProcessStatus { get; set; }
    public DateTimeOffset LastProcessTimeInMs { get; set; }
    public long LatestMessageCreateTimeInMs { get; set; }

    public long UpperTime { get; set; }
    public long LowerTime { get; set; }
    public string UpperId { get; set; }
    public string LowerId { get; set; }

    public long Pos { get; set; }
}