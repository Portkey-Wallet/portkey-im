namespace IM.ChannelContact.Dto;

public class ChannelDetailResponseDto
{
    public string Uuid { get; set; }
    
    public string Name { get; set; }
    
    public string Icon { get; set; }
    
    public string Announcement { get; set; }
    
    public bool PinAnnouncement { get; set; }

    public bool OpenAccess { get; set; }
    
    public string Type { get; set; }
    
    public bool Mute { get; set; }
    
    public bool Pin { get; set; }
    public MembersInfoResponseDto MemberInfos { get; set; }
}