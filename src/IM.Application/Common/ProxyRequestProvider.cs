using System.Collections.Generic;
using System.Threading.Tasks;
using IM.Commons;
using IM.RelationOne.Dtos;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Volo.Abp;
using Volo.Abp.DependencyInjection;

namespace IM.Common;

public class ProxyRequestProvider : IProxyRequestProvider, ISingletonDependency
{
    private readonly IProxyClientProvider _proxyClientProvider;
    private readonly ILogger<ProxyRequestProvider> _logger;

    public ProxyRequestProvider(IProxyClientProvider proxyClientProvider, ILogger<ProxyRequestProvider> logger)
    {
        _proxyClientProvider = proxyClientProvider;
        _logger = logger;
    }

    public async Task<T> GetAsync<T>(string url)
    {
        var response =
            await _proxyClientProvider.GetAsync<RelationOneResponseDto<T>>(url);

        return GetData(response);
    }

    public async Task<T> GetAsync<T>(string url, IDictionary<string, string> headers)
    {
        var response =
            await _proxyClientProvider.GetAsync<RelationOneResponseDto<T>>(url, headers);

        return GetData(response);
    }

    public async Task<T> PostAsync<T>(string url)
    {
        var response =
            await _proxyClientProvider.GetAsync<RelationOneResponseDto<T>>(url);

        return GetData(response);
    }

    public async Task<T> PostAsync<T>(string url, object paramObj)
    {
        var response =
            await _proxyClientProvider.PostAsync<RelationOneResponseDto<T>>(url, paramObj);

        return GetData(response);
    }

    public async Task<T> PostAsync<T>(string url, object paramObj, Dictionary<string, string> headers)
    {
        var response =
            await _proxyClientProvider.PostAsync<RelationOneResponseDto<T>>(url, paramObj, headers);

        return GetData(response);
    }

    public async Task<T> PostAsync<T>(string url, RequestMediaType requestMediaType, object paramObj,
        Dictionary<string, string> headers)
    {
        var response =
            await _proxyClientProvider.PostAsync<RelationOneResponseDto<T>>(url, requestMediaType, paramObj, headers);

        return GetData(response);
    }

    private T GetData<T>(RelationOneResponseDto<T> response)
    {
        _logger.LogDebug("Response is {result}",JsonConvert.SerializeObject(response));
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