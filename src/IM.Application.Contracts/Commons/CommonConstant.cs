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
    public const string ImageMessage = "[image]";
    public const string CardMessage = "[card]";
    public const string RedPackageMessage = "[Red Packet]";
    public const string DefaultDisplayName = "Wallet 01";
    public const string DefaultNetWork = "MainNet";
}