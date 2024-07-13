using System;

namespace IM.ChatBot;

public class ChatBotRequestDto
{
    public string RelationId { get; set; }

    public Guid UserId { get; set; }
}