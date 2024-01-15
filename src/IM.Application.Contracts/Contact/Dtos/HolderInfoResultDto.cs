using System;
using System.Collections.Generic;

namespace IM.Contact.Dtos;

public class HolderInfoResultDto
{
    public Guid UserId { get; set; }
    public string CaHash { get; set; }
    public string WalletName { get; set; }
    public string Avatar { get; set; }
    public List<AddressResultDto> AddressInfos { get; set; }
}

public class AddressResultDto
{
    public string ChainId { get; set; }
    public string ChainName { get; set; }
    public string Address { get; set; }
}