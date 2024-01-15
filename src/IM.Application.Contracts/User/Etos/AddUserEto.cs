using System;
using System.Collections.Generic;
using Volo.Abp.EventBus;

namespace IM.User.Etos;

[EventName("AddUserEto")]
public class AddUserEto
{
    public Guid Id { get; set; }
    public string CaHash { get; set; }
    public List<CaAddressInfoDto> CaAddresses { get; set; }
    public string RelationId { get; set; }
    public string Name { get; set; }
    public string Avatar { get; set; }
    public long CreateTime { get; set; }
}

public class CaAddressInfoDto
{
    public string ChainId { get; set; }
    public string ChainName { get; set; }
    public string Address { get; set; }
}