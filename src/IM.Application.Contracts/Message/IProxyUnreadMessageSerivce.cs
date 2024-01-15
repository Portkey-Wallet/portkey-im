using System;
using System.Threading.Tasks;
using IM.ChannelContact.Dto;

namespace IM.Message;

public interface IProxyUnreadMessageService
{
    Task<Object> UpdateUnReadMessageCountAsync(UnreadMessageDto unreadMessageDto);
}