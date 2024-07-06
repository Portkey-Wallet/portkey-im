using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AElf;
using IM.Cache;
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

    public ChatBotAppService(ICacheProvider cacheProvider, IUserAppService userAppService,
        IOptionsSnapshot<ChatBotBasicInfoOptions> chatBotBasicInfoOptions,
        IOptionsSnapshot<ChatBotConfigOptions> chatBotConfigOptions, ILogger<ChatBotAppService> logger)
    {
        _cacheProvider = cacheProvider;
        _userAppService = userAppService;
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
        try
        {
            var message = DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString();
            var data = Encoding.UTF8.GetBytes(message).ComputeHash();
            var privateKey = _chatBotBasicInfoOptions.BotKey;
            var signature = AElf.Cryptography.CryptoHelper.SignWithPrivateKey(ByteArrayHelper.HexStringToByteArray(privateKey), data);
            var signatureRequest = new SignatureRequestDto
            {
                Address = _chatBotBasicInfoOptions.Address,
                CaHash = _chatBotBasicInfoOptions.CaHash,
                Message = message,
                Signature = signature.ToHex()
            };
            var result = await _userAppService.GetSignatureAsync(signatureRequest);
            _logger.LogDebug("Portkey token is {token}",result.Token);
            var authToken = new AuthRequestDto
            {
                AddressAuthToken = result.Token
            };
            var token = await _userAppService.GetAuthTokenAsync(authToken);
            _logger.LogDebug("Relation one Token is {token} ",token.Token);
            var expire = TimeSpan.FromHours(24);
            await _cacheProvider.Set(RelationTokenCacheKey, token.Token, expire);
        }
        catch (Exception e)
        {
            _logger.LogError("Refresh Relation Token failed:{ex}", e.Message);
        }
    }

    public async Task InitBotUsageRankAsync()
    {
        var value = await _cacheProvider.Get(InitBotUsageRankCacheKey);
        
        if (value.HasValue)
        {
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