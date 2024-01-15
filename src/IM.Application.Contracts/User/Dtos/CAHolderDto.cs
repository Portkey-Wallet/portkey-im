using System;

namespace IM.User.Dtos;

public class CAHolderDto
{
    public Guid UserId { get; set; }
    public string CaHash { get; set; }
    public string Nickname { get; set; }
    public DateTime CreateTime { get; set; }
}