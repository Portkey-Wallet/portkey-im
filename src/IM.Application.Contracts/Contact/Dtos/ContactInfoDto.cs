using System;
using System.Collections.Generic;

namespace IM.Contact.Dtos;

public class ContactInfoDto
{
    public string Id { get; set; }
    public string Index { get; set; }
    public string Name { get; set; }
    public string Avatar { get; set; }
    public List<ContactAddressDto> Addresses { get; set; } = new();
    public string UserId { get; set; }
    public CaHolderDto CaHolderInfo { get; set; }
    public ImInfoDto ImInfo { get; set; }
    public DateTime CreateTime { get; set; }
    public long ModificationTime { get; set; }
    public bool IsImputation { get; set; }
    public List<PermissionSetting> LoginAccounts { get; set; } = new();

}

public class CaHolderDto
{
    public string UserId { get; set; }
    public string CaHash { get; set; }
    public string WalletName { get; set; }
}

