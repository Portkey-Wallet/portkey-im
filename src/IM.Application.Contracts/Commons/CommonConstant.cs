using System;

namespace IM.Commons;

public static class CommonConstant
{
    public const string JwtPrefix = "Bearer";
    public const string AuthHeader = "Authorization";
    public const string ResourceTokenKey = "ResourceToken";
    public static DateTimeOffset DefaultAbsoluteExpiration = DateTime.Parse("2099-01-01 12:00:00");
    public const string ClientName = "CAServer";
    public const string DefaultChainName = "aelf";
    public const string MessagePushServiceName = "MessagePush";
    public const string AppIdName = "AppId";
    public const string PushMessageUri = "api/v1/messagePush/push";
    public const string UpdateUnreadMessageUri = "api/v1/userDevice/updateUnreadMessage";

    public const string RedPackageMessageName = "REDPACKAGE-CARD";
    public const string RedPackageTypeName = "REDPACKAGE_CARD";
    public const string TransferMessageName = "TRANSFER-CARD";
    public const string TransferTypeName = "TRANSFER_CARD";
    public const string PinSysMessageName = "PIN-SYS";
    public const string PinSysTypeName = "PIN_SYS";
    public const string PinSysMessage = "[PinSys]";
    public const string PinSysName = "PIN-SYS";
    public const string ImageMessage = "[image]";
    public const string CardMessage = "[card]";
    public const string TransferMessage = "[Transfer]";
    public const string RedPackageMessage = "[Crypto Box]";
    public const string DefaultDisplayName = "Wallet 01";
    public const string DefaultNetWork = "MainNet";

    public const string MessageHasBeenHidden = "the message has been hidden";
    public const string MessageHasBeenDeletedLower = "the message has been deleted";
    public const string MessageHasBeenDeleted = "The message has been deleted";
    public const string NoPermission = "No Permission.";
    public const string UserNotExist = "User Not Exist.";
    public const string MessagePinned = "The message has been pinned.";
    public const string PinMessageNotExist = "The message is not exists.";
    public const string MessageNotExist = "The message is not exists.";
    

    public const string OverPinnedLimit =
        "Pinned Message has reached its limit. Please unpin some messages and try again.";

    public const int RegisterChainCount = 1;
    public const string NumberSign = "#";
    public const int AddressLengthCount = 35;
}