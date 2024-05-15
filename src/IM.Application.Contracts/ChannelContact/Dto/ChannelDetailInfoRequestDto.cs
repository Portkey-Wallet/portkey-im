using System.ComponentModel.DataAnnotations;
using Volo.Abp.Application.Dtos;

namespace IM.ChannelContact.Dto;

public class ChannelDetailInfoRequestDto : PagedResultRequestDto
{
    [Required] public string ChannelUuid { get; set; }
}