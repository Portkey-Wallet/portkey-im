using System;
using System.Collections.Generic;
using IM.Entities.Es;
using IM.User.Dtos;

namespace IM.User;

public class ImUser
{
    public Guid PortkeyId { get; set; }
    public string CaHash { get; set; }
    public string RelationId { get; set; }
    public string Name { get; set; }
    public string Avatar { get; set; }
    public List<CaAddressInfo> CaAddresses { get; set; }
}