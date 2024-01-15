using System;

namespace IM.Commons;

public class RedPackageHelper
{
    public static string BuildUserViewKey(Guid userId,Guid redpackageId) => $"UserView:{userId}:{redpackageId}";
}