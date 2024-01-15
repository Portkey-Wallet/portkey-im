using System.Collections.Generic;

namespace IM.Message.Dtos;

public class MessagePushDto
{
    public List<string> UserIds { get; set; }
    public string Title { get; set; }
    public string Content { get; set; }
    public string Icon { get; set; }
    public Dictionary<string,string> Data { get; set; }
}