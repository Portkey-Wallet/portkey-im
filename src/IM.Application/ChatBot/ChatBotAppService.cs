using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Mime;
using System.Text;
using System.Threading.Tasks;
using AElf;
using AElf.Cryptography;
using IM.Auth.Dtos;
using IM.Cache;
using IM.Commons;
using IM.Options;
using IM.RelationOne.Dtos;
using IM.User.Dtos;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using RestSharp;
using Volo.Abp;
using Volo.Abp.Auditing;

namespace IM.ChatBot;

[RemoteService(false), DisableAuditing]
public class ChatBotAppService : ImAppService, IChatBotAppService
{
    private readonly ICacheProvider _cacheProvider;
    private const string BotUsageRankKey = "IM:BotUsageRankKey:";
    private const string UserBotCacheKey = "IM:UserBotKey:";
    private const string RelationTokenCacheKey = "IM:RelationTokenKey:";
    private const string InitBotUsageRankCacheKey = "IM:InitBotUsageRank:";
    private const string PortkeyTokenCacheKey = "PortKey:AuthToken:";
    private readonly ChatBotBasicInfoOptions _chatBotBasicInfoOptions;
    private readonly ChatBotConfigOptions _chatBotConfigOptions;
    private readonly ILogger<ChatBotAppService> _logger;


    public ChatBotAppService(ICacheProvider cacheProvider, 
        IOptionsSnapshot<ChatBotBasicInfoOptions> chatBotBasicInfoOptions,
        IOptionsSnapshot<ChatBotConfigOptions> chatBotConfigOptions, ILogger<ChatBotAppService> logger
    )
    {
        _cacheProvider = cacheProvider;
        _logger = logger;
        _chatBotConfigOptions = chatBotConfigOptions.Value;
        _chatBotBasicInfoOptions = chatBotBasicInfoOptions.Value;
    }

    public async Task<string> SendMessageToChatBotAsync(string content, string from)
    {
        var apiUrl = CommonConstant.ChatBotUrl;
        var apiKey = await GetKeyAsync(from);
        var options = new RestClientOptions(apiUrl);
        var client = new RestClient(options);

        var request = new RestRequest("", Method.Post);

        request.AddHeader("Authorization", "Bearer " + apiKey);
        var messageList = new List<Dictionary<string, string>>();
        var dic = new Dictionary<string, string>
        {
            { "role", "user" },
            { "content", content }
        };
        messageList.Add(dic);
        request.AddJsonBody(new
        {
            model = _chatBotConfigOptions.Model,
            messages = messageList,
            max_tokens = _chatBotConfigOptions.Token,
            temperature = 0.5
        });

        try
        {
            var response = await client.ExecuteAsync(request);
            var chatBotResponseDto = JsonConvert.DeserializeObject<ChatBotResponseDto>(response.Content);
            await _cacheProvider.AddScoreAsync(BotUsageRankKey, apiKey, 1);
            return chatBotResponseDto.Choices.First().Message.Content;
        }
        catch (Exception e)
        {
            _logger.LogError("Send message to GPT Error,ex is {error}", e.Message);
            throw new UserFriendlyException("Please try again later.");
        }
    }

    public async Task RefreshBotTokenAsync()
    {
        var value = await _cacheProvider.Get(RelationTokenCacheKey);
         if (value.HasValue)
         {
             _logger.LogDebug("Token has been init.");
             return;
         }
        try
        {
            var pToken = await GetPortkeyTokenAsync();
            _logger.LogDebug("pToken is {token}", pToken);
            var authToken = await GetAuthTokenAsync(pToken);
            _logger.LogDebug("authToken is {token}",authToken);
            await GetAndCacheRelationOneTokenAsync(pToken, authToken);
        }
        catch (Exception e)
        {
            _logger.LogError("Refresh Relation Token failed:{ex}", e.Message);
        }
    }

    private async Task GetAndCacheRelationOneTokenAsync(string pToken, string authToken)
    {
        var auth = new AuthRequestDto
        {
            AddressAuthToken = authToken
        };

        var requestInput = JsonConvert.SerializeObject(auth, Formatting.None);
        var requestContent = new StringContent(
            requestInput,
            Encoding.UTF8,
            MediaTypeNames.Application.Json);

        var client = new HttpClient();
        client.DefaultRequestHeaders.Add("Authorization", $"{CommonConstant.JwtPrefix} {pToken}");

        var response = await client.PostAsync(_chatBotConfigOptions.AuthUrl, requestContent);
        var responseData = await response.Content.ReadAsStringAsync();
        var dto = JsonConvert.DeserializeObject<RelationOneResponseDto>(responseData);
        var tokenData = JsonConvert.DeserializeObject<SignatureDto>(dto.Data.ToString());
        _logger.LogDebug("relation token is {token}", JsonConvert.SerializeObject(tokenData));
        var expire = TimeSpan.FromHours(1);
        await _cacheProvider.Set(RelationTokenCacheKey, tokenData.Token, expire);
    }

