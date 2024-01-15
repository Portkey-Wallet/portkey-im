using System;
using System.Collections.Generic;

namespace IM.User.Dtos;

public class ImUserDto
{
    public Guid PortkeyId { get; set; }
    public string CaHash { get; set; }
    public string RelationId { get; set; }
    public string Name { get; set; }
    public string Avatar { get; set; }
    public List<AddressWithChain> AddressWithChain { get; set; } = new();
}