using System;
using System.ComponentModel.DataAnnotations;

namespace IM.RedPackage.Dtos;

public class GrabRedPackageInputDto
{
    [Required] public Guid Id { get; set; }
    [Required] public string ChannelUuid { get; set; }
    public string UserCaAddress { get; set; }
}