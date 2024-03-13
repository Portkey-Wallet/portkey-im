using System.ComponentModel.DataAnnotations;
using Volo.Abp.Application.Dtos;

namespace IM.ChannelContact.Dto;

public class SearchMembersRequestDto : PagedResultRequestDto
{
    [Required] public string ChannelUuid { get; set; }
    [Required] public string Keyword { get; set; }
}