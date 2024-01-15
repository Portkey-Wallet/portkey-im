using System;
using System.Threading.Tasks;
using IM.ChannelContact.Dto;
using IM.Common;
using IM.Commons;
using IM.Message;
using IM.Options;
using Microsoft.Extensions.Options;
using Volo.Abp;
using Volo.Abp.Auditing;

namespace IM.RelationOne;

[RemoteService(false), DisableAuditing]
public class ProxyUnreadMessageService : ImAppService, IProxyUnreadMessageService
{
    private readonly IHttpClientProvider _httpClientProvider;
    private readonly MessagePushOptions _messagePushOptions;

    public ProxyUnreadMessageService(IHttpClientProvider httpClientProvider, IOptionsSnapshot<MessagePushOptions> messagePushOptions)
    {
        _httpClientProvider = httpClientProvider;
        _messagePushOptions = messagePushOptions.Value;
    }

    public async Task<Object> UpdateUnReadMessageCountAsync(UnreadMessageDto unreadMessageDto)
    {
        var url = GetUrl(CommonConstant.UpdateUnreadMessageUri);
        var response = await _httpClientProvider.PostAsync<Object>(url, unreadMessageDto);
        return response;
    }
    
    private string GetUrl(string url)
    {
        if (_messagePushOptions == null || _messagePushOptions.BaseUrl.IsNullOrWhiteSpace())
        {
            return url;
        }

        return $"{_messagePushOptions.BaseUrl.TrimEnd('/')}/{url}";
    }
}