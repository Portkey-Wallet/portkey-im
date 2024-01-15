using System;
using System.Collections.Generic;

namespace IM.User.Dtos;

public class UserInfoDto
{
    public string RelationId { get; set; }
    public string PortkeyId { get; set; }
    public string Name { get; set; }
    public string CAName { get; set; }
    public string Avatar { get; set; }
    public DateTime CreatedAt { get; set; }
    public FollowCount FollowCount { get; set; }
    public List<AddressWithChain> AddressWithChain { get; set; } = new();
}

public class FollowCount
{
    public int FollowingCount { get; set; }
    public int FollowerCount { get; set; }
}

public class AddressWithChain
{
    public string Address { get; set; }
    public string ChainName { get; set; }
}