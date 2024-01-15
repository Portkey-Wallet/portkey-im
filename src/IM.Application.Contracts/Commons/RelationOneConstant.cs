using System.Collections.Generic;

namespace IM.Commons;

public static class RelationOneConstant
{
    public const string ClientName = "RelationOne";
    public const string KeyName = "ApiKey";
    public const string GetTokenHeader = "I-Authorization";
    public const string AuthHeader = "R-Authorization";
    public const string SuccessCode = "0";
    public const string FailCode = "-1";

    public static readonly Dictionary<string, ImResponseMapping> ImResponseMappings = new()
    {
        { "0", new ImResponseMapping("20000", "") },
        { "-1", new ImResponseMapping("40001", "error") },
        { "10111", new ImResponseMapping("40111", "daily limit") },
        { "10100", new ImResponseMapping("40100", "token expired") },
        { "10101", new ImResponseMapping("40101", "token error") },
        { "10102", new ImResponseMapping("40102", "token issuer error") },
        { "10110", new ImResponseMapping("40110", "rate limit") },
        { "10120", new ImResponseMapping("40120", "permission denied") },
        { "11601", new ImResponseMapping("31601", "this field not support in current version") },
        { "11602", new ImResponseMapping("31602", "user not found") },
        { "11201", new ImResponseMapping("31201", "address is already bound") },
        { "11202", new ImResponseMapping("31202", "address is not bound") },
        { "11203", new ImResponseMapping("31203", "not the address holder") }
    };
    
    public static readonly Dictionary<string, ImResponseMapping> ImTokenErrorMappings = new()
    {
        { "40100", new ImResponseMapping("42000", "token expired") },
        { "40101", new ImResponseMapping("42001", "token error") },
        { "40102", new ImResponseMapping("42002", "token issuer error") }
    };
}

public class ImResponseMapping
{
    public ImResponseMapping(string code, string message)
    {
        Code = code;
        Message = message;
    }

    public string Code { get; set; }
    public string Message { get; set; }
}