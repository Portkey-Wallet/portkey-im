using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace IM.ChatBot;


public class ChatBotResponseDto
{
    [JsonPropertyName("model")] 
    public string Model { get; set; }
    
    [JsonPropertyName("choices")] 
    public List<Choice> Choices { get; set; }
}

public class Choice
{
    [JsonPropertyName("message")] 
    public Message Message { get; set; }
    
    [JsonPropertyName("index")] 
    public int Index { get; set; }
    
}

public class Message
{
    [JsonPropertyName("role")] 
    public string Role { get; set; }
    
    [JsonPropertyName("Content")] 
    public string Content { get; set; }
}