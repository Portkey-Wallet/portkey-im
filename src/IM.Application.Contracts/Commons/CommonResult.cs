using System;
using System.Collections.Generic;

namespace IM.Commons;

public static class CommonResult
{
    public const string SuccessCode = "20000";

    public const string UserNotExistCode = "30001";
    public const string UserExistCode = "30002";
    
    
    public const string GroupAlreadyExistCode = "30003";
    public const string GroupNotExistCode = "30004";
    public const string GroupDeletedCode = "30005";
    public const string MuteNotExistCode = "30003";

    public static readonly Dictionary<string, string> ResponseMappings = new()
    {
        { "20000", "success" },
        { "30001", "user not exist" },
        { "30002", "user already exists" }
    };

    public static string GetMessage(string code)
    {
        if (code.IsNullOrWhiteSpace())
        {
            return string.Empty;
        }

        return ResponseMappings.GetOrDefault(code);
    }
}