using System.ComponentModel.DataAnnotations;
using Volo.Abp.Application.Dtos;

namespace IM.ChannelContact.Dto;

public class SearchMembersRequestDto : PagedResultRequestDto
{
    [Required] public string ChannelUuid { get; set; }
    public string Keyword { get; set; }
    public string FilteredMember { get; set; }
}