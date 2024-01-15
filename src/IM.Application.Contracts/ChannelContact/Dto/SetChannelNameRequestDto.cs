using System.ComponentModel.DataAnnotations;

namespace IM.ChannelContact.Dto;

public class SetChannelNameRequestDto
{
    [Required] public string ChannelUuid { get; set; }
    [Required] public string ChannelName { get; set; }
    public string ChannelIcon { get; set; }
}