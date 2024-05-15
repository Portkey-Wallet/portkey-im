using System;
using IM.Enum.PinMessage;

namespace IM.PinMessage.Dtos;

public class PinMessageSysInfo
{
    public UserInfo UserInfo { get; set; }
    public PinMessageOperationType PinType { get; set; }
    public string MessageType { get; set; }
    public string Content { get; set; }
    public string MessageId { get; set; }
    public string SendUuid { get; set; }
    
    public int UnpinnedCount { get; set; }
    
}



public class UserInfo
{
    public string PortkeyId { get; set; }
    public string RelationId { get; set; }
    public string Name { get; set; }
    public string Avatar { get; set; }
}