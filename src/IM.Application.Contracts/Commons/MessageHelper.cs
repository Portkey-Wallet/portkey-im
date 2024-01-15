using System;
using IM.Message;
using Newtonsoft.Json;

namespace IM.Commons;

public static class MessageHelper
{
    public static string GetContent(string type, string content)
    {
        var message = content;
        if (type.IsNullOrWhiteSpace()) return message;
        if (type == CommonConstant.RedPackageMessageName)
        {
            type = CommonConstant.RedPackageTypeName;
        }

        var messageType = (MessageType)Enum.Parse(typeof(MessageType), type);
        switch (messageType)
        {
            case MessageType.TEXT:
                break;
            case MessageType.REDPACKAGE_CARD:
                var package = JsonConvert.DeserializeObject<PackageMessage>(content);
                message = $"{CommonConstant.RedPackageMessage}{package.Data.Memo}";
                break;
            case MessageType.SYS:
                break;
            case MessageType.CARD:
                message = CommonConstant.CardMessage;
                break;
            case MessageType.IMAGE:
                message = CommonConstant.ImageMessage;
                break;
            case MessageType.EMOJI:
                break;
            case MessageType.NFT:
                break;
            default:
                break;
        }

        return message;
    }
}

public class PackageMessage
{
    public string Image { get; set; }
    public string Link { get; set; }
    public PackageData Data { get; set; }
}

public class PackageData
{
    public string Id { get; set; }
    public string SenderId { get; set; }
    public string Memo { get; set; }
}