    private async Task<string> GetAuthTokenAsync(string pToken)
    {
        var message = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds().ToString();
        var data = Encoding.UTF8.GetBytes(message).ComputeHash();
        var privateKey = _chatBotBasicInfoOptions.BotKey;
        var signature =
            CryptoHelper.SignWithPrivateKey(ByteArrayHelper.HexStringToByteArray(privateKey),
                data);
        var signatureRequest = new SignatureRequestDto
        {
            Address = _chatBotBasicInfoOptions.Address,
            CaHash = _chatBotBasicInfoOptions.CaHash,
            Message = message,
            Signature = signature.ToHex()
        };

        var input = JsonConvert.SerializeObject(signatureRequest, Formatting.None);
        var content = new StringContent(
            input,
            Encoding.UTF8,
            MediaTypeNames.Application.Json);

        var client = new HttpClient();
        client.DefaultRequestHeaders.Add("Authorization", $"{CommonConstant.JwtPrefix} {pToken}");
        var tokenResponse =
            await client.PostAsync(_chatBotConfigOptions.TokenUrl, content);

        var resp = await tokenResponse.Content.ReadAsStringAsync();
        var token = JsonConvert.DeserializeObject<RelationOneResponseDto>(resp);
        var signatureDto = JsonConvert.DeserializeObject<SignatureDto>(token.Data.ToString());
        return signatureDto.Token;
    }

    private async Task<string> GetPortkeyTokenAsync()
    {
        var cacheToken = await _cacheProvider.Get(PortkeyTokenCacheKey);
        if (cacheToken.HasValue)
        {
            _logger.LogDebug("Token is exist,{token}", cacheToken);
            return cacheToken;
        }

        var now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        var data = Encoding.UTF8.GetBytes(_chatBotBasicInfoOptions.Address + "-" + now).ComputeHash();
        var signature =
            CryptoHelper.SignWithPrivateKey(
                ByteArrayHelper.HexStringToByteArray(_chatBotBasicInfoOptions.BotKey),
                data);

        var dict = new Dictionary<string, string>();
        dict.Add("ca_hash", _chatBotBasicInfoOptions.CaHash);
        dict.Add("chain_id", "AELF");
        dict.Add("chainId", "AELF");
        dict.Add("client_id", "CAServer_App");
        dict.Add("scope", "CAServer");
        dict.Add("grant_type", "signature");
        dict.Add("pubkey", _chatBotBasicInfoOptions.Pubkey);
        dict.Add("signature", signature.ToHex());
        dict.Add("timestamp", now.ToString());

        var client = new HttpClient();
        var req =
            new HttpRequestMessage(HttpMethod.Post, _chatBotConfigOptions.PortkeyTokenUrl)
                { Content = new FormUrlEncodedContent(dict) };
        var res = await client.SendAsync(req);

        var stringAsync = await res.Content.ReadAsStringAsync();
        var authTokenDto = JsonConvert.DeserializeObject<AuthResponseDto>(stringAsync);
        _logger.LogDebug("GetToken is {token}", JsonConvert.SerializeObject(authTokenDto));
        var expire = TimeSpan.FromHours(2);
        await _cacheProvider.Set(PortkeyTokenCacheKey, authTokenDto.AccessToken, expire);
        return authTokenDto.AccessToken;
    }

    public async Task InitBotUsageRankAsync()
    {
        var value = await _cacheProvider.Get(InitBotUsageRankCacheKey);

        if (value.HasValue)
        {
            _logger.LogDebug("Rank has been init.{value}", value.ToString());
            return;
        }

        var botKeys = _chatBotConfigOptions.BotKeys;
        foreach (var key in botKeys)
        {
            await _cacheProvider.AddScoreAsync(BotUsageRankKey, key, 0);
        }

        var expire = TimeSpan.FromDays(360);
        await _cacheProvider.Set(InitBotUsageRankCacheKey, "Init", expire);
    }

    private async Task<string> GetKeyAsync(string from)
    {
        var key = await _cacheProvider.Get(UserBotCacheKey + from);
        if (key.HasValue)
        {
            return key;
        }

        var rank = await _cacheProvider.GetTopAsync(BotUsageRankKey, 0, 0, false);
        var expire = TimeSpan.FromDays(1);
        await _cacheProvider.Set(UserBotCacheKey + from, rank.First().Element, expire);
        return rank.First().Element;
    }
}