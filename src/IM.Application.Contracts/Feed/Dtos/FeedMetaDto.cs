using System;
using IM.Chat;

namespace IM.Feed.Dtos;

public class FeedMetaDto
{
    public string Id { get; set; }
    public ProcessStatus ProcessStatus { get; set; }

    public int MaxIndex { get; set; }
    public int EndIndex { get; set; }
    public DateTimeOffset LastUpdateTime { get; set; }

    public bool IsEmpty()
    {
        return string.IsNullOrEmpty(Id);
    }
}