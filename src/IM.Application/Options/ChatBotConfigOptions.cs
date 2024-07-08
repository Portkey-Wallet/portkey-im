using System.Collections.Generic;

namespace IM.Options;

public class ChatBotConfigOptions
{
    public List<string> BotKeys { get; set; }

    public string Model { get; set; }

    public int Token { get; set; }

    public string TokenUrl { get; set; }
    
    public string AuthUrl { get; set; }
    
    public string PortkeyTokenUrl { get; set; }
    
    

}