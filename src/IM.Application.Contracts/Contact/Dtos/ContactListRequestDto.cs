using System;
using System.Collections.Generic;

namespace IM.Contact.Dtos;

public class ContactListRequestDto
{
    public List<Guid> ContactUserIds { get; set; }
    public string Address { get; set; }
}