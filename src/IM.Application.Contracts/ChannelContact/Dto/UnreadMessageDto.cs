using IM.Chat;

namespace IM.ChannelContact.Dto;

public class UnreadMessageDto
{ 
    public string UserId { get; set; }
    
    public string DeviceId { get; set; }
    
    public NetworkType NetworkType { get; set; }
    
    public int UnreadCount { get; set; }
}