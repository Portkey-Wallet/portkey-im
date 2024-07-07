using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using AElf;
using IM.Auth.Dtos;
using IM.Cache;
using IM.Common;
using IM.Commons;
using IM.Options;
using IM.User;
using IM.User.Dtos;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using RestSharp;
using Volo.Abp;
using Volo.Abp.Auditing;
using ILogger = DnsClient.Internal.ILogger;

namespace IM.ChatBot;

[RemoteService(false), DisableAuditing]
public class ChatBotAppService : ImAppService, IChatBotAppService
{
    private readonly ICacheProvider _cacheProvider;
    private readonly IUserAppService _userAppService;
    private const string BotUsageRankKey = "IM:BotUsageRankKey:";
    private const string UserBotCacheKey = "IM:UserBotKey:";
    private const string RelationTokenCacheKey = "IM:RelationTokenKey:";
    private const string InitBotUsageRankCacheKey = "IM:InitBotUsageRank:";
    private readonly ChatBotBasicInfoOptions _chatBotBasicInfoOptions;
    private readonly ChatBotConfigOptions _chatBotConfigOptions;
    private readonly ILogger<ChatBotAppService> _logger;
    private readonly IHttpClientProvider _httpClientProvider;
    private readonly IHttpClientFactory _httpClientFactory;

    public ChatBotAppService(ICacheProvider cacheProvider, IUserAppService userAppService,
        IOptionsSnapshot<ChatBotBasicInfoOptions> chatBotBasicInfoOptions,
        IOptionsSnapshot<ChatBotConfigOptions> chatBotConfigOptions, ILogger<ChatBotAppService> logger,
        IHttpClientProvider httpClientProvider, IHttpClientFactory httpClientFactory)
    {
        _cacheProvider = cacheProvider;
        _userAppService = userAppService;
        _logger = logger;
        _httpClientProvider = httpClientProvider;
        _httpClientFactory = httpClientFactory;
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
        var dic = new Dictionary<string, string>();
        dic.Add("role", "user");
        dic.Add("content", content);
        messageList.Add(dic);
        request.AddJsonBody(new
        {
            model = _chatBotConfigOptions.Model,
            messages = messageList,
            max_tokens = _chatBotConfigOptions.Token,
            //stop = null,
            temperature = 0.5
        });

        var response = await client.ExecuteAsync(request);
        var chatBotResponseDto = JsonConvert.DeserializeObject<ChatBotResponseDto>(response.Content);
        await _cacheProvider.AddScoreAsync(BotUsageRankKey, apiKey, 1);
        return chatBotResponseDto.Choices.First().Message.Content;
    }

    public async Task RefreshBotTokenAsync()
    {
        var value = await _cacheProvider.Get(RelationTokenCacheKey);
        if (value.HasValue)
        {
            _logger.LogDebug("token has been init.");
            return;
        }

        try
        {
            var pToken = await GetPortkeyToken();
            var headers = new Dictionary<string, string>
            {
                { CommonConstant.AuthHeader, pToken }
            };

            var message = DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString();
            var data = Encoding.UTF8.GetBytes(message).ComputeHash();
            var privateKey = _chatBotBasicInfoOptions.BotKey;
            var signature =
                AElf.Cryptography.CryptoHelper.SignWithPrivateKey(ByteArrayHelper.HexStringToByteArray(privateKey),
                    data);
            var signatureRequest = new SignatureRequestDto
            {
                Address = _chatBotBasicInfoOptions.Address,
                CaHash = _chatBotBasicInfoOptions.CaHash,
                Message = message,
                Signature = signature.ToHex()
            };

            var response = await _httpClientProvider.PostAsync<SignatureDto>(
                ImUrlConstant.AddressToken, signatureRequest, headers);

            // var result = await _userAppService.GetSignatureAsync(signatureRequest);
            _logger.LogDebug("Portkey token is {token}", response.Token);
            var authToken = new AuthRequestDto
            {
                AddressAuthToken = response.Token
            };
            var token = await _userAppService.GetAuthTokenAsync(authToken);
            _logger.LogDebug("Relation one Token is {token} ", token.Token);
            var expire = TimeSpan.FromHours(24);
            await _cacheProvider.Set(RelationTokenCacheKey, token.Token, expire);
        }
        catch (Exception e)
        {
            _logger.LogError("Refresh Relation Token failed:{ex}", e.Message);
        }
    }

    private async Task<string> GetPortkeyToken()
    {
        var now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        var data = Encoding.UTF8.GetBytes(_chatBotBasicInfoOptions.Address + "-" + now).ComputeHash();
        var signature =
            AElf.Cryptography.CryptoHelper.SignWithPrivateKey(
                ByteArrayHelper.HexStringToByteArray(_chatBotBasicInfoOptions.BotKey),
                data);
        
        // var formContent = new FormUrlEncodedContent(new[]
        // {
        //     new KeyValuePair<string, string>("ca_hash", _chatBotBasicInfoOptions.CaHash),
        //     new KeyValuePair<string, string>("chain_id", "AELF"),
        //     new KeyValuePair<string, string>("chainId", "AELF"),
        //     new KeyValuePair<string, string>("client_id", "CAServer_App"),
        //     new KeyValuePair<string, string>("scope", "CAServer"),
        //     new KeyValuePair<string, string>("grant_type", "signature"),
        //     new KeyValuePair<string, string>("pubkey",
        //         "04ef8c06f061c7e80b5b1d7901212c836c78608ca40afa9eecb373c9c51fff87ee79b208cf3cc0b46f1a991470c071dcdacba3a82222245100b0bba56fcf210750"),
        //     new KeyValuePair<string, string>("signature", signature.ToHex()),
        //     new KeyValuePair<string, string>("signature", now.ToString())
        //
        // });
        //
        // var client = _httpClientFactory.CreateClient();
        // var response =
        //     await client.PostAsync("https://auth-aa-portkey-test.portkey.finance/connect/token", formContent);
        
        
        
        var dict = new Dictionary<string, string>();
        dict.Add("ca_hash", _chatBotBasicInfoOptions.CaHash);
        dict.Add("chain_id", "AELF");
        dict.Add("chainId", "AELF");
        dict.Add("client_id", "CAServer_App");
        dict.Add("scope", "CAServer");
        dict.Add("grant_type", "signature");
        dict.Add("pubkey",
                     "04ef8c06f061c7e80b5b1d7901212c836c78608ca40afa9eecb373c9c51fff87ee79b208cf3cc0b46f1a991470c071dcdacba3a82222245100b0bba56fcf210750");
        dict.Add("signature", signature.ToHex());
        dict.Add("timestamp", now.ToString());
        
        using var client = new HttpClient();
        using var req = new HttpRequestMessage(HttpMethod.Post, "https://auth-aa-portkey-test.portkey.finance/connect/token") { Content = new FormUrlEncodedContent(dict) };
        using var res = await client.SendAsync(req);
     
        _logger.LogDebug("GetToken is {token}", JsonConvert.SerializeObject(res));
        return res.ToString();
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
        _logger.LogDebug("Keys length is {length}", botKeys.Count);
        foreach (var key in botKeys)
        {
            _logger.LogDebug("BotKey is {key}", key);
        }

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