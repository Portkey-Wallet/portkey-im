using System;
using IM.Message;
using IM.RedPackage;
using Newtonsoft.Json;

namespace IM.Commons;

public static class MessageHelper
{
    public static string GetContent(string type, string content)
    {
        var message = content;
        if (type.IsNullOrWhiteSpace())
        {
            return message;
        }

        type = type switch
        {
            CommonConstant.RedPackageMessageName => CommonConstant.RedPackageTypeName,
            CommonConstant.TransferMessageName => CommonConstant.TransferTypeName,
            CommonConstant.PinSysMessageName => CommonConstant.PinSysTypeName,
            _ => type
        };

        var messageType = (MessageType)System.Enum.Parse(typeof(MessageType), type);
        switch (messageType)
        {
            case MessageType.TEXT:
                break;
            case MessageType.REDPACKAGE_CARD:
                var package = JsonConvert.DeserializeObject<CustomMessage<RedPackageCard>>(content);
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
            case MessageType.TRANSFER_CARD:
                message = GetTransferMessage(content);
                break;
            case MessageType.EMOJI:
                break;
            case MessageType.NFT:
                break;
            case MessageType.PIN_SYS:
                break;
            default:
                break;
        }

        return message;
    }

    private static string GetTransferMessage(string content)
    {
        var transfer = JsonConvert.DeserializeObject<TransferCustomMessage<TransferCard>>(content);

        if (transfer.TransferExtraData.TokenInfo != null)
        {
            var amount = transfer.TransferExtraData.TokenInfo.Amount /
                         Math.Pow(10, transfer.TransferExtraData.TokenInfo.Decimal);
            return
                $"{CommonConstant.TransferMessage} {amount} {transfer.TransferExtraData.TokenInfo.Symbol}";
        }

        return
            $"{CommonConstant.TransferMessage} {transfer.TransferExtraData.NftInfo?.Alias}";
    }
}

public class TransferCustomMessage<T> : CustomMessage<T>
{
    public TransferExtraData TransferExtraData { get; set; }
}

public class TransferCard
{
    public string Id { get; set; }
    public Guid SenderId { get; set; }
    public string SenderName { get; set; }
    public string Memo { get; set; }
    public string TransactionId { get; set; }
    public string BlockHash { get; set; }
    public string ToUserId { get; set; }
    public string ToUserName { get; set; }
}

public class TransferExtraData
{
    public TransferTokenInfo TokenInfo { get; set; }
    public TransferNftInfo NftInfo { get; set; }
}

public class TransferTokenInfo
{
    public long Amount { get; set; }
    public int Decimal { get; set; }
    public string Symbol { get; set; }
}

public class TransferNftInfo
{
    public string NftId { get; set; }
    public string Alias { get; set; }
}