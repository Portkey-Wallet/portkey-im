using System;
using System.Net;
using System.Net.Http;
using System.Net.Mime;
using System.Text;
using System.Threading.Tasks;
using IM.Cache;
using IM.ChatBot;
using IM.Commons;
using IM.Message.Dtos;
using IM.Message.Etos;
using IM.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Net.Http.Headers;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using Orleans.Runtime;
using Volo.Abp;
using Volo.Abp.DependencyInjection;
using Volo.Abp.EventBus.Distributed;

namespace IM.EntityEventHandler.Core;

public class SendMessageToChatBotHandler : IDistributedEventHandler<BotMessageEto>,
    ITransientDependency
{
    private readonly IChatBotAppService _chatBotAppService;
    private readonly ILogger<SendMessageToChatBotHandler> _logger;
    private readonly ICacheProvider _cacheProvider;
    private const string RelationTokenCacheKey = "IM:RelationTokenKey:";
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly RelationOneOptions _relationOneOptions;

    public SendMessageToChatBotHandler(IChatBotAppService chatBotAppService,
        ILogger<SendMessageToChatBotHandler> logger, ICacheProvider cacheProvider, IHttpClientFactory httpClientFactory,
        IOptionsSnapshot<RelationOneOptions> relationOneOptions)
    {
        _chatBotAppService = chatBotAppService;
        _logger = logger;
        _cacheProvider = cacheProvider;
        _httpClientFactory = httpClientFactory;
        _relationOneOptions = relationOneOptions.Value;
    }


    public async Task HandleEventAsync(BotMessageEto eventData)
    {
        var response = await _chatBotAppService.SendMessageToChatBotAsync(eventData.Content, eventData.From);
        _logger.LogDebug("Response from ChatGpt is {response}", response);

        var start = 0;
        var increase = 500;
        while (true)
        {
            var content = response.Substring(start, increase);
            if (content.Length == 0)
            {
                break;
            }
            var message = new SendMessageRequestDto
            {
                ChannelUuid = eventData.ChannelUuid,
                SendUuid = BuildSendUUid(eventData.ToRelationId, eventData.ChannelUuid),
                Content = content,
                From = eventData.ToRelationId,
                Type = "TEXT"
            };
            await SendBotMessageAsync(message);
            _logger.Debug("Bot send user message is {message}", JsonConvert.SerializeObject(message));
            start += increase;
        }
    }

    private string BuildSendUUid(string toRelationId, string channelUuid)
    {
        return toRelationId + "-" + channelUuid + "-" + DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() + "-" +
               Guid.NewGuid().ToString("N");
    }

    private async Task SendBotMessageAsync(SendMessageRequestDto message)
    {
        var url = GetRealUrl("api/v1/message/send");
        var serializerSettings = new JsonSerializerSettings
        {
            ContractResolver = new CamelCasePropertyNamesContractResolver()
        };
        var requestInput = message == null
            ? string.Empty
            : JsonConvert.SerializeObject(message, Formatting.None, serializerSettings);

        var requestContent = new StringContent(
            requestInput,
            Encoding.UTF8,
            MediaTypeNames.Application.Json);
        var client = await GetClient();
        var response = await client.PostAsync(url, requestContent);
        var content = await response.Content.ReadAsStringAsync();
        _logger.LogDebug("Content is {content}",content);
        if (response.StatusCode != HttpStatusCode.OK)
        {
            _logger.LogError("Response status code not good, code:{code}, message: {message}, params:{param}",
                response.StatusCode, content, JsonConvert.SerializeObject(message));
            throw new UserFriendlyException(content, ((int)response.StatusCode).ToString());
        }
    }

    private string GetRealUrl(string url)
    {
        if (_relationOneOptions == null || _relationOneOptions.UrlPrefix.IsNullOrWhiteSpace())
        {
            return url;
        }
        return $"{_relationOneOptions.UrlPrefix.TrimEnd('/')}/{url}";
    }


    private async Task<HttpClient> GetClient()
    {
        var client = _httpClientFactory.CreateClient(RelationOneConstant.ClientName);
        var auth = await _cacheProvider.Get(RelationTokenCacheKey);
        _logger.LogDebug("bot auth is {auth}",auth);
        if (auth.HasValue)
        {
            client.DefaultRequestHeaders.Add(HeaderNames.Authorization, $"{CommonConstant.JwtPrefix} {auth}");
        }

        return client;
    }
}