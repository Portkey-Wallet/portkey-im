using System.Collections.Generic;

namespace IM.Options;

public class ChatBotConfigOptions
{
    public List<string> BotKeys { get; set; }

    public string Model { get; set; }

    public int Token { get; set; }
    
    
}