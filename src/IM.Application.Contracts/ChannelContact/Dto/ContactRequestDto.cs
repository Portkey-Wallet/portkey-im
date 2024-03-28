using Volo.Abp.Application.Dtos;

namespace IM.ChannelContact.Dto;

public class ContactRequestDto : PagedResultRequestDto
{
    public string ChannelUuid { get; set; }
    public string Keyword { get; set; }
}