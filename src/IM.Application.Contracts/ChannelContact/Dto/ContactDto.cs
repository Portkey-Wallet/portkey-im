using System;
using System.Collections.Generic;
using IM.Contact.Dtos;

namespace IM.ChannelContact.Dto;

public class ContactResultDto
{
    public int TotalCount { get; set; }
    public List<ContactDto> Contacts { get; set; } = new();
}
public class ContactDto
{
    public Guid Id { get; set; }
    public string Index { get; set; }
    public string Name { get; set; }
    public string Avatar { get; set; }
    public List<ContactAddressDto> Addresses { get; set; } = new();
    public Guid UserId { get; set; }
    public CaHolderInfo CaHolderInfo { get; set; }
    public ImInfo ImInfo { get; set; }
    public DateTime CreateTime { get; set; }
    public long ModificationTime { get; set; }
    public bool IsGroupMember { get; set; }
}