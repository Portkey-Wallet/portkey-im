using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Mime;
using System.Text;
using System.Threading.Tasks;
using AElf;
using IM.Auth.Dtos;
using IM.Cache;
using IM.Common;
using IM.Commons;
using IM.Options;
using IM.RelationOne;
using IM.RelationOne.Dtos;
using IM.User;
using IM.User.Dtos;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using Orleans.Runtime;
using RestSharp;
using Volo.Abp;
using Volo.Abp.Auditing;

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
    private const string PortkeyTokenCacheKey = "PortKey:AuthToken:";
    private readonly ChatBotBasicInfoOptions _chatBotBasicInfoOptions;
    private readonly ChatBotConfigOptions _chatBotConfigOptions;
    private readonly ILogger<ChatBotAppService> _logger;
    private readonly IHttpClientProvider _httpClientProvider;
    private readonly RelationOneOptions _relationOneOptions;
    private readonly IProxyUserAppService _proxyUserAppService;
    private readonly IHttpClientFactory _httpClientFactory;

    public ChatBotAppService(ICacheProvider cacheProvider, IUserAppService userAppService,
        IOptionsSnapshot<ChatBotBasicInfoOptions> chatBotBasicInfoOptions,
        IOptionsSnapshot<ChatBotConfigOptions> chatBotConfigOptions, ILogger<ChatBotAppService> logger,
        IHttpClientProvider httpClientProvider, IOptionsSnapshot<RelationOneOptions> relationOneOptions,
        IProxyUserAppService proxyUserAppService, IHttpClientFactory httpClientFactory)
    {
        _cacheProvider = cacheProvider;
        _userAppService = userAppService;
        _logger = logger;
        _httpClientProvider = httpClientProvider;
        _proxyUserAppService = proxyUserAppService;
        _httpClientFactory = httpClientFactory;
        _relationOneOptions = relationOneOptions.Value;
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

            var message = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds().ToString();
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

            _logger.LogDebug("Request to im url is {url}", GetUrl(ImUrlConstant.AddressToken));
            // var response = await _httpClientProvider.PostAsync<SignatureDto>(
            //     GetUrl(ImUrlConstant.AddressToken), signatureRequest, headers);
            _logger.LogDebug("Param is {param}", JsonConvert.SerializeObject(signatureRequest));
            var result = await _userAppService.GetSignatureAsync(signatureRequest);

            _logger.LogDebug("result is {result}", JsonConvert.SerializeObject(result));

            //_logger.LogDebug("Portkey token is {token}", response.Token);
            // var authToken = new AuthRequestDto
            // {
            //     AddressAuthToken = result.Token
            // };
            //var token = await _userAppService.GetAuthTokenAsync(authToken);
            //var token = await _proxyUserAppService.GetAuthTokenAsync(authToken);

            // var header = new Dictionary<string, string>()
            //     { { RelationOneConstant.GetTokenHeader, $"{CommonConstant.JwtPrefix} {authToken.AddressAuthToken}" } };
            // var responseDto = await PostJsonAsync<RelationOneResponseDto>(ImUrlConstant.AuthToken, authToken, header);
            // _logger.LogDebug("Relation one Token is {response} ", JsonConvert.SerializeObject(responseDto));


            var auth = new AuthRequestDto
            {
                AddressAuthToken = result.Token
                    //"eyJhbGciOiJFUzI1NiJ9.eyJqdGkiOiIzYmY2YmU3MThkYmI0Mzg1OTU1Y2U4MTczNWZkMzcwNCIsImlzcyI6InJlbGF0aW9ubGFicy5haSIsImlhdCI6MTcyMDQyNjMwMCwic3ViIjoiMmdFWFBXQTVieGFtUTNBWHVDMWEzTmhVcWZEbVFnenVRSDZtNnhMUVB4Z2VxOFlIMjUiLCJjaGFpbk5hbWUiOiJhZWxmIiwiYWNjb3VudFNvdXJjZSI6ImFlbGYiLCJtYW5hZ2VyQWRkcmVzcyI6IjJoUU10RVZVN0ZpRFd3WUZ2ZTd2S0pDaGZoU3VrRldBU1lMeHJkWWdVUXBWTmlCcDFhIiwiY2FIYXNoIjoiYjljMjdhNjRmMWYxODI3Yzk3MTY3YzBlMmVlNzgyY2ZjN2VkMDU2YWE2NzhkYWU4NWIzNDg2ZGE5ZjA0YTUwYiIsImNhU2lkZUNoYWluQWRkcmVzc2VzIjpbIjJnRVhQV0E1YnhhbVEzQVh1QzFhM05oVXFmRG1RZ3p1UUg2bTZ4TFFQeGdlcThZSDI1Il0sImV4cCI6MTcyMTAzMTEwMH0.DqZOUwJCxqOg-2SK3Umn7gVEeKCx-sJOoVuTajYHUsI_mKcrgmRaLS2mqrpFeFnv8p_w0HYEvB0fv764QaXHcg"
            };

            var requestInput = JsonConvert.SerializeObject(auth, Formatting.None);

            var requestContent = new StringContent(
                requestInput,
                Encoding.UTF8,
                MediaTypeNames.Application.Json);

            var client = new HttpClient();
            client.DefaultRequestHeaders.Add("Authorization",
                "Bearer eyJhbGciOiJSUzI1NiIsImtpZCI6IkIzRTVBRTk3MTI4RTk2RjE1RjNCQzQ4NzNFQTdDMjNGNjhBQjhBNTUiLCJ4NXQiOiJzLVd1bHhLT2x2RmZPOFNIUHFmQ1AyaXJpbFUiLCJ0eXAiOiJhdCtqd3QifQ.eyJzdWIiOiIxYjRiZmQ1OS1mYmVmLTQ0ZGYtODE4Yi1lOGExNWUxZGE3MDQiLCJvaV9wcnN0IjoiQ0FTZXJ2ZXJfQXBwIiwiY2xpZW50X2lkIjoiQ0FTZXJ2ZXJfQXBwIiwib2lfdGtuX2lkIjoiOTE3ODM2YzQtZDc0ZC0xNGJhLTM2ZWItM2ExM2EzM2QzY2JlIiwiYXVkIjoiQ0FTZXJ2ZXIiLCJzY29wZSI6IkNBU2VydmVyIiwianRpIjoiZTNjYWQxZjUtN2M2NS00MTRjLWI4YjctZDE5MDVlOGNlODQ3IiwiZXhwIjoxNzIwNTkzNDg4LCJpc3MiOiJodHRwOi8vMTAuMTAuMzIuOTk6ODAwMS8iLCJpYXQiOjE3MjA0MjA2ODl9.hRYVwYLQpsPo3Ol-mK7SS38XwXfibMNuBL0Nw-neob72Ec1qFMeJ8seieajP_1q6P1UDQzGzji7yoHz7FwfmzaL4hdex9tCdYNE_fp5iNVrIQdiB-nsgEU5fbVXCLTPg-zD9JhZzRKt5fLFUB3xxRjyCAlvkjthq6TR_v-DHnygSKZr2dbpzh2ikXuTuQMFTWMVAkvnp15sQrdHrp4xm4Fsc2qrs-1wlwLGe9WK0S-ZuXpBLQ_nya0SMcPMlCYom5Q0kJ1mYDa3J-F1f2rOKIKPG2gXR8bqUthogTsumN1Vh3IonEJKXhcyTKciLwgv7cxE-c3BaS4XNcdf4JDcEfQ");

            var response =
                await client.PostAsync("https://im-api-test.portkey.finance/api/v1/users/auth", requestContent);
            var async = await response.Content.ReadAsStringAsync();
            _logger.LogDebug("relation token is {token}",async);

            var expire = TimeSpan.FromHours(24);
            //await _cacheProvider.Set(RelationTokenCacheKey, token.Token, expire);
        }
        catch (Exception e)
        {
            _logger.LogError("Refresh Relation Token failed:{ex}", e.Message);
        }
    }

    private async Task<string> GetPortkeyToken()
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
            AElf.Cryptography.CryptoHelper.SignWithPrivateKey(
                ByteArrayHelper.HexStringToByteArray(_chatBotBasicInfoOptions.BotKey),
                data);

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
        using var req =
            new HttpRequestMessage(HttpMethod.Post, "https://auth-aa-portkey-test.portkey.finance/connect/token")
                { Content = new FormUrlEncodedContent(dict) };
        using var res = await client.SendAsync(req);

        var stringAsync = await res.Content.ReadAsStringAsync();
        var authTokenDto = JsonConvert.DeserializeObject<AuthResponseDto>(stringAsync);
        _logger.LogDebug("GetToken is {token}", JsonConvert.SerializeObject(authTokenDto));
        var expire = TimeSpan.FromDays(1);
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

    private string GetUrl(string url)
    {
        return $"{_relationOneOptions.BaseUrl.TrimEnd('/')}/{url}";
    }

    private async Task<T> PostJsonAsync<T>(string url, object paramObj, Dictionary<string, string> headers)
    {
        url = "http://10.10.32.99:9902/" + url;
        var serializerSettings = new JsonSerializerSettings
        {
            ContractResolver = new CamelCasePropertyNamesContractResolver()
        };
        var requestInput = paramObj == null
            ? string.Empty
            : JsonConvert.SerializeObject(paramObj, Formatting.None, serializerSettings);

        var requestContent = new StringContent(
            requestInput,
            Encoding.UTF8,
            MediaTypeNames.Application.Json);

        var client = GetClient();

        if (headers is { Count: > 0 })
        {
            foreach (var header in headers)
            {
                client.DefaultRequestHeaders.Add(header.Key, header.Value);
            }
        }

        var response = await client.PostAsync(url, requestContent);
        var content = await response.Content.ReadAsStringAsync();
        _logger.LogDebug("content is {content}", content);
        if (response.StatusCode != HttpStatusCode.OK)
        {
            _logger.LogError("Response status code not good, code:{code}, message: {message}, params:{param}",
                response.StatusCode, content, JsonConvert.SerializeObject(paramObj));

            throw new UserFriendlyException(content, ((int)response.StatusCode).ToString());
        }

        return JsonConvert.DeserializeObject<T>(content);
    }

    private HttpClient GetClient()
    {
        var client = _httpClientFactory.CreateClient(RelationOneConstant.ClientName);

        // var auth = _httpContextAccessor?.HttpContext?.Request?.Headers[RelationOneConstant.AuthHeader]
        //     .FirstOrDefault();
        // _logger.LogDebug("auth is {auth}",auth);
        // var authToken = _httpContextAccessor?.HttpContext?.Request?.Headers["Authorization"]
        //     .FirstOrDefault();
        // if (!auth.IsNullOrWhiteSpace())
        // {
        //     client.DefaultRequestHeaders.Add(HeaderNames.Authorization, auth);
        // }

        return client;
    }

    private T GetData<T>(RelationOneResponseDto<T> response)
    {
        if (response.Code == RelationOneConstant.SuccessCode)
        {
            return response.Data;
        }

        if (response.Code == RelationOneConstant.FailCode)
        {
            throw new UserFriendlyException(response.Desc,
                RelationOneConstant.ImResponseMappings[RelationOneConstant.FailCode].Code);
        }

        var responseMapping = RelationOneConstant.ImResponseMappings.GetOrDefault(response.Code);
        if (responseMapping == null)
        {
            throw new UserFriendlyException(response.Desc, response.Code);
        }

        throw new UserFriendlyException(responseMapping.Message, responseMapping.Code);
    }
}