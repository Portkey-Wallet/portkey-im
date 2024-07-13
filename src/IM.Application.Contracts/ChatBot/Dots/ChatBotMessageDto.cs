using System;

namespace IM.ChatBot;

public class ChatBotMessageDto
{
    public string Content { get; set; }

    public string From { get; set; }

    public DateTime CreateTime { get; set; }

    public DateTime ModifyTime { get; set; }
    

}