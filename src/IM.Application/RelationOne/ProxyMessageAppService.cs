using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using IM.Common;
using IM.Commons;
using IM.Message;
using IM.Message.Dtos;
using Volo.Abp;
using Volo.Abp.Auditing;

namespace IM.RelationOne;

[RemoteService(false), DisableAuditing]
public class ProxyMessageAppService : ImAppService, IProxyMessageAppService
{
    private readonly IProxyRequestProvider _proxyRequestProvider;


    public ProxyMessageAppService(IProxyRequestProvider proxyRequestProvider)
    {
        _proxyRequestProvider = proxyRequestProvider;
       
    }

    public async Task<int> ReadMessageAsync(ReadMessageRequestDto input)
    {
        var result =
            await _proxyRequestProvider.PostAsync<int>(
                "api/v1/message/read", input, null);

        return result;
    }

    public async Task HideMessageAsync(HideMessageRequestDto input)
    {
        var result =
            await _proxyRequestProvider.PostAsync<object>(
                "api/v1/message/hide", input, null);
    }

    public async Task<SendMessageResponseDto> SendMessageAsync(SendMessageRequestDto input)
    {
        var result =
            await _proxyRequestProvider.PostAsync<SendMessageResponseDto>(
                "api/v1/message/send", input, null);

        return result;
    }

    public async Task<List<ListMessageResponseDto>> ListMessageWithHeaderAsync(ListMessageRequestDto input,
        Dictionary<string, string> headers)
    {
        var baseUrl = "api/v1/message/list";
        var queryString = new StringBuilder();
        queryString.Append("?limit=").Append(input.Limit == 0 ? 10 : input.Limit);

        if (!string.IsNullOrEmpty(input.ChannelUuid))
        {
            queryString.Append("&channelUuid=").Append(input.ChannelUuid);
        }
        else if (!string.IsNullOrEmpty(input.ToRelationId))
        {
            queryString.Append("&toRelationId=").Append(input.ToRelationId);
        }

        queryString.Append("&maxCreateAt=").Append(input.MaxCreateAt);
        var result =
            await _proxyRequestProvider.GetAsync<List<ListMessageResponseDto>>(
                baseUrl + queryString, headers);

        return result;
    }

    public async Task<UnreadCountResponseDto> GetUnreadMessageCountAsync()
    {
        var result =
            await _proxyRequestProvider.GetAsync<UnreadCountResponseDto>(
                "api/v1/message/unreadCount");

        return result;
    }

    public async Task<UnreadCountResponseDto> GetUnreadMessageCountWithTokenAsync(string authToken)
    {
        var header = new Dictionary<string, string>()
        {
            [CommonConstant.AuthHeader] = authToken,
        };

        var result =
            await _proxyRequestProvider.GetAsync<UnreadCountResponseDto>(
                "api/v1/message/unreadCount", header);

        return result;
    }

    public async Task<List<ListMessageResponseDto>> ListMessageAsync(
        ListMessageRequestDto input)
    {
        var baseUrl = "api/v1/message/list";
        var queryString = new StringBuilder();
        queryString.Append("?limit=").Append(input.Limit == 0 ? 10 : input.Limit);

        if (!string.IsNullOrEmpty(input.ChannelUuid))
        {
            queryString.Append("&channelUuid=").Append(input.ChannelUuid);
        }
        else if (!string.IsNullOrEmpty(input.ToRelationId))
        {
            queryString.Append("&toRelationId=").Append(input.ToRelationId);
        }

        queryString.Append("&maxCreateAt=").Append(input.MaxCreateAt);
        var result =
            await _proxyRequestProvider.GetAsync<List<ListMessageResponseDto>>(
                baseUrl + queryString);

        return result;
    }
}