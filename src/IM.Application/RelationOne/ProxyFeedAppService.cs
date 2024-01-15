using System.Collections.Generic;
using System.Threading.Tasks;
using IM.Common;
using IM.Feed.Dtos;
using Microsoft.AspNetCore.Http;
using Volo.Abp;
using Volo.Abp.Auditing;

namespace IM.RelationOne;

[RemoteService(false), DisableAuditing]
public class ProxyFeedAppService : ImAppService, IProxyFeedAppService
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IProxyRequestProvider _proxyRequestProvider;

    public ProxyFeedAppService(IProxyRequestProvider proxyRequestProvider, IHttpContextAccessor httpContextAccessor)
    {
        _proxyRequestProvider = proxyRequestProvider;
        _httpContextAccessor = httpContextAccessor;
    }

    public async Task PinFeedAsync(PinFeedRequestDto input)
    {
        var url = "api/v1/userChannels/pin";
        await _proxyRequestProvider.PostAsync<object>(
            url, input);
    }

    public async Task MuteFeedAsync(MuteFeedRequestDto input)
    {
        var url = "api/v1/userChannels/mute";
        await _proxyRequestProvider.PostAsync<object>(
            url, input);
    }

    public async Task HideFeedAsync(HideFeedRequestDto input)
    {
        var url = "api/v1/userChannels/hide";
        await _proxyRequestProvider.PostAsync<object>(
            url, input);
    }

    public async Task<ListFeedResponseDto> ListFeedAsync(ListFeedRequestDto input, IDictionary<string, string> headers)
    {
        var url = "api/v1/userChannels/list" +
                  "?cursor=" + (string.IsNullOrEmpty(input.Cursor) ? "" : input.Cursor) +
                  "&limit=" + (input.MaxResultCount == 0 ? 10 : input.MaxResultCount) +
                  "&channelUuid=" + (string.IsNullOrEmpty(input.ChannelUuid) ? "" : input.ChannelUuid) +
                  "&keyword=" + (string.IsNullOrEmpty(input.Keyword) ? "" : input.Keyword);
        var result =
            await _proxyRequestProvider.GetAsync<ListFeedResponseDto>(
                url, headers);
        return result;
    }
}