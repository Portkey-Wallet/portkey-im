using System;

namespace IM.ChannelContact.Dto;

public class MemberInfo
{
    public string RelationId { get; set; }
    
    public string Name { get; set; }
    
    public string Avatar { get; set; }
    
    public bool IsAdmin { get; set; }
    
    public Guid UserId { get; set; }
}