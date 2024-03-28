using System.ComponentModel.DataAnnotations;
using Volo.Abp.Application.Dtos;

namespace IM.ChannelContact.Dto;

public class ChannelMembersRequestDto : PagedResultRequestDto
{
    [Required] public string ChannelUuid { get; set; }
}