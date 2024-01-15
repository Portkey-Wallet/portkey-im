using System;

namespace IM.Contact.Dtos;

public class ContactListDto
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string RelationId { get; set; }
    public string Name { get; set; }
    public string WalletName { get; set; }
    public string Avatar { get; set; }
    public DateTime CreateTime { get; set; }
    public bool IsImputation { get; set; }
}