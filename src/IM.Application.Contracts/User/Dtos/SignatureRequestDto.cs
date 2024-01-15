using System.ComponentModel.DataAnnotations;

namespace IM.User.Dtos;

public class SignatureRequestDto
{
    [Required] public string Message { get; set; }
    [Required] public string Signature { get; set; }
    [Required] public string Address { get; set; }
    [Required] public string CaHash { get; set; }
}