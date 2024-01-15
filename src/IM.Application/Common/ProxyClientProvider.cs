using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Mime;
using System.Text;
using System.Threading.Tasks;
using IM.Commons;
using IM.Options;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Net.Http.Headers;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using Volo.Abp;
using Volo.Abp.DependencyInjection;

namespace IM.Common;

public class ProxyClientProvider : IProxyClientProvider, ISingletonDependency
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ILogger<HttpClientProvider> _logger;
    private readonly RelationOneOptions _relationOneOptions;

    public ProxyClientProvider(IHttpClientFactory httpClientFactory, ILogger<HttpClientProvider> logger,
        IHttpContextAccessor httpContextAccessor,
        IOptionsSnapshot<RelationOneOptions> relationOneOptions)
    {
        _httpClientFactory = httpClientFactory;
        _logger = logger;
        _httpContextAccessor = httpContextAccessor;
        _relationOneOptions = relationOneOptions.Value;
    }

    public async Task<T> GetAsync<T>(string url)
    {
        url = GetUrl(url);
        var client = GetClient();
        var response = await client.GetAsync(url);
        var content = await response.Content.ReadAsStringAsync();

        if (response.StatusCode != HttpStatusCode.OK)
        {
            _logger.LogError("Response status code not good, code:{code}, message: {message}, url:{url}",
                response.StatusCode, content, url);

            throw new UserFriendlyException(content, ((int)response.StatusCode).ToString());
        }

        return JsonConvert.DeserializeObject<T>(content);
    }

    public async Task<T> GetAsync<T>(string url, IDictionary<string, string> headers)
    {
        url = GetUrl(url);
        if (headers == null)
        {
            return await GetAsync<T>(url);
        }

        var client = GetClient();
        foreach (var keyValuePair in headers)
        {
            client.DefaultRequestHeaders.Add(keyValuePair.Key, keyValuePair.Value);
        }

        var response = await client.GetAsync(url);
        var content = await response.Content.ReadAsStringAsync();

        if (response.StatusCode != HttpStatusCode.OK)
        {
            _logger.LogError("Response status code not good, code:{code}, message: {message}, url:{url}",
                response.StatusCode, content, url);

            throw new UserFriendlyException(content, ((int)response.StatusCode).ToString());
        }

        return JsonConvert.DeserializeObject<T>(content);
    }

    public async Task<T> PostAsync<T>(string url)
    {
        return await PostJsonAsync<T>(url, null, null);
    }

    public async Task<T> PostAsync<T>(string url, object paramObj)
    {
        return await PostJsonAsync<T>(url, paramObj, null);
    }

    public async Task<T> PostAsync<T>(string url, object paramObj, Dictionary<string, string> headers)
    {
        return await PostJsonAsync<T>(url, paramObj, headers);
    }

    public async Task<T> PostAsync<T>(string url, RequestMediaType requestMediaType, object paramObj,
        Dictionary<string, string> headers)
    {
        if (requestMediaType == RequestMediaType.Json)
        {
            return await PostJsonAsync<T>(url, paramObj, headers);
        }

        return await PostFormAsync<T>(url, (Dictionary<string, string>)paramObj, headers);
    }

    private async Task<T> PostJsonAsync<T>(string url, object paramObj, Dictionary<string, string> headers)
    {
        url = GetUrl(url);
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

        if (response.StatusCode != HttpStatusCode.OK)
        {
            _logger.LogError("Response status code not good, code:{code}, message: {message}, params:{param}",
                response.StatusCode, content, JsonConvert.SerializeObject(paramObj));

            throw new UserFriendlyException(content, ((int)response.StatusCode).ToString());
        }

        return JsonConvert.DeserializeObject<T>(content);
    }

    private async Task<T> PostFormAsync<T>(string url, Dictionary<string, string> paramDic,
        Dictionary<string, string> headers)
    {
        url = GetUrl(url);
        var client = GetClient();

        if (headers is { Count: > 0 })
        {
            foreach (var header in headers)
            {
                client.DefaultRequestHeaders.Add(header.Key, header.Value);
            }
        }

        var param = new List<KeyValuePair<string, string>>();
        if (paramDic is { Count: > 0 })
        {
            param.AddRange(paramDic.ToList());
        }

        var response = await client.PostAsync(url, new FormUrlEncodedContent(param));
        var content = await response.Content.ReadAsStringAsync();

        if (response.StatusCode != HttpStatusCode.OK)
        {
            _logger.LogError("Response status code not good, code:{code}, message: {message}, params:{param}",
                response.StatusCode, content, JsonConvert.SerializeObject(paramDic));

            throw new Exception("");
        }

        return JsonConvert.DeserializeObject<T>(content);
    }

    private HttpClient GetClient()
    {
        var client = _httpClientFactory.CreateClient(RelationOneConstant.ClientName);

        var auth = _httpContextAccessor?.HttpContext?.Request?.Headers[RelationOneConstant.AuthHeader]
            .FirstOrDefault();
        var authToken = _httpContextAccessor?.HttpContext?.Request?.Headers["Authorization"]
            .FirstOrDefault();
        if (!auth.IsNullOrWhiteSpace())
        {
            client.DefaultRequestHeaders.Add(HeaderNames.Authorization, auth);
        }

        return client;
    }

    private string GetUrl(string url)
    {
        if (_relationOneOptions == null || _relationOneOptions.UrlPrefix.IsNullOrWhiteSpace())
        {
            return url;
        }

        return $"{_relationOneOptions.UrlPrefix.TrimEnd('/')}/{url}";
    }
}