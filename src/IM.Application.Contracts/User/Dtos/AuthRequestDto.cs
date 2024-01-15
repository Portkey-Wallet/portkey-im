namespace IM.User.Dtos;

public class AuthRequestDto
{
    public string AddressAuthToken { get; set; }
    public string Name { get; set; }
    public string InviteCode { get; set; }
}