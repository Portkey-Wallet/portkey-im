using System.Collections.Generic;
using IM.Commons;
using IM.Message.Dtos;

namespace IM.Feed.Dtos;

public class ListFeedResponseDto
{
    public string Cursor { get; set; }
    public List<ListFeedResponseItemDto> List { get; set; }
}

public class ListFeedResponseItemDto
{
    public int Status { get; set; }
    public string ChannelUuid { get; set; }
    public string DisplayName { get; set; }
    public string ChannelIcon { get; set; }
    public string ChannelType { get; set; }
    public int UnreadMessageCount { get; set; }
    public int MentionsCount { get; set; }
    public string LastMessageType { get; set; }
    public string LastMessageContent { get; set; }
    public string LastPostAt { get; set; }
    public string ToRelationId { get; set; }
    public bool Mute { get; set; }
    public bool Pin { get; set; }
    public string PinAt { get; set; }
    public RedPackageMessage RedPackage { get; set; } = new();

    public bool BotChannel { get; set; } = false;
}