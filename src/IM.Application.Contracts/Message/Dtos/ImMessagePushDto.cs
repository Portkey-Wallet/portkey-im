using System.Collections.Generic;
using IM.Chat;

namespace IM.Message.Dtos;

public class ImMessagePushDto
{
    public string ChannelId { get; set; }
    public ChatType ChatType { get; set; }
    public string Content { get; set; }
    public string GroupName { get; set; }
    public string SenderName { get; set; }
    public string Icon { get; set; }
    public List<string> ToUserIds { get; set; }
    public List<string> MentionedUsers { get; set; }
}