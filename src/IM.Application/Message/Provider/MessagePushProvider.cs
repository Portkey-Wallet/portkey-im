using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using IM.Chat;
using IM.Common;
using IM.Commons;
using IM.Message.Dtos;
using Volo.Abp.DependencyInjection;

namespace IM.Message.Provider;

public interface IMessagePushProvider
{
    Task PushImMessageAsync(ImMessagePushDto imMessage);
}

public class MessagePushProvider : IMessagePushProvider, ISingletonDependency
{
    private readonly IMessagePushRequestProvider _messagePushRequestProvider;

    public MessagePushProvider(IMessagePushRequestProvider messagePushRequestProvider)
    {
        _messagePushRequestProvider = messagePushRequestProvider;
    }

    public async Task PushImMessageAsync(ImMessagePushDto imMessage)
    {
        var message = new MessagePushDto
        {
            Title = GetTitle(imMessage),
            Content = GetContent(imMessage),
            Icon = GetIcon(imMessage.Icon),
            UserIds = imMessage.ToUserIds,
            Data = new Dictionary<string, string>()
            {
                ["network"] = CommonConstant.DefaultNetWork,
                ["channelId"] = imMessage.ChannelId,
                ["channelType"] = imMessage.ChatType == ChatType.P2P ? GroupType.P.ToString() : GroupType.G.ToString()
            }
        };
        
        await _messagePushRequestProvider.PostAsync(CommonConstant.PushMessageUri, message);
    }

    private string GetContent(ImMessagePushDto imMessage)
    {
        // sender name, may be contact, if contact sender name is remark
        if (imMessage.ChatType == ChatType.P2P)
        {
            return $"{imMessage.SenderName ?? ""}: {imMessage.Content}";
        }

        return $"{imMessage.SenderName ?? ""}: {imMessage.Content}";
    }

    private string GetTitle(ImMessagePushDto imMessage)
    {
        return imMessage.GroupName.IsNullOrWhiteSpace() ? imMessage.SenderName : imMessage.GroupName;
    }

    private string GetIcon(string icon)
    {
        return icon.IsNullOrWhiteSpace() ? null : icon;
    }
}