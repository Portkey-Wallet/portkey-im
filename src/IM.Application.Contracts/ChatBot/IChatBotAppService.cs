using System.Threading.Tasks;

namespace IM.ChatBot;

public interface IChatBotAppService
{

    Task<string> SendMessageToChatBotAsync(string content,string relationId);
    Task RefreshBotTokenAsync();
    Task InitBotUsageRankAsync();
}