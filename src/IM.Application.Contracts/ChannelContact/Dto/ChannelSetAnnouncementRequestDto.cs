namespace IM.ChannelContact.Dto;

public class ChannelSetAnnouncementRequestDto
{
    public string ChannelUuid { get; set; }
    public string Announcement { get; set;}
    public bool PinAnnouncement { get; set; }
}