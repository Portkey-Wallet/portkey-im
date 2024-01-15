using System;
using Volo.Abp.Application.Dtos;

namespace IM.Contact.Dtos;

public class ContactGetListRequestDto : PagedResultRequestDto
{
    public string KeyWord { get; set; }
    public Guid UserId { get; set; }
    
    public bool IsAbleChat { get; set; }
    
    public long ModificationTime { get; set; }
}