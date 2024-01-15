using System;
using System.Collections.Generic;

namespace IM.Contact.Dtos;

public class ContactProfileDto
{
    public Guid Id { get; set; }
    public string Index { get; set; }
    public string Name { get; set; }
    public string Avatar { get; set; }
    public List<ContactAddressDto> Addresses { get; set; } = new();
    public Guid UserId { get; set; }
    public CaHolderInfoDto CaHolderInfo { get; set; }
    public ImInfoDto ImInfo { get; set; }
    public DateTime CreateTime { get; set; }
    public long ModificationTime { get; set; }
    public bool IsImputation { get; set; }
    public List<PermissionSetting> LoginAccounts { get; set; } = new();
}

public class ContactAddressDto
{
    public string ChainId { get; set; }
    public string Address { get; set; }
    public string ChainName { get; set; }
    public string Image { get; set; }
}

public class ContractExistDto
{
    public bool Existed { get; set; }
}

public class CaHolderInfoDto
{
    public Guid UserId { get; set; }
    public string CaHash { get; set; }
    public string WalletName { get; set; }
}

public class ImInfoDto
{
    public string RelationId { get; set; }
    public string PortkeyId { get; set; }
    public string Name { get; set; }
}

public class GetPrivacyPermissionAsyncResponseDto
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public List<PermissionSetting> Permissions { get; set; } = new();
}

public class PermissionSetting
{
    public Guid Id { get; set; }
    public string Identifier { get; set; }
    public PrivacyType PrivacyType { get; set; }
    /*public PrivacySetting Permission { get; set; }*/
}

public enum PrivacyType
{
    Email,
    Phone,
    Google,
    Apple,
    Unknow,
}

public enum PrivacySetting
{
    EveryBody = 0,
    MyContacts = 1,
    Nobody = 2
}